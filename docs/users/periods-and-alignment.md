# Periods & Alignment

The `Period` enum defines the time granularity of a series. It controls how timestamps are validated, how slot indices are computed, and how aggregation buckets are formed.

## The Period Enum

```csharp
public enum Period
{
    NonStandard,     // No validation, no alignment rules
    FiveMinutes,     // 5-minute intervals
    QuarterHour,     // 15-minute intervals (note: spelled "QuaterHour" in code)
    HalfHour,        // 30-minute intervals
    Hour,            // 1-hour intervals
    HalfDay,         // 12-hour intervals
    Day,             // Daily intervals
    Week,            // Weekly intervals (Monday-aligned)
    Month,           // Calendar month
    QuarterYear,     // Calendar quarter (note: spelled "QuaterYear" in code)
    HalfYear,        // Calendar half-year
    Year,            // Calendar year
}
```

## Fixed vs Calendar Periods

Periods fall into two categories based on whether they have a constant number of ticks:

### Fixed-Length Periods

These periods have a constant duration in ticks and support O(1) slot indexing:

| Period | Duration |
|---|---|
| `FiveMinutes` | 5 minutes |
| `QuarterHour` | 15 minutes |
| `HalfHour` | 30 minutes |
| `Hour` | 1 hour |
| `HalfDay` | 12 hours |
| `Day` | 24 hours |
| `Week` | 7 days |

These are the only periods supported by `FixedSlotTimeSeries<T>`.

### Calendar-Length Periods

These periods have variable durations (e.g., months have 28–31 days) and are handled via calendar bucket flooring:

| Period | Bucket Start |
|---|---|
| `Month` | 1st of the month, 00:00 UTC |
| `QuarterYear` | Jan 1, Apr 1, Jul 1, or Oct 1 |
| `HalfYear` | Jan 1 or Jul 1 |
| `Year` | Jan 1 |

Calendar periods are supported by `SortedArrayTimeSeries<T>`, `DynamicSlotTimeSeries<T>`, and `StepwiseTimeSeries<T>`, and as aggregation targets.

## Canonical UTC Alignment

Every standard period uses the same canonical UTC grid in `SortedArrayTimeSeries`, `FixedSlotTimeSeries`, `DynamicSlotTimeSeries`, and `StepwiseTimeSeries`. Alignment is determined by the represented UTC instant, so a timestamp with a non-zero offset is accepted when its UTC instant lies on the grid. Stored and enumerated timestamps are returned in UTC.

Fixed-length periods compute an **absolute slot index** using:

```
slot = (timestamp.UtcTicks - anchor) / stepTicks
```

Where `anchor` is the Unix epoch, except for `Week`, whose anchor is Monday, January 5, 1970 at 00:00 UTC. If the timestamp does not divide evenly, an `ArgumentException` is thrown. The resulting rules are:

| Period | Canonical UTC boundary |
|---|---|
| `FiveMinutes` | Minute divisible by 5, with seconds and smaller components zero |
| `QuarterHour` | Minute divisible by 15, with seconds and smaller components zero |
| `HalfHour` | Minute 0 or 30, with seconds and smaller components zero |
| `Hour` | Top of the UTC hour |
| `HalfDay` | 00:00 or 12:00 UTC |
| `Day` | 00:00 UTC |
| `Week` | Monday at 00:00 UTC |
| `Month` | First day of the month at 00:00 UTC |
| `QuarterYear` | January 1, April 1, July 1, or October 1 at 00:00 UTC |
| `HalfYear` | January 1 or July 1 at 00:00 UTC |
| `Year` | January 1 at 00:00 UTC |

This means timestamps must be exactly aligned to the period grid:

```csharp
var series = new SortedArrayTimeSeries<double>(Period.Hour);

// ✅ Aligned to the hour
series[new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)] = 1.0;

// ❌ Throws ArgumentException — 30 minutes doesn't align to hourly grid
series[new DateTimeOffset(2024, 1, 1, 12, 30, 0, TimeSpan.Zero)] = 2.0;
```

`DynamicSlotTimeSeries` with `AlignMode.Truncate` is the exception to strict input validation: it floors input to the canonical bucket boundary before operating. The other families, and `DynamicSlotTimeSeries` with `AlignMode.Strict`, reject off-grid timestamps.

`Period.NonStandard` has no grid and remains unrestricted. It is supported by `SortedArrayTimeSeries` for arbitrary timestamps.

> [!IMPORTANT]
> This canonical model is a compatibility break from versions that let the first `SortedArrayTimeSeries` write define a reference-relative grid. A timestamp such as `2024-01-01T00:02:03Z` is now rejected immediately for `Period.FiveMinutes`, even if every later timestamp would have repeated that two-minute, three-second phase. Use canonical timestamps, truncate before writing when that matches your application policy, or use `Period.NonStandard` for genuinely irregular data.

The former public `PeriodConverter` validation helper has been removed with this change. Alignment is now an internal invariant of the time-series implementations rather than a caller-configurable reference comparison.

## Bucket Flooring for Aggregation

When aggregating into calendar periods, timestamps are floored to the start of their containing bucket:

```csharp
// A timestamp in the middle of March
var ts = new DateTimeOffset(2024, 3, 15, 14, 30, 0, TimeSpan.Zero);

// Floored to different calendar buckets:
// Month    → 2024-03-01T00:00:00Z
// Quarter  → 2024-01-01T00:00:00Z
// HalfYear → 2024-01-01T00:00:00Z
// Year     → 2024-01-01T00:00:00Z
```

For fixed-length periods, truncation uses the same UTC anchor and duration as slot validation. Weekly flooring therefore always returns Monday at 00:00 UTC, including for dates before the 1970 anchor.
