using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class GenericTimeSeriesTest
{
    [Fact]
    public void SparseContract_ShouldExposeExplicitPointCountAndExplicitPointEnumeration()
    {
        var first = new DateTimeOffset(2022, 2, 6, 5, 5, 0, TimeSpan.Zero);
        var missing = first.AddMinutes(5);
        var last = missing.AddMinutes(5);

        ISparseTimeSeries<int> series = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        series[first] = 5;
        series[last] = 10;

        series.ExplicitPointCount.Should().Be(2);
        series.TryGetValue(missing, out _).Should().BeFalse();
        series.GetPoints().Select(point => point.Timestamp).Should().Equal(first, last);
    }

    [Fact]
    public void SparseImplementations_ShouldSupportPointLifecycleThroughSparseContract()
    {
        var first = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var second = first.AddMinutes(5);

        foreach (var series in CreateSparseSeries())
        {
            series[first] = 5;
            series.Set(second, 10);

            series.MinDate.Should().Be(first);
            series.MaxDate.Should().Be(second);
            series.Remove(first).Should().BeTrue();
            series.TryGetValue(first, out _).Should().BeFalse();
            series[second].Should().Be(10);
            series.ExplicitPointCount.Should().Be(1);
        }
    }

    [Fact]
    public void SparseImplementations_ShouldMaterializeExplicitPointsForEachCoveredSlotViaSegmentWrite()
    {
        var first = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var second = first.AddMinutes(5);
        var third = first.AddMinutes(10);
        var end = first.AddMinutes(15);

        foreach (var series in CreateSparseSeries())
        {
            series.SetSegment(second, end, 10);

            series.TryGetValue(first, out _).Should().BeFalse();
            series[second].Should().Be(10);
            series[third].Should().Be(10);
            series.TryGetValue(end, out _).Should().BeFalse();
            series.GetPoints().Select(point => point.Timestamp).Should().Equal(second, third);
        }
    }

    [Fact]
    public void SparseSegmentWrite_ShouldPreserveMissingPointsOutsideWrittenSlots()
    {
        var first = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var second = first.AddMinutes(5);
        var third = first.AddMinutes(10);
        var end = first.AddMinutes(15);
        var after = first.AddMinutes(20);

        foreach (var series in CreateSparseSeries())
        {
            series[after] = 99;

            series.SetSegment(second, end, 10);

            series.TryGetValue(first, out _).Should().BeFalse();
            series.TryGetValue(end, out _).Should().BeFalse();
            series[after].Should().Be(99);
            series.GetPoints().Select(point => point.Timestamp).Should().Equal(second, third, after);
        }
    }

    [Fact]
    public void SortedArrayTimeSeries_ShouldSupportIntDoubleDecimal()
    {
        var t1 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t2 = t1.AddMinutes(5);

        var intSeries = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        intSeries[t1] = 5;
        intSeries[t2] = 10;

        var doubleSeries = new SortedArrayTimeSeries<double>(Period.FiveMinutes);
        doubleSeries[t1] = 3.0;

        var decimalSeries = new SortedArrayTimeSeries<decimal>(Period.FiveMinutes);
        decimalSeries[t1] = 1.75m;

        intSeries[t1].Should().Be(5);
        intSeries[t2].Should().Be(10);
        doubleSeries[t1].Should().Be(3.0);
        decimalSeries[t1].Should().Be(1.75m);
    }

    [Fact]
    public void FixedSlotTimeSeries_ShouldSupportO1GridAccess()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var t1 = start;
        var t2 = start.AddMinutes(5);
        var t3 = start.AddMinutes(10);

        var series = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        series[t1] = 1;
        series[t2] = 2;
        series[t3] = 3;

        series[t1].Should().Be(1);
        series[t2].Should().Be(2);
        series[t3].Should().Be(3);
        series.ExplicitPointCount.Should().Be(3);
    }

    private static ISparseTimeSeries<int>[] CreateSparseSeries() =>
    [
        new SortedArrayTimeSeries<int>(Period.FiveMinutes),
        new FixedSlotTimeSeries<int>(Period.FiveMinutes),
        new DynamicSlotTimeSeries<int>(Period.FiveMinutes)
    ];
}
