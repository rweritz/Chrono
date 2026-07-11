# Benchmarks

Chrono benchmarks are organized as an advanced scenario suite. The suite replaces the legacy `TimeSeriesPerformance`, `TimeSeriesMathBenchmarks`, and `TimeSeriesAggregationBenchmarks` classes with scenario classes that cover the same storage, math, and aggregation operations while adding gapped, irregular, bounded stepwise, and mixed-family workloads.

All benchmark classes run through [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on .NET 10. Each scenario class is annotated with `MemoryDiagnoser`, so benchmark reports include allocation and GC columns in addition to timing data. Results are written to `BenchmarkDotNet.Artifacts/` from the repository root.

## Running Benchmarks

Run the full scenario suite from the repository root:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

List the available benchmark cases:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --list flat
```

Run one scenario family with BenchmarkDotNet filters:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --filter "*DenseFixedStepBenchmarks*"
```

## Scenario Filters

Use these filters to run the main benchmark families:

| Scenario | Filter | Main workload |
|---|---|---|
| Dense fixed-step | `--filter "*DenseFixedStepBenchmarks*"` | Fully populated 5-minute series across `FixedSlotTimeSeries`, `DynamicSlotTimeSeries`, and `SortedArrayTimeSeries` |
| Gapped fixed-step | `--filter "*GappedFixedStepBenchmarks*"` | Fixed-step series with deterministic occupancy gaps at 1%, 10%, 50%, and 90% |
| Sparse irregular | `--filter "*SparseIrregularBenchmarks*"` | Sparse point series with clustered, bursty, and wide-gap irregular timestamp shapes |
| Calendar-period | `--filter "*CalendarPeriodBenchmarks*"` | Month-based insert, lookup, math, aggregation, resampling, and truncate-mode alignment |
| Bounded stepwise | `--filter "*StepwiseBenchmarks*"` | Dense logical reads over compressed `StepwiseTimeSeries<T>` change-points |
| Mixed-family | `--filter "*MixedFamily*"` | Cross-family sparse/stepwise operations and explicit specialization paths |

Filters can also target individual benchmark names, parameter values, or fully qualified class names using BenchmarkDotNet's normal wildcard syntax.

## Scenario Membership

Scenario membership is shape-based rather than implementation-only. A workload belongs in the suite when it represents a realistic data shape or operation family that Chrono users should compare:

- Dense fixed-step scenarios use contiguous aligned timestamps and include ordered insert, random insert, ordered lookup, random lookup, scalar math, binary math, aggregation, and resampling.
- Gapped fixed-step scenarios keep a fixed timestamp grid but vary occupancy, so lookup and aggregation costs include hit, miss, and mixed lookup behavior.
- Sparse irregular scenarios model explicit-point storage where timestamps are aligned to the period but not evenly populated across the observed range.
- Calendar-period scenarios cover variable-length periods and calendar-aware slot math, including month-to-year aggregation and resampling.
- Bounded stepwise scenarios exercise logical-slot reads, compressed change-point storage, segment writes, contiguous expansion, arithmetic, aggregation, resampling, and materialized sparse comparisons.
- Mixed-family scenarios measure default cross-family behavior and explicit result-specialization APIs across sparse and bounded stepwise families.

Invalid alignment, disconnected bounded ranges, and unsupported specializations remain correctness-test concerns. They are intentionally not main-suite benchmark workloads because their expected behavior is validation failure or rejected specialization rather than representative steady-state performance.

## Deterministic Data

Benchmark data factories use fixed seeds for shuffled insert orders, random lookup orders, value generation, occupancy selection, irregular shapes, and stepwise change-point placement. This keeps every run deterministic while still exercising non-ordered paths.

The suite scales scenarios with `Params` values instead of changing data generation between runs. Most sparse and dense classes cover multiple point counts, gapped scenarios add occupancy parameters, and stepwise scenarios add logical slot count plus change-point density.

## Coverage

The scenario suite subsumes the legacy benchmark coverage:

| Legacy coverage | Scenario coverage |
|---|---|
| Ordered insert and ordered access for fixed-step sparse types | `DenseFixedStepBenchmarks` |
| Dynamic-slot strict insert and access | `DenseFixedStepBenchmarks` |
| Dynamic-slot truncate alignment | `CalendarPeriodBenchmarks` |
| Dynamic-slot month insert and access | `CalendarPeriodBenchmarks` |
| Scalar math for sparse implementations | Dense, gapped, sparse irregular, and calendar scenarios |
| Binary math for sparse implementations | Dense, gapped, sparse irregular, calendar, and mixed-family scenarios |
| Fixed-step aggregation | Dense, gapped, and sparse irregular scenarios |
| Calendar aggregation | `CalendarPeriodBenchmarks` |

The replacement suite adds memory diagnostics, deterministic data-shape variation, bounded stepwise operations, mixed-family operation routing, explicit specialization benchmarks, and resampling workloads.
