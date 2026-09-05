# Chrono.Testing

Chrono.Testing adds deterministic test data generators and assertions for Chrono time-series tests. Use it when consumer tests need repeatable sparse data, intentional gaps, waveform/composite data, static assertions, or FluentAssertions-style `series.Should()` checks.

## Install

```bash
dotnet add package Chrono.Testing
```

Chrono.Testing references Chrono.TimeSeries and FluentAssertions, so test projects only need to reference this package for the helper APIs shown below.

## Deterministic generators

Use `TimeSeriesGenerator` as the primary entry point for deterministic Chrono sparse data. It starts explicit generator patterns such as constants, random walks, trends, step functions, seasonal waves, sawtooth waves, and impulses.

```csharp
using Chrono.TimeSeries;
using Chrono.Testing;

var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

var series = TimeSeriesGenerator
    .RandomWalk<double>(Period.Hour)
    .WithStart(start)
    .WithCount(4)
    .WithSeed(123)
    .WithInitialValue(10.0)
    .WithVolatility(0.25)
    .AsFixedSlot()
    .Build();
```

Use `.AsSortedArray()`, `.AsFixedSlot()`, or `.AsDynamicSlot()` before `.Build()` to choose the materialized sparse series type. `ChronoTimeSeriesGenerator.For<T>()` remains available as a compact convenience builder for one-off fixtures that set period, start, count, seed, shape, and flow in one chain.

## Sparse gaps

Sparse gaps remove explicit points from the generated sparse series. The same seed and probability produce the same missing timestamps.

```csharp
var gapped = TimeSeriesGenerator
    .Sparse(TimeSeriesGenerator
        .LinearTrend<double>(Period.Hour)
        .WithStart(start)
        .WithCount(8)
        .WithInitialValue(10.0)
        .WithStep(0.5))
    .WithSeed(42)
    .WithGapProbability(0.25)
    .Build();
```

## Composite and waveform generators

Reusable `TimeSeriesGenerator` builders can be composed. This example combines a linear trend with a sawtooth waveform, then removes deterministic gaps.

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

`TimeSeriesAssert` is framework-agnostic. It throws `TimeSeriesAssertionException` when a check fails, so it works from xUnit, NUnit, MSTest, or custom test harnesses.

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

Other static checks include `AllValuesInRange`, `HasDateRange`, and `SumCloseTo`.

## FluentAssertions style

Import `Chrono.Testing` to make Chrono's time-series-specific `Should()` extensions available.

```csharp
actual.Should()
    .HavePeriod(Period.Hour)
    .And.HaveCount(3)
    .And.ContainValueAt(start.AddHours(2), expectedValue: 8.0, tolerance: 0.05)
    .And.BeEquivalentTo(expected, tolerance: 0.05)
    .And.HaveAllValuesGreaterThan(4.0)
    .And.HaveNoGaps();
```

Fluent checks also include `HaveMinDate`, `HaveMaxDate`, `HaveSumCloseTo`, `BeStructurallyEquivalentTo`, and `OnlyContainValuesInRange`.
