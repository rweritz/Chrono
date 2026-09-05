using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public enum GappedFixedStepImplementation
{
    FixedSlot,
    DynamicSlot,
    SortedArray
}

[MemoryDiagnoser]
public class GappedFixedStepBenchmarks
{
    private const Period SourcePeriod = Period.FiveMinutes;
    private const Period AggregationTargetPeriod = Period.Hour;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const Period ResampleTargetPeriod = Period.FiveMinutes;
    private const int SegmentSlotCount = 12;

    private GappedFixedStepBenchmarkData _data = null!;
    private FixedSlotTimeSeries<double> _fixedSlotA = null!;
    private FixedSlotTimeSeries<double> _fixedSlotB = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotA = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotB = null!;
    private SortedArrayTimeSeries<double> _sortedArrayA = null!;
    private SortedArrayTimeSeries<double> _sortedArrayB = null!;
    private FixedSlotTimeSeries<double> _fixedSlotResample = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotResample = null!;
    private SortedArrayTimeSeries<double> _sortedArrayResample = null!;

    [Params(
        GappedFixedStepImplementation.FixedSlot,
        GappedFixedStepImplementation.DynamicSlot,
        GappedFixedStepImplementation.SortedArray)]
    public GappedFixedStepImplementation Implementation { get; set; }

    [Params(1_000, 10_000, 100_000)]
    public int PointCount { get; set; }

    [Params(0.01, 0.10, 0.50, 0.90)]
    public double Occupancy { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = GappedFixedStepBenchmarkDataFactory.Create(PointCount, Occupancy);

        _fixedSlotA = new FixedSlotTimeSeries<double>(SourcePeriod, _data.WindowSlotCount);
        _fixedSlotB = new FixedSlotTimeSeries<double>(SourcePeriod, _data.WindowSlotCount);
        _dynamicSlotA = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, _data.WindowSlotCount);
        _dynamicSlotB = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, _data.WindowSlotCount);
        _sortedArrayA = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayB = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _fixedSlotResample = new FixedSlotTimeSeries<double>(ResampleSourcePeriod, _data.ResampleWindowSlotCount);
        _dynamicSlotResample = new DynamicSlotTimeSeries<double>(ResampleSourcePeriod, AlignMode.Strict, _data.ResampleWindowSlotCount);
        _sortedArrayResample = new SortedArrayTimeSeries<double>(ResampleSourcePeriod, PointCount);

        FillSeries(_fixedSlotA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_fixedSlotB, _data.SecondaryOrderedTimestamps, _data.Values, 1d);
        FillSeries(_dynamicSlotA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_dynamicSlotB, _data.SecondaryOrderedTimestamps, _data.Values, 1d);
        FillSeries(_sortedArrayA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_sortedArrayB, _data.SecondaryOrderedTimestamps, _data.Values, 1d);
        FillSeries(_fixedSlotResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
        FillSeries(_dynamicSlotResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
        FillSeries(_sortedArrayResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
    }

    [Benchmark]
    public int OrderedInsert()
    {
        var series = CreateEmptySeries(PointCount);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, 0d);
        return series.ExplicitPointCount;
    }

    [Benchmark]
    public int RandomInsert()
    {
        var series = CreateEmptySeries(PointCount, preWindowSlotSeries: true);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, _data.RandomInsertIndices, 0d);
        return series.ExplicitPointCount;
    }

    [Benchmark]
    public double HitLookupTryGetValue() => SumTryGetValueLookups(ActiveSeriesA(), _data.RandomHitLookupTimestamps);

    [Benchmark]
    public double MissLookupTryGetValue() => SumTryGetValueLookups(ActiveSeriesA(), _data.RandomMissLookupTimestamps);

    [Benchmark]
    public double MixedLookupTryGetValue() => SumTryGetValueLookups(ActiveSeriesA(), _data.MixedLookupTimestamps);

    [Benchmark]
    public int ShortContiguousSetSegment()
    {
        var series = CreateEmptySeries(SegmentSlotCount, preWindowSlotSeries: true);
        series.SetSegment(_data.SegmentStart, _data.SegmentEndExclusive, 42d);
        return series.ExplicitPointCount;
    }

    [Benchmark]
    public int Remove()
    {
        var series = CreateFilledSeries();
        var removed = 0;
        for (var i = 0; i < _data.RandomHitLookupTimestamps.Length; i++)
        {
            if (series.Remove(_data.RandomHitLookupTimestamps[i]))
                removed++;
        }

        return removed;
    }

    [Benchmark]
    public object ScalarMultiply() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesMath.Multiply(_fixedSlotA, 1.5d),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesMath.Multiply(_dynamicSlotA, 1.5d),
            GappedFixedStepImplementation.SortedArray => TimeSeriesMath.Multiply(_sortedArrayA, 1.5d),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAddIntersection() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesMath.Add(_fixedSlotA, _fixedSlotB, MissingValuePolicy.Intersection),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB, MissingValuePolicy.Intersection),
            GappedFixedStepImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB, MissingValuePolicy.Intersection),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAddUnionWithZero() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesMath.Add(_fixedSlotA, _fixedSlotB, MissingValuePolicy.UnionWithZero),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB, MissingValuePolicy.UnionWithZero),
            GappedFixedStepImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB, MissingValuePolicy.UnionWithZero),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateSum() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Sum(_fixedSlotA, AggregationTargetPeriod),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Sum(_dynamicSlotA, AggregationTargetPeriod),
            GappedFixedStepImplementation.SortedArray => TimeSeriesAggregation.Sum(_sortedArrayA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateCount() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Count(_fixedSlotA, AggregationTargetPeriod),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Count(_dynamicSlotA, AggregationTargetPeriod),
            GappedFixedStepImplementation.SortedArray => TimeSeriesAggregation.Count(_sortedArrayA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object Resample() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Resample((IReadOnlySparseTimeSeries<double>)_fixedSlotResample, ResampleTargetPeriod),
            GappedFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Resample(_dynamicSlotResample, ResampleTargetPeriod),
            GappedFixedStepImplementation.SortedArray => TimeSeriesAggregation.Resample(_sortedArrayResample, ResampleTargetPeriod),
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> ActiveSeriesA() =>
        Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => _fixedSlotA,
            GappedFixedStepImplementation.DynamicSlot => _dynamicSlotA,
            GappedFixedStepImplementation.SortedArray => _sortedArrayA,
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> CreateFilledSeries()
    {
        var series = CreateEmptySeries(PointCount, preWindowSlotSeries: true);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, 0d);
        return series;
    }

    private ISparseTimeSeries<double> CreateEmptySeries(int capacity, bool preWindowSlotSeries = false)
    {
        ISparseTimeSeries<double> series = Implementation switch
        {
            GappedFixedStepImplementation.FixedSlot => new FixedSlotTimeSeries<double>(SourcePeriod, preWindowSlotSeries ? _data.WindowSlotCount : capacity),
            GappedFixedStepImplementation.DynamicSlot => new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, preWindowSlotSeries ? _data.WindowSlotCount : capacity),
            GappedFixedStepImplementation.SortedArray => new SortedArrayTimeSeries<double>(SourcePeriod, capacity),
            _ => throw new NotSupportedException()
        };

        if (preWindowSlotSeries && Implementation != GappedFixedStepImplementation.SortedArray)
        {
            series[_data.WindowStart] = 0d;
            series[_data.WindowEnd] = 0d;
            series.Clear();
        }

        return series;
    }

    private static void FillSeries(
        ISparseTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps,
        IReadOnlyList<double> values,
        double offset)
    {
        for (var i = 0; i < timestamps.Count; i++)
            series[timestamps[i]] = values[i] + offset;
    }

    private static void FillSeries(
        ISparseTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps,
        IReadOnlyList<double> values,
        IReadOnlyList<int> indices,
        double offset)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            series[timestamps[index]] = values[index] + offset;
        }
    }

    private static double SumTryGetValueLookups(
        IReadOnlyTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps)
    {
        var sum = 0d;
        for (var i = 0; i < timestamps.Count; i++)
        {
            if (series.TryGetValue(timestamps[i], out var value))
                sum += value;
        }

        return sum;
    }
}

