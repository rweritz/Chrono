using Chrono.Testing;
using Chrono.TimeSeries;
using FluentAssertions;

namespace Chrono.Testing.Test;

public sealed class TimeSeriesFluentAssertionsTest
{
    [Fact]
    public void SparseTimeSeriesSupportsChainedMetadataAssertions()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(1.0, 1.0)
            .Build();

        series.Should()
            .HaveCount(3)
            .And.HavePeriod(Period.Hour)
            .And.HaveMinDate(start)
            .And.HaveMaxDate(start.AddHours(2));

        var act = () => series.Should().HaveCount(4);

        act.Should().Throw<Exception>().WithMessage("*count mismatch*Expected 4*Actual 3*");
    }

    [Fact]
    public void ContainValueAtSupportsToleranceAndReportsMissingOrIncorrectValues()
    {
        var start = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(10.0, 0.25)
            .Build();

        series.Should().ContainValueAt(start.AddHours(1), 10.28, tolerance: 0.05);

        var missing = () => series.Should().ContainValueAt(start.AddHours(3), 10.75, tolerance: 0.05);
        var incorrect = () => series.Should().ContainValueAt(start.AddHours(1), 10.5, tolerance: 0.05);

        missing.Should().Throw<Exception>().WithMessage("*missing value*");
        incorrect.Should().Throw<Exception>().WithMessage("*value mismatch*Expected 10.5*Actual 10.25*Tolerance 0.05*");
    }

    [Fact]
    public void BeStructurallyEquivalentToComparesMetadataTimestampsAndValuesWithTolerance()
    {
        var start = new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero);
        var expected = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(5.0, 1.0)
            .Build();
        var equivalent = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(5.02, 1.0)
            .Build();
        var different = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(5.2, 1.0)
            .Build();

        equivalent.Should().BeStructurallyEquivalentTo(expected, tolerance: 0.05);
        var act = () => different.Should().BeStructurallyEquivalentTo(expected, tolerance: 0.05);

        act.Should().Throw<Exception>().WithMessage("*value mismatch*Expected 5*Actual 5.2*Tolerance 0.05*");
    }

    [Fact]
    public void PrdCompatibleAliasesCompareSeriesAndCheckLowerBound()
    {
        var start = new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero);
        var expected = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(5.0, 0.5)
            .Build();
        var equivalent = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(5.02, 0.5)
            .Build();
        var lowerBoundViolation = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(5.0, 0.5)
            .Build();

        equivalent.Should()
            .BeEquivalentTo(expected, tolerance: 0.05)
            .And.HaveAllValuesGreaterThan(4.99);
        var equality = () => equivalent.Should().BeEquivalentTo(expected, tolerance: 0.005);
        var lowerBound = () => lowerBoundViolation.Should().HaveAllValuesGreaterThan(5.0);

        equality.Should().Throw<Exception>().WithMessage("*value mismatch*Expected 5*Actual 5.02*Tolerance 0.005*");
        lowerBound.Should().Throw<Exception>().WithMessage("*not greater than*Threshold 5*");
    }

    [Fact]
    public void HaveNoGapsReportsMissingExplicitPeriodSlots()
    {
        var start = new DateTimeOffset(2026, 6, 4, 0, 0, 0, TimeSpan.Zero);
        var contiguous = ChronoTimeSeriesGenerator
            .For<int>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(1, 1)
            .Build();
        var gapped = new SortedArrayTimeSeries<int>(Period.Hour)
        {
            [start] = 1,
            [start.AddHours(2)] = 3
        };

        contiguous.Should().HaveNoGaps();
        var act = () => gapped.Should().HaveNoGaps();

        act.Should().Throw<Exception>().WithMessage($"*gap*{start.AddHours(1):O}*");
    }

    [Fact]
    public void AggregateAndValueBoundAssertionsReuseStaticSemantics()
    {
        var start = new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(1.0, 0.5)
            .Build();

        series.Should()
            .HaveSumCloseTo(4.48, tolerance: 0.05)
            .And.OnlyContainValuesInRange(1.0, 2.0);

        var sum = () => series.Should().HaveSumCloseTo(4.0, tolerance: 0.05);
        var range = () => series.Should().OnlyContainValuesInRange(1.0, 1.75);

        sum.Should().Throw<Exception>().WithMessage("*sum mismatch*Expected 4*Actual 4.5*Tolerance 0.05*");
        range.Should().Throw<Exception>().WithMessage("*outside expected range*Range [1, 1.75]*");
    }
}
