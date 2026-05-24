using System.Numerics;
using Chrono.TimeSeries;

namespace Chrono.Testing;

/// <summary>
/// Provides FluentAssertions <c>Should()</c> extensions for Chrono time-series values.
/// </summary>
public static class TimeSeriesShouldExtensions
{
    /// <summary>
    /// Returns FluentAssertions assertions for a Chrono time series.
    /// </summary>
    public static TimeSeriesAssertions<T> Should<T>(this IReadOnlyTimeSeries<T> series)
        where T : struct, INumber<T> =>
        new(series);

    /// <summary>
    /// Returns FluentAssertions assertions for a Chrono sorted-array time series.
    /// </summary>
    public static TimeSeriesAssertions<T> Should<T>(this SortedArrayTimeSeries<T> series)
        where T : struct, INumber<T> =>
        new(series);

    /// <summary>
    /// Returns FluentAssertions assertions for a Chrono fixed-slot time series.
    /// </summary>
    public static TimeSeriesAssertions<T> Should<T>(this FixedSlotTimeSeries<T> series)
        where T : struct, INumber<T> =>
        new(series);

    /// <summary>
    /// Returns FluentAssertions assertions for a Chrono dynamic-slot time series.
    /// </summary>
    public static TimeSeriesAssertions<T> Should<T>(this DynamicSlotTimeSeries<T> series)
        where T : struct, INumber<T> =>
        new(series);

    /// <summary>
    /// Returns FluentAssertions assertions for a Chrono stepwise time series.
    /// </summary>
    public static TimeSeriesAssertions<T> Should<T>(this StepwiseTimeSeries<T> series)
        where T : struct, INumber<T> =>
        new(series);
}
