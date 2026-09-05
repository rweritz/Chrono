# PostgreSQL storage for immutable time-series versions

**Status:** Research complete  
**Decision input:** GitHub issue #62  
**Scope:** Storage for immutable source and derived time-series versions, formulas, exact lineage, window reads, and asynchronous recalculation. The expected local-development load is about 10 million points, without treating that number as a product limit.

## Recommendation

Use **plain PostgreSQL with one unpartitioned point table for V1**. Keep identities, definition revisions, completed versions, exact dependency lineage, recalculation attempts, and calculation-sheet metadata in ordinary relational tables. Store point values in a narrow table keyed by `(time_series_version_id, observed_at)`, and bulk-load each newly calculated version with PostgreSQL `COPY`.

Do not add native table partitioning or TimescaleDB to V1 until measurements show that the point table and its composite B-tree no longer meet the window-read, import, recalculation, backup, or storage targets. Ten million rows is a benchmark fixture, not a boundary or proof that partitioning is useful. PostgreSQL's own guidance says the partition key should follow common `WHERE` clauses and warns that too many partitions can increase planning time; the useful layout therefore depends on observed production access and retention patterns, not row count alone ([PostgreSQL partitioning overview and best practices](https://www.postgresql.org/docs/current/ddl-partitioning.html#DDL-PARTITIONING-DECLARATIVE-BEST-PRACTICES)).

Keep the point-storage boundary narrow enough to permit a later move to a TimescaleDB hypertable. Before that move, benchmark two physical Timescale layouts: partitioning by observation time, and partitioning by version creation time while ordering within each version by observation time. The latter better matches immutable snapshot publication, while the former better matches conventional time-series ingestion and time-bucket aggregation. This workload is unusual because recalculation writes a new version containing historical observation timestamps; it is not a conventional append-only stream along the observation-time axis.

## Why plain PostgreSQL fits V1

The dominant read has an equality predicate on a version followed by a timestamp range:

```sql
SELECT observed_at, value
FROM time_series_point
WHERE time_series_version_id = $1
  AND observed_at >= $2
  AND observed_at < $3
ORDER BY observed_at;
```

