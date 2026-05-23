# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) or later
- C# 12+ (required for `INumber<T>` generic math)

## Installation

Add the project reference to your `.csproj`:

```xml
<ProjectReference Include="path/to/Chrono.TimeSeries/Chrono.TimeSeries.csproj" />
```

Then add the namespace:

```csharp
using Chrono.TimeSeries;
```

## Your First Time Series

### Creating a series and adding data

```csharp
// A sparse 5-minute interval series of doubles
var series = new FixedSlotTimeSeries<double>(Period.FiveMinutes);

var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
series[start] = 10.0;
series[start.AddMinutes(5)] = 20.0;
series[start.AddMinutes(10)] = 30.0;

Console.WriteLine(series.ExplicitPointCount); // 3
Console.WriteLine(series.MinDate);            // 2024-01-01T00:00:00+00:00
Console.WriteLine(series.MaxDate);            // 2024-01-01T00:10:00+00:00
```

### Reading values

```csharp
// Indexer access (throws KeyNotFoundException if not present)
double value = series[start];

// Safe access
if (series.TryGetValue(start, out double v))
    Console.WriteLine(v);
```

### Enumerating sparse points

Sparse time series expose explicit points through `GetPoints()`:

```csharp
foreach (var point in series.GetPoints())
    Console.WriteLine($"{point.Timestamp:O} => {point.Value}");
```

Bounded stepwise time series expose change-points separately from their logical range:

```csharp
var stepwise = new StepwiseTimeSeries<double>(
    Period.FiveMinutes,
    start,
    start.AddMinutes(10),
    10.0);

Console.WriteLine(stepwise.LogicalSlotCount); // 3 logical slots
Console.WriteLine(stepwise.ChangePointCount); // 2 stored change-points

foreach (var changePoint in stepwise.GetChangePoints())
    Console.WriteLine($"{changePoint.Timestamp:O} => {changePoint.Value}");
```

### Removing values

```csharp
bool removed = series.Remove(start);
series.Clear(); // Remove all entries
```

## Next Steps

- Learn about [Time Series Types](time-series-types.md) to choose the right storage for your use case
- See [Periods & Alignment](periods-and-alignment.md) for period options and timestamp rules
- Explore [Arithmetic Operations](arithmetic-operations.md) and [Aggregation](aggregation.md)
