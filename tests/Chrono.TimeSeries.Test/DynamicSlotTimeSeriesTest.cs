using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class DynamicSlotTimeSeriesTest
{
    [Theory]
    [InlineData(Period.FiveMinutes)]
    [InlineData(Period.QuaterHour)]
    [InlineData(Period.HalfHour)]
    [InlineData(Period.Hour)]
    [InlineData(Period.HalfDay)]
    [InlineData(Period.Day)]
    [InlineData(Period.Week)]
    [InlineData(Period.Month)]
    [InlineData(Period.QuaterYear)]
    [InlineData(Period.HalfYear)]
    [InlineData(Period.Year)]
    public void StrictMode_AlignedTimestamps_ShouldRoundTrip(Period period)
    {
        var t1 = Aligned(period);
        var t2 = Next(period, t1);
        var series = new DynamicSlotTimeSeries<int>(period);

        series[t1] = 10;
        series[t2] = 20;

        series[t1].Should().Be(10);
        series[t2].Should().Be(20);
        series.Count.Should().Be(2);
        series.MinDate.Should().Be(t1);
        series.MaxDate.Should().Be(t2);
    }

    [Theory]
    [InlineData(Period.FiveMinutes)]
    [InlineData(Period.QuaterHour)]
    [InlineData(Period.HalfHour)]
    [InlineData(Period.Hour)]
    [InlineData(Period.HalfDay)]
    [InlineData(Period.Day)]
    [InlineData(Period.Week)]
    [InlineData(Period.Month)]
    [InlineData(Period.QuaterYear)]
    [InlineData(Period.HalfYear)]
    [InlineData(Period.Year)]
    public void StrictMode_MisalignedTimestamps_ShouldThrow(Period period)
    {
        var series = new DynamicSlotTimeSeries<int>(period);
        var misaligned = Misaligned(period);

        var act = () => series[misaligned] = 1;

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(Period.FiveMinutes)]
    [InlineData(Period.Hour)]
    [InlineData(Period.Week)]
    [InlineData(Period.Month)]
    [InlineData(Period.QuaterYear)]
    [InlineData(Period.HalfYear)]
    [InlineData(Period.Year)]
    public void TruncateMode_ShouldFloorOnSetGetAndRemove(Period period)
    {
        var aligned = Aligned(period);
        var misaligned = Misaligned(period);
        var series = new DynamicSlotTimeSeries<int>(period, AlignMode.Truncate);

        series[misaligned] = 7;

        series[aligned].Should().Be(7);
        series[misaligned].Should().Be(7);
        series.Remove(misaligned).Should().BeTrue();
        series.Count.Should().Be(0);
    }

    [Fact]
    public void BasicLifecycle_ShouldSupportClearAndEnumeration()
    {
        var period = Period.Month;
        var t1 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var series = new DynamicSlotTimeSeries<decimal>(period);
        series[t1] = 1.5m;
        series[t2] = 2.5m;

        var points = series.ToArray();
        points.Length.Should().Be(2);
        points[0].Timestamp.Should().Be(t1);
        points[1].Timestamp.Should().Be(t2);

        series.Clear();
        series.Count.Should().Be(0);
        var read = () => _ = series.MinDate;
        read.Should().Throw<InvalidOperationException>();
    }

    private static DateTimeOffset Aligned(Period period) =>
        period switch
        {
            Period.FiveMinutes => new DateTimeOffset(2024, 1, 1, 10, 5, 0, TimeSpan.Zero),
            Period.QuaterHour => new DateTimeOffset(2024, 1, 1, 10, 15, 0, TimeSpan.Zero),
            Period.HalfHour => new DateTimeOffset(2024, 1, 1, 10, 30, 0, TimeSpan.Zero),
            Period.Hour => new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero),
            Period.HalfDay => new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Period.Day => new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
            Period.Week => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Period.Month => new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
            Period.QuaterYear => new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero),
            Period.HalfYear => new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero),
            Period.Year => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
        };

    private static DateTimeOffset Misaligned(Period period) =>
        period switch
        {
            Period.FiveMinutes => new DateTimeOffset(2024, 1, 1, 10, 7, 1, TimeSpan.Zero),
            Period.QuaterHour => new DateTimeOffset(2024, 1, 1, 10, 16, 1, TimeSpan.Zero),
            Period.HalfHour => new DateTimeOffset(2024, 1, 1, 10, 31, 1, TimeSpan.Zero),
            Period.Hour => new DateTimeOffset(2024, 1, 1, 10, 1, 1, TimeSpan.Zero),
            Period.HalfDay => new DateTimeOffset(2024, 1, 1, 1, 1, 1, TimeSpan.Zero),
            Period.Day => new DateTimeOffset(2024, 1, 2, 1, 1, 1, TimeSpan.Zero),
            Period.Week => new DateTimeOffset(2024, 1, 2, 1, 1, 1, TimeSpan.Zero),
            Period.Month => new DateTimeOffset(2024, 2, 2, 1, 1, 1, TimeSpan.Zero),
            Period.QuaterYear => new DateTimeOffset(2024, 5, 2, 1, 1, 1, TimeSpan.Zero),
            Period.HalfYear => new DateTimeOffset(2024, 8, 2, 1, 1, 1, TimeSpan.Zero),
            Period.Year => new DateTimeOffset(2024, 2, 2, 1, 1, 1, TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
        };

    private static DateTimeOffset Next(Period period, DateTimeOffset timestamp) =>
        period switch
        {
            Period.FiveMinutes => timestamp.AddMinutes(5),
            Period.QuaterHour => timestamp.AddMinutes(15),
            Period.HalfHour => timestamp.AddMinutes(30),
            Period.Hour => timestamp.AddHours(1),
            Period.HalfDay => timestamp.AddHours(12),
            Period.Day => timestamp.AddDays(1),
            Period.Week => timestamp.AddDays(7),
            Period.Month => timestamp.AddMonths(1),
            Period.QuaterYear => timestamp.AddMonths(3),
            Period.HalfYear => timestamp.AddMonths(6),
            Period.Year => timestamp.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
        };
}
