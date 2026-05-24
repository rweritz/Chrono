using Chrono.TimeSeries;
using Chrono.Testing;

namespace Chrono.Testing.Test;

public sealed class ChronoTestingDocumentationExamplesTest
{
    [Fact]
    public void ReadmeGeneratorFlowBuildsDeterministicSparseCompositeData()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

        var series = TimeSeriesGenerator
            .Sparse(TimeSeriesGenerator.Composite(baseline, waveform, (left, right) => left + right))
            .WithSeed(42)
            .WithGapProbability(0.25)
            .Build();

        TimeSeriesAssert.HasPeriod(series, Period.Hour);
        TimeSeriesAssert.AllValuesInRange(series, min: 10.0, max: 14.0);
        Assert.True(series.ExplicitPointCount is > 0 and < 5);
        Assert.False(series.TryGetValue(start.AddHours(2), out _));
    }

    [Fact]
    public void ReadmeAssertionFlowSupportsStaticAndFluentAssertions()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

        actual.Should()
            .HavePeriod(Period.Hour)
            .And.HaveCount(3)
            .And.ContainValueAt(start.AddHours(2), expectedValue: 8.0, tolerance: 0.05)
            .And.BeStructurallyEquivalentTo(expected, tolerance: 0.05)
            .And.HaveNoGaps();
    }
}
