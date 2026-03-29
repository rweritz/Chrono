# Benchmarks

All benchmarks use [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) with the default job configuration on .NET 10. Every benchmark operates on N = 10,000 data points at 5-minute intervals using `double` values.

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

Benchmarks from `TimeSeriesPerformance`. Measures sequential ordered insertion and random access across 10,000 timestamps.

| Method                | Mean       | Error    | StdDev   |
|---------------------- |-----------:|---------:|---------:|
| RegularOrderedInsert  |  92.27 μs  | 1.819 μs | 2.429 μs |
| SparseOrderedInsert   | 320.49 μs  | 5.786 μs | 5.683 μs |
| RegularOrderedAccess  |  35.66 μs  | 0.681 μs | 0.784 μs |
| SparseOrderedAccess   | 349.85 μs  | 2.315 μs | 1.933 μs |

### Analysis

- **Insert:** `RegularTimeSeries` is **~3.5× faster**. Each write is an O(1) slot calculation + array write, with only occasional backing-array growth. `SparseTimeSeries` requires a binary search and an O(n) array shift even for in-order appends.
- **Access:** `RegularTimeSeries` is **~10× faster**. Slot lookup is pure integer arithmetic; `SparseTimeSeries` needs O(log n) binary search (~13 comparisons at N=10,000).

---

## Arithmetic — `TimeSeriesMath`

Benchmarks from `TimeSeriesMathBenchmarks`. Two pre-populated series are combined element-wise (binary), or a scalar is applied to every point (scalar).

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

### Analysis

**Binary operations:**
- Results are close across both types (~21–37 μs).
- `RegularAdd` is notably faster (21 μs) because when both series are fully dense and share the same slot range, a SIMD fast-path runs directly over the value arrays with no slot lookup overhead.
- `RegularSubtract/Multiply/Divide` fall back to the general merge loop (no aligned-dense optimisation), landing at ~36 μs — similar to sparse.
- Sparse binary ops use a two-pointer merge O(n+m) over sorted tick arrays, producing compact results without any presence-bit checking.

**Scalar operations:**
- `SparseScalar*` (~15–17 μs) outperforms `RegularScalar*` (~22–38 μs) because sparse operates over a compact array of exactly N values. Regular must also iterate the slot grid and check presence bits.
- `RegularScalarAdd` and `RegularScalarDivide` (~22 μs) are faster than `RegularScalarMultiply` (~38 μs) because the multiply path goes through the SIMD `MultiplyDense` helper which has more setup overhead at this data size.

---

## Aggregation — `TimeSeriesAggregation`

Benchmarks from `TimeSeriesAggregationBenchmarks`. 10,000 five-minute points are rolled up into hourly buckets (factor = 12), producing ~834 hourly output points.

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

### Analysis

- **Regular** (~14–18 μs) is **~3× faster** than Sparse (~51–60 μs) for this fixed-to-fixed aggregation.
- `RegularTimeSeries` aggregation uses direct slot arithmetic: no `DateTimeOffset` allocation occurs in the inner loop. Bucket boundaries are computed as integer multiples of the factor (12 for 5 min → 1 hour).
- `SparseTimeSeries` aggregation calls `FirstBucket()` for every point, which involves `DateTimeOffset` creation and either truncation or calendar-flooring math.
- `RegularMin`/`RegularMax` are roughly equal to `RegularSum` because the inner loop iterates the same slot range regardless of the aggregator used.
- `SparseMin`/`SparseMax` are slightly slower than `SparseSum` due to the extra conditional comparison in `MinAggregator`/`MaxAggregator`.

---

## Summary Table

| Category | Regular | Sparse | Faster |
|---|---|---|---|
| Ordered insert | 92 μs | 320 μs | Regular 3.5× |
| Random access | 36 μs | 350 μs | Regular 10× |
| Binary math | 21–37 μs | 24–25 μs | Comparable |
| Scalar math | 22–38 μs | 15–17 μs | Sparse 1.4–2.5× |
| Aggregation | 14–18 μs | 51–60 μs | Regular 3× |

## Running Benchmarks Yourself

From the repository root:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

Results are written to `BenchmarkDotNet.Artifacts/` in the repository root. Three benchmark classes run automatically: `TimeSeriesPerformance`, `TimeSeriesMathBenchmarks`, and `TimeSeriesAggregationBenchmarks`.
