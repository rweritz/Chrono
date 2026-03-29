using System.Numerics;

namespace Chrono.TimeSeries;

public static class TimeSeriesAggregation
{
    public static RegularTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        RegularTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (source.Count == 0)
            return new RegularTimeSeries<TOut>(targetPeriod);

        if (PeriodMath.TryGetFixedTicks(source.Period, out var sourceTicks) &&
            PeriodMath.TryGetFixedTicks(targetPeriod, out var targetTicks) &&
            targetTicks >= sourceTicks &&
            targetTicks % sourceTicks == 0)
        {
            return AggregateFixed<TIn, TOut, TAggregator>(source, targetPeriod, sourceTicks, targetTicks, aggregator);
        }

        return AggregateCalendar<TIn, TOut, TAggregator>(source, targetPeriod, aggregator);
    }

    public static RegularTimeSeries<T> Sum<T>(RegularTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static RegularTimeSeries<T> Average<T>(RegularTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static RegularTimeSeries<T> Min<T>(RegularTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static RegularTimeSeries<T> Max<T>(RegularTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static RegularTimeSeries<int> Count<T>(RegularTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    public static SparseTimeSeries<TOut> Aggregate<TIn, TOut, TAggregator>(
        SparseTimeSeries<TIn> source,
        Period targetPeriod,
        TAggregator aggregator = default)
        where TIn : struct, INumber<TIn>
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<TIn, TOut>
    {
        if (source.Count == 0)
            return new SparseTimeSeries<TOut>(targetPeriod);

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

        return SparseTimeSeries<TOut>.CreateFromSortedRaw(outKeys.AsSpan(0, outCount), outValues.AsSpan(0, outCount),
            targetPeriod);
    }

    public static SparseTimeSeries<T> Sum<T>(SparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, SumAggregator<T>>(source, targetPeriod);

    public static SparseTimeSeries<T> Average<T>(SparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, T, AverageAggregator<T>>(source, targetPeriod);

    public static SparseTimeSeries<T> Min<T>(SparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MinAggregator<T>>(source, targetPeriod);

    public static SparseTimeSeries<T> Max<T>(SparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>, IMinMaxValue<T>
        => Aggregate<T, T, MaxAggregator<T>>(source, targetPeriod);

    public static SparseTimeSeries<int> Count<T>(SparseTimeSeries<T> source, Period targetPeriod)
        where T : struct, INumber<T>
        => Aggregate<T, int, CountAggregator<T>>(source, targetPeriod);

    private static RegularTimeSeries<TOut> AggregateFixed<TIn, TOut, TAggregator>(
        RegularTimeSeries<TIn> source,
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
        var result = new RegularTimeSeries<TOut>(targetPeriod, bucketCount);
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

    private static RegularTimeSeries<TOut> AggregateCalendar<TIn, TOut, TAggregator>(
        RegularTimeSeries<TIn> source,
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

        var result = new RegularTimeSeries<TOut>(targetPeriod, temp.Count);
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
}
