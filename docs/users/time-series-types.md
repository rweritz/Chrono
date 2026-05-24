# Time Series Types

Chrono provides sparse time series and bounded stepwise time series with family-specific count and enumeration semantics.

At the shared read-only level, `IReadOnlyTimeSeries<T>` exposes `MinDate` and `MaxDate` as explicit-point bounds for compatibility-style checks. For sparse series those bounds describe the first and last stored points. For bounded stepwise series they describe the stored change-point bounds, while `LogicalRangeStart` and `LogicalRangeEnd` remain the family-specific way to talk about dense logical coverage.

## Sparse time series

Sparse time series only contain explicitly stored points. They implement `ISparseTimeSeries<T>`, expose `ExplicitPointCount`, and enumerate stored points through `GetPoints()`.

### FixedSlotTimeSeries\<T\>

Dense slot-indexed storage for fixed-length periods (`FiveMinutes` through `Week`).

- O(1) reads/writes by absolute slot
- Best raw performance for fixed cadence data
- Not suitable for variable-length calendar periods

### SortedArrayTimeSeries\<T\>

Sorted parallel arrays (`long` ticks + values), with binary-search access.

- Supports all periods, including `NonStandard`
- Memory usage proportional to point count
- Reference-based alignment validation for non-`NonStandard` periods

### DynamicSlotTimeSeries\<T\>

Calendar-aware slot-indexed storage for all periods except `NonStandard`.

- O(1) slot operations with calendar slot math
- Supports `AlignMode.Strict` and `AlignMode.Truncate`
- Best when you need fast access for calendar periods (`Month`, `QuaterYear`, etc.)

## Bounded stepwise time series

`StepwiseTimeSeries<T>` implements `IBoundedStepwiseTimeSeries<T>`. It exposes an explicit logical range through `LogicalRangeStart`, `LogicalRangeEnd`, and `LogicalSlotCount`, while stored change-points are surfaced separately through `ChangePointCount` and `GetChangePoints()`. The shared `MinDate`/`MaxDate` metadata does not replace that logical-range vocabulary; use the logical-range members when you need dense coverage semantics.

- Dense logical reads within the logical range
- Canonical compression of stored change-points
- Contiguous-only logical range expansion

## Quick Comparison

| Criterion | FixedSlotTimeSeries | SortedArrayTimeSeries | DynamicSlotTimeSeries | StepwiseTimeSeries |
|---|---|---|---|---|
| Family | Sparse | Sparse | Sparse | Bounded stepwise |
| Period support | Fixed only | All (incl. NonStandard) | All except NonStandard | All except NonStandard |
| Read semantics | Explicit points only | Explicit points only | Explicit points only | Dense logical reads in logical range |
| Count surface | `ExplicitPointCount` | `ExplicitPointCount` | `ExplicitPointCount` | `LogicalSlotCount` + `ChangePointCount` |
| Enumeration surface | `GetPoints()` | `GetPoints()` | `GetPoints()` | `GetChangePoints()` |
| Alignment mode | Strict fixed-grid | Reference-based | Strict/Truncate | Strict |
