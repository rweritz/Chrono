using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesMathTest
{
    [Fact]
    public void Sparse_Add_Subtract_Multiply_Divide_ShouldWorkWithIntersection()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);
        var t2 = t1.AddMinutes(5);

        var a = new SparseTimeSeries<double>(Period.FiveMinutes);
        var b = new SparseTimeSeries<double>(Period.FiveMinutes);

        a[t0] = 10;
        a[t1] = 20;
        b[t1] = 4;
        b[t2] = 8;

        var add = TimeSeriesMath.Add(a, b, MissingValuePolicy.Intersection);
        var sub = TimeSeriesMath.Subtract(a, b, MissingValuePolicy.Intersection);
        var mul = TimeSeriesMath.Multiply(a, b, MissingValuePolicy.Intersection);
        var div = TimeSeriesMath.Divide(a, b, MissingValuePolicy.Intersection);

        add.Count.Should().Be(1);
        add[t1].Should().Be(24);
        sub[t1].Should().Be(16);
        mul[t1].Should().Be(80);
        div[t1].Should().Be(5);
    }

    [Fact]
    public void Sparse_UnionWithZero_ShouldIncludeAllKeys()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);

        var a = new SparseTimeSeries<int>(Period.FiveMinutes);
        var b = new SparseTimeSeries<int>(Period.FiveMinutes);

        a[t0] = 2;
        b[t1] = 3;

        var add = TimeSeriesMath.Add(a, b, MissingValuePolicy.UnionWithZero);

        add.Count.Should().Be(2);
        add[t0].Should().Be(2);
        add[t1].Should().Be(3);
    }

    [Fact]
    public void ScalarOperations_ShouldWork()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);

        var source = new SparseTimeSeries<decimal>(Period.FiveMinutes);
        source[t0] = 2m;
        source[t1] = 8m;

        var multiplied = TimeSeriesMath.Multiply(source, 2m);
        var added = TimeSeriesMath.Add(source, 3m);
        var divided = TimeSeriesMath.Divide(source, 2m);

        multiplied[t0].Should().Be(4m);
        multiplied[t1].Should().Be(16m);
        added[t0].Should().Be(5m);
        divided[t1].Should().Be(4m);
    }

    [Fact]
    public void RegularSeries_BinaryAndScalarOperations_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);

        var a = new RegularTimeSeries<double>(Period.FiveMinutes);
        var b = new RegularTimeSeries<double>(Period.FiveMinutes);

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
}
