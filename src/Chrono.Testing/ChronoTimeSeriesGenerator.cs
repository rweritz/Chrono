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
    : ITimeSeriesGenerator<T>
    where T : struct, INumber<T>
{
    private Period _period = Period.Hour;
    private int _count;
    private DateTimeOffset _start;
    private int _seed;
    private ChronoSeriesShape _shape = ChronoSeriesShape.SortedArray;
    private ChronoGeneratorKind _kind = ChronoGeneratorKind.SeededRandom;
    private T _constantValue;
    private T _initialValue;
    private T _volatility = T.One;
    private T _step = T.One;
    private int _stepLength = 1;
    private T[] _stepValues = [T.Zero];
    private T _baseline;
    private T _amplitude = T.One;
    private int _cycleLength = 1;
    private T _noiseAmplitude;
    private Dictionary<int, T> _spikes = [];
    private double? _gapProbability;

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
    /// Sets the first generated timestamp.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> WithStart(DateTimeOffset start) => StartingAt(start);

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
    /// Materializes a <c>SortedArrayTimeSeries&lt;T&gt;</c>.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> AsSortedArray() => As(ChronoSeriesShape.SortedArray);

    /// <summary>
    /// Materializes a <c>FixedSlotTimeSeries&lt;T&gt;</c>.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> AsFixedSlot() => As(ChronoSeriesShape.FixedSlot);

    /// <summary>
    /// Materializes a <c>DynamicSlotTimeSeries&lt;T&gt;</c>.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> AsDynamicSlot() => As(ChronoSeriesShape.DynamicSlot);

    /// <summary>
    /// Selects constant value generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> Constant(T value)
    {
        _kind = ChronoGeneratorKind.Constant;
        _constantValue = value;
        return this;
    }

    /// <summary>
    /// Selects deterministic random-walk generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> RandomWalk(T initialValue, T volatility)
    {
        if (volatility < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(volatility), "Volatility must not be negative.");

        _kind = ChronoGeneratorKind.RandomWalk;
        _initialValue = initialValue;
        _volatility = volatility;
        return this;
    }

    /// <summary>
    /// Selects linear-trend generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> LinearTrend(T initialValue, T step)
    {
        _kind = ChronoGeneratorKind.LinearTrend;
        _initialValue = initialValue;
        _step = step;
        return this;
    }

    /// <summary>
    /// Selects step-function generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> StepFunction(int stepLength, params T[] values)
    {
        if (stepLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepLength), "Step length must be positive.");

        if (values.Length == 0)
            throw new ArgumentException("At least one step value is required.", nameof(values));

        _kind = ChronoGeneratorKind.StepFunction;
        _stepLength = stepLength;
        _stepValues = values.ToArray();
        return this;
    }

    /// <summary>
    /// Selects sinusoidal seasonal generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> Seasonal(T amplitude, int cycleLength, T baseline = default, T noiseAmplitude = default)
    {
        if (cycleLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(cycleLength), "Cycle length must be positive.");

        if (noiseAmplitude < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(noiseAmplitude), "Noise amplitude must not be negative.");

        _kind = ChronoGeneratorKind.Seasonal;
        _amplitude = amplitude;
        _cycleLength = cycleLength;
        _baseline = baseline;
        _noiseAmplitude = noiseAmplitude;
        return this;
    }

    /// <summary>
    /// Selects repeating sawtooth ramp generation.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> Sawtooth(T amplitude, int cycleLength, T baseline = default)
    {
        if (cycleLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(cycleLength), "Cycle length must be positive.");

        _kind = ChronoGeneratorKind.Sawtooth;
        _amplitude = amplitude;
        _cycleLength = cycleLength;
        _baseline = baseline;
        return this;
    }

    /// <summary>
    /// Selects baseline generation with configured impulse spikes at generated point indexes.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> Impulse(T baseline, params (int Index, T Value)[] spikes)
    {
        if (spikes.Any(spike => spike.Index < 0))
            throw new ArgumentOutOfRangeException(nameof(spikes), "Spike indexes must not be negative.");

        _kind = ChronoGeneratorKind.Impulse;
        _baseline = baseline;
        _spikes = spikes.ToDictionary(spike => spike.Index, spike => spike.Value);
        return this;
    }

    /// <summary>
    /// Removes explicit points from the selected generator with the configured probability.
    /// </summary>
    public ChronoTimeSeriesGeneratorBuilder<T> Sparse(double gapProbability)
    {
        if (gapProbability is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(gapProbability), "Gap probability must be between 0 and 1.");

        _gapProbability = gapProbability;
        return this;
    }

    /// <summary>
    /// Materializes a deterministic Chrono sparse time series from the configured values.
    /// </summary>
    public ISparseTimeSeries<T> Build()
    {
        var series = TimeSeriesGeneratorBuilderSupport.CreateSparseSeries<T>(_shape, _period, _count);

        Populate(series);
        RemoveGaps(series);

        return series;
    }

    private void Populate(ISparseTimeSeries<T> series)
    {
        var strategy = CreateStrategy();

        for (var i = 0; i < _count; i++)
            series[TimeSeriesGeneratorBuilderSupport.AddPeriod(_start, _period, i)] = strategy.GetValue(i);
    }

    private IGeneratorStrategy<T> CreateStrategy() =>
        _kind switch
        {
            ChronoGeneratorKind.SeededRandom => new SeededRandomGeneratorStrategy<T>(_seed),
            ChronoGeneratorKind.Constant => new ConstantGeneratorStrategy<T>(_constantValue),
            ChronoGeneratorKind.RandomWalk => new RandomWalkGeneratorStrategy<T>(_seed, _initialValue, _volatility),
            ChronoGeneratorKind.LinearTrend => new LinearTrendGeneratorStrategy<T>(_initialValue, _step),
            ChronoGeneratorKind.StepFunction => new StepFunctionGeneratorStrategy<T>(_stepLength, _stepValues),
            ChronoGeneratorKind.Seasonal => new SeasonalGeneratorStrategy<T>(
                _seed,
                _amplitude,
                _cycleLength,
                _baseline,
                _noiseAmplitude),
            ChronoGeneratorKind.Sawtooth => new SawtoothGeneratorStrategy<T>(_amplitude, _cycleLength, _baseline),
            ChronoGeneratorKind.Impulse => new ImpulseGeneratorStrategy<T>(_baseline, _spikes),
            _ => throw new NotSupportedException($"Generator kind {_kind} is not supported."),
        };

    private void RemoveGaps(ISparseTimeSeries<T> series)
    {
        if (_gapProbability is not { } gapProbability)
            return;

        var random = new Random(_seed);
        foreach (var point in series.GetPoints().ToArray())
        {
            if (random.NextDouble() < gapProbability)
                series.Remove(point.Timestamp);
        }
    }
}

internal enum ChronoGeneratorKind
{
    SeededRandom,
    Constant,
    RandomWalk,
    LinearTrend,
    StepFunction,
    Seasonal,
    Sawtooth,
    Impulse,
}
