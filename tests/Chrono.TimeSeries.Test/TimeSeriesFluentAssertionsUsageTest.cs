using Chrono.Testing;
using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public sealed class TimeSeriesFluentAssertionsUsageTest
{
    [Fact]
    public void DoubleSeriesShouldResolveToPublicChronoTestingFluentAssertions()
    {
        var start = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var series = new SortedArrayTimeSeries<double>(Period.Hour)
        {
            [start] = 1.0,
            [start.AddHours(1)] = 2.0
        };

        series.Should()
            .HaveCount(2)
            .And.HavePeriod(Period.Hour)
            .And.ContainValueAt(start.AddHours(1), 2.01, tolerance: 0.05);
    }
}
