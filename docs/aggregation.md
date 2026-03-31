# Aggregation

The `TimeSeriesAggregation` static class rolls up time series data from a finer period to a coarser period. For example, aggregating 5-minute readings into hourly sums, or daily data into monthly averages.

## Quick Example

```csharp
var series = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

// 12 five-minute data points (one hour of data)
for (int i = 0; i < 12; i++)
    series[start.AddMinutes(5 * i)] = i + 1;

// Aggregate to hourly
var hourlySum = TimeSeriesAggregation.Sum(series, Period.Hour);
var hourlyAvg = TimeSeriesAggregation.Average(series, Period.Hour);
var hourlyCnt = TimeSeriesAggregation.Count(series, Period.Hour);

Console.WriteLine(hourlySum[start]);  // 78  (1+2+...+12)
Console.WriteLine(hourlyAvg[start]);  // 6   (78/12, integer division)
Console.WriteLine(hourlyCnt[start]);  // 12
```

## Aggregation Functions

Five built-in aggregation kinds are available, each with a convenience method:

| Method | Description | Output Type |
|---|---|---|
| `TimeSeriesAggregation.Sum(source, targetPeriod)` | Sum of all values in each bucket | Same as input |
| `TimeSeriesAggregation.Average(source, targetPeriod)` | Arithmetic mean of values in each bucket | Same as input |
| `TimeSeriesAggregation.Min(source, targetPeriod)` | Minimum value in each bucket | Same as input |
| `TimeSeriesAggregation.Max(source, targetPeriod)` | Maximum value in each bucket | Same as input |
| `TimeSeriesAggregation.Count(source, targetPeriod)` | Number of data points in each bucket | `int` |

### Min and Max

`Min` and `Max` require the additional `IMinMaxValue<T>` constraint. All built-in numeric types (`int`, `double`, `decimal`, etc.) satisfy this:

```csharp
var series = new SortedArrayTimeSeries<decimal>(Period.FiveMinutes);
var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

for (int i = 0; i < 12; i++)
    series[start.AddMinutes(5 * i)] = i + 0.5m;

var max = TimeSeriesAggregation.Max(series, Period.Hour);
Console.WriteLine(max[start]); // 11.5
```

## Custom Aggregators

For custom roll-up logic, implement `IAggregator<TIn, TOut>` and call the generic `Aggregate` method:

```csharp
public interface IAggregator<TIn, TOut>
    where TIn : struct, INumber<TIn>
    where TOut : struct, INumber<TOut>
{
    void Reset();           // Called at the start of each bucket
    void Add(TIn value);    // Called for each value in the bucket
    TOut Complete(int count); // Called to produce the bucket result
}
```

### Example: Weighted Average Aggregator

```csharp
public struct WeightedSumAggregator : IAggregator<double, double>
{
    private double _sum;
    private int _position;

    public void Reset() { _sum = 0; _position = 0; }

    public void Add(double value)
    {
        _position++;
        _sum += value * _position;  // weight increases with position
    }

    public double Complete(int count) => _sum;
}

// Usage
var result = TimeSeriesAggregation.Aggregate<double, double, WeightedSumAggregator>(
    source, Period.Hour);
```

## Built-in Aggregator Structs

The library provides these aggregator structs (all are value types for zero-allocation usage):

| Struct | TIn → TOut | Behavior |
|---|---|---|
| `SumAggregator<T>` | T → T | Running sum |
| `AverageAggregator<T>` | T → T | Sum / count |
| `MinAggregator<T>` | T → T | Tracks minimum (requires `IMinMaxValue<T>`) |
| `MaxAggregator<T>` | T → T | Tracks maximum (requires `IMinMaxValue<T>`) |
| `CountAggregator<T>` | T → int | Counts entries |

## Aggregation Strategies

The library automatically selects the best strategy based on the source and target periods:

### Fixed-to-Fixed Aggregation

When both source and target are fixed-length periods and the target is an exact multiple of the source (e.g., 5 minutes → 1 hour = 12× multiple), aggregation uses direct slot arithmetic. No `DateTimeOffset` objects are created during the inner loop — this is the fastest path.

```csharp
// 5 min → 1 hour (fixed-to-fixed, factor = 12)
var hourly = TimeSeriesAggregation.Sum(fiveMinSeries, Period.Hour);

// 1 hour → 1 day (fixed-to-fixed, factor = 24)
var daily = TimeSeriesAggregation.Sum(hourlySeries, Period.Day);
```

### Fixed-to-Calendar Aggregation

When aggregating a fixed-length period into a calendar period (e.g., hourly → monthly), the library iterates over the source and floors each timestamp to its calendar bucket:

```csharp
// Hour → Month (requires calendar flooring)
var monthly = TimeSeriesAggregation.Sum(hourlySeries, Period.Month);
```

### Sparse Aggregation

`SortedArrayTimeSeries` aggregation always uses bucket flooring (either fixed or calendar). The output is a new `SortedArrayTimeSeries`:

```csharp
var sparse = new SortedArrayTimeSeries<double>(Period.FiveMinutes);
// ... populate ...

var hourly = TimeSeriesAggregation.Sum(sparse, Period.Hour);
// Returns SortedArrayTimeSeries<double> with Period.Hour
```

## FixedSlotTimeSeries vs SortedArrayTimeSeries Aggregation

Both series types have full aggregation support with the same method names:

```csharp
// FixedSlotTimeSeries input → FixedSlotTimeSeries output
FixedSlotTimeSeries<T> result = TimeSeriesAggregation.Sum(regularSeries, Period.Hour);

// SortedArrayTimeSeries input → SortedArrayTimeSeries output
SortedArrayTimeSeries<T> result = TimeSeriesAggregation.Sum(sparseSeries, Period.Hour);
```

The return type matches the input type, so you stay in the same storage model throughout your pipeline.

## Empty Series

Aggregating an empty series returns a new empty series of the target period:

```csharp
var empty = new FixedSlotTimeSeries<double>(Period.FiveMinutes);
var result = TimeSeriesAggregation.Sum(empty, Period.Hour);
Console.WriteLine(result.Count); // 0
```

## Handling Gaps

Aggregation only produces output buckets that contain at least one source data point. If a bucket has no source values (e.g., an hour with no 5-minute readings), it is simply absent from the result:

```csharp
var series = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
series[hour0.AddMinutes(0)] = 1;
series[hour0.AddMinutes(5)] = 2;
// hour1 has no data
series[hour2.AddMinutes(0)] = 10;

var hourly = TimeSeriesAggregation.Sum(series, Period.Hour);
// hourly has 2 entries: hour0=3, hour2=10
// hour1 is not present
```
