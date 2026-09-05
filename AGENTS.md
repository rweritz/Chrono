# Agent Instructions

## Build & Test Commands

```bash
# Build
dotnet build Chrono.slnx
dotnet build Chrono.slnx -c Release

# Test (all)
dotnet test tests/Chrono.TimeSeries.Test/Chrono.TimeSeries.Test.csproj

# Test (single test by name)
dotnet test tests/Chrono.TimeSeries.Test/Chrono.TimeSeries.Test.csproj --filter "FullyQualifiedName~<TestMethodName>"

# Benchmarks (must run Release)
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

## Architecture

This is a .NET 10 library comparing time-series data structure implementations. All implementations share a single interface and are benchmarked against each other.

**`ITimeSeries<T> where T : struct`** - the central abstraction. Exposes period-aligned `DateTimeOffset -> T` storage with `MinDate`/`MaxDate` range tracking. The `Add()` method is defined on the interface but intentionally unimplemented (`NotImplementedException`) in all current implementations - use the indexer instead.

Two implementations exist, both in `Chrono.TimeSeries`:

| Class | Backend | Notes |
|---|---|---|
| `SortedArrayTimeSeries<T>` | Sorted parallel arrays (`long[]` keys + `T[]` values) | General-purpose sparse/irregular storage with binary-search lookup |
| `FixedSlotTimeSeries<T>` | Fixed-step slot array + presence bitset | Fast path for fixed-tick periods with O(1) slot addressing |

**Period validation** - every implementation validates that inserted `DateTimeOffset` values align with the first-inserted value according to the `Period` enum (e.g. `FiveMinutes` requires `minute % 5 == reference.minute % 5`). Sub-minute components (second, ms, us, ns) must also match. This logic lives in `PeriodConverter`.

**Benchmark focus** - current benchmark coverage is organized by workload scenario: dense fixed-step, gapped fixed-step, sparse irregular, calendar-period, bounded stepwise, and mixed-family operations. The benchmark project uses `BenchmarkSwitcher` so filters such as `--filter *DenseFixedStep*` can run individual scenarios.

## Conventions

- **Namespaces**: `Chrono.TimeSeries` (core), `Chrono.TimeSeries.RedBlack` (tree internals)
- **Private fields**: underscore-prefixed (`_keys`, `_values`, `_reference`)
- **Test classes**: named `[ClassName]Test`; use xUnit `[Fact]` with FluentAssertions
- **Custom assertions**: `TimeSeriesAssertions` extends `ReferenceTypeAssertions<ITimeSeries<double>>`; accessed via `ShouldExtensions.Should()` on `ITimeSeries<double>`
- **Nullable + implicit usings** are enabled in all three projects
- **Benchmark classes** are scenario-focused types with `[Benchmark]`-attributed public methods; `Program.cs` runs all discovered benchmarks by default and honors BenchmarkDotNet CLI filters when arguments are provided.

## Pull Request Titles

- **PR titles must use Conventional Commits** because squash merges use the PR title as the commit message.
- **Format**: `<type>[optional scope]: <description>`
- **Allowed types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- **Rule of thumb**: use `feat` for user-visible capability, `fix` for bug fixes, and `docs` for documentation-only changes.
- **Examples**: `feat: add bounded stepwise time series`, `fix(storage): preserve trailing value on contiguous expansion`, `docs: clarify sparse versus stepwise aggregation`
- **When creating or editing a PR, always set a compliant title immediately** instead of relying on a later manual fix.

## Agent Skills

### Issue tracker

Issues are tracked in this repo's GitHub Issues. See `docs/agents/issue-tracker.md`.

### Triage labels

The triage label vocabulary uses the canonical labels `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Domain docs use a single-context layout. See `docs/agents/domain.md`.
