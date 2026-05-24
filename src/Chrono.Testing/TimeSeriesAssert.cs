using System.Globalization;
using System.Numerics;
using Chrono.TimeSeries;

namespace Chrono.Testing;

/// <summary>
/// Provides framework-agnostic assertions for Chrono time-series values.
/// </summary>
public static class TimeSeriesAssert
{
    /// <summary>
    /// Asserts that two sparse time series have the same metadata and values within the supplied tolerance.
    /// </summary>
    public static void Equal<T>(
        IReadOnlySparseTimeSeries<T> expected,
        IReadOnlySparseTimeSeries<T> actual,
        T tolerance)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (tolerance < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must not be negative.");

        if (expected.Period != actual.Period)
            Fail($"Time series period mismatch. Expected {expected.Period}. Actual {actual.Period}.");

        if (expected.ExplicitPointCount != actual.ExplicitPointCount)
            Fail($"Time series count mismatch. Expected {expected.ExplicitPointCount}. Actual {actual.ExplicitPointCount}.");

        var expectedPoints = expected.GetPoints().ToArray();
        var actualPoints = actual.GetPoints().ToArray();

        for (var i = 0; i < expectedPoints.Length; i++)
        {
            var expectedPoint = expectedPoints[i];
            var actualPoint = actualPoints[i];

            if (expectedPoint.Timestamp != actualPoint.Timestamp)
            {
                Fail(
                    $"Time series timestamp mismatch at index {i}. Expected {FormatTimestamp(expectedPoint.Timestamp)}. Actual {FormatTimestamp(actualPoint.Timestamp)}.");
            }

            var difference = T.Abs(expectedPoint.Value - actualPoint.Value);
            if (difference > tolerance)
            {
                Fail(
                    $"Time series value mismatch at {FormatTimestamp(expectedPoint.Timestamp)}. Expected {FormatValue(expectedPoint.Value)}. Actual {FormatValue(actualPoint.Value)}. Difference {FormatValue(difference)}. Tolerance {FormatValue(tolerance)}.");
            }
        }
    }

    /// <summary>
    /// Asserts that every explicit sparse time-series value is within the inclusive range.
    /// </summary>
    public static void AllValuesInRange<T>(IReadOnlySparseTimeSeries<T> series, T min, T max)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (min > max)
            throw new ArgumentException("Minimum value must not be greater than maximum value.", nameof(min));

        foreach (var point in series.GetPoints())
        {
            if (point.Value < min || point.Value > max)
            {
                Fail(
                    $"Time series value outside expected range at {FormatTimestamp(point.Timestamp)}. Actual {FormatValue(point.Value)}. Range [{FormatValue(min)}, {FormatValue(max)}].");
            }
        }
    }

    /// <summary>
    /// Asserts that a sparse time series has the expected explicit point count.
    /// </summary>
    public static void HasCount<T>(IReadOnlySparseTimeSeries<T> series, int expectedCount)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (series.ExplicitPointCount != expectedCount)
        {
            Fail(
                $"Time series count mismatch. Expected {expectedCount}. Actual {series.ExplicitPointCount}.");
        }
    }

    /// <summary>
    /// Asserts that a time series has the expected period.
    /// </summary>
    public static void HasPeriod<T>(IReadOnlyTimeSeries<T> series, Period expectedPeriod)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (series.Period != expectedPeriod)
            Fail($"Time series period mismatch. Expected {expectedPeriod}. Actual {series.Period}.");
    }

    /// <summary>
    /// Asserts that a sparse time series has the expected minimum and maximum dates.
    /// </summary>
    public static void HasDateRange<T>(
        IReadOnlySparseTimeSeries<T> series,
        DateTimeOffset expectedMinDate,
        DateTimeOffset expectedMaxDate)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (series.MinDate != expectedMinDate || series.MaxDate != expectedMaxDate)
        {
            Fail(
                $"Time series date range mismatch. Expected [{FormatTimestamp(expectedMinDate)}, {FormatTimestamp(expectedMaxDate)}]. Actual [{FormatTimestamp(series.MinDate)}, {FormatTimestamp(series.MaxDate)}].");
        }
    }

    /// <summary>
    /// Asserts that the sum of explicit sparse time-series values is within the supplied tolerance.
    /// </summary>
    public static void SumCloseTo<T>(IReadOnlySparseTimeSeries<T> series, T expectedSum, T tolerance)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (tolerance < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must not be negative.");

        var actualSum = T.Zero;
        foreach (var point in series.GetPoints())
            actualSum += point.Value;

        var difference = T.Abs(expectedSum - actualSum);
        if (difference > tolerance)
        {
            Fail(
                $"Time series sum mismatch. Expected {FormatValue(expectedSum)}. Actual {FormatValue(actualSum)}. Difference {FormatValue(difference)}. Tolerance {FormatValue(tolerance)}.");
        }
    }

    /// <summary>
    /// Asserts that every adjacent explicit sparse time-series point is separated by exactly one period.
    /// </summary>
    public static void HasNoGaps<T>(IReadOnlySparseTimeSeries<T> series)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        using var points = series.GetPoints().GetEnumerator();
        if (!points.MoveNext())
            return;

        var previous = points.Current.Timestamp;
        while (points.MoveNext())
        {
            var expectedNext = AddPeriod(previous, series.Period);
            var actualNext = points.Current.Timestamp;

            if (actualNext != expectedNext)
            {
                Fail(
                    $"Time series gap detected. Expected next timestamp {FormatTimestamp(expectedNext)} after {FormatTimestamp(previous)}. Actual {FormatTimestamp(actualNext)}.");
            }

            previous = actualNext;
        }
    }

    /// <summary>
    /// Asserts that a time series has a value at the timestamp within the supplied tolerance.
    /// </summary>
    public static void ValueAtCloseTo<T>(
        IReadOnlyTimeSeries<T> series,
        DateTimeOffset timestamp,
        T expectedValue,
        T tolerance)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);

        if (tolerance < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must not be negative.");

        if (!series.TryGetValue(timestamp, out var actualValue))
            Fail($"Time series missing value at {FormatTimestamp(timestamp)}. Expected {FormatValue(expectedValue)}.");

        var difference = T.Abs(expectedValue - actualValue);
        if (difference > tolerance)
        {
            Fail(
                $"Time series value mismatch at {FormatTimestamp(timestamp)}. Expected {FormatValue(expectedValue)}. Actual {FormatValue(actualValue)}. Difference {FormatValue(difference)}. Tolerance {FormatValue(tolerance)}.");
        }
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToString("O");

    private static string FormatValue<T>(T value)
        where T : struct, INumber<T> =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private static DateTimeOffset AddPeriod(DateTimeOffset timestamp, Period period) =>
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
            _ => throw new NotSupportedException($"Period {period} does not support gap checks.")
        };

    private static void Fail(string message) => throw new TimeSeriesAssertionException(message);
}
