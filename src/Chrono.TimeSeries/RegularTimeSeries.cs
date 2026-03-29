using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class RegularTimeSeries<T> : ITimeSeries<T>
    where T : struct, INumber<T>
{
    private readonly long _stepTicks;
    private long _startSlot;
    private int _length;
    private int _count;
    private T[] _values;
    private ulong[] _presentBits;

    public RegularTimeSeries(Period period, int capacity = 0)
    {
        if (!PeriodMath.TryGetFixedTicks(period, out _stepTicks))
            throw new NotSupportedException($"Use SparseTimeSeries for period {period}.");

        Period = period;
        _values = capacity == 0 ? Array.Empty<T>() : GC.AllocateUninitializedArray<T>(capacity);
        _presentBits = capacity == 0 ? Array.Empty<ulong>() : new ulong[(capacity + 63) >> 6];
    }

    public Period Period { get; }

    public int Count => _count;

    public DateTimeOffset MinDate
    {
        get
        {
            if (_count == 0)
                throw new InvalidOperationException("Series is empty.");

            return PeriodMath.FromAbsoluteSlot(_startSlot + FirstPresentIndex(), Period);
        }
    }

    public DateTimeOffset MaxDate
    {
        get
        {
            if (_count == 0)
                throw new InvalidOperationException("Series is empty.");

            return PeriodMath.FromAbsoluteSlot(_startSlot + LastPresentIndex(), Period);
        }
    }

    public T this[DateTimeOffset timestamp]
    {
        get
        {
            if (!TryGetValue(timestamp, out var value))
                throw new KeyNotFoundException($"No value exists at {timestamp:O}");

            return value;
        }
        set => Set(timestamp, value);
    }

    public void Set(DateTimeOffset timestamp, T value)
    {
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        var index = EnsureSlot(slot);

        _values[index] = value;
        if (!IsPresent(index))
        {
            MarkPresent(index);
            _count++;
        }
    }

    public bool Remove(DateTimeOffset timestamp)
    {
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        var index64 = slot - _startSlot;

        if ((ulong)index64 >= (ulong)_length)
            return false;

        var index = (int)index64;
        if (!IsPresent(index))
            return false;

        _values[index] = T.Zero;
        ClearPresent(index);
        _count--;
        return true;
    }

    public void Clear()
    {
        Array.Clear(_values, 0, _length);
        Array.Clear(_presentBits, 0, _presentBits.Length);
        _count = 0;
    }

    public bool TryGetValue(DateTimeOffset timestamp, out T value)
    {
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        var index64 = slot - _startSlot;

        if ((ulong)index64 >= (ulong)_length)
        {
            value = T.Zero;
            return false;
        }

        var index = (int)index64;
        if (!IsPresent(index))
        {
            value = T.Zero;
            return false;
        }

        value = _values[index];
        return true;
    }

    public IEnumerator<TimeSeriesPoint<T>> GetEnumerator()
    {
        for (var i = 0; i < _length; i++)
        {
            if (!IsPresent(i))
                continue;

            yield return new TimeSeriesPoint<T>(
                PeriodMath.FromAbsoluteSlot(_startSlot + i, Period),
                _values[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal long StartSlot => _startSlot;

    internal int SlotLength => _length;

    internal bool IsDense => _count == _length;

    internal ReadOnlySpan<T> ValueSpan => _values.AsSpan(0, _length);

    internal Span<T> MutableValueSpan => _values.AsSpan(0, _length);

    internal ReadOnlySpan<ulong> PresenceBits => _presentBits;

    internal bool TryGetSlotValue(long slot, out T value)
    {
        var index64 = slot - _startSlot;
        if ((ulong)index64 >= (ulong)_length)
        {
            value = T.Zero;
            return false;
        }

        var index = (int)index64;
        if (!IsPresent(index))
        {
            value = T.Zero;
            return false;
        }

        value = _values[index];
        return true;
    }

    internal void InitializeWindow(long startSlot, int length)
    {
        EnsureCapacity(length);
        _startSlot = startSlot;
        _length = length;
        _count = 0;
        Array.Clear(_values, 0, _length);
        Array.Clear(_presentBits, 0, _presentBits.Length);
    }

    internal void MarkPresentAt(int index)
    {
        if (!IsPresent(index))
        {
            MarkPresent(index);
            _count++;
        }
    }

    private int EnsureSlot(long slot)
    {
        if (_length == 0)
        {
            EnsureCapacity(1);
            _startSlot = slot;
            _length = 1;
            return 0;
        }

        if (slot < _startSlot)
            GrowLeft(checked((int)(_startSlot - slot)));
        else if (slot >= _startSlot + _length)
            GrowRight(checked((int)(slot - (_startSlot + _length) + 1)));

        return checked((int)(slot - _startSlot));
    }

    private void EnsureCapacity(int min)
    {
        if (_values.Length >= min)
            return;

        var newCapacity = Math.Max(min, Math.Max(4, _values.Length * 2));
        Array.Resize(ref _values, newCapacity);
        Array.Resize(ref _presentBits, (newCapacity + 63) >> 6);
    }

    private void GrowRight(int extra)
    {
        var newLength = checked(_length + extra);
        EnsureCapacity(newLength);
        _length = newLength;
    }

    private void GrowLeft(int extra)
    {
        var newLength = checked(_length + extra);
        EnsureCapacity(newLength);

        Array.Copy(_values, 0, _values, extra, _length);

        var oldBits = _presentBits;
        _presentBits = new ulong[(newLength + 63) >> 6];
        for (var i = 0; i < _length; i++)
        {
            if (((oldBits[i >> 6] >> (i & 63)) & 1UL) != 0)
                _presentBits[(i + extra) >> 6] |= 1UL << ((i + extra) & 63);
        }

        _startSlot -= extra;
        _length = newLength;
    }

    private bool IsPresent(int index) =>
        ((_presentBits[index >> 6] >> (index & 63)) & 1UL) != 0;

    private void MarkPresent(int index) =>
        _presentBits[index >> 6] |= 1UL << (index & 63);

    private void ClearPresent(int index) =>
        _presentBits[index >> 6] &= ~(1UL << (index & 63));

    private int FirstPresentIndex()
    {
        for (var i = 0; i < _length; i++)
            if (IsPresent(i))
                return i;

        throw new InvalidOperationException("Series is empty.");
    }

    private int LastPresentIndex()
    {
        for (var i = _length - 1; i >= 0; i--)
            if (IsPresent(i))
                return i;

        throw new InvalidOperationException("Series is empty.");
    }
}
