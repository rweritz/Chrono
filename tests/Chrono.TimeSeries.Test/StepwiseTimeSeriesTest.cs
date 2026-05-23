using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class StepwiseTimeSeriesTest
{
    [Fact]
    public void BoundedStepwiseContract_ShouldProvideDenseLogicalReadsWithinLogicalRange()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);

        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 7);

        series.TryGetValue(start, out var firstValue).Should().BeTrue();
        firstValue.Should().Be(7);
        series[middle].Should().Be(7);
        series.TryGetValue(end, out var lastValue).Should().BeTrue();
        lastValue.Should().Be(7);
        series.TryGetValue(start.AddMinutes(-5), out _).Should().BeFalse();
        series.TryGetValue(end.AddMinutes(5), out _).Should().BeFalse();
    }

    [Fact]
    public void BoundedStepwiseContract_ShouldDistinguishChangePointsFromLogicalRangeSlots()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);

        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 7);

        series.LogicalSlotCount.Should().Be(3);
        series.ChangePointCount.Should().Be(2);
        series[middle].Should().Be(7);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 7),
            new TimeSeriesPoint<int>(end, 7));
    }

    [Fact]
    public void StepwiseTimeSeries_ShouldKeepChangePointsCanonicallyCompressed()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = start.AddMinutes(5);
        var third = start.AddMinutes(10);
        var end = start.AddMinutes(15);

        var series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);
        series[second] = 2;
        series[third] = 2;

        series.ChangePointCount.Should().Be(3);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(second, 2),
            new TimeSeriesPoint<int>(end, 1));
    }

    [Fact]
    public void BoundedStepwiseContract_ShouldUseAbsentValueBehaviorOutsideLogicalRange()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(10);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 7);

        Action readBeforeRange = () => _ = series[start.AddMinutes(-5)];
        Action readAfterRange = () => _ = series[end.AddMinutes(5)];

        readBeforeRange.Should().Throw<KeyNotFoundException>();
        readAfterRange.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Clear_ShouldRestoreOriginalInitialValueAcrossTheLogicalRange()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 7);

        series[middle] = 11;

        series.Clear();

        series[start].Should().Be(7);
        series[middle].Should().Be(7);
        series[end].Should().Be(7);
        series.ChangePointCount.Should().Be(2);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 7),
            new TimeSeriesPoint<int>(end, 7));
    }

    [Fact]
    public void SegmentWrite_ShouldUseInclusiveStartAndExclusiveEndSemantics()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = start.AddMinutes(5);
        var third = start.AddMinutes(10);
        var end = start.AddMinutes(15);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        series.SetSegment(second, end, 2);

        series[start].Should().Be(1);
        series[second].Should().Be(2);
        series[third].Should().Be(2);
        series[end].Should().Be(1);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(second, 2),
            new TimeSeriesPoint<int>(end, 1));
    }

    [Fact]
    public void PointWrite_ShouldAffectExactlyOneAlignedSlot()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = start.AddMinutes(5);
        var third = start.AddMinutes(10);
        var end = start.AddMinutes(15);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        series[second] = 9;

        series[start].Should().Be(1);
        series[second].Should().Be(9);
        series[third].Should().Be(1);
        series[end].Should().Be(1);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(second, 9),
            new TimeSeriesPoint<int>(third, 1),
            new TimeSeriesPoint<int>(end, 1));
    }

    [Fact]
    public void ContiguousPointWrite_ShouldExpandTheLogicalRange()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        var contiguousNext = end.AddMinutes(5);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        series[contiguousNext] = 3;

        series.LogicalRangeStart.Should().Be(start);
        series.LogicalRangeEnd.Should().Be(contiguousNext);
        series[start].Should().Be(1);
        series[middle].Should().Be(1);
        series[end].Should().Be(1);
        series[contiguousNext].Should().Be(3);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(contiguousNext, 3));
    }

    [Fact]
    public void LeftContiguousSegmentExpansion_ShouldPreserveTrailingValuesPastTheWrittenSegment()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = start.AddMinutes(5);
        var third = start.AddMinutes(10);
        var end = start.AddMinutes(15);
        var contiguousPrevious = start.AddMinutes(-5);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        series[third] = 9;
        series.SetSegment(contiguousPrevious, third, 2);

        series.LogicalRangeStart.Should().Be(contiguousPrevious);
        series.LogicalRangeEnd.Should().Be(end);
        series[contiguousPrevious].Should().Be(2);
        series[start].Should().Be(2);
        series[second].Should().Be(2);
        series[third].Should().Be(9);
        series[end].Should().Be(1);
        series.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(contiguousPrevious, 2),
            new TimeSeriesPoint<int>(third, 9),
            new TimeSeriesPoint<int>(end, 1));
    }

    [Fact]
    public void GapCreatingWrite_ShouldBeRejected()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        var gappedStart = end.AddMinutes(10);
        var gappedEnd = end.AddMinutes(15);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        Action write = () => series.SetSegment(gappedStart, gappedEnd, 4);

        write.Should().Throw<ArgumentOutOfRangeException>();
        series.LogicalRangeStart.Should().Be(start);
        series.LogicalRangeEnd.Should().Be(end);
        series[start].Should().Be(1);
        series[middle].Should().Be(1);
        series[end].Should().Be(1);
    }
}
