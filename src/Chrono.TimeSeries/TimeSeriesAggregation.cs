using System.Numerics;

namespace Chrono.TimeSeries;

public static class TimeSeriesAggregation
{
    public static bool TryAggregateAsFixedSlotTimeSeries<TIn, TOut, TAggregator>(
        IReadOnlyTimeSeries<TIn> source,
        Period targetPeriod,
        out FixedSlotTimeSeries<TOut>? result,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
        => TryAggregateAsSparseTarget<TIn, TOut, TAggregator, FixedSlotTimeSeries<TOut>>(source, targetPeriod,
            TimeSeriesMath.TimeSeriesSpecializationTarget.FixedSlot,
            static sparse => TimeSeriesMath.ToFixedSlotTimeSeries(sparse), out result, aggregator);

    public static bool TryAggregateAsDynamicSlotTimeSeries<TIn, TOut, TAggregator>(
        IReadOnlyTimeSeries<TIn> source,
        Period targetPeriod,
        out DynamicSlotTimeSeries<TOut>? result,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
        => TryAggregateAsSparseTarget<TIn, TOut, TAggregator, DynamicSlotTimeSeries<TOut>>(source, targetPeriod,
            TimeSeriesMath.TimeSeriesSpecializationTarget.DynamicSlot,
            static sparse => TimeSeriesMath.ToDynamicSlotTimeSeries(sparse), out result, aggregator);

    public static bool TryAggregateAsBoundedStepwiseTimeSeries<TIn, TOut, TAggregator>(
        IReadOnlyTimeSeries<TIn> source,
        Period targetPeriod,
        out StepwiseTimeSeries<TOut>? result,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (!TimeSeriesMath.TryValidateSpecialization(source, targetPeriod, TimeSeriesMath.TimeSeriesSpecializationTarget.BoundedStepwise))
        {
            result = null;
            return false;
        }

        result = (StepwiseTimeSeries<TOut>)Aggregate<TIn, TOut, TAggregator>((IBoundedStepwiseTimeSeries<TIn>)source, targetPeriod, aggregator);
        return true;
    }

    public static bool TryResampleAsFixedSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> source,
        Period targetPeriod,
        out FixedSlotTimeSeries<T>? result)
        where T : struct, INumber<T>
        => TryAggregateAsFixedSlotTimeSeries<T, T, IdentityAggregator<T>>(source, targetPeriod, out result);

    public static bool TryResampleAsDynamicSlotTimeSeries<T>(
        IReadOnlyTimeSeries<T> source,
        Period targetPeriod,
        out DynamicSlotTimeSeries<T>? result)
        where T : struct, INumber<T>
        => TryAggregateAsDynamicSlotTimeSeries<T, T, IdentityAggregator<T>>(source, targetPeriod, out result);

    public static bool TryResampleAsBoundedStepwiseTimeSeries<T>(
        IReadOnlyTimeSeries<T> source,
        Period targetPeriod,
        out StepwiseTimeSeries<T>? result)
        where T : struct, INumber<T>
        => TryAggregateAsBoundedStepwiseTimeSeries<T, T, IdentityAggregator<T>>(source, targetPeriod, out result);

    public static IReadOnlySparseTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        IReadOnlySparseTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (source.ExplicitPointCount == 0)
            return new SortedArrayTimeSeries<TOut>(targetPeriod);

        var buckets = AggregateSparsePoints<TIn, TOut, TAggregator>(source.GetPoints(), targetPeriod, aggregator);
        return SortedArrayTimeSeries<TOut>.CreateFromSortedRaw(buckets.Keys.AsSpan(), buckets.Values.AsSpan(), targetPeriod);
    }

