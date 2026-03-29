using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public class SparseTimeSeries<T> : ITimeSeries<T>
    where T : struct, INumber<T>
{
    private long[] _keys;
    private T[] _values;
    private int _count;

    private readonly Func<DateTimeOffset, DateTimeOffset, bool>? _validationFunc;
    private DateTimeOffset? _reference;

    private const int DefaultCapacity = 16;

    public SparseTimeSeries(Period period, int capacity = DefaultCapacity)
    {
        Period = period;
        _keys = new long[Math.Max(0, capacity)];
        _values = new T[Math.Max(0, capacity)];

        if (period != Period.NonStandard)
            _validationFunc = PeriodConverter.GetValidationFunc(period);
    }

    public Period Period { get; }

    public int Count => _count;

    public DateTimeOffset MinDate =>
        _count == 0
            ? throw new InvalidOperationException("Series is empty")
            : new DateTimeOffset(_keys[0], TimeSpan.Zero);

    public DateTimeOffset MaxDate =>
        _count == 0
            ? throw new InvalidOperationException("Series is empty")
            : new DateTimeOffset(_keys[_count - 1], TimeSpan.Zero);

    internal ReadOnlySpan<long> TickKeys => _keys.AsSpan(0, _count);

    internal ReadOnlySpan<T> Values => _values.AsSpan(0, _count);

    public T this[DateTimeOffset timestamp]
    {
        get
        {
            var ticks = timestamp.UtcTicks;
            var index = Array.BinarySearch(_keys, 0, _count, ticks);
            if (index < 0)
                throw new KeyNotFoundException($"No entry for {timestamp:O}");

            return _values[index];
        }
        set => Set(timestamp, value);
    }

    public bool TryGetValue(DateTimeOffset timestamp, out T value)
    {
        var index = Array.BinarySearch(_keys, 0, _count, timestamp.UtcTicks);
        if (index >= 0)
        {
            value = _values[index];
            return true;
        }

        value = T.Zero;
        return false;
    }

    public void Set(DateTimeOffset timestamp, T value)
    {
        ValidatePeriod(timestamp);
        var ticks = timestamp.UtcTicks;
        var index = Array.BinarySearch(_keys, 0, _count, ticks);
        if (index >= 0)
        {
            _values[index] = value;
            return;
        }

        InsertAt(~index, ticks, value);
    }

    public bool Remove(DateTimeOffset timestamp)
    {
        var index = Array.BinarySearch(_keys, 0, _count, timestamp.UtcTicks);
        if (index < 0)
            return false;

        var moveCount = _count - index - 1;
        if (moveCount > 0)
        {
            Array.Copy(_keys, index + 1, _keys, index, moveCount);
            Array.Copy(_values, index + 1, _values, index, moveCount);
        }

        _count--;
        return true;
    }

    public void Clear()
    {
        _count = 0;
        _reference = null;
    }

    public IEnumerator<TimeSeriesPoint<T>> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
            yield return new TimeSeriesPoint<T>(new DateTimeOffset(_keys[i], TimeSpan.Zero), _values[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static SparseTimeSeries<T> CreateFromSortedRaw(ReadOnlySpan<long> keys, ReadOnlySpan<T> values, Period period)
    {
        if (keys.Length != values.Length)
            throw new ArgumentException("keys and values length mismatch");

        var ts = new SparseTimeSeries<T>(period, keys.Length)
        {
            _count = keys.Length
        };

        keys.CopyTo(ts._keys);
        values.CopyTo(ts._values);
        return ts;
    }

    private void ValidatePeriod(DateTimeOffset dateTime)
    {
        if (_validationFunc is null)
            return;

        _reference ??= dateTime;
        if (!_validationFunc(dateTime, _reference.Value))
            throw new ArgumentException($"The Argument {nameof(dateTime)} value: {dateTime} " +
                                        $"is not fitting to the reference value {_reference} " +
                                        $"and the {nameof(Period)} value {Period}");
    }

    private void InsertAt(int index, long ticks, T value)
    {
        if (_count == _keys.Length)
            Grow();

        if (index < _count)
        {
            Array.Copy(_keys, index, _keys, index + 1, _count - index);
            Array.Copy(_values, index, _values, index + 1, _count - index);
        }

        _keys[index] = ticks;
        _values[index] = value;
        _count++;
    }

    private void Grow()
    {
        var newCapacity = _keys.Length == 0 ? DefaultCapacity : _keys.Length * 2;
        Array.Resize(ref _keys, newCapacity);
        Array.Resize(ref _values, newCapacity);
    }
}
