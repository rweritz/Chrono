using System.Numerics;
using System.Runtime.InteropServices;

namespace Chrono.TimeSeries;

public static class TimeSeriesMath
{
    public static IReadOnlySparseTimeSeries<T> Add<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(IReadOnlySparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformSparseCompatibility(source, static (value, operand) => value * operand, scalar);

    public static IReadOnlySparseTimeSeries<T> Add<T>(IReadOnlySparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformSparseCompatibility(source, static (value, operand) => value + operand, scalar);

    public static IReadOnlySparseTimeSeries<T> Divide<T>(IReadOnlySparseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformSparseCompatibility(source, static (value, operand) => value / operand, scalar);

    public static StepwiseTimeSeries<T> Add<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeStepwise(left, right, policy, static (a, b) => a + b);
    }

    public static StepwiseTimeSeries<T> Subtract<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeStepwise(left, right, policy, static (a, b) => a - b);
    }

    public static StepwiseTimeSeries<T> Multiply<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeStepwise(left, right, policy, static (a, b) => a * b);
    }

    public static StepwiseTimeSeries<T> Divide<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeStepwise(left, right, policy, static (a, b) => a / b);
    }

    public static StepwiseTimeSeries<T> Multiply<T>(IBoundedStepwiseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformStepwise(source, static (value, operand) => value * operand, scalar);

    public static StepwiseTimeSeries<T> Add<T>(IBoundedStepwiseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformStepwise(source, static (value, operand) => value + operand, scalar);

    public static StepwiseTimeSeries<T> Divide<T>(IBoundedStepwiseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformStepwise(source, static (value, operand) => value / operand, scalar);

    public static FixedSlotTimeSeries<T> Add<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);

        if (policy == MissingValuePolicy.Intersection &&
            left.IsDense && right.IsDense &&
            left.StartSlot == right.StartSlot &&
            left.SlotLength == right.SlotLength)
        {
            var result = new FixedSlotTimeSeries<T>(left.Period, left.SlotLength);
            result.InitializeWindow(left.StartSlot, left.SlotLength);
            AddDense(left.ValueSpan, right.ValueSpan, result.MutableValueSpan);
            for (var i = 0; i < left.SlotLength; i++)
                result.MarkPresentAt(i);
            return result;
        }

        return MergeRegular(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        FixedSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        SortedArrayTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        FixedSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        DynamicSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        SortedArrayTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        DynamicSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a + b);
    }

    public static FixedSlotTimeSeries<T> Subtract<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        FixedSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        SortedArrayTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        FixedSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        DynamicSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        SortedArrayTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        DynamicSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static FixedSlotTimeSeries<T> Multiply<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        FixedSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        SortedArrayTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        FixedSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        DynamicSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        SortedArrayTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        DynamicSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a * b);
    }

    public static FixedSlotTimeSeries<T> Divide<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeRegular(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        FixedSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        SortedArrayTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        FixedSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        DynamicSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        SortedArrayTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        DynamicSlotTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a / b);
    }

    public static FixedSlotTimeSeries<T> Multiply<T>(FixedSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new FixedSlotTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        MultiplyDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static FixedSlotTimeSeries<T> Add<T>(FixedSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new FixedSlotTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        AddScalarDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static FixedSlotTimeSeries<T> Divide<T>(FixedSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new FixedSlotTimeSeries<T>(input.Period, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        DivideDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static DynamicSlotTimeSeries<T> Add<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);

        if (policy == MissingValuePolicy.Intersection &&
            left.IsDense && right.IsDense &&
            left.StartSlot == right.StartSlot &&
            left.SlotLength == right.SlotLength)
        {
            var result = new DynamicSlotTimeSeries<T>(left.Period, AlignMode.Strict, left.SlotLength);
            result.InitializeWindow(left.StartSlot, left.SlotLength);
            AddDense(left.ValueSpan, right.ValueSpan, result.MutableValueSpan);
            for (var i = 0; i < left.SlotLength; i++)
                result.MarkPresentAt(i);
            return result;
        }

        return MergeCalendar(left, right, policy, static (a, b) => a + b);
    }

    public static DynamicSlotTimeSeries<T> Subtract<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeCalendar(left, right, policy, static (a, b) => a - b);
    }

    public static DynamicSlotTimeSeries<T> Multiply<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeCalendar(left, right, policy, static (a, b) => a * b);
    }

    public static DynamicSlotTimeSeries<T> Divide<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeCalendar(left, right, policy, static (a, b) => a / b);
    }

    public static DynamicSlotTimeSeries<T> Multiply<T>(DynamicSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new DynamicSlotTimeSeries<T>(input.Period, AlignMode.Strict, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        MultiplyDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static DynamicSlotTimeSeries<T> Add<T>(DynamicSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new DynamicSlotTimeSeries<T>(input.Period, AlignMode.Strict, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        AddScalarDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static DynamicSlotTimeSeries<T> Divide<T>(DynamicSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
    {
        var result = new DynamicSlotTimeSeries<T>(input.Period, AlignMode.Strict, input.SlotLength);
        result.InitializeWindow(input.StartSlot, input.SlotLength);
        DivideDense(input.ValueSpan, scalar, result.MutableValueSpan);

        for (var i = 0; i < input.SlotLength; i++)
            if (input.TryGetSlotValue(input.StartSlot + i, out _))
                result.MarkPresentAt(i);

        return result;
    }

    public static SortedArrayTimeSeries<T> Add<T>(
        SortedArrayTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparse(left, right, policy, static (a, b) => a + b);
    }

    public static SortedArrayTimeSeries<T> Subtract<T>(
        SortedArrayTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparse(left, right, policy, static (a, b) => a - b);
    }

    public static SortedArrayTimeSeries<T> Multiply<T>(
        SortedArrayTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparse(left, right, policy, static (a, b) => a * b);
    }

    public static SortedArrayTimeSeries<T> Divide<T>(
        SortedArrayTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparse(left, right, policy, static (a, b) => a / b);
    }

    public static SortedArrayTimeSeries<T> Multiply<T>(SortedArrayTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        MultiplyDense(values, scalar, outValues);
        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    public static SortedArrayTimeSeries<T> Add<T>(SortedArrayTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        AddScalarDense(values, scalar, outValues);
        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    public static SortedArrayTimeSeries<T> Divide<T>(SortedArrayTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        var keys = source.TickKeys;
        var values = source.Values;
        var outKeys = keys.ToArray();
        var outValues = new T[values.Length];

        DivideDense(values, scalar, outValues);
        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(outKeys, outValues, source.Period);
    }

    private static FixedSlotTimeSeries<T> MergeRegular<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
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
            return new FixedSlotTimeSeries<T>(left.Period);

        var result = new FixedSlotTimeSeries<T>(left.Period, checked((int)(endExclusive - start)));
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

    private static SortedArrayTimeSeries<T> MergeSparse<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        using var leftEnumerator = left.GetPoints().GetEnumerator();
        using var rightEnumerator = right.GetPoints().GetEnumerator();

        var hasLeft = leftEnumerator.MoveNext();
        var hasRight = rightEnumerator.MoveNext();
        var points = new List<TimeSeriesPoint<T>>(policy == MissingValuePolicy.Intersection
            ? Math.Min(left.ExplicitPointCount, right.ExplicitPointCount)
            : left.ExplicitPointCount + right.ExplicitPointCount);

        while (hasLeft && hasRight)
        {
            var leftPoint = leftEnumerator.Current;
            var rightPoint = rightEnumerator.Current;

            if (leftPoint.Timestamp == rightPoint.Timestamp)
            {
                points.Add(new TimeSeriesPoint<T>(leftPoint.Timestamp, op(leftPoint.Value, rightPoint.Value)));
                hasLeft = leftEnumerator.MoveNext();
                hasRight = rightEnumerator.MoveNext();
                continue;
            }

            if (leftPoint.Timestamp < rightPoint.Timestamp)
            {
                switch (policy)
                {
                    case MissingValuePolicy.UnionWithZero:
                        points.Add(new TimeSeriesPoint<T>(leftPoint.Timestamp, op(leftPoint.Value, T.Zero)));
                        break;
                    case MissingValuePolicy.Throw:
                        throw new InvalidOperationException($"Timestamp {leftPoint.Timestamp:O} exists in left but not right.");
                }

                hasLeft = leftEnumerator.MoveNext();
                continue;
            }

            switch (policy)
            {
                case MissingValuePolicy.UnionWithZero:
                    points.Add(new TimeSeriesPoint<T>(rightPoint.Timestamp, op(T.Zero, rightPoint.Value)));
                    break;
                case MissingValuePolicy.Throw:
                    throw new InvalidOperationException($"Timestamp {rightPoint.Timestamp:O} exists in right but not left.");
            }

            hasRight = rightEnumerator.MoveNext();
        }

        if (policy == MissingValuePolicy.UnionWithZero)
        {
            while (hasLeft)
            {
                var leftPoint = leftEnumerator.Current;
                points.Add(new TimeSeriesPoint<T>(leftPoint.Timestamp, op(leftPoint.Value, T.Zero)));
                hasLeft = leftEnumerator.MoveNext();
            }

            while (hasRight)
            {
                var rightPoint = rightEnumerator.Current;
                points.Add(new TimeSeriesPoint<T>(rightPoint.Timestamp, op(T.Zero, rightPoint.Value)));
                hasRight = rightEnumerator.MoveNext();
            }
        }

        return CreateSparseResult(left.Period, points);
    }

    private static SortedArrayTimeSeries<T> MergeSparse<T>(
        SortedArrayTimeSeries<T> left,
        SortedArrayTimeSeries<T> right,
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

        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(outK.AsSpan(0, n), outV.AsSpan(0, n), left.Period);
    }

    private static SortedArrayTimeSeries<T> TransformSparse<T>(
        IReadOnlySparseTimeSeries<T> source,
        Func<T, T, T> op,
        T operand)
        where T : struct, INumber<T>
    {
        var points = new List<TimeSeriesPoint<T>>(source.ExplicitPointCount);
        foreach (var point in source.GetPoints())
            points.Add(new TimeSeriesPoint<T>(point.Timestamp, op(point.Value, operand)));

        return CreateSparseResult(source.Period, points);
    }

    private static IReadOnlySparseTimeSeries<T> MergeSparseCompatibility<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
        => MergeSparse(left, right, policy, op);

    private static IReadOnlySparseTimeSeries<T> TransformSparseCompatibility<T>(
        IReadOnlySparseTimeSeries<T> source,
        Func<T, T, T> op,
        T operand)
        where T : struct, INumber<T>
        => TransformSparse(source, op, operand);

    private static StepwiseTimeSeries<T> MergeStepwise<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var leftStart = CalendarSlotMath.ToSlot(left.LogicalRangeStart, left.Period);
        var leftEnd = CalendarSlotMath.ToSlot(left.LogicalRangeEnd, left.Period);
        var rightStart = CalendarSlotMath.ToSlot(right.LogicalRangeStart, right.Period);
        var rightEnd = CalendarSlotMath.ToSlot(right.LogicalRangeEnd, right.Period);

        long resultStart;
        long resultEnd;

        switch (policy)
        {
            case MissingValuePolicy.Intersection:
                resultStart = Math.Max(leftStart, rightStart);
                resultEnd = Math.Min(leftEnd, rightEnd);
                if (resultEnd < resultStart)
                    throw new InvalidOperationException("Bounded stepwise intersection requires an overlapping logical range.");
                 break;
            case MissingValuePolicy.Throw:
                if (leftStart != rightStart || leftEnd != rightEnd)
                    throw new InvalidOperationException("Bounded stepwise throw semantics require identical logical ranges.");

                resultStart = leftStart;
                resultEnd = leftEnd;
                break;
            case MissingValuePolicy.UnionWithZero:
                resultStart = Math.Min(leftStart, rightStart);
                resultEnd = Math.Max(leftEnd, rightEnd);
                if (Math.Max(leftStart, rightStart) - Math.Min(leftEnd, rightEnd) > 1)
                {
                    throw new InvalidOperationException(
                        "Bounded stepwise union-with-zero requires overlapping or contiguous logical ranges.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(policy), policy, null);
        }

        return BuildStepwiseResult(left.Period, resultStart, resultEnd, slot =>
        {
            var timestamp = CalendarSlotMath.FromSlot(slot, left.Period);
            var hasLeft = left.TryGetValue(timestamp, out var leftValue);
            var hasRight = right.TryGetValue(timestamp, out var rightValue);

            return op(hasLeft ? leftValue : T.Zero, hasRight ? rightValue : T.Zero);
        });
    }

    private static StepwiseTimeSeries<T> TransformStepwise<T>(
        IBoundedStepwiseTimeSeries<T> source,
        Func<T, T, T> op,
        T operand)
        where T : struct, INumber<T>
    {
        var startSlot = CalendarSlotMath.ToSlot(source.LogicalRangeStart, source.Period);
        var endSlot = CalendarSlotMath.ToSlot(source.LogicalRangeEnd, source.Period);

        return BuildStepwiseResult(source.Period, startSlot, endSlot, slot =>
        {
            var timestamp = CalendarSlotMath.FromSlot(slot, source.Period);
            return op(source[timestamp], operand);
        });
    }

    private static StepwiseTimeSeries<T> BuildStepwiseResult<T>(
        Period period,
        long startSlot,
        long endSlot,
        Func<long, T> valueFactory)
        where T : struct, INumber<T>
    {
        var start = CalendarSlotMath.FromSlot(startSlot, period);
        var end = CalendarSlotMath.FromSlot(endSlot, period);
        var currentValue = valueFactory(startSlot);
        var result = new StepwiseTimeSeries<T>(period, start, end, currentValue);
        var runStart = startSlot;

        for (var slot = startSlot + 1; slot <= endSlot; slot++)
        {
            var value = valueFactory(slot);
            if (EqualityComparer<T>.Default.Equals(value, currentValue))
                continue;

            if (runStart > startSlot)
            {
                result.SetSegment(
                    CalendarSlotMath.FromSlot(runStart, period),
                    CalendarSlotMath.FromSlot(slot, period),
                    currentValue);
            }

            runStart = slot;
            currentValue = value;
        }

        if (runStart > startSlot)
        {
            result.SetSegment(
                CalendarSlotMath.FromSlot(runStart, period),
                CalendarSlotMath.FromSlot(endSlot + 1, period),
                currentValue);
        }

        return result;
    }

    private static SortedArrayTimeSeries<T> CreateSparseResult<T>(Period period, IReadOnlyList<TimeSeriesPoint<T>> points)
        where T : struct, INumber<T>
    {
        var keys = new long[points.Count];
        var values = new T[points.Count];

        for (var i = 0; i < points.Count; i++)
        {
            keys[i] = points[i].Timestamp.UtcTicks;
            values[i] = points[i].Value;
        }

        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(keys, values, period);
    }

    private static DynamicSlotTimeSeries<T> MergeCalendar<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
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
            return new DynamicSlotTimeSeries<T>(left.Period);

        var result = new DynamicSlotTimeSeries<T>(left.Period, AlignMode.Strict, checked((int)(endExclusive - start)));
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

    private static void EnsureCompatible<T>(IReadOnlyTimeSeries<T> left, IReadOnlyTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Periods must match.");
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

    private static void EnsureCompatible<T>(FixedSlotTimeSeries<T> left, FixedSlotTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Series periods must match.");
    }

    private static void EnsureCompatible<T>(DynamicSlotTimeSeries<T> left, DynamicSlotTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Series periods must match.");
    }

    private static void EnsureCompatible<T>(SortedArrayTimeSeries<T> left, SortedArrayTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Series periods must match.");
    }
}
