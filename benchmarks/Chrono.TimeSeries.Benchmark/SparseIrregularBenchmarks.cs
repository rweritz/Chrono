using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public enum SparseIrregularImplementation
{
    SortedArray,
    DynamicSlot
}

public enum SparseIrregularShape
{
    Wide,
    Clustered
}

public sealed record SparseIrregularCase(
    SparseIrregularImplementation Implementation,
    SparseIrregularShape Shape)
{
    public override string ToString() => $"{Implementation}/{Shape}";
}

public sealed record SparseIrregularBenchmarkData(
    DateTimeOffset[] OrderedTimestamps,
    DateTimeOffset[] BinaryRightTimestamps,
    DateTimeOffset[] HitLookupTimestamps,
    DateTimeOffset[] MissLookupTimestamps,
    DateTimeOffset[] MixedLookupTimestamps,
    DateTimeOffset[] SegmentStarts,
    DateTimeOffset[] SegmentEnds,
    DateTimeOffset[] ResampleSourceTimestamps,
    int[] RandomInsertIndices,
    int[] RemoveIndices,
    double[] Values,
    double[] BinaryRightValues,
    double[] ResampleSourceValues);

public static class SparseIrregularBenchmarkDataFactory
{
    private const int InsertOrderSeed = 43041;
    private const int HitLookupSeed = 43042;
    private const int MissLookupSeed = 43043;
    private const int MixedLookupSeed = 43044;
    private const int RemoveOrderSeed = 43045;
    private const int ValueSeed = 43046;
    private const int BinaryValueSeed = 43047;
    private const int WideGapSeed = 43048;
    private const int ResampleValueSeed = 43049;

    public static SparseIrregularBenchmarkData Create(SparseIrregularShape shape, int pointCount)
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var slots = shape switch
        {
            SparseIrregularShape.Wide => CreateWideRangeSlots(pointCount),
            SparseIrregularShape.Clustered => CreateClusteredSlots(pointCount),
            _ => throw new NotSupportedException()
        };

        var orderedTimestamps = ToTimestamps(start, slots);
        var binaryRightSlots = CreateBinaryRightSlots(shape, pointCount, slots);
        var binaryRightTimestamps = ToTimestamps(start, binaryRightSlots);
        var missTimestamps = CreateMissTimestamps(start, slots, pointCount);
        var values = CreateValues(pointCount, ValueSeed);
        var binaryValues = CreateValues(pointCount, BinaryValueSeed);
        var resampleSourceTimestamps = CreateHourlyTimestamps(start, pointCount);
        var resampleValues = CreateValues(pointCount, ResampleValueSeed);
        var segmentStarts = CreateSegmentStarts(start, pointCount);
        var segmentEnds = new DateTimeOffset[segmentStarts.Length];
        for (var i = 0; i < segmentStarts.Length; i++)
            segmentEnds[i] = segmentStarts[i].AddMinutes(5 * 24);

