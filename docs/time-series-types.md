# Time Series Types

Chrono provides two concrete time series implementations, both implementing `ITimeSeries<T>`. They share the same interface but use fundamentally different storage strategies.

## Common Interface

```csharp
public interface IReadOnlyTimeSeries<T> : IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    Period Period { get; }
    int Count { get; }
    DateTimeOffset MinDate { get; }
    DateTimeOffset MaxDate { get; }
    bool TryGetValue(DateTimeOffset timestamp, out T value);
    T this[DateTimeOffset timestamp] { get; }
}

public interface ITimeSeries<T> : IReadOnlyTimeSeries<T>
    where T : struct, INumber<T>
{
    new T this[DateTimeOffset timestamp] { get; set; }
    void Set(DateTimeOffset timestamp, T value);
    bool Remove(DateTimeOffset timestamp);
    void Clear();
}
```

The `TimeSeriesPoint<T>` is a simple record struct:

```csharp
public readonly record struct TimeSeriesPoint<T>(DateTimeOffset Timestamp, T Value);
```

## RegularTimeSeries\<T\>

A dense, slot-indexed storage engine for **fixed-length periods** (5 minutes through weekly).

### How It Works

`RegularTimeSeries` converts each `DateTimeOffset` to an absolute **slot index** via integer division on UTC ticks. Values are stored in a flat array at the slot's offset from the series start. A separate bitfield tracks which slots actually contain values, allowing O(1) reads and writes while supporting sparse data within the grid.

### Characteristics

| Property | Value |
|---|---|
| **Read/Write** | O(1) — direct array index |
| **Insertion** | O(1) amortized — may grow the backing array |
| **Memory** | Proportional to the span of time covered (min to max slot), not the number of data points |
| **Supported Periods** | `FiveMinutes`, `QuarterHour`, `HalfHour`, `Hour`, `HalfDay`, `Day`, `Week` |

### When to Use

- Your data has a **fixed, regular period** (e.g., 5-minute meter readings)
- You need the **fastest possible access** by timestamp
- The time range is bounded (the backing array spans from the earliest to latest entry)

### Example

```csharp
var series = new RegularTimeSeries<double>(Period.Hour);

var t1 = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
var t2 = t1.AddHours(1);
var t3 = t1.AddHours(2);

series[t1] = 100.0;
series[t2] = 200.0;
series[t3] = 300.0;

// O(1) access
Console.WriteLine(series[t2]); // 200

// Safe access — returns false for missing slots
series.TryGetValue(t1.AddHours(5), out var value); // false
```

### Capacity Hint

You can provide an initial capacity to reduce array resizing:

```csharp
// Pre-allocate for 288 five-minute slots (one full day)
var series = new RegularTimeSeries<double>(Period.FiveMinutes, capacity: 288);
```

### Unsupported Periods

`RegularTimeSeries` requires a fixed-length period. If you pass `Period.Month`, `Period.QuarterYear`, `Period.HalfYear`, or `Period.Year`, the constructor throws `NotSupportedException` because these periods have variable lengths. Use `SparseTimeSeries` for calendar-based periods.

## SparseTimeSeries\<T\>

A sorted array-backed storage engine that supports **any period**, including calendar-based periods like monthly and yearly.

### How It Works

`SparseTimeSeries` stores timestamps as sorted `long` tick keys with a parallel value array. Lookups use binary search (O(log n)), and insertions maintain sorted order via array copying. For non-`NonStandard` periods, it validates that every inserted timestamp is aligned to the period relative to the first entry.

### Characteristics

| Property | Value |
|---|---|
| **Read** | O(log n) — binary search |
| **Insertion** | O(n) worst case — array shift for in-order insert; O(log n) amortized for append-only |
| **Memory** | Proportional to the number of data points (no wasted slots) |
| **Supported Periods** | All `Period` values including `Month`, `QuarterYear`, `HalfYear`, `Year`, `NonStandard` |

### When to Use

- Your data has a **calendar-based period** (monthly, quarterly, yearly)
- Your data is **irregularly spaced** or uses `Period.NonStandard`
- Memory efficiency matters more than raw access speed
- The time range is very large with few actual data points

### Example

```csharp
var series = new SparseTimeSeries<decimal>(Period.Month);

var jan = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
var feb = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
var mar = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);

series[jan] = 1500.50m;
series[feb] = 1620.75m;
series[mar] = 1480.25m;

Console.WriteLine(series.Count); // 3
```

### Period Validation

When using a defined period (not `NonStandard`), `SparseTimeSeries` validates that each inserted timestamp is consistent with the first entry's alignment. For example, with `Period.FiveMinutes`, if your first entry is at minute :06, all subsequent entries must also be at :06, :11, :16, etc.

```csharp
var series = new SparseTimeSeries<int>(Period.FiveMinutes);
var t1 = new DateTimeOffset(2024, 1, 1, 0, 6, 7, 8, TimeSpan.FromHours(1));
series[t1] = 1;

// This works — same sub-minute alignment, 5 minutes later
series[t1.AddMinutes(5)] = 2;

// This throws ArgumentException — minute alignment doesn't match
series[new DateTimeOffset(2024, 1, 1, 0, 8, 0, 0, TimeSpan.FromHours(1))] = 3;
```

### NonStandard Period

Use `Period.NonStandard` when timestamps don't follow any regular cadence. No validation is performed:

```csharp
var series = new SparseTimeSeries<double>(Period.NonStandard);
series[DateTimeOffset.Parse("2024-01-01T00:00:00Z")] = 1.0;
series[DateTimeOffset.Parse("2024-01-01T00:03:17Z")] = 2.0; // any timestamp is valid
```

## Choosing Between Regular and Sparse

| Criterion | RegularTimeSeries | SparseTimeSeries |
|---|---|---|
| Period type | Fixed only (5 min – week) | Any period |
| Access speed | O(1) | O(log n) |
| Insert speed | O(1) amortized | O(n) worst case |
| Memory model | Slot grid (may have gaps) | Packed entries only |
| Best for | Dense, fixed-interval data | Sparse or calendar data |
| Aggregation target | Can be output of any aggregation | Can be output of sparse aggregation |
| SIMD math | Yes (dense fast-paths) | Scalar multiplication fast-path |