    public static IReadOnlySparseTimeSeries<T> Sum<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static IReadOnlySparseTimeSeries<T> Average<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static IReadOnlySparseTimeSeries<T> Min<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static IReadOnlySparseTimeSeries<T> Max<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static IReadOnlySparseTimeSeries<int> Count<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static IReadOnlySparseTimeSeries<T> Resample<T>(IReadOnlySparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        IBoundedStepwiseTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
        => AggregateStepwise<TIn, TOut, TAggregator>(source, targetPeriod, aggregator);

    public static IBoundedStepwiseTimeSeries<T> Sum<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<T> Average<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<T> Min<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<T> Max<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<int> Count<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static IBoundedStepwiseTimeSeries<T> Resample<T>(IBoundedStepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        FixedSlotTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (source.ExplicitPointCount == 0)
            return new FixedSlotTimeSeries<TOut>(targetPeriod);

        if (PeriodMath.TryGetFixedTicks(source.Period, out var sourceTicks) &&
            PeriodMath.TryGetFixedTicks(targetPeriod, out var targetTicks) &&
            targetTicks >= sourceTicks &&
            targetTicks % sourceTicks == 0)
        {
            return AggregateFixed<TIn, TOut, TAggregator>(source, targetPeriod, sourceTicks, targetTicks, aggregator);
        }

        return AggregateCalendar<TIn, TOut, TAggregator>(source, targetPeriod, aggregator);
    }

    public static FixedSlotTimeSeries<T> Sum<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<T> Average<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<T> Min<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<T> Max<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<int> Count<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static FixedSlotTimeSeries<T> Resample<T>(FixedSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        SortedArrayTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (source.ExplicitPointCount == 0)
            return new SortedArrayTimeSeries<TOut>(targetPeriod);

        var keys = source.TickKeys;
        var values = source.Values;

        var outKeys = new long[keys.Length];
        var outValues = new TOut[keys.Length];
        var outCount = 0;

        DateTimeOffset FirstBucket(DateTimeOffset ts)
        {
            if (PeriodMath.TryGetFixedTicks(targetPeriod, out _))
                return PeriodMath.TruncateToFixedBucket(ts, targetPeriod);

            return PeriodMath.FloorToCalendarBucket(ts, targetPeriod);
        }

        var currentBucket = FirstBucket(new DateTimeOffset(keys[0], TimeSpan.Zero));
        aggregator.Reset();
        aggregator.Add(values[0]);
        var bucketCount = 1;

        for (var i = 1; i < keys.Length; i++)
        {
            var bucket = FirstBucket(new DateTimeOffset(keys[i], TimeSpan.Zero));
            if (bucket == currentBucket)
            {
                aggregator.Add(values[i]);
                bucketCount++;
                continue;
            }

            outKeys[outCount] = currentBucket.UtcTicks;
            outValues[outCount] = aggregator.Complete(bucketCount);
            outCount++;

            currentBucket = bucket;
            aggregator.Reset();
            aggregator.Add(values[i]);
            bucketCount = 1;
        }

        outKeys[outCount] = currentBucket.UtcTicks;
        outValues[outCount] = aggregator.Complete(bucketCount);
        outCount++;

        return SortedArrayTimeSeries<TOut>.CreateFromSortedRaw(outKeys.AsSpan(0, outCount), outValues.AsSpan(0, outCount),
            targetPeriod);
    }

    public static SortedArrayTimeSeries<T> Sum<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<T> Average<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<T> Min<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<T> Max<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<int> Count<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static SortedArrayTimeSeries<T> Resample<T>(SortedArrayTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        DynamicSlotTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (targetPeriod == Period.NonStandard)
            throw new NotSupportedException($"Period {targetPeriod} is not supported.");

        if (source.ExplicitPointCount == 0)
            return new DynamicSlotTimeSeries<TOut>(targetPeriod);

        if (PeriodMath.TryGetFixedTicks(source.Period, out var sourceTicks) &&
            PeriodMath.TryGetFixedTicks(targetPeriod, out var targetTicks) &&
            targetTicks >= sourceTicks &&
            targetTicks % sourceTicks == 0)
        {
            return AggregateFixed<TIn, TOut, TAggregator>(source, targetPeriod, sourceTicks, targetTicks, aggregator);
        }

        return AggregateCalendar<TIn, TOut, TAggregator>(source, targetPeriod, aggregator);
    }

    public static DynamicSlotTimeSeries<T> Sum<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<T> Average<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<T> Min<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<T> Max<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<int> Count<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static DynamicSlotTimeSeries<T> Resample<T>(DynamicSlotTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        StepwiseTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
        => AggregateStepwise<TIn, TOut, TAggregator>(source, targetPeriod, aggregator);

    public static StepwiseTimeSeries<T> Sum<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<T> Average<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<T> Min<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<T> Max<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<int> Count<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static StepwiseTimeSeries<T> Resample<T>(StepwiseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, IdentityAggregator<T>>(source, targetPeriod);

    private static bool TryAggregateAsSparseTarget<TIn, TOut, TAggregator, TResult>(
        IReadOnlyTimeSeries<TIn> source,
        Period targetPeriod,
        TimeSeriesMath.TimeSeriesSpecializationTarget target,
        Func<IReadOnlySparseTimeSeries<TOut>, TResult> converter,
        out TResult? result,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
        where TResult : class, IReadOnlyTimeSeries<TOut>
    {
        if (!TimeSeriesMath.TryValidateSpecialization(source, targetPeriod, target))
        {
            result = null;
            return false;
        }

        result = converter(Aggregate<TIn, TOut, TAggregator>((IReadOnlySparseTimeSeries<TIn>)source, targetPeriod, aggregator));
        return true;
    }

    private static (long[] Keys, TOut[] Values) AggregateSparsePoints<TIn, TOut, TAggregator>(
        IEnumerable<TimeSeriesPoint<TIn>> points,
        Period targetPeriod,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        using var enumerator = points.GetEnumerator();
        if (!enumerator.MoveNext())
            return (Array.Empty<long>(), Array.Empty<TOut>());

        var keys = new List<long>();
        var values = new List<TOut>();

        var currentBucket = FirstBucket(enumerator.Current.Timestamp, targetPeriod);
        aggregator.Reset();
        aggregator.Add(enumerator.Current.Value);
        var bucketCount = 1;

        while (enumerator.MoveNext())
        {
            var bucket = FirstBucket(enumerator.Current.Timestamp, targetPeriod);
            if (bucket == currentBucket)
            {
                aggregator.Add(enumerator.Current.Value);
                bucketCount++;
                continue;
            }

            keys.Add(currentBucket.UtcTicks);
            values.Add(aggregator.Complete(bucketCount));

            currentBucket = bucket;
            aggregator.Reset();
            aggregator.Add(enumerator.Current.Value);
            bucketCount = 1;
        }

        keys.Add(currentBucket.UtcTicks);
        values.Add(aggregator.Complete(bucketCount));
        return ([.. keys], [.. values]);
    }

    private static StepwiseTimeSeries<TOut> AggregateStepwise<TIn, TOut, TAggregator>(
        IBoundedStepwiseTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (targetPeriod == Period.NonStandard)
            throw new NotSupportedException($"Period {targetPeriod} is not supported.");

        var startSlot = CalendarSlotMath.ToSlot(source.LogicalRangeStart, source.Period);
        var endSlot = CalendarSlotMath.ToSlot(source.LogicalRangeEnd, source.Period);
        var buckets = new List<(long BucketSlot, TOut Value)>();

        long? currentBucketSlot = null;
        var count = 0;

        for (var slot = startSlot; slot <= endSlot; slot++)
        {
            var timestamp = CalendarSlotMath.FromSlot(slot, source.Period);
            var bucketSlot = CalendarSlotMath.ToSlot(CalendarSlotMath.AlignToSlot(timestamp, targetPeriod), targetPeriod);

            if (currentBucketSlot != bucketSlot)
            {
                if (currentBucketSlot.HasValue)
                    buckets.Add((currentBucketSlot.Value, aggregator.Complete(count)));

                aggregator.Reset();
                currentBucketSlot = bucketSlot;
                count = 0;
            }

            aggregator.Add(source[timestamp]);
            count++;
        }

        if (currentBucketSlot.HasValue)
            buckets.Add((currentBucketSlot.Value, aggregator.Complete(count)));

        return CreateStepwiseResult(targetPeriod, buckets);
    }

    private static DateTimeOffset FirstBucket(DateTimeOffset timestamp, Period targetPeriod)
    {
        if (PeriodMath.TryGetFixedTicks(targetPeriod, out _))
            return PeriodMath.TruncateToFixedBucket(timestamp, targetPeriod);

        return PeriodMath.FloorToCalendarBucket(timestamp, targetPeriod);
    }

    private static StepwiseTimeSeries<T> CreateStepwiseResult<T>(Period targetPeriod, IReadOnlyList<(long BucketSlot, T Value)> buckets)
        where T : struct, INumber<T>
    {
        if (buckets.Count == 0)
            throw new InvalidOperationException("Bounded stepwise aggregation requires at least one bucket.");

        var start = CalendarSlotMath.FromSlot(buckets[0].BucketSlot, targetPeriod);
        var end = CalendarSlotMath.FromSlot(buckets[^1].BucketSlot, targetPeriod);
        var result = new StepwiseTimeSeries<T>(targetPeriod, start, end, buckets[0].Value);

        var runStartIndex = 0;
        for (var i = 1; i < buckets.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(buckets[i].Value, buckets[runStartIndex].Value))
                continue;

            if (runStartIndex > 0)
            {
                result.SetSegment(
                    CalendarSlotMath.FromSlot(buckets[runStartIndex].BucketSlot, targetPeriod),
                    CalendarSlotMath.FromSlot(buckets[i].BucketSlot, targetPeriod),
                    buckets[runStartIndex].Value);
            }

            runStartIndex = i;
        }

        if (runStartIndex > 0)
        {
            result.SetSegment(
                CalendarSlotMath.FromSlot(buckets[runStartIndex].BucketSlot, targetPeriod),
                CalendarSlotMath.FromSlot(buckets[^1].BucketSlot + 1, targetPeriod),
                buckets[runStartIndex].Value);
        }

        return result;
    }

    private static FixedSlotTimeSeries<TOut> AggregateFixed<TIn, TOut, TAggregator>(
        FixedSlotTimeSeries<TIn> source,
        Period targetPeriod,
        long sourceTicks,
        long targetTicks,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        var factor = checked((int)(targetTicks / sourceTicks));
        var firstBucket = Math.DivRem(source.StartSlot, factor, out var remStart);
        if (remStart < 0)
            firstBucket--;

        var lastSourceSlot = source.StartSlot + source.SlotLength - 1;
        var lastBucket = Math.DivRem(lastSourceSlot, factor, out var remEnd);
        if (remEnd < 0)
            lastBucket--;

        var bucketCount = checked((int)(lastBucket - firstBucket + 1));
        var result = new FixedSlotTimeSeries<TOut>(targetPeriod, bucketCount);
        result.InitializeWindow(firstBucket, bucketCount);

        for (var bucket = firstBucket; bucket <= lastBucket; bucket++)
        {
            aggregator.Reset();
            var count = 0;

            var bucketStart = bucket * factor;
            var bucketEndExclusive = bucketStart + factor;

            var localStart = (int)Math.Max(0, bucketStart - source.StartSlot);
            var localEnd = (int)Math.Min(source.SlotLength, bucketEndExclusive - source.StartSlot);

            for (var i = localStart; i < localEnd; i++)
            {
                if (!source.TryGetSlotValue(source.StartSlot + i, out var value))
                    continue;

                aggregator.Add(value);
                count++;
            }

            if (count == 0)
                continue;

            var idx = (int)(bucket - firstBucket);
            result.MutableValueSpan[idx] = aggregator.Complete(count);
            result.MarkPresentAt(idx);
        }

        return result;
    }

    private static FixedSlotTimeSeries<TOut> AggregateCalendar<TIn, TOut, TAggregator>(
        FixedSlotTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        var temp = new SortedDictionary<long, (TAggregator Aggregator, int Count)>();
        foreach (var point in source)
        {
            var bucket = PeriodMath.FloorToCalendarBucket(point.Timestamp, targetPeriod).UtcTicks;
            if (!temp.TryGetValue(bucket, out var state))
            {
                state = (aggregator, 0);
                state.Aggregator.Reset();
            }

            state.Aggregator.Add(point.Value);
            state.Count++;
            temp[bucket] = state;
        }

        var result = new FixedSlotTimeSeries<TOut>(targetPeriod, temp.Count);
        if (temp.Count == 0)
            return result;

        var firstBucketTick = temp.First().Key;
        var firstSlot = PeriodMath.ToAbsoluteSlot(new DateTimeOffset(firstBucketTick, TimeSpan.Zero), targetPeriod);
        var lastBucketTick = temp.Last().Key;
        var lastSlot = PeriodMath.ToAbsoluteSlot(new DateTimeOffset(lastBucketTick, TimeSpan.Zero), targetPeriod);
        var len = checked((int)(lastSlot - firstSlot + 1));
        result.InitializeWindow(firstSlot, len);

        foreach (var kvp in temp)
        {
            var slot = PeriodMath.ToAbsoluteSlot(new DateTimeOffset(kvp.Key, TimeSpan.Zero), targetPeriod);
            var index = checked((int)(slot - firstSlot));
            result.MutableValueSpan[index] = kvp.Value.Aggregator.Complete(kvp.Value.Count);
            result.MarkPresentAt(index);
        }

        return result;
    }

    private static DynamicSlotTimeSeries<TOut> AggregateFixed<TIn, TOut, TAggregator>(
        DynamicSlotTimeSeries<TIn> source,
        Period targetPeriod,
        long sourceTicks,
        long targetTicks,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        var factor = checked((int)(targetTicks / sourceTicks));
        var firstBucket = Math.DivRem(source.StartSlot, factor, out var remStart);
        if (remStart < 0)
            firstBucket--;

        var lastSourceSlot = source.StartSlot + source.SlotLength - 1;
        var lastBucket = Math.DivRem(lastSourceSlot, factor, out var remEnd);
        if (remEnd < 0)
            lastBucket--;

        var bucketCount = checked((int)(lastBucket - firstBucket + 1));
        var result = new DynamicSlotTimeSeries<TOut>(targetPeriod, AlignMode.Strict, bucketCount);
        result.InitializeWindow(firstBucket, bucketCount);

        for (var bucket = firstBucket; bucket <= lastBucket; bucket++)
        {
            aggregator.Reset();
            var count = 0;

            var bucketStart = bucket * factor;
            var bucketEndExclusive = bucketStart + factor;

            var localStart = (int)Math.Max(0, bucketStart - source.StartSlot);
            var localEnd = (int)Math.Min(source.SlotLength, bucketEndExclusive - source.StartSlot);

            for (var i = localStart; i < localEnd; i++)
            {
                if (!source.TryGetSlotValue(source.StartSlot + i, out var value))
                    continue;

                aggregator.Add(value);
                count++;
            }

            if (count == 0)
                continue;

            var idx = (int)(bucket - firstBucket);
            result.MutableValueSpan[idx] = aggregator.Complete(count);
            result.MarkPresentAt(idx);
        }

        return result;
    }

    private static DynamicSlotTimeSeries<TOut> AggregateCalendar<TIn, TOut, TAggregator>(
        DynamicSlotTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        var temp = new SortedDictionary<long, (TAggregator Aggregator, int Count)>();
        foreach (var point in source)
        {
            var bucket = CalendarSlotMath.ToSlot(CalendarSlotMath.AlignToSlot(point.Timestamp, targetPeriod), targetPeriod);
            if (!temp.TryGetValue(bucket, out var state))
            {
                state = (aggregator, 0);
                state.Aggregator.Reset();
            }

            state.Aggregator.Add(point.Value);
            state.Count++;
            temp[bucket] = state;
        }

        var result = new DynamicSlotTimeSeries<TOut>(targetPeriod, AlignMode.Strict, temp.Count);
        if (temp.Count == 0)
            return result;

        var firstSlot = temp.First().Key;
        var lastSlot = temp.Last().Key;
        var len = checked((int)(lastSlot - firstSlot + 1));
        result.InitializeWindow(firstSlot, len);

        foreach (var kvp in temp)
        {
            var index = checked((int)(kvp.Key - firstSlot));
            result.MutableValueSpan[index] = kvp.Value.Aggregator.Complete(kvp.Value.Count);
            result.MarkPresentAt(index);
        }

        return result;
    }
}
