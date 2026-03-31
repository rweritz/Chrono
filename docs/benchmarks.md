# Benchmarks

All benchmarks use [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) with the default job configuration on .NET 10. Every benchmark operates on N = 10,000 data points using `double` values. `FixedSlotTimeSeries` and `SortedArrayTimeSeries` benchmarks use 5-minute intervals; `DynamicSlotTimeSeries` benchmarks use hourly or monthly intervals depending on the scenario.

## Environment

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.8037)
12th Gen Intel Core i7-12700KF, 1 CPU, 20 logical and 12 physical cores
.NET SDK 10.0.104
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
```

---

## Storage — Insert & Access

Benchmarks from `TimeSeriesPerformance`. Measures sequential ordered insertion and random access across 10,000 timestamps. Regular and Sparse use 5-minute intervals; Calendar uses hourly (Strict / Truncate) and monthly (Month) intervals.

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

### Analysis

- **FixedSlot** remains the fastest for both insert (154 μs) and access (84 μs) thanks to O(1) slot indexing on a fixed-tick grid.
- **DynamicSlot Strict** (hourly) is competitive with FixedSlot — insert at 93 μs and access at 102 μs. The overhead comes from `CalendarSlotMath` which performs division-based slot mapping instead of simple tick arithmetic.
- **DynamicSlot Truncate** adds **~44% insert overhead** and **~52% access overhead** over Strict, because every timestamp passes through `CalendarSlotMath.AlignToSlot` to floor it to the nearest period boundary before the slot lookup.
- **DynamicSlot Month** is the slowest calendar variant at 259 μs insert and 238 μs access. Calendar-period slot math for months involves year/month decomposition rather than simple division, making it more expensive per operation.
- **SortedArray** insert (401 μs) reflects O(n) array shifts; access (482 μs) reflects O(log n) binary search.

---

## Arithmetic — `TimeSeriesMath`

Benchmarks from `TimeSeriesMathBenchmarks`. Two pre-populated series are combined element-wise (binary), or a scalar is applied to every point (scalar). Regular and Sparse use 5-minute intervals; Calendar uses monthly intervals.

| Method                    | Mean        | Error     | StdDev    |
|-------------------------- |------------:|----------:|----------:|
| FixedSlotAdd              |    31.83 μs |  0.626 μs |  1.251 μs |
| FixedSlotSubtract         |    69.83 μs |  1.377 μs |  1.352 μs |
| FixedSlotMultiply         |    69.98 μs |  1.240 μs |  1.160 μs |
| FixedSlotDivide           |    70.89 μs |  1.373 μs |  1.832 μs |
| FixedSlotScalarAdd        |    38.02 μs |  0.573 μs |  0.478 μs |
| FixedSlotScalarMultiply   |    72.78 μs |  1.350 μs |  1.263 μs |
| FixedSlotScalarDivide     |    39.60 μs |  0.790 μs |  1.319 μs |
| SortedArrayAdd            |    66.41 μs |  1.988 μs |  5.736 μs |
| SortedArraySubtract       |    65.91 μs |  1.806 μs |  5.123 μs |
| SortedArrayMultiply       |    64.62 μs |  1.592 μs |  4.695 μs |
| SortedArrayDivide         |    59.80 μs |  1.179 μs |  2.002 μs |
| SortedArrayScalarAdd      |    41.99 μs |  1.167 μs |  3.291 μs |
| SortedArrayScalarMultiply |    46.26 μs |  0.881 μs |  1.049 μs |
| SortedArrayScalarDivide   |    40.34 μs |  0.748 μs |  1.577 μs |
| DynamicSlotAdd            |    31.99 μs |  0.636 μs |  0.912 μs |
| DynamicSlotSubtract       |    69.32 μs |  1.378 μs |  1.289 μs |
| DynamicSlotMultiply       |    69.22 μs |  0.356 μs |  0.333 μs |
| DynamicSlotDivide         |    68.76 μs |  0.421 μs |  0.373 μs |
| DynamicSlotScalarAdd      |    37.63 μs |  0.729 μs |  0.895 μs |
| DynamicSlotScalarMultiply |    72.85 μs |  0.703 μs |  0.657 μs |
| DynamicSlotScalarDivide   |    38.35 μs |  0.760 μs |  0.961 μs |

### Analysis

**Binary operations:**
- `FixedSlotAdd` and `DynamicSlotAdd` both benefit from the dense SIMD fast-path when series share the same slot range (~32 μs).
- `FixedSlotSubtract/Multiply/Divide` use the general merge loop (~70 μs). `DynamicSlotSubtract/Multiply/Divide` use the same merge loop with calendar slot-to-timestamp conversion overhead, landing at ~69 μs — comparable to FixedSlot.
- SortedArray binary ops use a two-pointer merge O(n+m) over sorted tick arrays (~60–66 μs), producing compact results without any presence-bit checking.

**Scalar operations:**
- `DynamicSlotScalar*` (~38–73 μs) is **nearly identical** to `FixedSlotScalar*` (~38–73 μs) because both operate on a dense value array with presence-bit iteration.
- `SortedArrayScalar*` (~40–46 μs) iterates a compact contiguous array with no presence-bit checking.

---

## Aggregation — `TimeSeriesAggregation`

Benchmarks from `TimeSeriesAggregationBenchmarks`. FixedSlot and SortedArray roll up 10,000 five-minute points into hourly buckets (factor = 12, ~834 output points). DynamicSlot rolls up 10,000 hourly points into monthly buckets (~14 output points) to exercise the calendar-aware aggregation path.

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

### Analysis

- **FixedSlot** (~15–18 μs) is **~4× faster** than SortedArray (~56–132 μs) for fixed-to-fixed aggregation. Bucket boundaries are computed as integer multiples of the factor (12 for 5 min → 1 hour) with no `DateTimeOffset` allocation in the inner loop.
- **DynamicSlot** aggregation (Hour → Month, ~860–964 μs) is slower because it exercises the **calendar-aware aggregation path**. Each hourly point must be mapped to its enclosing month via `CalendarSlotMath`, which involves year/month decomposition rather than simple division.
- `SortedArrayTimeSeries` aggregation calls `FirstBucket()` for every point, involving `DateTimeOffset` creation and either truncation or calendar-flooring math.
- The calendar aggregation cost is the trade-off for **correctness** when aggregating across calendar boundaries — `FixedSlotTimeSeries` cannot represent monthly output periods at all.

---

## Summary Table

| Category | FixedSlot | SortedArray | DynamicSlot (Strict) | DynamicSlot (Month) |
|---|---|---|---|---|
| Ordered insert | 154 μs | 401 μs | 93 μs | 259 μs |
| Ordered access | 84 μs | 482 μs | 102 μs | 238 μs |
| Binary math | 32–71 μs | 60–66 μs | 32–69 μs | — |
| Scalar math | 38–73 μs | 40–46 μs | 38–73 μs | — |
| Aggregation (fixed→fixed) | 15–18 μs | 56–132 μs | — | — |
| Aggregation (hour→month) | — | — | 860–964 μs | — |

### Key Takeaways

- **FixedSlot** is fastest for fixed-period access and aggregation thanks to O(1) slot arithmetic.
- **DynamicSlot (Strict, hourly)** is a strong middle ground — fastest insert at 93 μs with full calendar period support.
- **DynamicSlot Truncate mode** adds ~44–52% overhead over Strict due to the timestamp flooring step on every operation.
- **DynamicSlot (Month)** insert/access is slower due to calendar-period slot math involving year/month decomposition.
- **SortedArray** binary math is competitive with slot-based types (~60–66 μs) but aggregation is 4–8× slower than FixedSlot.
- **Scalar math** is nearly identical across FixedSlot and DynamicSlot because both iterate a dense value array.
- **Calendar aggregation** (hour → month) is the most expensive path but is the **only correct way** to aggregate into variable-length calendar periods.

## Running Benchmarks Yourself

From the repository root:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

Results are written to `BenchmarkDotNet.Artifacts/` in the repository root. Three benchmark classes run automatically: `TimeSeriesPerformance`, `TimeSeriesMathBenchmarks`, and `TimeSeriesAggregationBenchmarks`. Each class includes benchmarks for all three time series types: `FixedSlotTimeSeries`, `SortedArrayTimeSeries`, and `DynamicSlotTimeSeries`.
