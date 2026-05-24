# Testing Chrono Code

Chrono.Testing is the companion package for consumer tests. It provides deterministic generators for Chrono sparse time-series data plus two assertion styles: static `TimeSeriesAssert` calls and FluentAssertions-style `series.Should()` checks.

## Install

```bash
dotnet add package Chrono.Testing
```

Then import both namespaces in test files:

```csharp
using Chrono.TimeSeries;
using Chrono.Testing;
```

## Generate deterministic data

`ChronoTimeSeriesGenerator` is the shortest path for one-off test fixtures. Set the period, start, count, seed, storage shape, and value flow, then call `Build()`.

```csharp
var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

var series = ChronoTimeSeriesGenerator
    .For<double>()
    .WithPeriod(Period.Hour)
    .StartingAt(start)
    .WithCount(4)
    .WithSeed(123)
    .AsFixedSlot()
    .RandomWalk(initialValue: 10.0, volatility: 0.25)
    .Build();
```

Use `.AsSortedArray()`, `.AsFixedSlot()`, or `.AsDynamicSlot()` to choose the sparse implementation. Generator flows include constants, seeded random values, random walks, linear trends, step functions, seasonal waves, sawtooth waves, impulses, and sparse gaps.

## Reproduce sparse gaps

Sparse generation removes explicit points rather than writing default values. The same seed and probability produce the same gap pattern.

```csharp
var gapped = ChronoTimeSeriesGenerator
    .For<double>()
    .WithPeriod(Period.Hour)
    .StartingAt(start)
    .WithCount(8)
    .WithSeed(42)
    .LinearTrend(initialValue: 10.0, step: 0.5)
    .Sparse(gapProbability: 0.25)
    .Build();
```

## Compose waveforms

Use the `TimeSeriesGenerator` facade when composing reusable generators. This example adds a sawtooth waveform to a baseline trend, then applies deterministic sparse gaps.

```csharp
var baseline = TimeSeriesGenerator
    .LinearTrend<double>(Period.Hour)
    .WithStart(start)
    .WithCount(5)
    .WithInitialValue(10.0)
    .WithStep(0.5);

var waveform = TimeSeriesGenerator
    .Sawtooth<double>(Period.Hour)
    .WithStart(start)
    .WithCount(5)
    .WithBaseline(0.0)
    .WithAmplitude(2.0)
    .WithCycleLength(4);

var composite = TimeSeriesGenerator
    .Sparse(TimeSeriesGenerator.Composite(baseline, waveform, (left, right) => left + right))
    .WithSeed(42)
    .WithGapProbability(0.25)
    .Build();
```

## Static assertions

`TimeSeriesAssert` is framework-agnostic and throws `TimeSeriesAssertionException` on failure.

```csharp
var expected = ChronoTimeSeriesGenerator
    .For<double>()
    .WithPeriod(Period.Hour)
    .StartingAt(start)
    .WithCount(3)
    .LinearTrend(initialValue: 5.0, step: 1.5)
    .Build();

var actual = ChronoTimeSeriesGenerator
    .For<double>()
    .WithPeriod(Period.Hour)
    .StartingAt(start)
    .WithCount(3)
    .LinearTrend(initialValue: 5.01, step: 1.5)
    .Build();

TimeSeriesAssert.Equal(expected, actual, tolerance: 0.05);
TimeSeriesAssert.ValueAtCloseTo(actual, start.AddHours(1), expectedValue: 6.5, tolerance: 0.05);
TimeSeriesAssert.HasPeriod(actual, Period.Hour);
TimeSeriesAssert.HasCount(actual, 3);
TimeSeriesAssert.HasNoGaps(actual);
```

## Fluent assertions

Import `Chrono.Testing` to use Chrono-specific FluentAssertions extensions.

```csharp
actual.Should()
    .HavePeriod(Period.Hour)
    .And.HaveCount(3)
    .And.ContainValueAt(start.AddHours(2), expectedValue: 8.0, tolerance: 0.05)
    .And.BeStructurallyEquivalentTo(expected, tolerance: 0.05)
    .And.HaveNoGaps();
```

Other fluent checks include `HaveMinDate`, `HaveMaxDate`, `HaveSumCloseTo`, and `OnlyContainValuesInRange`.
