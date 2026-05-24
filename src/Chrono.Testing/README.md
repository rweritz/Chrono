# Chrono.Testing

Chrono.Testing adds deterministic test data generators and assertions for Chrono time-series tests. Use it when consumer tests need repeatable sparse data, intentional gaps, waveform/composite data, static assertions, or FluentAssertions-style `series.Should()` checks.

## Install

```bash
dotnet add package Chrono.Testing
```

Chrono.Testing references Chrono.TimeSeries and FluentAssertions, so test projects only need to reference this package for the helper APIs shown below.

## Deterministic generators

Use `ChronoTimeSeriesGenerator` for a compact builder that can produce multiple Chrono sparse storage shapes.

```csharp
using Chrono.TimeSeries;
using Chrono.Testing;

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

Supported value flows include seeded random values, constants, random walks, linear trends, step functions, seasonal waves, sawtooth waves, impulses, and sparse gaps. Use `.AsSortedArray()`, `.AsFixedSlot()`, or `.AsDynamicSlot()` before `.Build()` to choose the materialized sparse series type.

## Sparse gaps

Sparse gaps remove explicit points from the generated sparse series. The same seed and probability produce the same missing timestamps.

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

## Composite and waveform generators

Use `TimeSeriesGenerator` when composing reusable generator objects. This example combines a linear trend with a sawtooth waveform, then removes deterministic gaps.
The facade starts constant, random walk, linear trend, step function, seasonal, sawtooth, and impulse generators, each with storage shape selectors.

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
    .And.BeStructurallyEquivalentTo(expected, tolerance: 0.05)
    .And.HaveNoGaps();
```

Fluent checks also include `HaveMinDate`, `HaveMaxDate`, `HaveSumCloseTo`, and `OnlyContainValuesInRange`.
