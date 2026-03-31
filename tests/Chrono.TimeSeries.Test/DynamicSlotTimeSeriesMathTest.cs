using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class DynamicSlotTimeSeriesMathTest
{
    [Fact]
    public void BinaryAndScalarOperations_FixedPeriod_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var a = new DynamicSlotTimeSeries<double>(Period.FiveMinutes);
        var b = new DynamicSlotTimeSeries<double>(Period.FiveMinutes);

        a[start] = 1;
        a[start.AddMinutes(5)] = 2;
        b[start] = 10;
        b[start.AddMinutes(5)] = 20;

        var add = TimeSeriesMath.Add(a, b);
        var scaled = TimeSeriesMath.Multiply(a, 3d);

        add[start].Should().Be(11);
        add[start.AddMinutes(5)].Should().Be(22);
        scaled[start].Should().Be(3);
        scaled[start.AddMinutes(5)].Should().Be(6);
    }

    [Fact]
    public void BinaryOperations_CalendarPeriod_WithUnionWithZero_ShouldWork()
    {
        var jan = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var feb = jan.AddMonths(1);
        var mar = jan.AddMonths(2);

        var a = new DynamicSlotTimeSeries<int>(Period.Month);
        var b = new DynamicSlotTimeSeries<int>(Period.Month);
        a[jan] = 2;
        a[feb] = 4;
        b[feb] = 10;
        b[mar] = 20;

        var sum = TimeSeriesMath.Add(a, b, MissingValuePolicy.UnionWithZero);

        sum.Count.Should().Be(3);
        sum[jan].Should().Be(2);
        sum[feb].Should().Be(14);
        sum[mar].Should().Be(20);
    }
}
