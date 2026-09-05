using System.Numerics;

namespace Chrono.TimeSeries;

/// <summary>
/// Owns the storage lifecycle for a contiguous window of period slots.
/// </summary>
internal sealed class SlotWindow<T>
    where T : struct, INumber<T>
{
    private long _startSlot;
    private int _length;
    private int _count;
    private T[] _values;
    private ulong[] _presentBits;

    public SlotWindow(int capacity = 0)
    {
        _values = capacity == 0 ? Array.Empty<T>() : GC.AllocateUninitializedArray<T>(capacity);
        _presentBits = capacity == 0 ? Array.Empty<ulong>() : new ulong[(capacity + 63) >> 6];
    }

    public long StartSlot => _startSlot;

    public int Length => _length;

    public int Count => _count;

    public bool IsDense => _count == _length;

    public void Set(long slot, T value)
    {
        var index = EnsureSlot(slot);
        _values[index] = value;

        if (!IsPresent(index))
        {
            MarkPresent(index);
            _count++;
        }
    }

    public bool Remove(long slot)
    {
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

    public bool TryGetValue(long slot, out T value)
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

    public IEnumerable<(long Slot, T Value)> GetPoints()
    {
        for (var i = 0; i < _length; i++)
        {
            if (IsPresent(i))
                yield return (_startSlot + i, _values[i]);
        }
    }

    public long FirstPresentSlot()
    {
        for (var i = 0; i < _length; i++)
            if (IsPresent(i))
                return _startSlot + i;

        throw new InvalidOperationException("Series is empty.");
    }

    public long LastPresentSlot()
    {
        for (var i = _length - 1; i >= 0; i--)
            if (IsPresent(i))
                return _startSlot + i;

        throw new InvalidOperationException("Series is empty.");
    }

    public SlotWindow<TOut> Aggregate<TOut, TAggregator>(int factor, TAggregator aggregator)
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<T, TOut>
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor));

        if (_length == 0)
            return new SlotWindow<TOut>();

        var firstBucket = Math.DivRem(_startSlot, factor, out var firstRemainder);
        if (firstRemainder < 0)
            firstBucket--;

        var lastSourceSlot = _startSlot + _length - 1;
        var lastBucket = Math.DivRem(lastSourceSlot, factor, out var lastRemainder);
        if (lastRemainder < 0)
            lastBucket--;

        var bucketCount = checked((int)(lastBucket - firstBucket + 1));
        var result = new SlotWindow<TOut>(bucketCount);

        for (var bucket = firstBucket; bucket <= lastBucket; bucket++)
        {
            aggregator.Reset();
            var count = 0;
            var bucketStart = bucket * factor;
            var bucketEndExclusive = bucketStart + factor;
            var localStart = (int)Math.Max(0, bucketStart - _startSlot);
            var localEnd = (int)Math.Min(_length, bucketEndExclusive - _startSlot);

            for (var i = localStart; i < localEnd; i++)
            {
                if (!IsPresent(i))
                    continue;

                aggregator.Add(_values[i]);
                count++;
            }

            if (count > 0)
                result.Set(bucket, aggregator.Complete(count));
        }

        return result;
    }

    public SlotWindow<T> Add(SlotWindow<T> other, MissingValuePolicy policy)
    {
        if (policy == MissingValuePolicy.Intersection &&
            IsDense && other.IsDense &&
            _startSlot == other._startSlot &&
            _length == other._length)
        {
            var result = CreateInitialized(_startSlot, _length);
            NumericSpanOperations<T>.Add(
                _values.AsSpan(0, _length),
                other._values.AsSpan(0, _length),
                result._values.AsSpan(0, _length));
            result.MarkAllPresent();
            return result;
        }

        return Combine(other, policy, static (left, right) => left + right);
    }

    public SlotWindow<T> Combine(
        SlotWindow<T> other,
        MissingValuePolicy policy,
        Func<T, T, T> operation)
    {
        var start = policy == MissingValuePolicy.Intersection
            ? Math.Max(_startSlot, other._startSlot)
            : Math.Min(_startSlot, other._startSlot);

        var endExclusive = policy == MissingValuePolicy.Intersection
            ? Math.Min(_startSlot + _length, other._startSlot + other._length)
            : Math.Max(_startSlot + _length, other._startSlot + other._length);

        if (endExclusive <= start)
            return new SlotWindow<T>();

        var result = CreateInitialized(start, checked((int)(endExclusive - start)));

        for (var slot = start; slot < endExclusive; slot++)
        {
            var hasLeft = TryGetValue(slot, out var left);
            var hasRight = other.TryGetValue(slot, out var right);

            switch (policy)
            {
                case MissingValuePolicy.Throw when hasLeft != hasRight:
                    throw new InvalidOperationException($"Missing value at slot {slot}.");
                case MissingValuePolicy.Intersection when !(hasLeft && hasRight):
                    continue;
                case MissingValuePolicy.UnionWithZero when !(hasLeft || hasRight):
                    continue;
            }

            result.Set(slot, operation(hasLeft ? left : T.Zero, hasRight ? right : T.Zero));
        }

        return result;
    }

    public SlotWindow<T> Add(T scalar) =>
        Transform(scalar, NumericSpanOperations<T>.AddScalar);

    public SlotWindow<T> Multiply(T scalar) =>
        Transform(scalar, NumericSpanOperations<T>.Multiply);

    public SlotWindow<T> Divide(T scalar) =>
        Transform(scalar, NumericSpanOperations<T>.Divide);

    private delegate void DenseTransform(ReadOnlySpan<T> input, T operand, Span<T> destination);

    private SlotWindow<T> Transform(T operand, DenseTransform transform)
    {
        if (_length == 0)
            return new SlotWindow<T>();

        var result = CreateInitialized(_startSlot, _length);
        transform(_values.AsSpan(0, _length), operand, result._values.AsSpan(0, _length));
        Array.Copy(_presentBits, result._presentBits, (_length + 63) >> 6);
        result._count = _count;
        return result;
    }

    private static SlotWindow<T> CreateInitialized(long startSlot, int length)
    {
        var result = new SlotWindow<T>(length)
        {
            _startSlot = startSlot,
            _length = length
        };

        return result;
    }

    private void MarkAllPresent()
    {
        Array.Fill(_presentBits, ulong.MaxValue);
        if ((_length & 63) != 0)
            _presentBits[^1] = (1UL << (_length & 63)) - 1;

        _count = _length;
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
}
