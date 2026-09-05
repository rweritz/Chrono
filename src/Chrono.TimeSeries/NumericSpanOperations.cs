using System.Numerics;
using System.Runtime.InteropServices;

namespace Chrono.TimeSeries;

internal static class NumericSpanOperations<T>
    where T : struct, INumber<T>
{
    public static void Add(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> destination)
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

    public static void AddScalar(ReadOnlySpan<T> input, T scalar, Span<T> destination)
    {
        for (var i = 0; i < input.Length; i++)
            destination[i] = input[i] + scalar;
    }

    public static void Multiply(ReadOnlySpan<T> input, T scalar, Span<T> destination)
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

    public static void Divide(ReadOnlySpan<T> input, T scalar, Span<T> destination)
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
            var vectorScalar = new Vector<double>(scalar);
            for (; i <= input.Length - width; i += width)
            {
                (new Vector<double>(input.Slice(i, width)) * vectorScalar)
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
            var vectorScalar = new Vector<int>(scalar);
            for (; i <= input.Length - width; i += width)
            {
                (new Vector<int>(input.Slice(i, width)) * vectorScalar)
                    .CopyTo(destination.Slice(i, width));
            }
        }

        for (; i < input.Length; i++)
            destination[i] = input[i] * scalar;
    }
}