public sealed record GappedFixedStepBenchmarkData(
    int WindowSlotCount,
    int ResampleWindowSlotCount,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    DateTimeOffset SegmentStart,
    DateTimeOffset SegmentEndExclusive,
    DateTimeOffset[] OrderedTimestamps,
    DateTimeOffset[] SecondaryOrderedTimestamps,
    DateTimeOffset[] ResampleSourceTimestamps,
    int[] RandomInsertIndices,
    DateTimeOffset[] RandomHitLookupTimestamps,
    DateTimeOffset[] RandomMissLookupTimestamps,
    DateTimeOffset[] MixedLookupTimestamps,
    double[] Values,
    double[] ResampleSourceValues);

public static class GappedFixedStepBenchmarkDataFactory
{
    private const Period SourcePeriod = Period.FiveMinutes;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const int SegmentSlotCount = 12;
    private const int PrimarySlotSeed = 42041;
    private const int SecondarySlotSeed = 42042;
    private const int InsertOrderSeed = 42043;
    private const int HitLookupOrderSeed = 42044;
    private const int MissLookupOrderSeed = 42045;
    private const int MixedLookupOrderSeed = 42046;
    private const int ValueSeed = 42047;
    private const int ResampleValueSeed = 42048;
    private static readonly DateTimeOffset Start = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static GappedFixedStepBenchmarkData Create(int pointCount, double occupancy)
    {
        if (pointCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pointCount));

