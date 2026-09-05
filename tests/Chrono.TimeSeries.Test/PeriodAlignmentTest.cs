using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class PeriodAlignmentTest
{
    [Fact]
    public void FixedPeriods_ShouldUseTheSameCanonicalUtcGridAcrossFamilies()
    {
        var canonical = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var sameInstantWithOffset = new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.FromHours(2));
        var offGrid = canonical.AddMinutes(30);

        foreach (var series in CreateFixedSparseSeries(Period.Hour))
        {
            series[sameInstantWithOffset] = 7;

            series[canonical].Should().Be(7);
            series.GetPoints().Should().ContainSingle()
                .Which.Timestamp.Should().Be(canonical);
            FluentActions.Invoking(() => series[offGrid] = 9)
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => series.TryGetValue(offGrid, out _))
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => series.Remove(offGrid))
                .Should().Throw<ArgumentException>();
        }

        var stepwise = new StepwiseTimeSeries<int>(Period.Hour, canonical, canonical.AddHours(2), 1);
        stepwise[sameInstantWithOffset] = 7;
        stepwise[canonical].Should().Be(7);
        FluentActions.Invoking(() => stepwise[offGrid] = 9)
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalendarPeriods_ShouldUseUtcCalendarStartsAcrossFamilies()
    {
        var canonical = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var sameInstantWithOffset = new DateTimeOffset(2024, 4, 1, 2, 0, 0, TimeSpan.FromHours(2));
        var offGrid = canonical.AddDays(1);

        foreach (var series in CreateCalendarSparseSeries(Period.QuaterYear))
        {
            series[sameInstantWithOffset] = 7;

            series[canonical].Should().Be(7);
            series.GetPoints().Should().ContainSingle()
                .Which.Timestamp.Should().Be(canonical);
            FluentActions.Invoking(() => series[offGrid] = 9)
                .Should().Throw<ArgumentException>();
        }

        var stepwise = new StepwiseTimeSeries<int>(Period.QuaterYear, canonical, canonical.AddMonths(6), 1);
        stepwise[sameInstantWithOffset] = 7;
        stepwise[canonical].Should().Be(7);
        FluentActions.Invoking(() => stepwise[offGrid] = 9)
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WeeklyPeriods_ShouldUseMondayUtcAcrossFamilies()
    {
        var monday = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sameInstantWithOffset = new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.FromHours(1));
        var sunday = monday.AddDays(-1);

        foreach (var series in CreateFixedSparseSeries(Period.Week))
        {
            series[sameInstantWithOffset] = 7;

            series[monday].Should().Be(7);
            series.GetPoints().Should().ContainSingle()
                .Which.Timestamp.Should().Be(monday);
            FluentActions.Invoking(() => series[sunday] = 9)
                .Should().Throw<ArgumentException>();
        }

        var stepwise = new StepwiseTimeSeries<int>(Period.Week, monday, monday.AddDays(14), 1);
        stepwise[sameInstantWithOffset] = 7;
        stepwise[monday].Should().Be(7);
        FluentActions.Invoking(() => stepwise[sunday] = 9)
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WeeklyPeriods_ShouldRoundTripAndTruncateBeforeTheEpoch()
    {
        var mondayBeforeEpoch = new DateTimeOffset(1969, 12, 29, 0, 0, 0, TimeSpan.Zero);
        var sundayBeforeEpoch = new DateTimeOffset(1970, 1, 4, 23, 59, 59, TimeSpan.Zero);
        var strict = new FixedSlotTimeSeries<int>(Period.Week);
        var truncate = new DynamicSlotTimeSeries<int>(Period.Week, AlignMode.Truncate);

        strict[mondayBeforeEpoch] = 3;
        truncate[sundayBeforeEpoch] = 5;

        strict.GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(mondayBeforeEpoch);
        truncate.GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(mondayBeforeEpoch);
    }

    [Fact]
    public void CalendarPeriods_ShouldRoundTripAndTruncateBeforeTheEpoch()
    {
        var quarterBeforeEpoch = new DateTimeOffset(1969, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var timestampBeforeEpoch = new DateTimeOffset(1969, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var strict = new DynamicSlotTimeSeries<int>(Period.QuaterYear);
        var truncate = new DynamicSlotTimeSeries<int>(Period.QuaterYear, AlignMode.Truncate);

        strict[quarterBeforeEpoch] = 3;
        truncate[timestampBeforeEpoch] = 5;

        strict.GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(quarterBeforeEpoch);
        truncate.GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(quarterBeforeEpoch);
    }

    [Fact]
    public void StrictSparseFamilies_ShouldRejectOffGridSegmentBoundaries()
    {
        var canonical = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var offGrid = canonical.AddMinutes(30);

        foreach (var series in CreateFixedSparseSeries(Period.Hour))
        {
            FluentActions.Invoking(() => series.SetSegment(offGrid, canonical.AddHours(2), 7))
                .Should().Throw<ArgumentException>();
            series.GetPoints().Should().BeEmpty();
        }
    }

    [Fact]
    public void FixedToWeeklyAggregation_ShouldFormMondayUtcBucketsAcrossSparseFamilies()
    {
        var wednesday = new DateTimeOffset(2024, 1, 3, 12, 0, 0, TimeSpan.Zero);
        var monday = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var sorted = new SortedArrayTimeSeries<int>(Period.Hour);
        var fixedSlot = new FixedSlotTimeSeries<int>(Period.Hour);
        var dynamicSlot = new DynamicSlotTimeSeries<int>(Period.Hour);
        sorted[wednesday] = 1;
        fixedSlot[wednesday] = 1;
        dynamicSlot[wednesday] = 1;

        TimeSeriesAggregation.Sum(sorted, Period.Week).GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(monday);
        TimeSeriesAggregation.Sum(fixedSlot, Period.Week).GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(monday);
        TimeSeriesAggregation.Sum(dynamicSlot, Period.Week).GetPoints().Should().ContainSingle()
            .Which.Timestamp.Should().Be(monday);
    }

    [Fact]
    public void SortedArray_ShouldRejectLegacyReferenceRelativeAlignmentFromTheFirstWrite()
    {
        var legacyReference = new DateTimeOffset(2024, 1, 1, 0, 2, 3, TimeSpan.Zero);
        var series = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        FluentActions.Invoking(() => series[legacyReference] = 1)
            .Should().Throw<ArgumentException>();

        series.ExplicitPointCount.Should().Be(0);
    }

    [Fact]
    public void NonStandard_ShouldContinueToAcceptArbitraryTimestamps()
    {
        var first = new DateTimeOffset(2024, 1, 1, 0, 2, 3, 4, TimeSpan.FromHours(1));
        var second = new DateTimeOffset(2024, 2, 3, 7, 11, 13, 17, TimeSpan.FromHours(-3));
        var series = new SortedArrayTimeSeries<int>(Period.NonStandard);

        series[first] = 1;
        series[second] = 2;

        series[first].Should().Be(1);
        series[second].Should().Be(2);
        series.ExplicitPointCount.Should().Be(2);
    }

    [Fact]
    public void PeriodConverter_ShouldNoLongerBePubliclyExposed()
    {
        typeof(Period).Assembly.GetType("Chrono.TimeSeries.PeriodConverter")
            .Should().BeNull();
    }

    private static ISparseTimeSeries<int>[] CreateFixedSparseSeries(Period period) =>
    [
        new SortedArrayTimeSeries<int>(period),
        new FixedSlotTimeSeries<int>(period),
        new DynamicSlotTimeSeries<int>(period)
    ];

    private static ISparseTimeSeries<int>[] CreateCalendarSparseSeries(Period period) =>
    [
        new SortedArrayTimeSeries<int>(period),
        new DynamicSlotTimeSeries<int>(period)
    ];
}
