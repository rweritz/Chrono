# Choosing A Time-Series Type

Chrono has several storage types because time-series workloads differ in more than point count. Choose based on the shape of the timeline, the meaning of missing slots, and whether the series has a bounded logical range.

## Quick Choice

| Workload | Recommended type | Why |
|---|---|---|
| Dense, fixed-step data with a known regular period | `FixedSlotTimeSeries<T>` | O(1) slot addressing and compact presence tracking make dense reads and arithmetic fast. |
| Fixed-step data with gaps or misses | `FixedSlotTimeSeries<T>` when the grid is bounded and reasonably sized; otherwise `DynamicSlotTimeSeries<T>` | Both preserve the fixed grid. `FixedSlot` is strongest when the slot window is affordable; `DynamicSlot` avoids paying for a very wide window. |
| Irregular or very wide sparse timestamps | `SortedArrayTimeSeries<T>` | Stores explicit points without allocating the empty range. Ordered data is compact and predictable. |
| Calendar periods such as month or year | `DynamicSlotTimeSeries<T>` | Calendar-aware slot addressing supports variable-length periods; `FixedSlotTimeSeries<T>` is for fixed-tick periods. |
| A bounded range with long runs of the same value | `StepwiseTimeSeries<T>` | Stores change points instead of every logical slot and exposes the logical range explicitly. |
| Operations mixing storage families | Keep the input types that match the data; use the specialized `Try*As...` APIs when the output shape matters | Chrono routes mixed-family operations safely, while explicit specialization lets callers request a result representation. |

These are starting points, not guarantees. Run the benchmark scenario that matches your data shape with your own point counts, value type, and operation mix before making a production choice.

## What The New Benchmarks Measure

The benchmark suite is organized by workload shape:

- `DenseFixedStepBenchmarks`: contiguous five-minute data across `FixedSlot`, `DynamicSlot`, and `SortedArray`.
- `GappedFixedStepBenchmarks`: the same fixed grid at 1%, 10%, 50%, and 90% occupancy.
- `SparseIrregularBenchmarks`: wide and clustered irregular explicit-point data.
- `CalendarPeriodBenchmarks`: month-period insert, lookup, arithmetic, aggregation, and resampling.
- `StepwiseBenchmarks`: bounded logical reads, change-point writes, arithmetic, aggregation, and materialized comparisons.
- `MixedFamilyOperationBenchmarks` and `MixedFamilySpecializationBenchmarks`: cross-family operations and explicit result specialization.

All benchmark data is deterministic. The numbers below come from a Release `ShortRun` on .NET 10, x64, AVX2, using three warmups and three measured iterations. They are directional measurements for this machine, not API promises.

## Representative Results

### Dense Ordered Lookup

Mean time per benchmark invocation, lower is better:

| Implementation | 1,000 points | 10,000 points | 100,000 points |
|---|---:|---:|---:|
| `FixedSlotTimeSeries<double>` | 3.254 us | 33.564 us | 349.413 us |
| `DynamicSlotTimeSeries<double>` | 7.873 us | 67.532 us | 653.075 us |
| `SortedArrayTimeSeries<double>` | 26.428 us | 349.249 us | 3,917.718 us |

For a dense fixed-step timeline, `FixedSlotTimeSeries<T>` is the clear first candidate. `SortedArrayTimeSeries<T>` remains useful when the timeline is not actually dense, but its lookup cost grows with the number of explicit points and binary-search work.

### Other Representative Rows

These rows use 10,000 points and the named scenario parameter shown in the table:

| Scenario and operation | Case | Mean |
|---|---|---:|
| Gapped mixed lookup, 50% occupancy | `FixedSlot` | 87.322 us |
| Gapped mixed lookup, 50% occupancy | `DynamicSlot` | 124.473 us |
| Gapped mixed lookup, 50% occupancy | `SortedArray` | 720.492 us |
| Irregular mixed lookup, clustered shape | `DynamicSlot` | 125.030 us |
| Irregular mixed lookup, clustered shape | `SortedArray` | 626.203 us |
| Irregular mixed lookup, wide shape | `SortedArray` | 595.008 us |
| Stepwise random logical lookup, 1% change points | `StepwiseTimeSeries` | 389.01 us |
| Mixed-family binary intersection | `SortedArray + DynamicSlot` | 162.77 us |
| Mixed-family binary intersection | `FixedSlot + SortedArray` | 179.28 us |
| Mixed-family binary intersection | `Stepwise + Stepwise` | 382.61 us |

The gapped results show why occupancy alone is not enough to choose a type: a bounded fixed grid keeps lookup cheap even when many slots are absent, while a sorted explicit-point representation pays for searching the explicit collection. The irregular results show the opposite tradeoff: `SortedArrayTimeSeries<T>` avoids the cost of a very wide grid and is the natural choice when the empty range is part of the data shape.

### Scenario Interpretation

The benchmark families are intended to answer different questions:

| Question | Run | What to compare |
|---|---|---|
| Is a regular dense grid worth a slot array? | `*DenseFixedStepBenchmarks*` | `FixedSlot` versus `DynamicSlot` and `SortedArray` for your dominant operation. |
| How do misses affect a fixed grid? | `*GappedFixedStepBenchmarks*` | Change `Occupancy`; compare hit/miss/mixed lookup, aggregation, and resampling. |
| Does the empty range dominate memory or setup? | `*SparseIrregularBenchmarks*` | Compare wide and clustered shapes; include insert, remove, lookup, and aggregate operations. |
| Are month/year boundaries the bottleneck? | `*CalendarPeriodBenchmarks*` | Compare `DynamicSlot` and `SortedArray`; use truncate-mode cases only when that is your alignment policy. |
| Is the data mostly runs rather than observations? | `*StepwiseBenchmarks*` | Compare logical lookup and change-point writes against the materialized sparse equivalents. |
| What happens when inputs use different families? | `*MixedFamily*` | Compare default routing with explicit `Try...AsFixedSlotTimeSeries`, `Try...AsDynamicSlotTimeSeries`, and `Try...AsBoundedStepwiseTimeSeries` results. |

## Running The Suite

Run all discovered scenarios with the standard BenchmarkDotNet job. This is a large suite and can take a long time because each parameterized method is measured separately:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --filter "*Benchmarks*"
```

For a faster representative run, use the `ShortRun` job and filter the operation you care about:

```bash
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release -- --filter "*DenseFixedStepBenchmarks.OrderedLookup*" --job short
```

BenchmarkDotNet writes reports below `BenchmarkDotNet.Artifacts/`. Do not compare results across machines without recording the runtime, CPU features, operating system, job, point counts, and scenario parameters.

## Practical Rules

1. Use `FixedSlotTimeSeries<T>` when timestamps are aligned to a fixed-tick period and the slot window is not excessively wider than the data.
2. Use `DynamicSlotTimeSeries<T>` when you still need fixed-period semantics but the window can expand, the period is calendar-based, or alignment mode matters.
3. Use `SortedArrayTimeSeries<T>` when explicit points are sparse or irregular and allocating the complete slot window would be wasteful.
4. Use `StepwiseTimeSeries<T>` when a bounded logical range and change points describe the data better than explicit observations.
5. Preserve the input representation through ordinary operations unless measurements show that an explicit specialized result is worth the conversion cost.