        if (occupancy <= 0d || occupancy > 1d)
            throw new ArgumentOutOfRangeException(nameof(occupancy));

        var windowSlotCount = Math.Max(pointCount, (int)Math.Ceiling(pointCount / occupancy));
        var primarySlots = CreateOccupiedSlots(pointCount, windowSlotCount, PrimarySlotSeed);
        var secondarySlots = CreateOccupiedSlots(pointCount, windowSlotCount, SecondarySlotSeed);
        var missSlots = CreateMissSlots(pointCount, windowSlotCount, primarySlots);

        var orderedTimestamps = ToTimestamps(primarySlots, SourcePeriod);
        var secondaryOrderedTimestamps = ToTimestamps(secondarySlots, SourcePeriod);
        var missTimestamps = ToTimestamps(missSlots, SourcePeriod);
        var randomInsertIndices = ShuffleIndices(pointCount, InsertOrderSeed);
        var randomHitLookupTimestamps = Shuffle(orderedTimestamps, HitLookupOrderSeed);
        var randomMissLookupTimestamps = Shuffle(missTimestamps, MissLookupOrderSeed);
        var mixedLookupTimestamps = CreateMixedLookups(randomHitLookupTimestamps, randomMissLookupTimestamps, MixedLookupOrderSeed);
        var values = CreateValues(pointCount, ValueSeed);
        var resampleSourceTimestamps = ToTimestamps(primarySlots, ResampleSourcePeriod);
        var resampleSourceValues = CreateValues(pointCount, ResampleValueSeed);
        var segmentStartSlot = Math.Max(0, (windowSlotCount - SegmentSlotCount) / 2);
        var segmentStart = Start.AddMinutes(segmentStartSlot * 5d);

        return new GappedFixedStepBenchmarkData(
            windowSlotCount,
            windowSlotCount,
            Start,
            Start.AddMinutes((windowSlotCount - 1) * 5d),
            segmentStart,
            segmentStart.AddMinutes(SegmentSlotCount * 5d),
            orderedTimestamps,
            secondaryOrderedTimestamps,
            resampleSourceTimestamps,
            randomInsertIndices,
            randomHitLookupTimestamps,
            randomMissLookupTimestamps,
            mixedLookupTimestamps,
            values,
            resampleSourceValues);
    }

    private static int[] CreateOccupiedSlots(int pointCount, int windowSlotCount, int seed)
    {
        var slots = new HashSet<int>(pointCount) { 0 };
        if (pointCount > 1)
            slots.Add(windowSlotCount - 1);

        var random = new Random(seed);
        while (slots.Count < pointCount)
            slots.Add(random.Next(windowSlotCount));

        var ordered = slots.ToArray();
        Array.Sort(ordered);
        return ordered;
    }

    private static int[] CreateMissSlots(int count, int windowSlotCount, int[] occupiedSlots)
    {
        var occupied = occupiedSlots.ToHashSet();
        var misses = new List<int>(count);

        for (var slot = 0; slot < windowSlotCount && misses.Count < count; slot++)
        {
            if (!occupied.Contains(slot))
                misses.Add(slot);
        }

        for (var i = 0; misses.Count < count; i++)
            misses.Add(misses[i % misses.Count]);

        return [.. misses];
    }

    private static DateTimeOffset[] ToTimestamps(IReadOnlyList<int> slots, Period period)
    {
        var timestamps = new DateTimeOffset[slots.Count];
        for (var i = 0; i < slots.Count; i++)
        {
            timestamps[i] = period switch
            {
                SourcePeriod => Start.AddMinutes(slots[i] * 5d),
                ResampleSourcePeriod => Start.AddHours(slots[i]),
                _ => throw new NotSupportedException()
            };
        }

        return timestamps;
    }

    private static double[] CreateValues(int count, int seed)
    {
        var values = new double[count];
        var random = new Random(seed);
        for (var i = 0; i < values.Length; i++)
            values[i] = random.NextDouble() * 1000d;

        return values;
    }

    private static DateTimeOffset[] CreateMixedLookups(
        IReadOnlyList<DateTimeOffset> hits,
        IReadOnlyList<DateTimeOffset> misses,
        int seed)
    {
        var mixed = new DateTimeOffset[hits.Count];
        for (var i = 0; i < mixed.Length; i++)
            mixed[i] = (i & 1) == 0 ? hits[i] : misses[i];

        return Shuffle(mixed, seed);
    }

    private static int[] ShuffleIndices(int count, int seed)
    {
        var shuffled = new int[count];
        for (var i = 0; i < shuffled.Length; i++)
            shuffled[i] = i;

        var random = new Random(seed);

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    private static DateTimeOffset[] Shuffle(IReadOnlyList<DateTimeOffset> source, int seed)
    {
        var shuffled = source.ToArray();
        var random = new Random(seed);

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}
