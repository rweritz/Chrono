using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public enum MixedFamilyPairing
{
    FixedSorted,
    FixedDynamic,
    SortedDynamic,
    FixedStepwise,
    SortedStepwise,
    DynamicStepwise,
    StepwiseStepwise
}

public sealed record MixedFamilyOperationCase(MixedFamilyPairing Pairing)
{
    public override string ToString() => Pairing switch
    {
        MixedFamilyPairing.FixedSorted => "Fixed+Sorted",
        MixedFamilyPairing.FixedDynamic => "Fixed+Dynamic",
        MixedFamilyPairing.SortedDynamic => "Sorted+Dynamic",
        MixedFamilyPairing.FixedStepwise => "Fixed+Stepwise",
        MixedFamilyPairing.SortedStepwise => "Sorted+Stepwise",
        MixedFamilyPairing.DynamicStepwise => "Dynamic+Stepwise",
        MixedFamilyPairing.StepwiseStepwise => "Stepwise+Stepwise",
        _ => Pairing.ToString()
    };
}

public sealed record MixedFamilyOperationBenchmarkData(
    DateTimeOffset[] DenseTimestamps,
    DateTimeOffset[] SparseTimestamps,
    DateTimeOffset[] SparseRightTimestamps,
    DateTimeOffset[] ResampleSourceTimestamps,
    double[] DenseValues,
    double[] SparseValues,
    double[] SparseRightValues,
    double[] ResampleValues,
    DateTimeOffset StepwiseStart,
    DateTimeOffset StepwiseEnd,
    DateTimeOffset OverlapStepwiseStart,
    DateTimeOffset OverlapStepwiseEnd,
    int[] StepwiseChangePointStarts,
    int[] OverlapStepwiseChangePointStarts,
    double StepwiseInitialValueA,
    double StepwiseInitialValueB,
    double[] StepwiseChangePointValuesA,
    double[] StepwiseChangePointValuesB);

public static class MixedFamilyOperationBenchmarkDataFactory
{
    private const int DenseValueSeed = 46041;
    private const int SparseValueSeed = 46042;
    private const int SparseRightValueSeed = 46043;
    private const int ResampleValueSeed = 46044;
    private const int StepwiseChangePointSeed = 46045;
    private const int OverlapStepwiseChangePointSeed = 46046;
    private const int StepwiseValueSeed = 46047;

    public static MixedFamilyOperationBenchmarkData Create(int pointCount)
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var denseTimestamps = CreateFiveMinuteTimestamps(start, pointCount);
        var sparseSlots = CreateSparseSlots(pointCount);
        var sparseRightSlots = CreateSparseRightSlots(pointCount, sparseSlots);
        var sparseTimestamps = ToFiveMinuteTimestamps(start, sparseSlots);
        var sparseRightTimestamps = ToFiveMinuteTimestamps(start, sparseRightSlots);
        var resampleSourceTimestamps = CreateHourlyTimestamps(start, pointCount);
        var logicalSlotCount = Math.Max(pointCount, 24);
        var overlapStartSlot = logicalSlotCount / 4;
        var overlapEndSlot = logicalSlotCount + overlapStartSlot - 1;
        var stepwiseChangePointStarts = CreateChangePointStarts(logicalSlotCount, StepwiseChangePointSeed);
        var overlapStepwiseChangePointStarts = CreateChangePointStarts(logicalSlotCount, OverlapStepwiseChangePointSeed);
        var valueRandom = new Random(StepwiseValueSeed);

