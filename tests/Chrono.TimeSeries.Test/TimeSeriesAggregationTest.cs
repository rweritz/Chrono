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

        sum.Count.Should().Be(1);
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

        max.Count.Should().Be(1);
        max[start].Should().Be(11.5m);
    }
}
