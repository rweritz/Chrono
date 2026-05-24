using Chrono.Testing;
using Chrono.TimeSeries;

namespace Chrono.Testing.Test;

public sealed class TimeSeriesAssertTest
{
    [Fact]
    public void EqualAllowsValuesWithinToleranceAndReportsMismatchedValues()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expected = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(1.0, 1.0)
            .Build();
        var withinTolerance = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(1.01, 1.0)
            .Build();
        var outsideTolerance = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(1.2, 1.0)
            .Build();

        TimeSeriesAssert.Equal(expected, withinTolerance, tolerance: 0.05);
        var ex = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.Equal(expected, outsideTolerance, tolerance: 0.05));

        Assert.Contains("value mismatch", ex.Message);
        Assert.Contains(start.ToString("O"), ex.Message);
        Assert.Contains("Expected 1", ex.Message);
        Assert.Contains("Actual 1.2", ex.Message);
        Assert.Contains("Tolerance 0.05", ex.Message);
    }

    [Fact]
    public void AllValuesInRangeAllowsInclusiveBoundsAndReportsOutOfRangeValues()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var inRange = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(10.0, 2.5)
            .Build();
        var outOfRange = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(10.0, 5.0)
            .Build();

        TimeSeriesAssert.AllValuesInRange(inRange, min: 10.0, max: 15.0);
        var ex = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.AllValuesInRange(outOfRange, min: 10.0, max: 15.0));

        Assert.Contains("outside expected range", ex.Message);
        Assert.Contains(start.AddHours(2).ToString("O"), ex.Message);
        Assert.Contains("Actual 20", ex.Message);
        Assert.Contains("Range [10, 15]", ex.Message);
    }

    [Fact]
    public void MetadataAssertionsReportIncorrectCountPeriodAndDateRange()
    {
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<int>()
            .WithPeriod(Period.Day)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(5, 1)
            .Build();

        TimeSeriesAssert.HasCount(series, 3);
        TimeSeriesAssert.HasPeriod(series, Period.Day);
        TimeSeriesAssert.HasDateRange(series, start, start.AddDays(2));

        var countEx = Assert.Throws<TimeSeriesAssertionException>(() => TimeSeriesAssert.HasCount(series, 4));
        Assert.Contains("count mismatch", countEx.Message);
        Assert.Contains("Expected 4", countEx.Message);
        Assert.Contains("Actual 3", countEx.Message);

        var periodEx = Assert.Throws<TimeSeriesAssertionException>(() => TimeSeriesAssert.HasPeriod(series, Period.Hour));
        Assert.Contains("period mismatch", periodEx.Message);
        Assert.Contains("Expected Hour", periodEx.Message);
        Assert.Contains("Actual Day", periodEx.Message);

        var rangeEx = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.HasDateRange(series, start, start.AddDays(3)));
        Assert.Contains("date range mismatch", rangeEx.Message);
        Assert.Contains(start.ToString("O"), rangeEx.Message);
        Assert.Contains(start.AddDays(3).ToString("O"), rangeEx.Message);
        Assert.Contains(start.AddDays(2).ToString("O"), rangeEx.Message);
    }

    [Fact]
    public void SumCloseToAllowsAggregateWithinToleranceAndReportsIncorrectSum()
    {
        var start = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(3)
            .LinearTrend(1.0, 0.5)
            .Build();

        TimeSeriesAssert.SumCloseTo(series, expectedSum: 4.5, tolerance: 0.01);
        var ex = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.SumCloseTo(series, expectedSum: 4.0, tolerance: 0.01));

        Assert.Contains("sum mismatch", ex.Message);
        Assert.Contains("Expected 4", ex.Message);
        Assert.Contains("Actual 4.5", ex.Message);
        Assert.Contains("Tolerance 0.01", ex.Message);
    }

    [Fact]
    public void ValueAtCloseToReportsMissingTimestampAndIncorrectValue()
    {
        var start = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(2)
            .LinearTrend(3.0, 1.0)
            .Build();

        TimeSeriesAssert.ValueAtCloseTo(series, start.AddHours(1), expectedValue: 4.02, tolerance: 0.05);

        var missingEx = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.ValueAtCloseTo(series, start.AddHours(3), expectedValue: 6.0, tolerance: 0.05));
        Assert.Contains("missing value", missingEx.Message);
        Assert.Contains(start.AddHours(3).ToString("O"), missingEx.Message);

        var valueEx = Assert.Throws<TimeSeriesAssertionException>(
            () => TimeSeriesAssert.ValueAtCloseTo(series, start.AddHours(1), expectedValue: 4.2, tolerance: 0.05));
        Assert.Contains("value mismatch", valueEx.Message);
        Assert.Contains(start.AddHours(1).ToString("O"), valueEx.Message);
        Assert.Contains("Expected 4.2", valueEx.Message);
        Assert.Contains("Actual 4", valueEx.Message);
        Assert.Contains("Tolerance 0.05", valueEx.Message);
    }
}