        return new MixedFamilyOperationBenchmarkData(
            denseTimestamps,
            sparseTimestamps,
            sparseRightTimestamps,
            resampleSourceTimestamps,
            CreateValues(pointCount, DenseValueSeed),
            CreateValues(pointCount, SparseValueSeed),
            CreateValues(pointCount, SparseRightValueSeed),
            CreateValues(pointCount, ResampleValueSeed),
            start,
            start.AddMinutes((logicalSlotCount - 1L) * 5L),
            start.AddMinutes(overlapStartSlot * 5L),
            start.AddMinutes(overlapEndSlot * 5L),
            stepwiseChangePointStarts,
            overlapStepwiseChangePointStarts,
            NextValue(valueRandom),
            NextValue(valueRandom) + 11d,
            CreateValues(stepwiseChangePointStarts.Length, valueRandom, 0d),
            CreateValues(overlapStepwiseChangePointStarts.Length, valueRandom, 11d));
    }

    private static DateTimeOffset[] CreateFiveMinuteTimestamps(DateTimeOffset start, int count)
    {
        var timestamps = new DateTimeOffset[count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddMinutes(i * 5L);

        return timestamps;
    }

    private static DateTimeOffset[] CreateHourlyTimestamps(DateTimeOffset start, int count)
    {
        var timestamps = new DateTimeOffset[count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddHours(i);

        return timestamps;
    }

    private static int[] CreateSparseSlots(int count)
    {
        var slots = new int[count];
        for (var i = 0; i < slots.Length; i++)
            slots[i] = i * 3;

        return slots;
    }

    private static int[] CreateSparseRightSlots(int count, IReadOnlyList<int> leftSlots)
    {
        var slots = new int[count];
        var cursor = 0;

        for (var i = 0; cursor < slots.Length && i < leftSlots.Count; i += 2)
            slots[cursor++] = leftSlots[i];

        var extra = leftSlots.Count * 3 + 2;
        while (cursor < slots.Length)
        {
            slots[cursor++] = extra;
            extra += 3;
        }

        return slots;
    }

    private static DateTimeOffset[] ToFiveMinuteTimestamps(DateTimeOffset start, IReadOnlyList<int> slots)
    {
        var timestamps = new DateTimeOffset[slots.Count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddMinutes(slots[i] * 5L);

        return timestamps;
    }

    private static int[] CreateChangePointStarts(int logicalSlotCount, int seed)
    {
        var count = Math.Clamp(logicalSlotCount / 100, 2, Math.Max(2, logicalSlotCount / 4));
        var candidates = new int[Math.Max(0, logicalSlotCount - 2)];
        for (var i = 0; i < candidates.Length; i++)
            candidates[i] = i + 1;

        var random = new Random(seed);
        for (var i = candidates.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var result = new int[Math.Min(count, candidates.Length)];
        Array.Copy(candidates, result, result.Length);
        Array.Sort(result);
        return result;
    }

    private static double[] CreateValues(int count, int seed)
    {
        var random = new Random(seed);
        return CreateValues(count, random, 0d);
    }

    private static double[] CreateValues(int count, Random random, double offset)
    {
        var values = new double[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = NextValue(random) + offset;

        return values;
    }

    private static double NextValue(Random random) =>
        Math.Round(random.NextDouble() * 1000d, 6);
}

[MemoryDiagnoser]
public class MixedFamilyOperationBenchmarks
{
    private const Period SourcePeriod = Period.FiveMinutes;

    private MixedFamilyOperationBenchmarkData _data = null!;
    private FixedSlotTimeSeries<double> _fixedSlotSparse = null!;
    private SortedArrayTimeSeries<double> _sortedArraySparse = null!;
    private SortedArrayTimeSeries<double> _sortedArrayRight = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotSparse = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotRight = null!;
    private StepwiseTimeSeries<double> _stepwiseA = null!;
    private StepwiseTimeSeries<double> _stepwiseB = null!;

    public IEnumerable<MixedFamilyOperationCase> Cases =>
    [
        new(MixedFamilyPairing.FixedSorted),
        new(MixedFamilyPairing.FixedDynamic),
        new(MixedFamilyPairing.SortedDynamic),
        new(MixedFamilyPairing.FixedStepwise),
        new(MixedFamilyPairing.SortedStepwise),
        new(MixedFamilyPairing.DynamicStepwise),
        new(MixedFamilyPairing.StepwiseStepwise)
    ];

    [ParamsSource(nameof(Cases))]
    public MixedFamilyOperationCase Case { get; set; } = null!;

    [Params(1_000, 10_000)]
    public int PointCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = MixedFamilyOperationBenchmarkDataFactory.Create(PointCount);
        _fixedSlotSparse = new FixedSlotTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArraySparse = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayRight = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _dynamicSlotSparse = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _dynamicSlotRight = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);

        MixedFamilyBenchmarkHelpers.FillSeries(_fixedSlotSparse, _data.SparseTimestamps, _data.SparseValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_sortedArraySparse, _data.SparseTimestamps, _data.SparseValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_sortedArrayRight, _data.SparseRightTimestamps, _data.SparseRightValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_dynamicSlotSparse, _data.SparseTimestamps, _data.SparseValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_dynamicSlotRight, _data.SparseRightTimestamps, _data.SparseRightValues);

        _stepwiseA = MixedFamilyBenchmarkHelpers.CreateStepwise(
            _data.StepwiseInitialValueA,
            _data.StepwiseChangePointStarts,
            _data.StepwiseChangePointValuesA,
            _data.StepwiseStart,
            _data.StepwiseEnd,
            SourcePeriod);
        _stepwiseB = MixedFamilyBenchmarkHelpers.CreateStepwise(
            _data.StepwiseInitialValueB,
            _data.OverlapStepwiseChangePointStarts,
            _data.StepwiseChangePointValuesB,
            _data.OverlapStepwiseStart,
            _data.OverlapStepwiseEnd,
            SourcePeriod);
    }

    [Benchmark]
    public object BinaryAddIntersection() =>
        Case.Pairing switch
        {
            MixedFamilyPairing.FixedSorted => TimeSeriesMath.Add(_fixedSlotSparse, _sortedArrayRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.FixedDynamic => TimeSeriesMath.Add(_fixedSlotSparse, _dynamicSlotRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.SortedDynamic => TimeSeriesMath.Add(_sortedArraySparse, _dynamicSlotRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.FixedStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_fixedSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.SortedStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_sortedArraySparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.DynamicStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_dynamicSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.StepwiseStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseB), MissingValuePolicy.Intersection),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAddUnionWithZero() =>
        Case.Pairing switch
        {
            MixedFamilyPairing.FixedSorted => TimeSeriesMath.Add(_fixedSlotSparse, _sortedArrayRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.FixedDynamic => TimeSeriesMath.Add(_fixedSlotSparse, _dynamicSlotRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.SortedDynamic => TimeSeriesMath.Add(_sortedArraySparse, _dynamicSlotRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.FixedStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_fixedSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.SortedStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_sortedArraySparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.DynamicStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Sparse(_dynamicSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.StepwiseStepwise => TimeSeriesMath.Add(MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseB), MissingValuePolicy.UnionWithZero),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryMultiplyIntersection() =>
        Case.Pairing switch
        {
            MixedFamilyPairing.FixedSorted => TimeSeriesMath.Multiply(_fixedSlotSparse, _sortedArrayRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.FixedDynamic => TimeSeriesMath.Multiply(_fixedSlotSparse, _dynamicSlotRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.SortedDynamic => TimeSeriesMath.Multiply(_sortedArraySparse, _dynamicSlotRight, MissingValuePolicy.Intersection),
            MixedFamilyPairing.FixedStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_fixedSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.SortedStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_sortedArraySparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.DynamicStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_dynamicSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.Intersection),
            MixedFamilyPairing.StepwiseStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseB), MissingValuePolicy.Intersection),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryMultiplyUnionWithZero() =>
        Case.Pairing switch
        {
            MixedFamilyPairing.FixedSorted => TimeSeriesMath.Multiply(_fixedSlotSparse, _sortedArrayRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.FixedDynamic => TimeSeriesMath.Multiply(_fixedSlotSparse, _dynamicSlotRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.SortedDynamic => TimeSeriesMath.Multiply(_sortedArraySparse, _dynamicSlotRight, MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.FixedStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_fixedSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.SortedStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_sortedArraySparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.DynamicStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Sparse(_dynamicSlotSparse), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MissingValuePolicy.UnionWithZero),
            MixedFamilyPairing.StepwiseStepwise => TimeSeriesMath.Multiply(MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA), MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseB), MissingValuePolicy.UnionWithZero),
            _ => throw new NotSupportedException()
        };
}

[MemoryDiagnoser]
public class MixedFamilySpecializationBenchmarks
{
    private const Period SourcePeriod = Period.FiveMinutes;
    private const Period AggregationTargetPeriod = Period.Hour;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const Period ResampleTargetPeriod = Period.FiveMinutes;

    private MixedFamilyOperationBenchmarkData _data = null!;
    private FixedSlotTimeSeries<double> _fixedSlotSparse = null!;
    private SortedArrayTimeSeries<double> _sortedArraySparse = null!;
    private SortedArrayTimeSeries<double> _sortedArrayRight = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotRight = null!;
    private StepwiseTimeSeries<double> _stepwiseA = null!;
    private StepwiseTimeSeries<double> _stepwiseB = null!;
    private SortedArrayTimeSeries<double> _sortedArrayResample = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotResample = null!;
    private StepwiseTimeSeries<double> _stepwiseResample = null!;

    [Params(1_000, 10_000)]
    public int PointCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = MixedFamilyOperationBenchmarkDataFactory.Create(PointCount);
        _fixedSlotSparse = new FixedSlotTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArraySparse = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayRight = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _dynamicSlotRight = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _sortedArrayResample = new SortedArrayTimeSeries<double>(ResampleSourcePeriod, PointCount);
        _dynamicSlotResample = new DynamicSlotTimeSeries<double>(ResampleSourcePeriod, AlignMode.Strict, PointCount);

        MixedFamilyBenchmarkHelpers.FillSeries(_fixedSlotSparse, _data.SparseTimestamps, _data.SparseValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_sortedArraySparse, _data.SparseTimestamps, _data.SparseValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_sortedArrayRight, _data.SparseRightTimestamps, _data.SparseRightValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_dynamicSlotRight, _data.SparseRightTimestamps, _data.SparseRightValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_sortedArrayResample, _data.ResampleSourceTimestamps, _data.ResampleValues);
        MixedFamilyBenchmarkHelpers.FillSeries(_dynamicSlotResample, _data.ResampleSourceTimestamps, _data.ResampleValues);

        _stepwiseA = MixedFamilyBenchmarkHelpers.CreateStepwise(
            _data.StepwiseInitialValueA,
            _data.StepwiseChangePointStarts,
            _data.StepwiseChangePointValuesA,
            _data.StepwiseStart,
            _data.StepwiseEnd,
            SourcePeriod);
        _stepwiseB = MixedFamilyBenchmarkHelpers.CreateStepwise(
            _data.StepwiseInitialValueB,
            _data.OverlapStepwiseChangePointStarts,
            _data.StepwiseChangePointValuesB,
            _data.OverlapStepwiseStart,
            _data.OverlapStepwiseEnd,
            SourcePeriod);
        _stepwiseResample = MixedFamilyBenchmarkHelpers.CreateStepwise(
            _data.StepwiseInitialValueA,
            _data.StepwiseChangePointStarts,
            _data.StepwiseChangePointValuesA,
            _data.StepwiseStart,
            _data.StepwiseStart.AddHours(PointCount - 1L),
            ResampleSourcePeriod);
    }

    [Benchmark]
    public FixedSlotTimeSeries<double> TryAddAsFixedSlotTimeSeries()
    {
        if (!TimeSeriesMath.TryAddAsFixedSlotTimeSeries(
            _sortedArraySparse,
            _dynamicSlotRight,
            out var result,
            MissingValuePolicy.UnionWithZero))
        {
            throw new InvalidOperationException("Expected fixed-slot add specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public DynamicSlotTimeSeries<double> TryAddAsDynamicSlotTimeSeries()
    {
        if (!TimeSeriesMath.TryAddAsDynamicSlotTimeSeries(
            _fixedSlotSparse,
            _sortedArrayRight,
            out var result,
            MissingValuePolicy.UnionWithZero))
        {
            throw new InvalidOperationException("Expected dynamic-slot add specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public StepwiseTimeSeries<double> TryAddAsBoundedStepwiseTimeSeries()
    {
        if (!TimeSeriesMath.TryAddAsBoundedStepwiseTimeSeries(
            _stepwiseA,
            _stepwiseB,
            out var result,
            MissingValuePolicy.Intersection))
        {
            throw new InvalidOperationException("Expected bounded-stepwise add specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public FixedSlotTimeSeries<double> TryAggregateAsFixedSlotTimeSeries()
    {
        if (!TimeSeriesAggregation.TryAggregateAsFixedSlotTimeSeries<double, double, SumAggregator<double>>(
            _sortedArraySparse,
            AggregationTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected fixed-slot aggregate specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public DynamicSlotTimeSeries<double> TryAggregateAsDynamicSlotTimeSeries()
    {
        if (!TimeSeriesAggregation.TryAggregateAsDynamicSlotTimeSeries<double, double, SumAggregator<double>>(
            _fixedSlotSparse,
            AggregationTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected dynamic-slot aggregate specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public StepwiseTimeSeries<double> TryAggregateAsBoundedStepwiseTimeSeries()
    {
        if (!TimeSeriesAggregation.TryAggregateAsBoundedStepwiseTimeSeries<double, double, SumAggregator<double>>(
            _stepwiseA,
            AggregationTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected bounded-stepwise aggregate specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public FixedSlotTimeSeries<double> TryResampleAsFixedSlotTimeSeries()
    {
        if (!TimeSeriesAggregation.TryResampleAsFixedSlotTimeSeries(
            _sortedArrayResample,
            ResampleTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected fixed-slot resample specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public DynamicSlotTimeSeries<double> TryResampleAsDynamicSlotTimeSeries()
    {
        if (!TimeSeriesAggregation.TryResampleAsDynamicSlotTimeSeries(
            _dynamicSlotResample,
            ResampleTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected dynamic-slot resample specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public StepwiseTimeSeries<double> TryResampleAsBoundedStepwiseTimeSeries()
    {
        if (!TimeSeriesAggregation.TryResampleAsBoundedStepwiseTimeSeries(
            _stepwiseResample,
            ResampleTargetPeriod,
            out var result))
        {
            throw new InvalidOperationException("Expected bounded-stepwise resample specialization to succeed.");
        }

        return result!;
    }

    [Benchmark]
    public IReadOnlySparseTimeSeries<double> SparseStepwiseUnionWithZeroDefaultSparseResult() =>
        TimeSeriesMath.Add(
            MixedFamilyBenchmarkHelpers.Sparse(_sortedArraySparse),
            MixedFamilyBenchmarkHelpers.Stepwise(_stepwiseA),
            MissingValuePolicy.UnionWithZero);

    [Benchmark]
    public FixedSlotTimeSeries<double> SparseStepwiseUnionWithZeroExplicitFixedSlotResult()
    {
        if (!TimeSeriesMath.TryAddAsFixedSlotTimeSeries(
            _sortedArraySparse,
            _stepwiseA,
            out var result,
            MissingValuePolicy.UnionWithZero))
        {
            throw new InvalidOperationException("Expected sparse + stepwise fixed-slot specialization to succeed.");
        }

        return result!;
    }
}

internal static class MixedFamilyBenchmarkHelpers
{
    public static IReadOnlySparseTimeSeries<double> Sparse(IReadOnlySparseTimeSeries<double> source) => source;

    public static IBoundedStepwiseTimeSeries<double> Stepwise(IBoundedStepwiseTimeSeries<double> source) => source;

    public static void FillSeries(
        ISparseTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps,
        IReadOnlyList<double> values)
    {
        for (var i = 0; i < timestamps.Count; i++)
            series[timestamps[i]] = values[i];
    }

    public static StepwiseTimeSeries<double> CreateStepwise(
        double initialValue,
        IReadOnlyList<int> changePointStarts,
        IReadOnlyList<double> changePointValues,
        DateTimeOffset start,
        DateTimeOffset end,
        Period period)
    {
        var series = new StepwiseTimeSeries<double>(period, start, end, initialValue);
        var stepMinutes = period == Period.Hour ? 60 : 5;
        var slotCount = checked((int)((end - start).Ticks / TimeSpan.FromMinutes(stepMinutes).Ticks) + 1);

        for (var i = 0; i < changePointStarts.Count; i++)
        {
            var startSlot = changePointStarts[i];
            if (startSlot >= slotCount)
                break;

            var endExclusiveSlot = i + 1 < changePointStarts.Count ? changePointStarts[i + 1] : slotCount;
            series.SetSegment(
                start.AddMinutes(startSlot * (long)stepMinutes),
                start.AddMinutes(endExclusiveSlot * (long)stepMinutes),
                changePointValues[i]);
        }

        return series;
    }
}
