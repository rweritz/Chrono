using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class DynamicSlotTimeSeriesAggregationTest
{
    [Fact]
    public void Aggregate_FiveMinutesToHour_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 0, 0, 0, TimeSpan.Zero);
        var series = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);

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
    public void Aggregate_MonthToYear_ShouldWork()
    {
        var jan = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var series = new DynamicSlotTimeSeries<int>(Period.Month);

        for (var i = 0; i < 12; i++)
            series[jan.AddMonths(i)] = i + 1;

        var year = TimeSeriesAggregation.Sum(series, Period.Year);

        year.Count.Should().Be(1);
        year[new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)].Should().Be(78);
    }

    [Fact]
    public void Aggregate_HourToDay_Max_ShouldWork()
    {
        var start = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var series = new DynamicSlotTimeSeries<decimal>(Period.Hour);
        for (var i = 0; i < 24; i++)
            series[start.AddHours(i)] = i + 0.5m;

        var max = TimeSeriesAggregation.Max(series, Period.Day);

        max.Count.Should().Be(1);
        max[start].Should().Be(23.5m);
    }
}
