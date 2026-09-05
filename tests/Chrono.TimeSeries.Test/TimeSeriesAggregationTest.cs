using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesAggregationTest
{
    [Fact]
    public void AggregateRegular_FiveMinutesToHour_SumAverageCount_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 0, 0, 0, TimeSpan.Zero);
        var series = new FixedSlotTimeSeries<int>(Period.FiveMinutes);

        for (var i = 0; i < 12; i++)
            series[start.AddMinutes(5 * i)] = i + 1;

        var sum = TimeSeriesAggregation.Sum(series, Period.Hour);
        var avg = TimeSeriesAggregation.Average(series, Period.Hour);
        var cnt = TimeSeriesAggregation.Count(series, Period.Hour);

        sum.ExplicitPointCount.Should().Be(1);
        sum[start].Should().Be(78);
        avg[start].Should().Be(6);
        cnt[start].Should().Be(12);
    }

    [Fact]
    public void AggregateSortedArray_FiveMinutesToHour_Max_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 0, 0, 0, TimeSpan.Zero);
        var series = new SortedArrayTimeSeries<decimal>(Period.FiveMinutes);

        for (var i = 0; i < 12; i++)
            series[start.AddMinutes(5 * i)] = i + 0.5m;

        var max = TimeSeriesAggregation.Max(series, Period.Hour);

        max.ExplicitPointCount.Should().Be(1);
        max[start].Should().Be(11.5m);
    }

    [Fact]
    public void SparseFamilyAggregation_ShouldAggregateOnlyExplicitPointsThroughSparseContract()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(30);
        var series = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        IReadOnlySparseTimeSeries<int> sparse = series;

        series[start] = 1;
        series[middle] = 2;

        var sum = TimeSeriesAggregation.Sum(sparse, Period.Hour);
        var count = TimeSeriesAggregation.Count(sparse, Period.Hour);

        sum.ExplicitPointCount.Should().Be(1);
        sum[start].Should().Be(3);
        count[start].Should().Be(2);
    }

    [Fact]
    public void SlotBackedAggregation_ShouldPreserveEmptyBucketsAndConcreteFamilies()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var emptyBucket = start.AddHours(1);
        var end = start.AddHours(2);

        var fixedSlot = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        fixedSlot[start] = 2;
        fixedSlot[end] = 6;

        var dynamicSlot = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        dynamicSlot[start] = 3;
        dynamicSlot[end] = 9;

        var fixedResult = TimeSeriesAggregation.Sum(fixedSlot, Period.Hour);
        var dynamicResult = TimeSeriesAggregation.Sum(dynamicSlot, Period.Hour);

        fixedResult.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedResult.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 2),
            new TimeSeriesPoint<int>(end, 6));
        fixedResult.TryGetValue(emptyBucket, out _).Should().BeFalse();

        dynamicResult.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicResult.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 3),
            new TimeSeriesPoint<int>(end, 9));
        dynamicResult.TryGetValue(emptyBucket, out _).Should().BeFalse();
    }

    [Fact]
    public void BoundedStepwiseAggregation_ShouldAggregateDenseLogicalValuesWithinLogicalRange()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var midpoint = start.AddMinutes(30);
        var end = start.AddMinutes(55);
        IBoundedStepwiseTimeSeries<int> series = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 1);

        series.SetSegment(midpoint, end.AddMinutes(5), 2);

        var sum = TimeSeriesAggregation.Sum(series, Period.Hour);
        var count = TimeSeriesAggregation.Count(series, Period.Hour);

        sum.LogicalRangeStart.Should().Be(start);
        sum.LogicalRangeEnd.Should().Be(start);
        sum[start].Should().Be(18);
        count[start].Should().Be(12);
    }
}
