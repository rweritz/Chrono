using System.Numerics;

namespace Chrono.TimeSeries;

internal enum TimeSeriesSemanticFamily
{
    Sparse,
    BoundedStepwise,
    Mixed
}

internal enum TimeSeriesResultTarget
{
    Compatibility,
    FixedSlot,
    DynamicSlot,
    BoundedStepwise
}

internal enum TimeSeriesResultAdapter
{
    None,
    SortedArray,
    FixedSlot,
    DynamicSlot,
    BoundedStepwise
}

internal readonly record struct TimeSeriesOperationDecision(
    TimeSeriesSemanticFamily SemanticFamily,
    TimeSeriesResultTarget Target,
    TimeSeriesResultAdapter Adapter)
{
    public bool IsValid => Adapter != TimeSeriesResultAdapter.None;
}

/// <summary>
/// Owns the semantic policy shared by arithmetic, aggregation, and explicit resampling.
/// Operation implementations remain responsible only for computing values in the selected shape.
/// </summary>
internal static class TimeSeriesOperationPolicy
{
    public static TimeSeriesOperationDecision Decide(
        TimeSeriesSemanticFamily semanticFamily,
        TimeSeriesResultTarget target,
        Period resultPeriod)
    {
        var adapter = target switch
        {
            TimeSeriesResultTarget.Compatibility => semanticFamily switch
            {
                TimeSeriesSemanticFamily.BoundedStepwise when resultPeriod != Period.NonStandard =>
                    TimeSeriesResultAdapter.BoundedStepwise,
                TimeSeriesSemanticFamily.Sparse or TimeSeriesSemanticFamily.Mixed => TimeSeriesResultAdapter.SortedArray,
                _ => TimeSeriesResultAdapter.None
            },
            TimeSeriesResultTarget.FixedSlot
                when semanticFamily is TimeSeriesSemanticFamily.Sparse or TimeSeriesSemanticFamily.Mixed &&
                     PeriodGeometry.TryGetFixedTicks(resultPeriod, out _) => TimeSeriesResultAdapter.FixedSlot,
            TimeSeriesResultTarget.DynamicSlot
                when semanticFamily is TimeSeriesSemanticFamily.Sparse or TimeSeriesSemanticFamily.Mixed &&
                     resultPeriod != Period.NonStandard => TimeSeriesResultAdapter.DynamicSlot,
            TimeSeriesResultTarget.BoundedStepwise
                when semanticFamily == TimeSeriesSemanticFamily.BoundedStepwise &&
                     resultPeriod != Period.NonStandard => TimeSeriesResultAdapter.BoundedStepwise,
            _ => TimeSeriesResultAdapter.None
        };

        return new TimeSeriesOperationDecision(semanticFamily, target, adapter);
    }

    public static TimeSeriesOperationDecision DecideCompatibility<T>(IReadOnlyTimeSeries<T> source, Period resultPeriod)
        where T : struct, INumber<T>
        => Decide(Classify(source), TimeSeriesResultTarget.Compatibility, resultPeriod);

