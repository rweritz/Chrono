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

**Explicit-point bounds**:
The earliest and latest aligned timestamps that are explicitly stored in a sparse time series. These bounds do not imply that intermediate timestamps have values.
_Avoid_: Logical range, dense coverage, implied continuity

**Canonical compatibility result**:
The default result form for operations that combine different time series families. It favors predictable cross-family behavior over preserving a specialized storage shape.
_Avoid_: Arbitrary result shape, family leak

**Family-preserving operation**:
An operation specialized for one time series family that returns the same family so it can keep family-specific performance characteristics.
_Avoid_: Forced canonical fallback, cross-family default

**Cross-family math**:
An operation between different time series families that preserves each family's value semantics instead of silently converting one family into another. Sparse series remain sparse unless a caller explicitly requests a different transformation.
_Avoid_: Implicit densification, hidden carry-forward

**Compatibility operation**:
The default operation shape exposed through shared abstractions. It aims for predictable behavior across time series families rather than the most specialized storage result.
_Avoid_: Family-specific fast path, specialized-only API

**Compatibility fallback**:
The default result rule when an operation combines different concrete time series types. The operation returns the shared contract backed by the most broadly compatible concrete representation.
_Avoid_: Type-dependent surprise, hidden specialization

**Shared compatibility metadata**:
Read-only time series metadata that is safe to depend on across families for default operations. It belongs on the read-only contract because callers do not need mutation rights to reason about compatibility.
_Avoid_: Mutation-gated metadata, write-only compatibility

**Explicit resampling**:
Any change from one period to another must be requested as its own operation rather than being hidden inside arithmetic. Arithmetic assumes the compared slots already share the same period.
_Avoid_: Implicit period conversion, hidden rebucketing

**Semantically valid specialization**:
A requested specialized result is only valid when that family can represent the operation without changing the meaning of missing values, logical coverage, or period alignment. If not, the specialization must fail instead of silently converting semantics.
_Avoid_: Forced specialization, semantic drift

**Bounded stepwise time series**:
A time series with an explicit logical range whose effective value applies to every aligned slot in that range, while storage is kept in canonical compressed change-points.
_Avoid_: Sparse time series

**Segment write**:
An update that assigns one value across a contiguous aligned portion of a time series. Segment writes may be supported by multiple time series kinds even when their internal storage strategies differ.
_Avoid_: Repeated point writes for one logical block

**Contiguous expansion**:
A logical range change that extends a time series only when the new covered range overlaps the existing range or touches it directly at one boundary. Expansions that would create a gap are invalid.
_Avoid_: Disconnected range growth, gapped extension

## Flagged ambiguities

- **`StartDate` / `EndDate`** are ambiguous because they can mean either explicit-point bounds or logical-range bounds. Use **`MinDate` / `MaxDate`** for explicit-point bounds and **`LogicalRangeStart` / `LogicalRangeEnd`** for bounded stepwise coverage.

## Example dialogue

Developer: "This stepwise time series only stores change-points, but 10:00 still has a value even if nothing is stored at 10:00."

Domain expert: "Right. The value at 10:00 is whichever change-point was most recently established before or at 10:00, as long as 10:00 is inside the logical range."
