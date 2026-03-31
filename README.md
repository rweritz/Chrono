# Chrono

A high-performance time series library for .NET, built on .NET 10 and `System.Numerics.INumber<T>` for full generic math support. Chrono provides efficient storage, arithmetic, and aggregation of time-aligned numeric data.

## Features

- **Generic numeric types** — works with `int`, `double`, `decimal`, and any `INumber<T>` type
- **Three storage strategies** — `FixedSlotTimeSeries<T>`, `SortedArrayTimeSeries<T>`, `DynamicSlotTimeSeries<T>`
- **Calendar-aware storage** — `DynamicSlotTimeSeries<T>` with calendar-smart slot math for months, quarters, and years
- **Period alignment** — built-in periods from 5 minutes to yearly, with automatic timestamp validation and optional truncation (`AlignMode`)
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

For detailed guides and API explanations, see the [**full documentation**](docs/index.md):

- [Getting Started](docs/getting-started.md) — installation & first time series
- [Time Series Types](docs/time-series-types.md) — `FixedSlotTimeSeries<T>`, `SortedArrayTimeSeries<T>`, `DynamicSlotTimeSeries<T>`
- [Periods & Alignment](docs/periods-and-alignment.md) — period enum, validation, and slot math
- [Arithmetic Operations](docs/arithmetic-operations.md) — binary & scalar math, missing value policies
- [Aggregation](docs/aggregation.md) — Sum, Average, Min, Max, Count across time buckets
- [Benchmarks](docs/benchmarks.md) — performance characteristics and results

## Benchmarks

Measured with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on .NET 10.0, N = 10,000 data points (`double` series). See the [full benchmark analysis](docs/benchmarks.md) for details.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.8037)
12th Gen Intel Core i7-12700KF, 1 CPU, 20 logical and 12 physical cores
.NET SDK 10.0.104 — .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
```

### Storage — Insert & Access

| Method                    | Mean       | Error    | StdDev    |
|-------------------------- |-----------:|---------:|----------:|
| FixedSlotOrderedInsert    | 154.35 μs  | 2.944 μs |  5.156 μs |
| SortedArrayOrderedInsert  | 401.21 μs  | 8.017 μs |  7.874 μs |
| FixedSlotOrderedAccess    |  83.85 μs  | 1.080 μs |  1.010 μs |
| SortedArrayOrderedAccess  | 481.55 μs  | 6.908 μs |  6.462 μs |
| DynamicSlotStrictInsert   |  92.69 μs  | 1.802 μs |  1.686 μs |
| DynamicSlotTruncateInsert | 133.13 μs  | 2.653 μs |  2.839 μs |
| DynamicSlotMonthInsert    | 259.37 μs  | 3.765 μs |  3.522 μs |
| DynamicSlotStrictAccess   | 101.61 μs  | 0.654 μs |  0.612 μs |
| DynamicSlotTruncateAccess | 154.46 μs  | 0.508 μs |  0.450 μs |
| DynamicSlotMonthAccess    | 237.71 μs  | 1.353 μs |  1.265 μs |

### Arithmetic — `TimeSeriesMath`

| Method                      | Mean        | Error     | StdDev    |
|---------------------------- |------------:|----------:|----------:|
| FixedSlotAdd                |    31.83 μs |  0.626 μs |  1.251 μs |
| FixedSlotSubtract           |    69.83 μs |  1.377 μs |  1.352 μs |
| FixedSlotMultiply           |    69.98 μs |  1.240 μs |  1.160 μs |
| FixedSlotDivide             |    70.89 μs |  1.373 μs |  1.832 μs |
| FixedSlotScalarAdd          |    38.02 μs |  0.573 μs |  0.478 μs |
| FixedSlotScalarMultiply     |    72.78 μs |  1.350 μs |  1.263 μs |
| FixedSlotScalarDivide       |    39.60 μs |  0.790 μs |  1.319 μs |
| SortedArrayAdd              |    66.41 μs |  1.988 μs |  5.736 μs |
| SortedArraySubtract         |    65.91 μs |  1.806 μs |  5.123 μs |
| SortedArrayMultiply         |    64.62 μs |  1.592 μs |  4.695 μs |
| SortedArrayDivide           |    59.80 μs |  1.179 μs |  2.002 μs |
| SortedArrayScalarAdd        |    41.99 μs |  1.167 μs |  3.291 μs |
| SortedArrayScalarMultiply   |    46.26 μs |  0.881 μs |  1.049 μs |
| SortedArrayScalarDivide     |    40.34 μs |  0.748 μs |  1.577 μs |
| DynamicSlotAdd              |    31.99 μs |  0.636 μs |  0.912 μs |
| DynamicSlotSubtract         |    69.32 μs |  1.378 μs |  1.289 μs |
| DynamicSlotMultiply         |    69.22 μs |  0.356 μs |  0.333 μs |
| DynamicSlotDivide           |    68.76 μs |  0.421 μs |  0.373 μs |
| DynamicSlotScalarAdd        |    37.63 μs |  0.729 μs |  0.895 μs |
| DynamicSlotScalarMultiply   |    72.85 μs |  0.703 μs |  0.657 μs |
| DynamicSlotScalarDivide     |    38.35 μs |  0.760 μs |  0.961 μs |

### Aggregation — `TimeSeriesAggregation`

FixedSlot and SortedArray aggregate 5-min → hour; DynamicSlot aggregates hour → month.

| Method             | Mean       | Error     | StdDev    |
|------------------- |-----------:|----------:|----------:|
| FixedSlotSum       |  15.38 μs  |  0.142 μs |  0.118 μs |
| FixedSlotAverage   |  15.84 μs  |  0.144 μs |  0.128 μs |
| FixedSlotMin       |  17.42 μs  |  0.295 μs |  0.303 μs |
| FixedSlotMax       |  17.73 μs  |  0.118 μs |  0.105 μs |
| FixedSlotCount     |  14.76 μs  |  0.244 μs |  0.240 μs |
| SortedArraySum     |  58.04 μs  |  1.150 μs |  1.856 μs |
| SortedArrayAverage |  55.65 μs  |  1.104 μs |  2.046 μs |
| SortedArrayMin     | 118.34 μs  |  7.837 μs | 22.986 μs |
| SortedArrayMax     | 127.78 μs  |  2.083 μs |  2.046 μs |
| SortedArrayCount   | 132.42 μs  |  1.279 μs |  0.999 μs |
| DynamicSlotSum     | 860.12 μs  | 15.347 μs | 14.356 μs |
| DynamicSlotAverage | 863.65 μs  |  6.435 μs |  5.705 μs |
| DynamicSlotMin     | 956.08 μs  | 15.808 μs | 14.787 μs |
| DynamicSlotMax     | 963.53 μs  |  7.389 μs |  6.551 μs |
| DynamicSlotCount   | 870.50 μs  |  6.635 μs |  5.881 μs |

**Key takeaways:**
- `FixedSlotTimeSeries` is the fastest for insert and access due to O(1) slot indexing
- `DynamicSlotTimeSeries` (Strict, hourly) is a strong middle ground — insert at 93 μs with full calendar period support
- Fixed-to-fixed aggregation favours **FixedSlot** (~15–18 μs vs ~56–132 μs SortedArray)
- Calendar aggregation (hour → month) is slower (~860–964 μs) but is the only correct path for variable-length calendar periods

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
