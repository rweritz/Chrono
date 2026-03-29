using System.Numerics;

namespace Chrono.TimeSeries;

public interface IAggregator<TIn, TOut>
    where TIn : struct, INumber<TIn>
    where TOut : struct, INumber<TOut>
{
    void Reset();
    void Add(TIn value);
    TOut Complete(int count);
}

public struct SumAggregator<T> : IAggregator<T, T>
    where T : struct, INumber<T>
{
    private T _sum;

    public void Reset() => _sum = T.Zero;

    public void Add(T value) => _sum += value;

    public T Complete(int count) => _sum;
}

public struct AverageAggregator<T> : IAggregator<T, T>
    where T : struct, INumber<T>
{
    private T _sum;

    public void Reset() => _sum = T.Zero;

    public void Add(T value) => _sum += value;

    public T Complete(int count)
    {
        if (count == 0)
            return T.Zero;

        return _sum / T.CreateChecked(count);
    }
}

public struct MinAggregator<T> : IAggregator<T, T>
    where T : struct, INumber<T>, IMinMaxValue<T>
{
    private T _min;
    private bool _hasValue;

    public void Reset()
    {
        _min = T.MaxValue;
        _hasValue = false;
    }

    public void Add(T value)
    {
        if (!_hasValue || value < _min)
        {
            _min = value;
            _hasValue = true;
        }
    }

    public T Complete(int count) => _min;
}

public struct MaxAggregator<T> : IAggregator<T, T>
    where T : struct, INumber<T>, IMinMaxValue<T>
{
    private T _max;
    private bool _hasValue;

    public void Reset()
    {
        _max = T.MinValue;
        _hasValue = false;
    }

    public void Add(T value)
    {
        if (!_hasValue || value > _max)
        {
            _max = value;
            _hasValue = true;
        }
    }

    public T Complete(int count) => _max;
}

public struct CountAggregator<T> : IAggregator<T, int>
    where T : struct, INumber<T>
{
    private int _count;

    public void Reset() => _count = 0;

    public void Add(T value) => _count++;

    public int Complete(int count) => _count;
}