        return new SparseIrregularBenchmarkData(
            orderedTimestamps,
            binaryRightTimestamps,
            Shuffle(orderedTimestamps, HitLookupSeed),
            Shuffle(missTimestamps, MissLookupSeed),
            CreateMixedLookupTimestamps(orderedTimestamps, missTimestamps),
            segmentStarts,
            segmentEnds,
            resampleSourceTimestamps,
            ShuffleIndices(pointCount, InsertOrderSeed),
            ShuffleIndices(pointCount, RemoveOrderSeed),
            values,
            binaryValues,
            resampleValues);
    }

    private static int[] CreateWideRangeSlots(int count)
    {
        var slots = new int[count];
        var slot = 0;
        var random = new Random(WideGapSeed);

        for (var i = 0; i < slots.Length; i++)
        {
            slot += random.Next(19, 211);
            slots[i] = slot;
        }

        return slots;
    }

    private static int[] CreateClusteredSlots(int count)
    {
        var slots = new int[count];
        var clusterSize = 50;
        var clusterGap = 175;

        for (var i = 0; i < slots.Length; i++)
        {
            var cluster = i / clusterSize;
            var offset = i % clusterSize;
            slots[i] = cluster * (clusterSize + clusterGap) + offset;
        }

        return slots;
    }

    private static int[] CreateBinaryRightSlots(SparseIrregularShape shape, int count, IReadOnlyList<int> leftSlots)
    {
        var rightSlots = new int[count];
        var cursor = 0;

        for (var i = 0; cursor < rightSlots.Length && i < leftSlots.Count; i += 2)
            rightSlots[cursor++] = leftSlots[i];

        var extra = shape == SparseIrregularShape.Wide ? 2_000_000 : 80_000;
        while (cursor < rightSlots.Length)
        {
            rightSlots[cursor] = extra;
            extra += shape == SparseIrregularShape.Wide ? 127 : 3;
            cursor++;
        }

        return rightSlots;
    }

    private static DateTimeOffset[] ToTimestamps(DateTimeOffset start, IReadOnlyList<int> slots)
    {
        var timestamps = new DateTimeOffset[slots.Count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddMinutes(slots[i] * 5);

        return timestamps;
    }

    private static DateTimeOffset[] CreateHourlyTimestamps(DateTimeOffset start, int count)
    {
        var timestamps = new DateTimeOffset[count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddHours(i);

        return timestamps;
    }

    private static DateTimeOffset[] CreateMissTimestamps(DateTimeOffset start, IReadOnlyList<int> existingSlots, int count)
    {
        var misses = new DateTimeOffset[count];
        var existing = new HashSet<int>(existingSlots);
        var candidate = 1;

        for (var i = 0; i < misses.Length; i++)
        {
            while (existing.Contains(candidate))
                candidate++;

            misses[i] = start.AddMinutes(candidate * 5);
            candidate += 2;
        }

        return misses;
    }

    private static DateTimeOffset[] CreateMixedLookupTimestamps(
        IReadOnlyList<DateTimeOffset> hits,
        IReadOnlyList<DateTimeOffset> misses)
    {
        var mixed = new DateTimeOffset[hits.Count];
        for (var i = 0; i < mixed.Length; i++)
            mixed[i] = (i & 1) == 0 ? hits[i] : misses[i];

        return Shuffle(mixed, MixedLookupSeed);
    }

    private static DateTimeOffset[] CreateSegmentStarts(DateTimeOffset start, int pointCount)
    {
        var segmentCount = Math.Max(1, Math.Min(16, pointCount / 64));
        var starts = new DateTimeOffset[segmentCount];
        for (var i = 0; i < starts.Length; i++)
            starts[i] = start.AddMinutes((i * 300) * 5);

        return starts;
    }

    private static double[] CreateValues(int count, int seed)
    {
        var values = new double[count];
        var random = new Random(seed);

        for (var i = 0; i < values.Length; i++)
            values[i] = random.NextDouble() * 1000d;

        return values;
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

[MemoryDiagnoser]
public class SparseIrregularBenchmarks
{
    private const Period SourcePeriod = Period.FiveMinutes;
    private const Period AggregationTargetPeriod = Period.Hour;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const Period ResampleTargetPeriod = Period.FiveMinutes;

    private SparseIrregularBenchmarkData _data = null!;
    private SortedArrayTimeSeries<double> _sortedArrayA = null!;
    private SortedArrayTimeSeries<double> _sortedArrayB = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotA = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotB = null!;
    private SortedArrayTimeSeries<double> _sortedArrayResample = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotResample = null!;

    public IEnumerable<SparseIrregularCase> Cases =>
    [
        // FixedSlotTimeSeries is intentionally excluded: this scenario models irregular sparse data,
        // while FixedSlot is optimized for dense fixed-step windows and would pay for empty slots.
        new(SparseIrregularImplementation.SortedArray, SparseIrregularShape.Wide),
        new(SparseIrregularImplementation.SortedArray, SparseIrregularShape.Clustered),
        new(SparseIrregularImplementation.DynamicSlot, SparseIrregularShape.Clustered)
    ];

    [ParamsSource(nameof(Cases))]
    public SparseIrregularCase Case { get; set; } = null!;

    [Params(1_000, 10_000)]
    public int PointCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = SparseIrregularBenchmarkDataFactory.Create(Case.Shape, PointCount);
        _sortedArrayA = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayB = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _dynamicSlotA = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _dynamicSlotB = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _sortedArrayResample = new SortedArrayTimeSeries<double>(ResampleSourcePeriod, PointCount);
        _dynamicSlotResample = new DynamicSlotTimeSeries<double>(ResampleSourcePeriod, AlignMode.Strict, PointCount);

        FillSeries(_sortedArrayA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_sortedArrayB, _data.BinaryRightTimestamps, _data.BinaryRightValues, 0d);
        FillSeries(_sortedArrayResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);

        if (Case.Implementation == SparseIrregularImplementation.DynamicSlot)
        {
            FillSeries(_dynamicSlotA, _data.OrderedTimestamps, _data.Values, 0d);
            FillSeries(_dynamicSlotB, _data.BinaryRightTimestamps, _data.BinaryRightValues, 0d);
            FillSeries(_dynamicSlotResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
        }
    }

    [Benchmark]
    public int OrderedInsert()
    {
        var series = CreateEmptySeries();
        FillSeries(series, _data.OrderedTimestamps, _data.Values, 0d);
        return series.ExplicitPointCount;
    }

    [Benchmark]
    public int RandomInsert()
    {
        var series = CreateEmptySeries(preWindowDynamicSlot: true);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, _data.RandomInsertIndices, 0d);
        return series.ExplicitPointCount;
    }

    [Benchmark]
    public double HitLookupTryGetValue() => SumTryGetValue(ActiveSeriesA(), _data.HitLookupTimestamps);

    [Benchmark]
    public double MissLookupTryGetValue() => SumTryGetValue(ActiveSeriesA(), _data.MissLookupTimestamps);

    [Benchmark]
    public double MixedLookupTryGetValue() => SumTryGetValue(ActiveSeriesA(), _data.MixedLookupTimestamps);

    [Benchmark]
    public int ClusteredContiguousSetSegment()
    {
        var series = CreateEmptySeries(preWindowDynamicSlot: true);
        for (var i = 0; i < _data.SegmentStarts.Length; i++)
            series.SetSegment(_data.SegmentStarts[i], _data.SegmentEnds[i], i + 1d);

        return series.ExplicitPointCount;
    }

    [Benchmark]
    public int Remove()
    {
        var series = CreateEmptySeries(preWindowDynamicSlot: true);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, 0d);

        for (var i = 0; i < _data.RemoveIndices.Length; i++)
            series.Remove(_data.OrderedTimestamps[_data.RemoveIndices[i]]);

        return series.ExplicitPointCount;
    }

    [Benchmark]
    public object ScalarMultiply() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesMath.Multiply(_sortedArrayA, 1.5d),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesMath.Multiply(_dynamicSlotA, 1.5d),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAddIntersection() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB, MissingValuePolicy.Intersection),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB, MissingValuePolicy.Intersection),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAddUnionWithZero() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB, MissingValuePolicy.UnionWithZero),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB, MissingValuePolicy.UnionWithZero),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateSum() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesAggregation.Sum(_sortedArrayA, AggregationTargetPeriod),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesAggregation.Sum(_dynamicSlotA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateCount() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesAggregation.Count(_sortedArrayA, AggregationTargetPeriod),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesAggregation.Count(_dynamicSlotA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object Resample() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => TimeSeriesAggregation.Resample(_sortedArrayResample, ResampleTargetPeriod),
            SparseIrregularImplementation.DynamicSlot => TimeSeriesAggregation.Resample(_dynamicSlotResample, ResampleTargetPeriod),
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> ActiveSeriesA() =>
        Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => _sortedArrayA,
            SparseIrregularImplementation.DynamicSlot => _dynamicSlotA,
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> CreateEmptySeries(bool preWindowDynamicSlot = false)
    {
        ISparseTimeSeries<double> series = Case.Implementation switch
        {
            SparseIrregularImplementation.SortedArray => new SortedArrayTimeSeries<double>(SourcePeriod, PointCount),
            SparseIrregularImplementation.DynamicSlot => new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount),
            _ => throw new NotSupportedException()
        };

        if (preWindowDynamicSlot && Case.Implementation == SparseIrregularImplementation.DynamicSlot)
        {
            series[_data.OrderedTimestamps[0]] = 0d;
            series[_data.OrderedTimestamps[^1]] = 0d;
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

    private static double SumTryGetValue(
        IReadOnlyTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps)
    {
        var sum = 0d;
        for (var i = 0; i < timestamps.Count; i++)
            if (series.TryGetValue(timestamps[i], out var value))
                sum += value;

        return sum;
    }
}
