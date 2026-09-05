# Chrono

A high-performance time series library for .NET, built on .NET 10 and `System.Numerics.INumber<T>` for full generic math support. Chrono provides efficient storage, arithmetic, and aggregation of time-aligned numeric data.

## Features

- **Generic numeric types** — works with `int`, `double`, `decimal`, and any `INumber<T>` type
- **Three sparse storage strategies** — `FixedSlotTimeSeries<T>`, `SortedArrayTimeSeries<T>`, `DynamicSlotTimeSeries<T>`
- **Bounded stepwise series** — `StepwiseTimeSeries<T>` for dense logical reads with compressed change-points
- **Calendar-aware storage** — `DynamicSlotTimeSeries<T>` with calendar-smart slot math for months, quarters, and years
- **Period alignment** — one canonical UTC grid from 5 minutes to yearly, with Monday weeks and optional truncation (`AlignMode`)
- **Arithmetic operations** — element-wise Add, Subtract, Multiply, Divide between series, plus scalar operations
- **SIMD-accelerated math** — vectorized fast paths for `double` and `int` operations
- **Flexible aggregation** — Sum, Average, Min, Max, Count across time buckets (fixed and calendar-based)
- **Missing value policies** — Intersection, UnionWithZero, or Throw when combining series with mismatched timestamps

## Quick Start

```bash
dotnet add package Chrono.TimeSeries
```

```csharp
using Chrono.TimeSeries;

// Create a 5-minute interval time series
var series = new FixedSlotTimeSeries<double>(Period.FiveMinutes);

var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
for (int i = 0; i < 12; i++)
    series[start.AddMinutes(5 * i)] = i * 1.5;

// Aggregate to hourly sums
var hourly = TimeSeriesAggregation.Sum(series, Period.Hour);

// Scalar multiplication
var scaled = TimeSeriesMath.Multiply(series, 2.0);
```

## Documentation

For detailed guides and API explanations, see the [**full documentation**](docs/users/index.md):

- [Getting Started](docs/users/getting-started.md) — installation & first time series
- [Time Series Types](docs/users/time-series-types.md) — sparse types plus `StepwiseTimeSeries<T>`
- [Periods & Alignment](docs/users/periods-and-alignment.md) — period enum, validation, and slot math
- [Arithmetic Operations](docs/users/arithmetic-operations.md) — binary & scalar math, missing value policies
- [Aggregation](docs/users/aggregation.md) — Sum, Average, Min, Max, Count across time buckets
- [Benchmarks](docs/users/benchmarks.md) — performance characteristics and results

## Benchmarks

Measured with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on .NET 10. The current benchmark project uses a scenario suite covering dense fixed-step, gapped fixed-step, sparse irregular, calendar-period, bounded stepwise, and mixed-family workloads. Each scenario class includes memory diagnostics and deterministic data generation.

Run the full suite:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

List or filter scenarios:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --list flat
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --filter "*DenseFixedStepBenchmarks*"
```

See the [full benchmark guide](docs/users/benchmarks.md) for scenario membership rules and filter examples.

## Requirements

- .NET 10.0 or later
- C# 12+ (for generic math / `INumber<T>`)

## Building & Testing

```bash
# Build
dotnet build Chrono.slnx

# Run tests
dotnet test tests/Chrono.TimeSeries.Test/Chrono.TimeSeries.Test.csproj

# Run benchmarks
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

## Contributing

Contributions are welcome! Please read the [Contributing Guide](CONTRIBUTING.md) before submitting a pull request.

This project uses [Conventional Commits](https://www.conventionalcommits.org/) and automated releases via [release-please](https://github.com/googleapis/release-please). PR titles must follow the conventional commit format (e.g., `feat: add new feature`, `fix: resolve bug`).

## License

This project is licensed under the [MIT License](LICENSE).
