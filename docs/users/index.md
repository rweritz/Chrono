# Chrono Documentation

Welcome to the Chrono time series library documentation. Chrono provides high-performance, generic-math time series storage, arithmetic, and aggregation for .NET 10+.

## Table of Contents

1. [Getting Started](getting-started.md) — installation, prerequisites, and your first time series
2. [Time Series Types](time-series-types.md) — sparse types plus `StepwiseTimeSeries<T>`
3. [Periods & Alignment](periods-and-alignment.md) — the `Period` enum, timestamp validation, and slot math
4. [Arithmetic Operations](arithmetic-operations.md) — binary operations between series, scalar math, missing value policies
5. [Aggregation](aggregation.md) — roll-up to coarser periods with Sum, Average, Min, Max, Count
6. [Testing Chrono Code](testing.md) — deterministic test data generators and assertion helpers from `Chrono.Testing`
7. [Benchmarks](benchmarks.md) — performance characteristics, results, and guidance
8. [Choosing a Time-Series Type](choosing-a-time-series.md) — benchmark-informed storage selection by workload shape

## Overview

Chrono.TimeSeries is designed for scenarios where you need to store, combine, and aggregate time-aligned numeric data — think energy metering, IoT sensor readings, financial ticks, or any domain where measurements are taken at regular or irregular intervals.

**Core design principles:**

- **Generic math everywhere** — all types are constrained to `INumber<T>`, so you can use `int`, `double`, `decimal`, or any custom numeric type
- **Three sparse storage engines plus bounded stepwise series** — choose between explicit-point storage and `StepwiseTimeSeries` based on your read semantics and range model
- **Immutable results** — arithmetic and aggregation operations return new series rather than mutating inputs
- **SIMD where it counts** — vectorized fast paths for `double` and `int` ensure bulk operations hit hardware limits

For a quick overview and benchmark summary, see the root [README](../../README.md).
