using System.Numerics;

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

    public static bool TryAddAsDynamicSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out DynamicSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.DynamicSlot,
            static (a, b) => a + b, out result);

    public static bool TryAddAsFixedSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out FixedSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.FixedSlot,
            static (a, b) => a + b, out result);

    public static bool TryAddAsBoundedStepwiseTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out StepwiseTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsBoundedStepwiseTarget(left, right, policy, static (a, b) => a + b, out result);

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        IReadOnlySparseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseCompatibility(left, right, policy, static (a, b) => a - b);
    }

    public static bool TrySubtractAsFixedSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out FixedSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.FixedSlot,
            static (a, b) => a - b, out result);

    public static bool TrySubtractAsDynamicSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out DynamicSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.DynamicSlot,
            static (a, b) => a - b, out result);

    public static bool TrySubtractAsBoundedStepwiseTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out StepwiseTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsBoundedStepwiseTarget(left, right, policy, static (a, b) => a - b, out result);

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        IReadOnlySparseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseWithBoundedStepwise(left, right, policy, static (a, b) => a - b);
    }

    public static IReadOnlySparseTimeSeries<T> Subtract<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeBoundedStepwiseWithSparse(left, right, policy, static (a, b) => a - b);
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

    public static bool TryMultiplyAsFixedSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out FixedSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.FixedSlot,
            static (a, b) => a * b, out result);

    public static bool TryMultiplyAsDynamicSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out DynamicSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.DynamicSlot,
            static (a, b) => a * b, out result);

    public static bool TryMultiplyAsBoundedStepwiseTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out StepwiseTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsBoundedStepwiseTarget(left, right, policy, static (a, b) => a * b, out result);

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        IReadOnlySparseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseWithBoundedStepwise(left, right, policy, static (a, b) => a * b);
    }

    public static IReadOnlySparseTimeSeries<T> Multiply<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeBoundedStepwiseWithSparse(left, right, policy, static (a, b) => a * b);
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

    public static bool TryDivideAsFixedSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out FixedSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.FixedSlot,
            static (a, b) => a / b, out result);

    public static bool TryDivideAsDynamicSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out DynamicSlotTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsSparseTarget(left, right, policy, TimeSeriesResultTarget.DynamicSlot,
            static (a, b) => a / b, out result);

    public static bool TryDivideAsBoundedStepwiseTimeSeries<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        out StepwiseTimeSeries<T>? result,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
        => TryBinaryAsBoundedStepwiseTarget(left, right, policy, static (a, b) => a / b, out result);

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        IReadOnlySparseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseWithBoundedStepwise(left, right, policy, static (a, b) => a / b);
    }

    public static IReadOnlySparseTimeSeries<T> Divide<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeBoundedStepwiseWithSparse(left, right, policy, static (a, b) => a / b);
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

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        IReadOnlySparseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeSparseWithBoundedStepwise(left, right, policy, static (a, b) => a + b);
    }

    public static IReadOnlySparseTimeSeries<T> Add<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IReadOnlySparseTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return MergeBoundedStepwiseWithSparse(left, right, policy, static (a, b) => a + b);
    }

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
    {
        EnsureCompatibilityAdapter(source, TimeSeriesResultAdapter.BoundedStepwise);
        return TransformStepwise(source, static (value, operand) => value * operand, scalar);
    }

    public static StepwiseTimeSeries<T> Add<T>(IBoundedStepwiseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        EnsureCompatibilityAdapter(source, TimeSeriesResultAdapter.BoundedStepwise);
        return TransformStepwise(source, static (value, operand) => value + operand, scalar);
    }

    public static StepwiseTimeSeries<T> Divide<T>(IBoundedStepwiseTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
    {
        EnsureCompatibilityAdapter(source, TimeSeriesResultAdapter.BoundedStepwise);
        return TransformStepwise(source, static (value, operand) => value / operand, scalar);
    }

    public static FixedSlotTimeSeries<T> Add<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return left.AddSlots(right, policy);
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
        => TransformExactScalar(input, scalar, ScalarOperation.Multiply);

    public static FixedSlotTimeSeries<T> Add<T>(FixedSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(input, scalar, ScalarOperation.Add);

    public static FixedSlotTimeSeries<T> Divide<T>(FixedSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(input, scalar, ScalarOperation.Divide);

    public static DynamicSlotTimeSeries<T> Add<T>(
        DynamicSlotTimeSeries<T> left,
        DynamicSlotTimeSeries<T> right,
        MissingValuePolicy policy = MissingValuePolicy.Intersection)
        where T : struct, INumber<T>
    {
        EnsureCompatible(left, right);
        return left.AddSlots(right, policy);
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
        => TransformExactScalar(input, scalar, ScalarOperation.Multiply);

    public static DynamicSlotTimeSeries<T> Add<T>(DynamicSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(input, scalar, ScalarOperation.Add);

    public static DynamicSlotTimeSeries<T> Divide<T>(DynamicSlotTimeSeries<T> input, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(input, scalar, ScalarOperation.Divide);

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
        => TransformExactScalar(source, scalar, ScalarOperation.Multiply);

    public static SortedArrayTimeSeries<T> Add<T>(SortedArrayTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(source, scalar, ScalarOperation.Add);

    public static SortedArrayTimeSeries<T> Divide<T>(SortedArrayTimeSeries<T> source, T scalar)
        where T : struct, INumber<T>
        => TransformExactScalar(source, scalar, ScalarOperation.Divide);

    private static FixedSlotTimeSeries<T> TransformExactScalar<T>(
        FixedSlotTimeSeries<T> source,
        T scalar,
        ScalarOperation operation)
        where T : struct, INumber<T>
    {
        TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(
            source, source.Period, TimeSeriesResultAdapter.FixedSlot);

        return operation switch
        {
            ScalarOperation.Multiply => source.MultiplyScalar(scalar),
            ScalarOperation.Add => source.AddScalar(scalar),
            ScalarOperation.Divide => source.DivideScalar(scalar),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static DynamicSlotTimeSeries<T> TransformExactScalar<T>(
        DynamicSlotTimeSeries<T> source,
        T scalar,
        ScalarOperation operation)
        where T : struct, INumber<T>
    {
        TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(
            source, source.Period, TimeSeriesResultAdapter.DynamicSlot);

        return operation switch
        {
            ScalarOperation.Multiply => source.MultiplyScalar(scalar),
            ScalarOperation.Add => source.AddScalar(scalar),
            ScalarOperation.Divide => source.DivideScalar(scalar),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static SortedArrayTimeSeries<T> TransformExactScalar<T>(
        SortedArrayTimeSeries<T> source,
        T scalar,
        ScalarOperation operation)
        where T : struct, INumber<T>
    {
        TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(
            source, source.Period, TimeSeriesResultAdapter.SortedArray);

        var values = source.Values;
        var outValues = new T[values.Length];
        switch (operation)
        {
            case ScalarOperation.Multiply:
                NumericSpanOperations<T>.Multiply(values, scalar, outValues);
                break;
            case ScalarOperation.Add:
                NumericSpanOperations<T>.AddScalar(values, scalar, outValues);
                break;
            case ScalarOperation.Divide:
                NumericSpanOperations<T>.Divide(values, scalar, outValues);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(
            source.TickKeys.ToArray(), outValues, source.Period);
    }

    private static FixedSlotTimeSeries<T> MergeRegular<T>(
        FixedSlotTimeSeries<T> left,
        FixedSlotTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
        => left.CombineSlots(right, policy, op);

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
    {
        var decision = TimeSeriesOperationPolicy.DecideCompatibility(left, right);
        return TimeSeriesOperationPolicy.AdaptSparse(MergeSparse(left, right, policy, op), decision);
    }

    private static IReadOnlySparseTimeSeries<T> TransformSparseCompatibility<T>(
        IReadOnlySparseTimeSeries<T> source,
        Func<T, T, T> op,
        T operand)
        where T : struct, INumber<T>
    {
        var decision = TimeSeriesOperationPolicy.DecideCompatibility(source, source.Period);
        return TimeSeriesOperationPolicy.AdaptSparse(TransformSparse(source, op, operand), decision);
    }

    private static IReadOnlySparseTimeSeries<T> MergeSparseWithBoundedStepwise<T>(
        IReadOnlySparseTimeSeries<T> sparse,
        IBoundedStepwiseTimeSeries<T> stepwise,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var points = new List<TimeSeriesPoint<T>>(sparse.ExplicitPointCount);
        foreach (var point in sparse.GetPoints())
        {
            var hasStepwise = stepwise.TryGetValue(point.Timestamp, out var stepwiseValue);
            switch (policy)
            {
                case MissingValuePolicy.Intersection when !hasStepwise:
                    continue;
                case MissingValuePolicy.Throw when !hasStepwise:
                    throw new InvalidOperationException(
                        $"Timestamp {point.Timestamp:O} exists in sparse input but not bounded stepwise logical range.");
            }

            points.Add(new TimeSeriesPoint<T>(point.Timestamp, op(point.Value, hasStepwise ? stepwiseValue : T.Zero)));
        }

        var compatibility = TimeSeriesOperationPolicy.DecideCompatibility(sparse, stepwise);
        return TimeSeriesOperationPolicy.AdaptSparse(CreateSparseResult(sparse.Period, points), compatibility);
    }

    private static IReadOnlySparseTimeSeries<T> MergeBoundedStepwiseWithSparse<T>(
        IBoundedStepwiseTimeSeries<T> stepwise,
        IReadOnlySparseTimeSeries<T> sparse,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var points = new List<TimeSeriesPoint<T>>(sparse.ExplicitPointCount);
        foreach (var point in sparse.GetPoints())
        {
            var hasStepwise = stepwise.TryGetValue(point.Timestamp, out var stepwiseValue);
            switch (policy)
            {
                case MissingValuePolicy.Intersection when !hasStepwise:
                    continue;
                case MissingValuePolicy.Throw when !hasStepwise:
                    throw new InvalidOperationException(
                        $"Timestamp {point.Timestamp:O} exists in sparse input but not bounded stepwise logical range.");
            }

            points.Add(new TimeSeriesPoint<T>(point.Timestamp, op(hasStepwise ? stepwiseValue : T.Zero, point.Value)));
        }

        var compatibility = TimeSeriesOperationPolicy.DecideCompatibility(stepwise, sparse);
        return TimeSeriesOperationPolicy.AdaptSparse(CreateSparseResult(stepwise.Period, points), compatibility);
    }

    private static StepwiseTimeSeries<T> MergeStepwise<T>(
        IBoundedStepwiseTimeSeries<T> left,
        IBoundedStepwiseTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        var leftStart = PeriodGeometry.ToSlot(left.LogicalRangeStart, left.Period);
        var leftEnd = PeriodGeometry.ToSlot(left.LogicalRangeEnd, left.Period);
        var rightStart = PeriodGeometry.ToSlot(right.LogicalRangeStart, right.Period);
        var rightEnd = PeriodGeometry.ToSlot(right.LogicalRangeEnd, right.Period);

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
            var timestamp = PeriodGeometry.FromSlot(slot, left.Period);
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
        var startSlot = PeriodGeometry.ToSlot(source.LogicalRangeStart, source.Period);
        var endSlot = PeriodGeometry.ToSlot(source.LogicalRangeEnd, source.Period);

        return BuildStepwiseResult(source.Period, startSlot, endSlot, slot =>
        {
            var timestamp = PeriodGeometry.FromSlot(slot, source.Period);
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
        var start = PeriodGeometry.FromSlot(startSlot, period);
        var end = PeriodGeometry.FromSlot(endSlot, period);
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
                    PeriodGeometry.FromSlot(runStart, period),
                    PeriodGeometry.FromSlot(slot, period),
                    currentValue);
            }

            runStart = slot;
            currentValue = value;
        }

        if (runStart > startSlot)
        {
            result.SetSegment(
                PeriodGeometry.FromSlot(runStart, period),
                PeriodGeometry.FromSlot(endSlot + 1, period),
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
        => left.CombineSlots(right, policy, op);

    private static bool TryBinaryAsSparseTarget<T, TResult>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        MissingValuePolicy policy,
        TimeSeriesResultTarget target,
        Func<T, T, T> op,
        out TResult? result)
        where T : struct, INumber<T>
        where TResult : class, IReadOnlyTimeSeries<T>
    {
        var decision = TimeSeriesOperationPolicy.DecideSpecialization(left, right, target);
        if (!decision.IsValid)
        {
            result = null;
            return false;
        }

        result = (TResult)TimeSeriesOperationPolicy.AdaptSparse(
            ExecuteSparseBinaryOperation(left, right, policy, op), decision);
        return true;
    }

    private static bool TryBinaryAsBoundedStepwiseTarget<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op,
        out StepwiseTimeSeries<T>? result)
        where T : struct, INumber<T>
    {
        var decision = TimeSeriesOperationPolicy.DecideSpecialization(left, right, TimeSeriesResultTarget.BoundedStepwise);
        if (!decision.IsValid)
        {
            result = null;
            return false;
        }

        result = ExecuteBoundedStepwiseBinaryOperation(left, right, policy, op);
        return true;
    }

    private static SortedArrayTimeSeries<T> ExecuteSparseBinaryOperation<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
        => (left, right) switch
        {
            (IReadOnlySparseTimeSeries<T> sparseLeft, IReadOnlySparseTimeSeries<T> sparseRight) =>
                MergeSparse(sparseLeft, sparseRight, policy, op),
            (IReadOnlySparseTimeSeries<T> sparseLeft, IBoundedStepwiseTimeSeries<T> stepwiseRight) =>
                (SortedArrayTimeSeries<T>)MergeSparseWithBoundedStepwise(sparseLeft, stepwiseRight, policy, op),
            (IBoundedStepwiseTimeSeries<T> stepwiseLeft, IReadOnlySparseTimeSeries<T> sparseRight) =>
                (SortedArrayTimeSeries<T>)MergeBoundedStepwiseWithSparse(stepwiseLeft, sparseRight, policy, op),
            _ => throw new InvalidOperationException("Sparse specialization requires sparse arithmetic semantics.")
        };

    private static StepwiseTimeSeries<T> ExecuteBoundedStepwiseBinaryOperation<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        MissingValuePolicy policy,
        Func<T, T, T> op)
        where T : struct, INumber<T>
    {
        if (left is not IBoundedStepwiseTimeSeries<T> stepwiseLeft || right is not IBoundedStepwiseTimeSeries<T> stepwiseRight)
            throw new InvalidOperationException("Bounded stepwise specialization requires bounded stepwise arithmetic semantics.");

        return MergeStepwise(stepwiseLeft, stepwiseRight, policy, op);
    }

    private static void EnsureCompatible<T>(IReadOnlyTimeSeries<T> left, IReadOnlyTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        var decision = TimeSeriesOperationPolicy.DecideCompatibility(left, right);
        var expectedAdapter = decision.SemanticFamily == TimeSeriesSemanticFamily.BoundedStepwise
            ? TimeSeriesResultAdapter.BoundedStepwise
            : TimeSeriesResultAdapter.SortedArray;

        if (decision.Adapter != expectedAdapter)
            throw new NotSupportedException("The selected compatibility result cannot represent this operation.");
    }

    private static void EnsureCompatibilityAdapter<T>(
        IReadOnlyTimeSeries<T> source,
        TimeSeriesResultAdapter expectedAdapter)
        where T : struct, INumber<T>
    {
        var decision = TimeSeriesOperationPolicy.DecideCompatibility(source, source.Period);
        if (decision.Adapter != expectedAdapter)
            throw new NotSupportedException("The selected compatibility result cannot represent this operation.");
    }

    private static void EnsureCompatible<T>(FixedSlotTimeSeries<T> left, FixedSlotTimeSeries<T> right)
        where T : struct, INumber<T>
        => TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(left, right, TimeSeriesResultAdapter.FixedSlot);

    private static void EnsureCompatible<T>(DynamicSlotTimeSeries<T> left, DynamicSlotTimeSeries<T> right)
        where T : struct, INumber<T>
        => TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(left, right, TimeSeriesResultAdapter.DynamicSlot);

    private static void EnsureCompatible<T>(SortedArrayTimeSeries<T> left, SortedArrayTimeSeries<T> right)
        where T : struct, INumber<T>
        => TimeSeriesOperationPolicy.EnsureExactConcreteAdapter(left, right, TimeSeriesResultAdapter.SortedArray);

    private enum ScalarOperation
    {
        Multiply,
        Add,
        Divide
    }

}
