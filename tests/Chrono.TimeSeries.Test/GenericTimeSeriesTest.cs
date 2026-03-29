using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class GenericTimeSeriesTest
{
    [Fact]
    public void SparseTimeSeries_ShouldSupportIntDoubleDecimal()
    {
        var t1 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t2 = t1.AddMinutes(5);

        var intSeries = new SparseTimeSeries<int>(Period.FiveMinutes);
        intSeries[t1] = 5;
        intSeries[t2] = 10;

        var doubleSeries = new SparseTimeSeries<double>(Period.FiveMinutes);
        doubleSeries[t1] = 3.0;

        var decimalSeries = new SparseTimeSeries<decimal>(Period.FiveMinutes);
        decimalSeries[t1] = 1.75m;

        intSeries[t1].Should().Be(5);
        intSeries[t2].Should().Be(10);
        doubleSeries[t1].Should().Be(3.0);
        decimalSeries[t1].Should().Be(1.75m);
    }

    [Fact]
    public void RegularTimeSeries_ShouldSupportO1GridAccess()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var t1 = start;
        var t2 = start.AddMinutes(5);
        var t3 = start.AddMinutes(10);

        var series = new RegularTimeSeries<int>(Period.FiveMinutes);
        series[t1] = 1;
        series[t2] = 2;
        series[t3] = 3;

        series[t1].Should().Be(1);
        series[t2].Should().Be(2);
        series[t3].Should().Be(3);
        series.Count.Should().Be(3);
    }
}
