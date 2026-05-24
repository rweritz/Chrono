using System.Numerics;
using Chrono.TimeSeries;

namespace Chrono.Testing;

/// <summary>
/// Starts deterministic Chrono time-series test data builders.
/// </summary>
public static class ChronoTimeSeriesGenerator
{
    /// <summary>
    /// Starts a deterministic time-series builder for values of type <typeparamref name="T"/>.
    /// </summary>
    public static ChronoTimeSeriesGeneratorBuilder<T> For<T>()
        where T : struct, INumber<T> =>
        new();
}

/// <summary>
/// Configures and materializes deterministic Chrono sparse time-series instances.
/// </summary>
/// <typeparam name="T">The numeric value type stored in the generated series.</typeparam>
public sealed class ChronoTimeSeriesGeneratorBuilder<T>
    where T : struct, INumber<T>
{
    private Period _period = Period.Hour;
    private int _count;
    private DateTimeOffset _start;
    private int _seed;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    /// <summary>
    /// Sets the period used for generated timestamps.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> WithPeriod(Period period)
    {
        _period = period;
        return this;
    }

    /// <summary>
    /// Sets the number of generated points.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> StartingAt(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the deterministic seed used to generate values.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>
    /// Sets the Chrono sparse series shape to materialize.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> As(ChronoSeriesShape shape)
    {
        _shape = shape;
        return this;
    }

    /// <summary>
    /// Materializes a deterministic Chrono sparse time series from the configured values.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        ISparseTimeSeries<T> series = _shape switch
        {
            ChronoSeriesShape.SortedArray => new SortedArrayTimeSeries<T>(_period, _count),
            ChronoSeriesShape.FixedSlot => new FixedSlotTimeSeries<T>(_period, _count),
            ChronoSeriesShape.DynamicSlot => new DynamicSlotTimeSeries<T>(_period, capacity: _count),
            _ => throw new NotSupportedException($"Series shape {_shape} is not supported."),
        };

        var random = new Random(_seed);
        for (var i = 0; i < _count; i++)
            series[AddPeriod(_start, _period, i)] = T.CreateChecked(random.Next(1, 10));

        return series;
    }

    private static DateTimeOffset AddPeriod(DateTimeOffset timestamp, Period period, int count) =>
        period switch
        {
            Period.FiveMinutes => timestamp.AddMinutes(5 * count),
            Period.QuaterHour => timestamp.AddMinutes(15 * count),
            Period.HalfHour => timestamp.AddMinutes(30 * count),
            Period.Hour => timestamp.AddHours(count),
            Period.HalfDay => timestamp.AddHours(12 * count),
            Period.Day => timestamp.AddDays(count),
            Period.Week => timestamp.AddDays(7 * count),
            Period.Month => timestamp.AddMonths(count),
            Period.QuaterYear => timestamp.AddMonths(3 * count),
            Period.HalfYear => timestamp.AddMonths(6 * count),
            Period.Year => timestamp.AddYears(count),
            Period.NonStandard => timestamp.AddDays(count),
            _ => throw new NotSupportedException($"Period {period} is not supported by the deterministic scaffold."),
        };
}
