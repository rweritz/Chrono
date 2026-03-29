# Copilot Instructions

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

**`ITimeSeries<T> where T : struct`** — the central abstraction. Exposes period-aligned `DateTimeOffset → T` storage with `MinDate`/`MaxDate` range tracking. The `Add()` method is defined on the interface but intentionally unimplemented (`NotImplementedException`) in all current implementations — use the indexer instead.

Two implementations exist, both in `Chrono.TimeSeries`:

| Class | Backend | Notes |
|---|---|---|
| `SparseTimeSeries<T>` | Sorted parallel arrays (`long[]` keys + `T[]` values) | General-purpose sparse/irregular storage with binary-search lookup |
| `RegularTimeSeries<T>` | Fixed-step slot array + presence bitset | Fast path for fixed-tick periods with O(1) slot addressing |

**Period validation** — every implementation validates that inserted `DateTimeOffset` values align with the first-inserted value according to the `Period` enum (e.g. `FiveMinutes` requires `minute % 5 == reference.minute % 5`). Sub-minute components (second, ms, µs, ns) must also match. This logic lives in `PeriodConverter`.

**Benchmark focus** — current benchmark coverage compares `SparseTimeSeries<double>` and `RegularTimeSeries<double>` in ordered insert, ordered access, and scalar multiply scenarios.

## Conventions

- **Namespaces**: `Chrono.TimeSeries` (core), `Chrono.TimeSeries.RedBlack` (tree internals)
- **Private fields**: underscore-prefixed (`_keys`, `_values`, `_reference`)
- **Test classes**: named `[ClassName]Test`; use xUnit `[Fact]` with FluentAssertions
- **Custom assertions**: `TimeSeriesAssertions` extends `ReferenceTypeAssertions<ITimeSeries<double>>`; accessed via `ShouldExtensions.Should()` on `ITimeSeries<double>`
- **Nullable + implicit usings** are enabled in all three projects
- **Benchmark class** (`TimeSeriesPerformance`) uses `[Benchmark]`-attributed public methods; `Program.cs` calls `BenchmarkRunner.Run<TimeSeriesPerformance>()`
