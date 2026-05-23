# Chrono Time Series

Chrono models period-aligned time series values. The core domain concern is how a value is understood at each aligned slot across a bounded time range.

## Language

**Stepwise time series**:
A time series whose logical value applies to every aligned slot from a starting boundary until the next change-point or the ending boundary, even when intermediate slots are not stored explicitly.
_Avoid_: Sparse series, reduced-memory series

**Change-point**:
An aligned timestamp where the effective value of a stepwise time series begins or resumes. A bounded stepwise time series stores the first boundary, the last boundary, and any internal value changes as change-points.
_Avoid_: Marker, sentinel, special value

**Logical range**:
The inclusive bounded span of aligned slots that a time series is defined over. A stepwise time series can only carry values within its logical range; timestamps outside that range are outside the series.
_Avoid_: Implicit duration, guessed end

**Canonical compression**:
The invariant that a stepwise time series stores only the first boundary, the last boundary, and the minimum set of internal change-points needed to reproduce the effective values across its logical range.
_Avoid_: Deferred compaction, history-shaped storage

**Sparse time series**:
A time series whose values only exist at explicitly stored timestamps. Reading a timestamp that was not stored does not imply a carried-forward value.
_Avoid_: Stepwise time series, bounded value range

**Bounded stepwise time series**:
A time series with an explicit logical range whose effective value applies to every aligned slot in that range, while storage is kept in canonical compressed change-points.
_Avoid_: Sparse time series

**Segment write**:
An update that assigns one value across a contiguous aligned portion of a time series. Segment writes may be supported by multiple time series kinds even when their internal storage strategies differ.
_Avoid_: Repeated point writes for one logical block

**Contiguous expansion**:
A logical range change that extends a time series only when the new covered range overlaps the existing range or touches it directly at one boundary. Expansions that would create a gap are invalid.
_Avoid_: Disconnected range growth, gapped extension

## Example dialogue

Developer: "This stepwise time series only stores change-points, but 10:00 still has a value even if nothing is stored at 10:00."

Domain expert: "Right. The value at 10:00 is whichever change-point was most recently established before or at 10:00, as long as 10:00 is inside the logical range."