A B-tree on `(time_series_version_id, observed_at)` directly matches that shape. PostgreSQL documents that a multicolumn B-tree is most efficient with equality constraints on leading columns followed by a constraint on the next column ([multicolumn indexes](https://www.postgresql.org/docs/current/indexes-multicolumn.html)). The same key prevents duplicate timestamps inside one immutable version.

Imports and derived-version publication are large sequential writes. PostgreSQL recommends `COPY` over repeated `INSERT`s because it is optimized for large loads and has substantially less overhead ([populating a database](https://www.postgresql.org/docs/current/populate.html#POPULATE-COPY-FROM)). Npgsql's binary importer can drive the PostgreSQL binary `COPY` protocol from the .NET worker; this is an implementation choice, not a storage-model dependency.

Use `timestamptz` for point instants. PostgreSQL converts these values to UTC internally and does not retain the originally supplied zone or offset ([date/time types](https://www.postgresql.org/docs/current/datatype-datetime.html#DATATYPE-DATETIME-INPUT-TIME-STAMPS)). Consequently, any zone, reference offset, calendar alignment, period, logical range, or sparse/stepwise semantics needed to interpret a Chrono series must be explicit version metadata rather than inferred from `observed_at`.

PostgreSQL materialized views are not the authoritative representation of derived time series. A PostgreSQL refresh replaces the materialized view contents, and even `CONCURRENTLY` is a refresh of one mutable database object ([`REFRESH MATERIALIZED VIEW`](https://www.postgresql.org/docs/current/sql-refreshmaterializedview.html)). Chrono instead needs immutable version publication, stored formulas, an exact dependency-version snapshot, failure history, and a mutable pointer to the latest successful version. Those are application-domain records.

## Proposed logical model

This is a storage sketch, not final migration SQL. Identifiers should use the project's chosen UUID strategy, and names should be unique only inside a workspace.

```text
workspace
  id, name

time_series
  id, workspace_id, name, kind
  latest_successful_version_id          -- mutable publication pointer
  recalculation_status                  -- Current/Recalculating/Stale/Failed

time_series_definition_revision         -- derived series only
  id, time_series_id, revision_number
  formula_text                          -- user-authored source
  canonical_ast_json                    -- parsed/validated representation
  expression_language_version
  created_at

definition_dependency
  definition_revision_id
  referenced_time_series_id             -- follows the stable identity
  formula_symbol

recalculation_attempt
  id, time_series_id, definition_revision_id
  status, requested_at, started_at, finished_at, error

time_series_version
  id, time_series_id, version_number
  definition_revision_id                -- null for imported versions
  successful_attempt_id
  created_at, published_at
  point_count, min_date, max_date
  period, value_semantics, alignment_metadata

version_dependency
  output_version_id
  input_version_id                      -- exact reproducible lineage
  formula_symbol

time_series_point
  time_series_version_id
  observed_at timestamptz
  value double precision
  PRIMARY KEY (time_series_version_id, observed_at)
```

Store both the source formula and a canonical parsed form. The source is what users authored; the canonical form avoids reparsing ambiguities and can support dependency inspection. Version that representation with an `expression_language_version`; a formula must not silently change meaning after an engine upgrade. `definition_dependency` expresses “follow the latest version of this identity,” while `version_dependency` records the exact inputs used for one completed output.

Relational foreign keys should connect points and lineage to their version records. PostgreSQL foreign keys enforce that referenced rows exist ([constraints and referential integrity](https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-FK)); JSON should therefore be reserved for the canonical AST and non-relational alignment metadata, not used as the only copy of dependency edges.

## Publication and recalculation

The application worker, rather than a database materialized-view mechanism, should own the dependency graph and topological scheduling:

1. Saving a definition creates an immutable definition revision after parsing and cycle validation.
2. A source-version publication or changed definition creates/coalesces recalculation jobs for affected derived identities.
3. A worker resolves each stable dependency identity to its latest successful version and records those exact version IDs.
4. It creates a private candidate, streams calculated points with `COPY`, and validates count/bounds/alignment.
5. A short transaction marks the candidate complete, writes exact lineage, advances `latest_successful_version_id`, and enqueues downstream work.
6. On failure, it records the failed attempt, removes unpublished candidate points, leaves the previous successful version current, and marks the identity stale/failed.

All normal reads must begin from `latest_successful_version_id` or an explicitly selected completed version, so partially loaded candidates are never visible. Completed version metadata, formula revision, lineage, and points are append-only. Only operational state and the latest-successful pointer mutate.

This scheme avoids holding a transaction open for an entire multi-million-row calculation while retaining an atomic publication boundary. A unique `(time_series_id, version_number)` constraint and compare-and-swap style publication update should prevent two workers from publishing the same logical generation. Exact locking and job claiming belong in the recalculation-engine decision, not in the point storage abstraction.

## Option comparison

| Option | Fit for immutable versions | Advantages | Costs and risks | Verdict |
|---|---|---|---|---|
| Plain PostgreSQL, unpartitioned | Strong at V1 scale. Composite version/time key exactly matches window reads. Metadata, formulas, lineage, points, and job records share transactions and constraints. | Lowest operational surface; first-class Aspire integration; standard backup/restore; no extension or additional licence; `COPY` and mature B-tree indexing. | Historical versions duplicate full point sets, so storage grows with each recalculation. No automatic chunking, columnstore, retention jobs, or incremental aggregate refresh. | **Choose for V1.** Benchmark with realistic version counts and window sizes. |
| PostgreSQL declarative partitioning | Technically viable, but no obvious single partition axis serves this workload. Observation-time range partitions help time-window pruning but every recalculation backfills old partitions; partitioning by version helps version reads but can create excessive partitions. | Core PostgreSQL; partition pruning at plan and execution time; partitions can have their own indexes and can be detached for bulk lifecycle operations ([declarative partitioning](https://www.postgresql.org/docs/current/ddl-partitioning.html)). | The application or migration process must create/manage partitions. A partitioned table's primary/unique constraint must include all partition-key columns ([partitioning limitations](https://www.postgresql.org/docs/current/ddl-partitioning.html#DDL-PARTITIONING-DECLARATIVE-LIMITATIONS)). Poor keys or too many partitions hurt planning. | **Do not use initially.** Add only after measured pruning or lifecycle benefits justify it. |
| TimescaleDB hypertable | Strong future candidate for a very large point payload, especially when compression/columnstore or automatic chunks are valuable. Metadata can remain ordinary PostgreSQL tables. | Automatic time-based chunks; normal SQL surface; standard constraints; a hypertable may reference a regular table, and a unique/primary key includes the partition column ([hypertable constraints](https://github.com/timescale/docs/blob/latest/use-timescale/schema-management/about-constraints.md), [hypertable creation](https://github.com/timescale/docs/blob/latest/api/hypertable/create_hypertable.md)). Columnstore can segment by a commonly filtered identity and order by time ([hypertable/columnstore API](https://github.com/timescale/docs/blob/latest/api/hypertable/index.md)). | Additional extension, image, upgrade, compatibility, observability, and licensing decisions. Observation-time chunking receives historical writes for every full recalculation. A regular table cannot be converted if it is already declaratively partitioned, and migrating a non-empty table can take a long lock ([`create_hypertable`](https://github.com/timescale/docs/blob/latest/api/hypertable/create_hypertable.md)). | **Revisit at a performance/storage gate.** Prefer it over hand-built native partitioning if automated chunking or columnstore is then required. |

### BRIN is not the first index for version windows

BRIN is designed for very large tables where indexed values are naturally correlated with physical row location ([BRIN introduction](https://www.postgresql.org/docs/current/brin.html)). Bulk-loading complete versions may create some physical correlation, but a window read always has an exact `version_id` and range of timestamps. The composite B-tree is the predictable first choice. Re-evaluate BRIN only from `EXPLAIN (ANALYZE, BUFFERS)` results on much larger append layouts; it should not replace the uniqueness constraint.

## TimescaleDB-specific findings

TimescaleDB is a PostgreSQL extension, so the relational metadata model and Npgsql access can remain unchanged. A point hypertable would still use a composite uniqueness key including its partition dimension; Timescale explicitly requires all partitioning columns in a unique or primary index ([Timescale indexing](https://github.com/timescale/docs/blob/latest/use-timescale/schema-management/about-indexing.md)).

Two physical layouts need proof before adoption:

- **Partition on `observed_at`:** best for ordinary time-window pruning and `time_bucket`, but every newly calculated historical snapshot writes across old chunks. Columnstore/compression policies and backfill behavior must be benchmarked under that pattern.
- **Partition on a duplicated `version_created_at` (or monotonic version sequence), segment by `time_series_version_id`, order by `observed_at`:** keeps a newly produced version physically append-oriented and makes old-version lifecycle management natural, but every repository query must include the physical partition key to prune chunks. It also gives up conventional observation-time continuous aggregates.

Timescale continuous aggregates should not implement Chrono's user formulas or canonical calculated versions. They incrementally refresh time-bucketed aggregate queries and materialize only changed buckets ([continuous aggregates](https://github.com/timescale/docs/blob/latest/use-timescale/continuous-aggregates/index.md)), but their dependency tracking is database-object-specific. In particular, changes to regular PostgreSQL tables participating in a join are not tracked ([join behavior](https://github.com/timescale/docs/blob/latest/use-timescale/continuous-aggregates/about-continuous-aggregates.md)). Chrono formulas include arbitrary series arithmetic, explicit missing-value policies, version identity, exact input-version provenance, and topological propagation. Continuous aggregates may later be disposable accelerators for fixed chart rollups; they are not domain versions.

There is also a licensing boundary. Basic hypertable and chunk functions exist in both the Apache 2 and Community editions, while continuous aggregates are Community-only. The Community edition is under the Timescale License rather than Apache 2.0 ([official edition comparison](https://github.com/timescale/docs/blob/latest/about/timescaledb-editions.md)); the source repository likewise contains both Apache-licensed and TSL code ([repository licence layout](https://github.com/timescale/timescaledb/blob/main/LICENSE)). Before relying on columnstore, continuous aggregates, policies, or Toolkit functions, record exactly which edition/features are used and obtain an appropriate product/legal review for the intended hosted offering.

Self-hosting also couples PostgreSQL and extension upgrades. Timescale documents separate extension upgrades, PostgreSQL-major upgrades, and Docker upgrade procedures, including running `ALTER EXTENSION` after changing the image ([self-hosted upgrades](https://github.com/timescale/docs/blob/latest/self-hosted/upgrades/index.md), [Docker upgrades](https://github.com/timescale/docs/blob/latest/self-hosted/upgrades/upgrade-docker.md)). This is manageable, but it is real operational work absent from plain PostgreSQL.

## Aspire local topology

For the recommended V1 dependency, use Aspire's official PostgreSQL hosting integration:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("chrono");

builder.AddProject<Projects.Chrono_Api>("api")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.Chrono_RecalculationWorker>("recalculation-worker")
    .WithReference(database)
    .WaitFor(database);
```

`AddPostgres` creates a PostgreSQL container, `AddDatabase` models the database, `WithDataVolume` persists data across container recreation, and references supply connection information to consumers ([Aspire AppHost PostgreSQL example](https://aspire.dev/get-started/app-host/)). A persistent container lifetime may additionally keep the container running between AppHost runs ([Aspire resource lifetimes](https://learn.microsoft.com/en-us/dotnet/aspire/app-host/persistent-containers)). Whether to keep the lifetime persistent should be a developer-experience choice; the named data volume is what protects local data across recreation.

If TimescaleDB is adopted later, preserve the typed PostgreSQL resource but replace its container with a pinned TimescaleDB image through a small Dockerfile. Aspire explicitly documents that `WithDockerfile` can replace an existing PostgreSQL resource's default image without losing PostgreSQL-specific methods ([customizing an existing container resource](https://learn.microsoft.com/en-us/dotnet/aspire/app-host/withdockerfile)). Timescale publishes official PostgreSQL-based Docker images and documents their configuration and tuning variables ([official TimescaleDB Docker repository](https://github.com/timescale/timescaledb-docker)). The migration must create the extension explicitly and test backup/restore plus extension upgrades; do not use an unpinned `latest` image for reproducible development.

## Scale gate and experiments

Before changing the physical storage, generate a dataset with at least:

- 10 million total points, 100 identities, and multiple immutable versions per identity;
- both ordered CSV import and derived full-history recalculation;
- narrow and wide timestamp windows, latest and pinned historical versions;
- concurrent UI reads during candidate-version bulk load and publication;
- a realistic dependency fan-out that causes coalesced recalculation;
- enough repeated versions to measure the true storage multiplier.

Capture import/recalculation throughput, p50/p95/p99 window latency, index and heap size, cache-warm versus cold behavior, vacuum/analyze cost, backup/restore time, and `EXPLAIN (ANALYZE, BUFFERS)` plans. Compare plain PostgreSQL first. Only if it misses explicit targets should the same workload be run against native partitioning and the two Timescale physical layouts.

The largest long-term risk is not a particular PostgreSQL index; it is storing a full duplicate point set for every derived version. Partitioning changes access and lifecycle costs but does not remove that multiplier. If retained versions become the dominant storage cost, investigate immutable content-addressed point chunks or structural sharing as a separate domain/storage decision. Do not introduce that complexity into V1 without evidence.

## Decision summary

1. Plain PostgreSQL is the V1 system of record for both relational metadata and point payloads.
2. Completed source and derived versions are immutable; publication advances a pointer only after bulk loading and validation.
3. Stored formula source, versioned canonical AST, stable-identity dependencies, and exact input-version lineage are first-class relational data.
4. A composite `(time_series_version_id, observed_at)` B-tree/primary key serves exact version-window reads; `COPY` serves bulk publication.
5. Application workers own formula evaluation and dependency-DAG recalculation. Database materialized views and Timescale continuous aggregates are optional accelerators, never canonical derived versions.
6. Native partitioning is deferred. TimescaleDB is the preferred specialized candidate to benchmark at the scale gate, with licensing and extension operations explicitly reviewed.
7. Aspire hosts plain PostgreSQL with its official integration now and can retain that typed resource while substituting a Timescale-enabled Dockerfile later.
