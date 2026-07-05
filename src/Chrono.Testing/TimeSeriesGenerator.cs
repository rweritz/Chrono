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
    /// Starts a generator that emits a sinusoidal seasonal cycle.
    /// </summary>
    public static SeasonalTimeSeriesGeneratorBuilder<T> Seasonal<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that emits a repeating sawtooth ramp.
    /// </summary>
    public static SawtoothTimeSeriesGeneratorBuilder<T> Sawtooth<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that emits a baseline with configured impulse spikes.
    /// </summary>
    public static ImpulseTimeSeriesGeneratorBuilder<T> Impulse<T>(Period period)
        where T : struct, INumber<T> =>
        new(period);

    /// <summary>
    /// Starts a generator that removes explicit points from another sparse generator.
    /// </summary>
    public static SparseTimeSeriesGeneratorBuilder<T> Sparse<T>(ITimeSeriesGenerator<T> source)
        where T : struct, INumber<T> =>
        new(source);

    /// <summary>
    /// Starts a generator that combines aligned points from two source generators.
    /// </summary>
    public static CompositeTimeSeriesGeneratorBuilder<T> Composite<T>(
        ITimeSeriesGenerator<T> left,
        ITimeSeriesGenerator<T> right,
        Func<T, T, T> combine)
        where T : struct, INumber<T> =>
        new(left, right, combine);
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
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new ConstantGeneratorStrategy<T>(_value));
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
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new RandomWalkGeneratorStrategy<T>(_seed, _initialValue, _volatility));
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
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new LinearTrendGeneratorStrategy<T>(_initialValue, _step));
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
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new StepFunctionGeneratorStrategy<T>(_stepLength, _values));
    }
}

/// <summary>
/// Configures deterministic seasonal Chrono sparse series generation.
/// </summary>
public sealed class SeasonalTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private int _seed;
    private T _amplitude = T.One;
    private int _cycleLength = 1;
    private T _baseline;
    private T _noiseAmplitude;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal SeasonalTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the deterministic seed used for optional seasonal noise.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>
    /// Sets the seasonal cycle amplitude.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithAmplitude(T amplitude)
    {
        _amplitude = amplitude;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps in each cycle.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithCycleLength(int cycleLength)
    {
        if (cycleLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(cycleLength), "Cycle length must be positive.");

        _cycleLength = cycleLength;
        return this;
    }

    /// <summary>
    /// Sets the value added to every generated seasonal point.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithBaseline(T baseline)
    {
        _baseline = baseline;
        return this;
    }

    /// <summary>
    /// Sets the maximum absolute optional noise added to each seasonal point.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> WithNoiseAmplitude(T noiseAmplitude)
    {
        if (noiseAmplitude < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(noiseAmplitude), "Noise amplitude must not be negative.");

        _noiseAmplitude = noiseAmplitude;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public SeasonalTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new SeasonalGeneratorStrategy<T>(_seed, _amplitude, _cycleLength, _baseline, _noiseAmplitude));
    }
}

/// <summary>
/// Configures deterministic sawtooth Chrono sparse series generation.
/// </summary>
public sealed class SawtoothTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private T _amplitude = T.One;
    private int _cycleLength = 1;
    private T _baseline;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal SawtoothTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the ramp amplitude across each cycle.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> WithAmplitude(T amplitude)
    {
        _amplitude = amplitude;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps in each cycle.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> WithCycleLength(int cycleLength)
    {
        if (cycleLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(cycleLength), "Cycle length must be positive.");

        _cycleLength = cycleLength;
        return this;
    }

    /// <summary>
    /// Sets the value added to every generated ramp point.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> WithBaseline(T baseline)
    {
        _baseline = baseline;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public SawtoothTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new SawtoothGeneratorStrategy<T>(_amplitude, _cycleLength, _baseline));
    }
}

/// <summary>
/// Configures deterministic impulse Chrono sparse series generation.
/// </summary>
public sealed class ImpulseTimeSeriesGeneratorBuilder<T>
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly Period _period;
    private DateTimeOffset _start;
    private int _count;
    private T _baseline;
    private Dictionary<int, T> _spikes = [];
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;

    internal ImpulseTimeSeriesGeneratorBuilder(Period period)
    {
        _period = period;
    }

    /// <summary>
    /// Sets the first generated timestamp.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start)
    {
        _start = start;
        return this;
    }

    /// <summary>
    /// Sets the number of generated timestamps.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> WithCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");

        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the value emitted when no spike exists at a generated point index.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> WithBaseline(T baseline)
    {
        _baseline = baseline;
        return this;
    }

    /// <summary>
    /// Sets spike values at generated point indexes.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> WithSpikes(params (int Index, T Value)[] spikes)
    {
        if (spikes.Any(spike => spike.Index < 0))
            throw new ArgumentOutOfRangeException(nameof(spikes), "Spike indexes must not be negative.");

        _spikes = spikes.ToDictionary(spike => spike.Index, spike => spike.Value);
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="SortedArrayTimeSeries{T}"/>.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> AsSortedArray()
    {
        _shape = ChronoSeriesShape.SortedArray;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="FixedSlotTimeSeries{T}"/>.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> AsFixedSlot()
    {
        _shape = ChronoSeriesShape.FixedSlot;
        return this;
    }

    /// <summary>
    /// Materializes a <see cref="DynamicSlotTimeSeries{T}"/>.
    /// </summary>
    public ImpulseTimeSeriesGeneratorBuilder<T> AsDynamicSlot()
    {
        _shape = ChronoSeriesShape.DynamicSlot;
        return this;
    }

    /// <summary>
    /// Materializes the configured Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        return TimeSeriesGeneratorBuilderSupport.BuildSparseSeries(
            _shape,
            _period,
            _start,
            _count,
            new ImpulseGeneratorStrategy<T>(_baseline, _spikes));
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

/// <summary>
/// Configures deterministic composite Chrono sparse series generation.
/// </summary>
public sealed class CompositeTimeSeriesGeneratorBuilder<T> : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private readonly ITimeSeriesGenerator<T> _left;
    private readonly ITimeSeriesGenerator<T> _right;
    private readonly Func<T, T, T> _combine;

    internal CompositeTimeSeriesGeneratorBuilder(
        ITimeSeriesGenerator<T> left,
        ITimeSeriesGenerator<T> right,
        Func<T, T, T> combine)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _combine = combine ?? throw new ArgumentNullException(nameof(combine));
    }

    /// <summary>
    /// Materializes the configured composite Chrono sparse series.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var left = _left.Build();
        var right = _right.Build();

        if (left.Period != right.Period)
            throw new InvalidOperationException("Composite generators must use the same period.");

        var leftPoints = left.GetPoints().ToArray();
        var series = new SortedArrayTimeSeries<T>(left.Period, leftPoints.Length);

        foreach (var point in leftPoints)
        {
            if (right.TryGetValue(point.Timestamp, out var rightValue))
                series[point.Timestamp] = _combine(point.Value, rightValue);
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

    public static ISparseTimeSeries<T> BuildSparseSeries<T>(
        ChronoSeriesShape shape,
        Period period,
        DateTimeOffset start,
        int count,
        IGeneratorStrategy<T> strategy)
        where T : struct, INumber<T>
    {
        var series = CreateSparseSeries<T>(shape, period, count);
        for (var i = 0; i < count; i++)
            series[AddPeriod(start, period, i)] = strategy.GetValue(i);

        return series;
    }

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
