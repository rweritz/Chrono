namespace Chrono.TimeSeries.Test;

public static class ShouldExtensions
{
    public static TimeSeriesAssertions Should(this IReadOnlyTimeSeries<double> instance)
    {
        return new TimeSeriesAssertions(instance);
    }
}
