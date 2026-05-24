using System.Numerics;
using Chrono.TimeSeries;

namespace Chrono.Testing;

/// <summary>
/// Starts deterministic Chrono time-series data generators.
/// </summary>
public static class TimeSeriesGenerator
{
    /// <summary>
    /// Starts a generator that emits the same value at every configured timestamp.
    /// </summary>
    public static ConstantTimeSeriesGeneratorBuilder<T> Constant<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that emits a deterministic random walk.
    /// </summary>
    public static RandomWalkTimeSeriesGeneratorBuilder<T> RandomWalk<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that emits an arithmetic sequence.
    /// </summary>
    public static LinearTrendTimeSeriesGeneratorBuilder<T> LinearTrend<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that emits configured values in fixed-length steps.
    /// </summary>
    public static StepFunctionTimeSeriesGeneratorBuilder<T> StepFunction<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that removes explicit points from another sparse generator.
    /// </summary>
    public static SparseTimeSeriesGeneratorBuilder<T> Sparse<T>(ITimeSeriesGenerator<T> source)
        where T : struct, INumber<T> =>
        new(source);
}

/// <summary>
/// Materializes deterministic Chrono sparse series.
/// </summary>
public interface ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    ISparseTimeSeries<T> Build();
}

/// <summary>
/// Configures deterministic constant Chrono sparse series generation.
/// </summary>
public sealed class ConstantTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private T _value;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal ConstantTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the value emitted at every generated timestamp.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> WithValue(T value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public ConstantTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = TimeSeriesGeneratorBuilderSupport.CreateSparseSeries<T>(_shape, _period, _count);
        for (var i = 0; i < _count; i++)
            series[TimeSeriesGeneratorBuilderSupport.AddPeriod(_start, _period, i)] = _value;

        return series;
    }
}

/// <summary>
/// Configures deterministic random-walk Chrono sparse series generation.
/// </summary>
public sealed class RandomWalkTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private int _seed;
    private T _initialValue;
    private T _volatility = T.One;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal RandomWalkTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the deterministic seed used for random walk steps.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>
    /// Sets the first random-walk value.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> WithInitialValue(T initialValue)
    {
        _initialValue = initialValue;
        return this;
    }

    /// <summary>
    /// Sets the maximum absolute size of each generated step.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> WithVolatility(T volatility)
    {
        if (volatility < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(volatility), "Volatility must not be negative.");

        _volatility = volatility;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public RandomWalkTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = TimeSeriesGeneratorBuilderSupport.CreateSparseSeries<T>(_shape, _period, _count);
        var random = new Random(_seed);
        var value = _initialValue;

        for (var i = 0; i < _count; i++)
        {
            if (i > 0)
            {
                var unitStep = T.CreateChecked((random.NextDouble() * 2.0) - 1.0);
                value += unitStep * _volatility;
            }

            series[TimeSeriesGeneratorBuilderSupport.AddPeriod(_start, _period, i)] = value;
        }

        return series;
    }
}

/// <summary>
/// Configures deterministic linear-trend Chrono sparse series generation.
/// </summary>
public sealed class LinearTrendTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private T _initialValue;
    private T _step = T.One;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal LinearTrendTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the first trend value.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> WithInitialValue(T initialValue)
    {
        _initialValue = initialValue;
        return this;
    }

    /// <summary>
    /// Sets the value added at each generated timestamp.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> WithStep(T step)
    {
        _step = step;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public LinearTrendTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = TimeSeriesGeneratorBuilderSupport.CreateSparseSeries<T>(_shape, _period, _count);
        var value = _initialValue;

        for (var i = 0; i < _count; i++)
        {
            series[TimeSeriesGeneratorBuilderSupport.AddPeriod(_start, _period, i)] = value;
            value += _step;
        }

        return series;
    }
}

/// <summary>
/// Configures deterministic step-function Chrono sparse series generation.
/// </summary>
public sealed class StepFunctionTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private int _stepLength = 1;
    private T[] _values = [T.Zero];
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal StepFunctionTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the step levels used in order.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> WithValues(params T[] values)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one step value is required.", nameof(values));

        _values = values.ToArray();
        return this;
    }

    /// <summary>
    /// Sets how many generated timestamps each level lasts.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> WithStepLength(int stepLength)
    {
        if (stepLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepLength), "Step length must be positive.");

        _stepLength = stepLength;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public StepFunctionTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = TimeSeriesGeneratorBuilderSupport.CreateSparseSeries<T>(_shape, _period, _count);
        for (var i = 0; i < _count; i++)
        {
            var valueIndex = Math.Min(i / _stepLength, _values.Length - 1);
            series[TimeSeriesGeneratorBuilderSupport.AddPeriod(_start, _period, i)] = _values[valueIndex];
        }

        return series;
    }
}

/// <summary>
/// Configures deterministic sparse-with-gaps Chrono series generation.
/// </summary>
public sealed class SparseTimeSeriesGeneratorBuilder<T> : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly ITimeSeriesGenerator<T> _source;
    private int _seed;
    private double _gapProbability;

    internal SparseTimeSeriesGeneratorBuilder(ITimeSeriesGenerator<T> source)
    {
        _source = source;
    }

    /// <summary>
    /// Sets the deterministic seed used for gap decisions.
    /// </summary>
    public SparseTimeSeriesGeneratorBuilder<T> WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>
    /// Sets the probability that each explicit point will be removed.
    /// </summary>
    public SparseTimeSeriesGeneratorBuilder<T> WithGapProbability(double gapProbability)
    {
        if (gapProbability is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(gapProbability), "Gap probability must be between 0 and 1.");

        _gapProbability = gapProbability;
        return this;
    }

    /// <summary>
    /// Materializes the configured sparse-with-gaps Chrono series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = _source.Build();
        var random = new Random(_seed);

        foreach (var point in series.GetPoints().ToArray())
        {
            if (random.NextDouble() < _gapProbability)
                series.Remove(point.Timestamp);
        }

        return series;
    }
}

internal static class TimeSeriesGeneratorBuilderSupport
{
    public static ISparseTimeSeries<T> CreateSparseSeries<T>(ChronoSeriesShape shape, Period period, int capacity)
        where T : struct, INumber<T> =>
        shape switch
        {
            ChronoSeriesShape.SortedArray => new SortedArrayTimeSeries<T>(period, capacity),
            ChronoSeriesShape.FixedSlot => new FixedSlotTimeSeries<T>(period, capacity),
            ChronoSeriesShape.DynamicSlot => new DynamicSlotTimeSeries<T>(period, capacity: capacity),
            _ => throw new NotSupportedException($"Series shape {shape} is not supported."),
        };

    public static DateTimeOffset AddPeriod(DateTimeOffset timestamp, Period period, int count) =>
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
            _ => throw new NotSupportedException($"Period {period} is not supported by deterministic generators."),
        };
}
