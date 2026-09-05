using System.Numerics;

namespace Chrono.Testing;

internal interface IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    T GetValue(int index);
}

internal sealed class SeededRandomGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly Random _random;

    public SeededRandomGeneratorStrategy(int seed)
    {
        _random = new Random(seed);
    }

    public T GetValue(int index) => T.CreateChecked(_random.Next(1, 10));
}

internal sealed class ConstantGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly T _value;

    public ConstantGeneratorStrategy(T value)
    {
        _value = value;
    }

    public T GetValue(int index) => _value;
}

internal sealed class RandomWalkGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly Random _random;
    private readonly T _volatility;
    private T _value;

    public RandomWalkGeneratorStrategy(int seed, T initialValue, T volatility)
    {
        _random = new Random(seed);
        _value = initialValue;
        _volatility = volatility;
    }

    public T GetValue(int index)
    {
        if (index == 0)
            return _value;

        var unitStep = T.CreateChecked((_random.NextDouble() * 2.0) - 1.0);
        _value += unitStep * _volatility;
        return _value;
    }
}

internal sealed class LinearTrendGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly T _step;
    private T _value;

    public LinearTrendGeneratorStrategy(T initialValue, T step)
    {
        _value = initialValue;
        _step = step;
    }

    public T GetValue(int index)
    {
        var current = _value;
        _value += _step;
        return current;
    }
}

internal sealed class StepFunctionGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly int _stepLength;
    private readonly T[] _values;

    public StepFunctionGeneratorStrategy(int stepLength, T[] values)
    {
        _stepLength = stepLength;
        _values = values.ToArray();
    }

    public T GetValue(int index) => _values[Math.Min(index / _stepLength, _values.Length - 1)];
}

internal sealed class SeasonalGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly Random _random;
    private readonly T _amplitude;
    private readonly int _cycleLength;
    private readonly T _baseline;
    private readonly T _noiseAmplitude;

    public SeasonalGeneratorStrategy(int seed, T amplitude, int cycleLength, T baseline, T noiseAmplitude)
    {
        _random = new Random(seed);
        _amplitude = amplitude;
        _cycleLength = cycleLength;
        _baseline = baseline;
        _noiseAmplitude = noiseAmplitude;
    }

    public T GetValue(int index)
    {
        var seasonal = Math.Sin((Math.Tau * index) / _cycleLength) * double.CreateChecked(_amplitude);
        var noise = _noiseAmplitude == T.Zero
            ? 0.0
            : ((_random.NextDouble() * 2.0) - 1.0) * double.CreateChecked(_noiseAmplitude);

        return _baseline + T.CreateChecked(seasonal + noise);
    }
}

internal sealed class SawtoothGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly T _amplitude;
    private readonly int _cycleLength;
    private readonly T _baseline;

    public SawtoothGeneratorStrategy(T amplitude, int cycleLength, T baseline)
    {
        _amplitude = amplitude;
        _cycleLength = cycleLength;
        _baseline = baseline;
    }

    public T GetValue(int index)
    {
        var position = index % _cycleLength;
        var ramp = (double.CreateChecked(_amplitude) * position) / _cycleLength;
        return _baseline + T.CreateChecked(ramp);
    }
}

internal sealed class ImpulseGeneratorStrategy<T> : IGeneratorStrategy<T>
    where T : struct, INumber<T>
{
    private readonly T _baseline;
    private readonly Dictionary<int, T> _spikes;

    public ImpulseGeneratorStrategy(T baseline, Dictionary<int, T> spikes)
    {
        _baseline = baseline;
        _spikes = new Dictionary<int, T>(spikes);
    }

    public T GetValue(int index) => _spikes.GetValueOrDefault(index, _baseline);
}
