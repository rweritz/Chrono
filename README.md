# Chrono

A high-performance time series library for .NET, built on .NET 10 and `System.Numerics.INumber<T>` for full generic math support. Chrono provides efficient storage, arithmetic, and aggregation of time-aligned numeric data.

## Features

- **Generic numeric types** — works with `int`, `double`, `decimal`, and any `INumber<T>` type
- **Two storage strategies** — `RegularTimeSeries<T>` (O(1) slot-indexed grid) and `SparseTimeSeries<T>` (sorted binary-search storage)
- **Period alignment** — built-in periods from 5 minutes to yearly, with automatic timestamp validation
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
var series = new RegularTimeSeries<double>(Period.FiveMinutes);

var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
for (int i = 0; i < 12; i++)
    series[start.AddMinutes(5 * i)] = i * 1.5;

// Aggregate to hourly sums
var hourly = TimeSeriesAggregation.Sum(series, Period.Hour);

// Scalar multiplication
var scaled = TimeSeriesMath.Multiply(series, 2.0);
```

## Documentation

For detailed guides and API explanations, see the [**full documentation**](docs/index.md):

- [Getting Started](docs/getting-started.md) — installation & first time series
- [Time Series Types](docs/time-series-types.md) — `RegularTimeSeries<T>` vs `SparseTimeSeries<T>`
- [Periods & Alignment](docs/periods-and-alignment.md) — period enum, validation, and slot math
- [Arithmetic Operations](docs/arithmetic-operations.md) — binary & scalar math, missing value policies
- [Aggregation](docs/aggregation.md) — Sum, Average, Min, Max, Count across time buckets
- [Benchmarks](docs/benchmarks.md) — performance characteristics and results

## Benchmarks

Measured with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on .NET 10.0, N = 10,000 data points (5-minute `double` series). See the [full benchmark analysis](docs/benchmarks.md) for details.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.8037)
12th Gen Intel Core i7-12700KF, 1 CPU, 20 logical and 12 physical cores
.NET SDK 10.0.104 — .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
```

### Storage — Insert & Access

| Method                | Mean       | Error    | StdDev   |
|---------------------- |-----------:|---------:|---------:|
| RegularOrderedInsert  |  92.27 μs  | 1.819 μs | 2.429 μs |
| SparseOrderedInsert   | 320.49 μs  | 5.786 μs | 5.683 μs |
| RegularOrderedAccess  |  35.66 μs  | 0.681 μs | 0.784 μs |
| SparseOrderedAccess   | 349.85 μs  | 2.315 μs | 1.933 μs |

### Arithmetic — `TimeSeriesMath`

| Method                | Mean      | Error    | StdDev   |
|---------------------- |----------:|---------:|---------:|
| RegularAdd            |  21.04 μs | 0.420 μs | 0.839 μs |
| RegularSubtract       |  36.84 μs | 0.724 μs | 0.889 μs |
| RegularMultiply       |  36.42 μs | 0.718 μs | 1.139 μs |
| RegularDivide         |  36.77 μs | 0.720 μs | 0.707 μs |
| RegularScalarAdd      |  22.56 μs | 0.443 μs | 0.924 μs |
| RegularScalarMultiply |  37.89 μs | 0.749 μs | 1.074 μs |
| RegularScalarDivide   |  22.50 μs | 0.447 μs | 0.942 μs |
| SparseAdd             |  24.90 μs | 0.553 μs | 1.631 μs |
| SparseSubtract        |  23.77 μs | 0.471 μs | 0.952 μs |
| SparseMultiply        |  24.46 μs | 0.485 μs | 1.252 μs |
| SparseDivide          |  23.58 μs | 0.464 μs | 1.048 μs |
| SparseScalarAdd       |  15.15 μs | 0.285 μs | 0.468 μs |
| SparseScalarMultiply  |  14.57 μs | 0.287 μs | 0.455 μs |
| SparseScalarDivide    |  16.71 μs | 0.631 μs | 1.810 μs |

### Aggregation — `TimeSeriesAggregation` (5-min → Hour)

| Method         | Mean      | Error    | StdDev   |
|--------------- |----------:|---------:|---------:|
| RegularSum     |  17.27 μs | 0.259 μs | 0.216 μs |
| RegularAverage |  16.17 μs | 0.190 μs | 0.169 μs |
| RegularMin     |  17.28 μs | 0.113 μs | 0.095 μs |
| RegularMax     |  17.77 μs | 0.166 μs | 0.147 μs |
| RegularCount   |  14.14 μs | 0.094 μs | 0.083 μs |
| SparseSum      |  53.61 μs | 1.061 μs | 2.261 μs |
| SparseAverage  |  53.43 μs | 1.001 μs | 0.887 μs |
| SparseMin      |  59.98 μs | 1.128 μs | 1.159 μs |
| SparseMax      |  59.66 μs | 1.182 μs | 1.618 μs |
| SparseCount    |  50.72 μs | 0.839 μs | 0.744 μs |

**Key takeaways:**
- `RegularTimeSeries` is **3–10× faster** for insert and access due to O(1) slot indexing
- Binary math is **comparable** between series types (~21–37 μs); sparse benefits from compact merge, regular benefits from SIMD dense fast-paths
- Scalar operations favour **Sparse** (~15–17 μs vs ~22–38 μs) because it operates on a compact contiguous array
- Aggregation favours **Regular** (~14–18 μs vs ~51–60 μs) due to the direct fixed-factor slot arithmetic path

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

## License

See repository for license details.