    public static TimeSeriesOperationDecision DecideCompatibility<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        EnsurePeriodsMatch(left, right);
        return Decide(Classify(left, right), TimeSeriesResultTarget.Compatibility, left.Period);
    }

    public static TimeSeriesResultAdapter SelectExactConcreteAdapter<T>(
        IReadOnlyTimeSeries<T> source,
        Period resultPeriod)
        where T : struct, INumber<T>
    {
        var target = source switch
        {
            FixedSlotTimeSeries<T> => TimeSeriesResultTarget.FixedSlot,
            DynamicSlotTimeSeries<T> => TimeSeriesResultTarget.DynamicSlot,
            SortedArrayTimeSeries<T> => TimeSeriesResultTarget.Compatibility,
            StepwiseTimeSeries<T> => TimeSeriesResultTarget.BoundedStepwise,
            _ => TimeSeriesResultTarget.Compatibility
        };

        return Decide(Classify(source), target, resultPeriod).Adapter;
    }

    public static TimeSeriesResultAdapter SelectExactConcreteAdapter<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        Period resultPeriod)
        where T : struct, INumber<T>
    {
        EnsurePeriodsMatch(left, right);
        return left.GetType() == right.GetType()
            ? SelectExactConcreteAdapter(left, resultPeriod)
            : Decide(Classify(left, right), TimeSeriesResultTarget.Compatibility, resultPeriod).Adapter;
    }

    public static void EnsureExactConcreteAdapter<T>(
        IReadOnlyTimeSeries<T> source,
        Period resultPeriod,
        TimeSeriesResultAdapter expectedAdapter)
        where T : struct, INumber<T>
    {
        var selectedAdapter = SelectExactConcreteAdapter(source, resultPeriod);
        if (selectedAdapter != expectedAdapter)
        {
            throw new NotSupportedException(
                $"{source.GetType().Name} cannot produce a {expectedAdapter} result for period {resultPeriod}.");
        }
    }

    public static void EnsureExactConcreteAdapter<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        TimeSeriesResultAdapter expectedAdapter)
        where T : struct, INumber<T>
    {
        var selectedAdapter = SelectExactConcreteAdapter(left, right, left.Period);
        if (selectedAdapter != expectedAdapter)
            throw new NotSupportedException($"The exact concrete operands cannot produce a {expectedAdapter} result.");
    }

    public static TimeSeriesOperationDecision DecideSpecialization<T>(
        IReadOnlyTimeSeries<T> source,
        Period resultPeriod,
        TimeSeriesResultTarget target)
        where T : struct, INumber<T>
        => Decide(Classify(source), target, resultPeriod);

    public static TimeSeriesOperationDecision DecideSpecialization<T>(
        IReadOnlyTimeSeries<T> left,
        IReadOnlyTimeSeries<T> right,
        TimeSeriesResultTarget target)
        where T : struct, INumber<T>
    {
        EnsurePeriodsMatch(left, right);
        return Decide(Classify(left, right), target, left.Period);
    }

    public static TimeSeriesSemanticFamily Classify<T>(IReadOnlyTimeSeries<T> source)
        where T : struct, INumber<T>
        => source switch
        {
            IReadOnlySparseTimeSeries<T> => TimeSeriesSemanticFamily.Sparse,
            IBoundedStepwiseTimeSeries<T> => TimeSeriesSemanticFamily.BoundedStepwise,
            _ => throw new InvalidOperationException("Unsupported time series family.")
        };

    public static TimeSeriesSemanticFamily Classify<T>(IReadOnlyTimeSeries<T> left, IReadOnlyTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        var leftFamily = Classify(left);
        var rightFamily = Classify(right);
        return leftFamily == rightFamily ? leftFamily : TimeSeriesSemanticFamily.Mixed;
    }

    public static void EnsurePeriodsMatch<T>(IReadOnlyTimeSeries<T> left, IReadOnlyTimeSeries<T> right)
        where T : struct, INumber<T>
    {
        if (left.Period != right.Period)
            throw new InvalidOperationException("Periods must match.");
    }

    public static IReadOnlySparseTimeSeries<T> AdaptSparse<T>(
        IReadOnlySparseTimeSeries<T> source,
        TimeSeriesOperationDecision decision)
        where T : struct, INumber<T>
        => decision.Adapter switch
        {
            TimeSeriesResultAdapter.SortedArray => ToSortedArrayTimeSeries(source),
            TimeSeriesResultAdapter.FixedSlot => ToFixedSlotTimeSeries(source),
            TimeSeriesResultAdapter.DynamicSlot => ToDynamicSlotTimeSeries(source),
            _ => throw new InvalidOperationException($"{decision.Target} does not select a sparse result adapter.")
        };

    public static SortedArrayTimeSeries<T> ToSortedArrayTimeSeries<T>(IReadOnlySparseTimeSeries<T> source)
        where T : struct, INumber<T>
    {
        if (source is SortedArrayTimeSeries<T> sorted)
            return sorted;

        var keys = new long[source.ExplicitPointCount];
        var values = new T[source.ExplicitPointCount];
        var index = 0;

        foreach (var point in source.GetPoints())
        {
            keys[index] = point.Timestamp.UtcTicks;
            values[index] = point.Value;
            index++;
        }

        return SortedArrayTimeSeries<T>.CreateFromSortedRaw(keys, values, source.Period);
    }

    public static DynamicSlotTimeSeries<T> ToDynamicSlotTimeSeries<T>(IReadOnlySparseTimeSeries<T> source)
        where T : struct, INumber<T>
        => new(source.Period, AlignMode.Strict, ToSlotWindow(source));

    public static FixedSlotTimeSeries<T> ToFixedSlotTimeSeries<T>(IReadOnlySparseTimeSeries<T> source)
        where T : struct, INumber<T>
        => new(source.Period, ToSlotWindow(source));

    private static SlotWindow<T> ToSlotWindow<T>(IReadOnlySparseTimeSeries<T> source)
        where T : struct, INumber<T>
    {
        var window = new SlotWindow<T>(source.ExplicitPointCount);
        foreach (var point in source.GetPoints())
            window.Set(PeriodGeometry.ToSlot(point.Timestamp, source.Period), point.Value);

        return window;
    }
}
