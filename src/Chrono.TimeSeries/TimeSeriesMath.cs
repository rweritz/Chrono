using System.Numerics;
using System.Runtime.InteropServices;

namespace Chrono.TimeSeries;

public static class TimeSeriesMath
{
    public static RegularTimeSeries<T> Add<T>(
        RegularTimeSeries<T> left,
        RegularTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);

        if (policy == MissingValuePolicy.Intersection &&
            left.IsDense && right.IsDense &&
            left.StartSlot == right.StartSlot &&
            left.SlotLength == right.SlotLength)
        {
            var result = new RegularTimeSeries<T>(left.Period, left.SlotLength);
            result.InitializeWindow(left.StartSlot, left.SlotLength);
            AddDense(left.ValueSpan, right.ValueSpan, result.MutableValueSpan);
            for (var i = 0; i < left.SlotLength; i++)
                result.MarkPresentAt(i);
            return result;
        }

        return MergeRegular(left, right, policy, static (a, b) => a + b);
    }

    public static RegularTimeSeries<T> Subtract<T>(
        RegularTimeSeries<T> left,
        RegularTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a - b);
    }

    public static RegularTimeSeries<T> Multiply<T>(
        RegularTimeSeries<T> left,
        RegularTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a * b);
    }

    public static RegularTimeSeries<T> Divide<T>(
        RegularTimeSeries<T> left,
        RegularTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a / b);
    }

    public static RegularTimeSeries<T> Multiply<T>(RegularTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new RegularTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        MultiplyDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static RegularTimeSeries<T> Add<T>(RegularTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new RegularTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        AddScalarDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static RegularTimeSeries<T> Divide<T>(RegularTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new RegularTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        DivideDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static SparseTimeSeries<T> Add<T>(
        SparseTimeSeries<T> left,
        SparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => MergeSparse(left, right, policy, static (a, b) => a + b);

    public static SparseTimeSeries<T> Subtract<T>(
        SparseTimeSeries<T> left,
        SparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => MergeSparse(left, right, policy, static (a, b) => a - b);

    public static SparseTimeSeries<T> Multiply<T>(
        SparseTimeSeries<T> left,
        SparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => MergeSparse(left, right, policy, static (a, b) => a * b);

    public static SparseTimeSeries<T> Divide<T>(
        SparseTimeSeries<T> left,
        SparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => MergeSparse(left, right, policy, static (a, b) => a / b);

    public static SparseTimeSeries<T> Multiply<T>(SparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        MultiplyDense(values, scalar, outValues);
        return SparseTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    public static SparseTimeSeries<T> Add<T>(SparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        AddScalarDense(values, scalar, outValues);
        return SparseTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    public static SparseTimeSeries<T> Divide<T>(SparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        DivideDense(values, scalar, outValues);
        return SparseTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    private static RegularTimeSeries<T> MergeRegular<T>(
        RegularTimeSeries<T> left,
        RegularTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var start = policy == MissingValuePolicy.Intersection
            ? Math.Max(left.StartSlot, right.StartSlot)
            : Math.Min(left.StartSlot, right.StartSlot);

        var endExclusive = policy == MissingValuePolicy.Intersection
            ? Math.Min(left.StartSlot + left.SlotLength, right.StartSlot + right.SlotLength)
            : Math.Max(left.StartSlot + left.SlotLength, right.StartSlot + right.SlotLength);

        if (endExclusive <= start)
            return new RegularTimeSeries<T>(left.Period);

        var result = new RegularTimeSeries<T>(left.Period, checked((int)(endExclusive - start)));
        result.InitializeWindow(start, checked((int)(endExclusive - start)));

        for (var slot = start; slot < endExclusive; slot++)
        {
            var hasLeft = left.TryGetSlotValue(slot, out var lv);
            var hasRight = right.TryGetSlotValue(slot, out var rv);

            switch (policy)
            {
                case MissingValuePolicy.Throw when hasLeft != hasRight:
                    throw new InvalidOperationException($"Missing value at slot {slot}.");
                case MissingValuePolicy.Intersection when !(hasLeft && hasRight):
                    continue;
                case MissingValuePolicy.UnionWithZero when !(hasLeft || hasRight):
                    continue;
            }

            var index = checked((int)(slot - start));
            result.MutableValueSpan[index] = op(hasLeft ? lv : T.Zero, hasRight ? rv : T.Zero);
            result.MarkPresentAt(index);
        }

        return result;
    }

    private static SparseTimeSeries<T> MergeSparse<T>(
        SparseTimeSeries<T> left,
        SparseTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var lk = left.TickKeys;
        var lv = left.Values;
        var rk = right.TickKeys;
        var rv = right.Values;

        var maxLen = policy == MissingValuePolicy.Intersection
            ? Math.Min(lk.Length, rk.Length)
            : lk.Length + rk.Length;

        var outK = new long[maxLen];
        var outV = new T[maxLen];
        var n = 0;
        var li = 0;
        var ri = 0;

        while (li < lk.Length && ri < rk.Length)
        {
            var lt = lk[li];
            var rt = rk[ri];
            if (lt == rt)
            {
                outK[n] = lt;
                outV[n] = op(lv[li], rv[ri]);
                n++;
                li++;
                ri++;
                continue;
            }

            if (lt < rt)
            {
                if (policy == MissingValuePolicy.UnionWithZero)
                {
                    outK[n] = lt;
                    outV[n] = op(lv[li], T.Zero);
                    n++;
                }
                else if (policy == MissingValuePolicy.Throw)
                {
                    throw new InvalidOperationException($"Tick {lt} exists in left but not right.");
                }

                li++;
                continue;
            }

            if (policy == MissingValuePolicy.UnionWithZero)
            {
                outK[n] = rt;
                outV[n] = op(T.Zero, rv[ri]);
                n++;
            }
            else if (policy == MissingValuePolicy.Throw)
            {
                throw new InvalidOperationException($"Tick {rt} exists in right but not left.");
            }

            ri++;
        }

        if (policy == MissingValuePolicy.UnionWithZero)
        {
            while (li < lk.Length)
            {
                outK[n] = lk[li];
                outV[n] = op(lv[li], T.Zero);
                n++;
                li++;
            }

            while (ri < rk.Length)
            {
                outK[n] = rk[ri];
                outV[n] = op(T.Zero, rv[ri]);
                n++;
                ri++;
            }
        }

        return SparseTimeSeries<T>.CreateFromSortedRaw(outK.AsSpan(0, n), outV.AsSpan(0, n), left.Period);
    }

    private static void AddDense<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> destination)
        where T : struct, INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            AddDouble(
                MemoryMarshal.Cast<T, double>(left),
                MemoryMarshal.Cast<T, double>(right),
                MemoryMarshal.Cast<T, double>(destination));
            return;
        }

        if (typeof(T) == typeof(int))
        {
            AddInt32(
                MemoryMarshal.Cast<T, int>(left),
                MemoryMarshal.Cast<T, int>(right),
                MemoryMarshal.Cast<T, int>(destination));
            return;
        }

        for (var i = 0; i < left.Length; i++)
            destination[i] = left[i] + right[i];
    }

    private static void AddScalarDense<T>(ReadOnlySpan<T> input, T scalar, Span<T> destination)
        where T : struct, INumber<T>
    {
        for (var i = 0; i < input.Length; i++)
            destination[i] = input[i] + scalar;
    }

    private static void MultiplyDense<T>(ReadOnlySpan<T> input, T scalar, Span<T> destination)
        where T : struct, INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            MultiplyDouble(
                MemoryMarshal.Cast<T, double>(input),
                double.CreateChecked(scalar),
                MemoryMarshal.Cast<T, double>(destination));
            return;
        }

        if (typeof(T) == typeof(int))
        {
            MultiplyInt32(
                MemoryMarshal.Cast<T, int>(input),
                int.CreateChecked(scalar),
                MemoryMarshal.Cast<T, int>(destination));
            return;
        }

        for (var i = 0; i < input.Length; i++)
            destination[i] = input[i] * scalar;
    }

    private static void DivideDense<T>(ReadOnlySpan<T> input, T scalar, Span<T> destination)
        where T : struct, INumber<T>
    {
        for (var i = 0; i < input.Length; i++)
            destination[i] = input[i] / scalar;
    }

    private static void AddDouble(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> destination)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var width = Vector<double>.Count;
            for (; i <= left.Length - width; i += width)
            {
                (new Vector<double>(left.Slice(i, width)) +
                 new Vector<double>(right.Slice(i, width)))
                    .CopyTo(destination.Slice(i, width));
            }
        }

        for (; i < left.Length; i++)
            destination[i] = left[i] + right[i];
    }

    private static void MultiplyDouble(ReadOnlySpan<double> input, double scalar, Span<double> destination)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var width = Vector<double>.Count;
            var sv = new Vector<double>(scalar);
            for (; i <= input.Length - width; i += width)
            {
                (new Vector<double>(input.Slice(i, width)) * sv)
                    .CopyTo(destination.Slice(i, width));
            }
        }

        for (; i < input.Length; i++)
            destination[i] = input[i] * scalar;
    }

    private static void AddInt32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> destination)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var width = Vector<int>.Count;
            for (; i <= left.Length - width; i += width)
            {
                (new Vector<int>(left.Slice(i, width)) +
                 new Vector<int>(right.Slice(i, width)))
                    .CopyTo(destination.Slice(i, width));
            }
        }

        for (; i < left.Length; i++)
            destination[i] = left[i] + right[i];
    }

    private static void MultiplyInt32(ReadOnlySpan<int> input, int scalar, Span<int> destination)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var width = Vector<int>.Count;
            var sv = new Vector<int>(scalar);
            for (; i <= input.Length - width; i += width)
            {
                (new Vector<int>(input.Slice(i, width)) * sv)
                    .CopyTo(destination.Slice(i, width));
            }
        }

        for (; i < input.Length; i++)
            destination[i] = input[i] * scalar;
    }

    private static void EnsureCompatible<T>(RegularTimeSeries<T> left, RegularTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Series periods must match.");
    }
}
