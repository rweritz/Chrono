# Time Series Types

Chrono provides three concrete time series implementations, all implementing `ITimeSeries<T>`.

## FixedSlotTimeSeries\<T\>

Dense slot-indexed storage for fixed-length periods (`FiveMinutes` through `Week`).

- O(1) reads/writes by absolute slot
- Best raw performance for fixed cadence data
- Not suitable for variable-length calendar periods

## SortedArrayTimeSeries\<T\>

Sorted parallel arrays (`long` ticks + values), with binary-search access.

- Supports all periods, including `NonStandard`
- Memory usage proportional to point count
- Reference-based alignment validation for non-`NonStandard` periods

## DynamicSlotTimeSeries\<T\>

Calendar-aware slot-indexed storage for all periods except `NonStandard`.

- O(1) slot operations with calendar slot math
- Supports `AlignMode.Strict` and `AlignMode.Truncate`
- Best when you need fast access for calendar periods (`Month`, `QuaterYear`, etc.)

## Quick Comparison

| Criterion | FixedSlotTimeSeries | SortedArrayTimeSeries | DynamicSlotTimeSeries |
|---|---|---|---|
| Period support | Fixed only | All (incl. NonStandard) | All except NonStandard |
| Access complexity | O(1) | O(log n) | O(1) |
| Insert complexity | O(1) amortized | O(n) worst | O(1) amortized |
| Enumeration | Slot order | Sorted | Slot order |
| Alignment mode | Strict fixed-grid | Reference-based | Strict/Truncate |
