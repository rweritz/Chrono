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

These are the only periods supported by `RegularTimeSeries<T>`.

### Calendar-Length Periods

These periods have variable durations (e.g., months have 28–31 days) and are handled via calendar bucket flooring:

| Period | Bucket Start |
|---|---|
| `Month` | 1st of the month, 00:00 UTC |
| `QuarterYear` | Jan 1, Apr 1, Jul 1, or Oct 1 |
| `HalfYear` | Jan 1 or Jul 1 |
| `Year` | Jan 1 |

Calendar periods are supported by `SparseTimeSeries<T>` and as aggregation targets.

## Timestamp Alignment

### For RegularTimeSeries

`RegularTimeSeries` computes an **absolute slot index** from each timestamp using:

```
slot = (timestamp.UtcTicks - anchor) / stepTicks
```

Where `anchor` is the Unix epoch (or January 5, 1970 for weekly alignment to Monday). If the timestamp doesn't divide evenly, an `ArgumentException` is thrown.

This means timestamps must be exactly aligned to the period grid:

```csharp
var series = new RegularTimeSeries<double>(Period.Hour);

// ✅ Aligned to the hour
series[new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)] = 1.0;

// ❌ Throws ArgumentException — 30 minutes doesn't align to hourly grid
series[new DateTimeOffset(2024, 1, 1, 12, 30, 0, TimeSpan.Zero)] = 2.0;
```

### For SparseTimeSeries

`SparseTimeSeries` uses a **reference-based validation** approach. The first timestamp inserted becomes the reference, and all subsequent timestamps must have matching sub-period components.

For example, with `Period.FiveMinutes`:
- Reference: `2024-01-01T00:06:07.008+01:00`
- Valid: any timestamp where `Minute % 5` matches, and seconds/milliseconds/microseconds/nanoseconds all match the reference
- Invalid: `2024-01-01T00:08:00.000+01:00` (minute alignment doesn't match)

For `Period.Hour`:
- All sub-hour components (minute, second, millisecond, microsecond, nanosecond) must match the reference

For `Period.NonStandard`, no validation is performed.

## Period Validation Functions

The `PeriodConverter` class provides validation functions for sub-daily periods:

| Period | Validation Rule |
|---|---|
| `FiveMinutes` | `minute % 5` matches reference, sub-minute components match |
| `QuarterHour` | `minute % 15` matches reference, sub-minute components match |
| `HalfHour` | `minute % 30` matches reference, sub-minute components match |
| `Hour` | All sub-hour components match reference |

Periods from `HalfDay` and above don't have reference-based validation in `PeriodConverter` — they rely on the slot alignment math in `RegularTimeSeries` or the calendar floor logic for aggregation.

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

For fixed-length periods, truncation uses simple modulo arithmetic on UTC ticks. For weekly periods, the anchor is set to Monday, January 5, 1970 to ensure weeks start on Monday.
