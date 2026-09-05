using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

[MemoryDiagnoser]
public class StepwiseBenchmarks
{
    private const Period SourcePeriod = Period.FiveMinutes;
    private const Period AggregationTargetPeriod = Period.Hour;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const Period ResampleTargetPeriod = Period.FiveMinutes;
    private const int RandomSingleSlotOperationCount = 512;
    private const int ShortSegmentOperationCount = 256;
    private const int ShortSegmentLength = 4;
    private const int LongSegmentOperationCount = 64;
    private const int ExpansionOperationCount = 256;

    private StepwiseBenchmarkData _data = null!;
    private StepwiseTimeSeries<double> _stepwiseA = null!;
    private StepwiseTimeSeries<double> _stepwiseB = null!;
    private StepwiseTimeSeries<double> _stepwiseResample = null!;
    private FixedSlotTimeSeries<double> _fixedSlotMaterialized = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotMaterialized = null!;

    [Params(1_000, 10_000, 100_000)]
    public int LogicalSlotCount { get; set; }

    [Params(0.001, 0.01, 0.10)]
    public double ChangePointDensity { get; set; }

    // Diagnostic benchmark methods below make these compression-shape values visible
    // in BenchmarkDotNet output without adding custom column infrastructure.
    public int ChangePointCount => _stepwiseA?.ChangePointCount ?? 0;

    public double CompressionRatio => LogicalSlotCount == 0 ? 0d : (double)ChangePointCount / LogicalSlotCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = StepwiseBenchmarkDataFactory.Create(LogicalSlotCount, ChangePointDensity);
        _stepwiseA = CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End);
        _stepwiseB = CreateStepwise(_data.InitialValueB, _data.ChangePointValuesB, SourcePeriod, _data.Start, _data.End);
        _stepwiseResample = CreateStepwise(
            _data.InitialValueA,
            _data.ResampleChangePointValues,
            ResampleSourcePeriod,
            _data.ResampleStart,
            _data.ResampleEnd,
            _data.ResampleChangePointStarts);
        _fixedSlotMaterialized = CreateFixedSlotMaterialized();
        _dynamicSlotMaterialized = CreateDynamicSlotMaterialized();
    }

    [Benchmark(Baseline = true)]
    public int InitialBoundedSeriesConstruction() =>
        CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End).ChangePointCount;

    [Benchmark(OperationsPerInvoke = RandomSingleSlotOperationCount)]
    public int RandomSingleSlotChanges()
    {
        var series = CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End);
        for (var i = 0; i < _data.RandomSingleSlotIndices.Length; i++)
            series[_data.OrderedTimestamps[_data.RandomSingleSlotIndices[i]]] = _data.RandomSingleSlotValues[i];

        return series.ChangePointCount;
    }

    [Benchmark(OperationsPerInvoke = ShortSegmentOperationCount)]
    public int ShortSegmentWrites()
    {
        var series = CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End);
        for (var i = 0; i < _data.ShortSegmentStarts.Length; i++)
            WriteSegment(series, _data.ShortSegmentStarts[i], ShortSegmentLength, _data.ShortSegmentValues[i]);

        return series.ChangePointCount;
    }

    [Benchmark(OperationsPerInvoke = LongSegmentOperationCount)]
    public int LongSegmentWrites()
    {
        var series = CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End);
        var segmentLength = LongSegmentLength();
        for (var i = 0; i < _data.LongSegmentStarts.Length; i++)
            WriteSegment(series, _data.LongSegmentStarts[i], segmentLength, _data.LongSegmentValues[i]);

        return series.ChangePointCount;
    }

    [Benchmark(OperationsPerInvoke = ExpansionOperationCount)]
    public int ContiguousLeftExpansion()
    {
        var expansionCount = _data.LeftExpansionValues.Length;
        var series = new StepwiseTimeSeries<double>(
            SourcePeriod,
            _data.OrderedTimestamps[expansionCount],
            _data.End,
            _data.InitialValueA);

        for (var i = expansionCount - 1; i >= 0; i--)
        {
            series.SetSegment(
                _data.OrderedTimestamps[i],
                _data.OrderedTimestamps[i + 1],
                _data.LeftExpansionValues[i]);
        }

        return series.LogicalSlotCount;
    }

    [Benchmark(OperationsPerInvoke = ExpansionOperationCount)]
    public int ContiguousRightExpansion()
    {
        var expansionCount = _data.RightExpansionValues.Length;
        var lastInitialSlot = LogicalSlotCount - expansionCount - 1;
        var series = new StepwiseTimeSeries<double>(
            SourcePeriod,
            _data.Start,
            _data.OrderedTimestamps[lastInitialSlot],
            _data.InitialValueA);

        for (var i = 0; i < expansionCount; i++)
        {
            var slot = lastInitialSlot + i + 1;
            series.SetSegment(
                _data.OrderedTimestamps[slot],
                _data.OrderedTimestamps[slot].AddMinutes(5),
                _data.RightExpansionValues[i]);
        }

        return series.LogicalSlotCount;
    }

    [Benchmark]
    public double OrderedLogicalLookupAcrossAllSlots() => SumLookups(_stepwiseA, _data.OrderedTimestamps);

    [Benchmark]
    public double RandomLogicalLookup() => SumLookups(_stepwiseA, _data.RandomLookupTimestamps);

    [Benchmark]
    public double GetChangePoints()
    {
        var sum = 0d;
        foreach (var point in _stepwiseA.GetChangePoints())
            sum += point.Value;

        return sum;
    }

    [Benchmark]
    public int Clear()
    {
        var series = CreateStepwise(_data.InitialValueA, _data.ChangePointValuesA, SourcePeriod, _data.Start, _data.End);
        series.Clear();
        return series.ChangePointCount;
    }

    [Benchmark]
    public object ScalarAdd() => TimeSeriesMath.Add(_stepwiseA, 5d);

    [Benchmark]
    public object ScalarMultiply() => TimeSeriesMath.Multiply(_stepwiseA, 1.5d);

    [Benchmark]
    public object StepwiseBinaryAdd() => TimeSeriesMath.Add(_stepwiseA, _stepwiseB);

    [Benchmark]
    public object StepwiseBinaryMultiply() => TimeSeriesMath.Multiply(_stepwiseA, _stepwiseB);

    [Benchmark]
    public object AggregateSum() => TimeSeriesAggregation.Sum(_stepwiseA, AggregationTargetPeriod);

    [Benchmark]
    public object AggregateAverage() => TimeSeriesAggregation.Average(_stepwiseA, AggregationTargetPeriod);

    [Benchmark]
    public object AggregateCount() => TimeSeriesAggregation.Count(_stepwiseA, AggregationTargetPeriod);

    [Benchmark]
    public object Resample() => TimeSeriesAggregation.Resample(_stepwiseResample, ResampleTargetPeriod);

    [Benchmark]
    public int DiagnosticLogicalSlotCount() => _stepwiseA.LogicalSlotCount;

    [Benchmark]
    public int DiagnosticChangePointCount() => ChangePointCount;

    [Benchmark]
    public double DiagnosticCompressionRatio() => CompressionRatio;

    [Benchmark]
    public int MaterializedEquivalentFixedSlotConstruction() => CreateFixedSlotMaterialized().ExplicitPointCount;

    [Benchmark]
    public int MaterializedEquivalentDynamicSlotConstruction() => CreateDynamicSlotMaterialized().ExplicitPointCount;

    [Benchmark]
    public double MaterializedEquivalentFixedSlotOrderedLookupComparison() =>
        SumLookups(_fixedSlotMaterialized, _data.OrderedTimestamps);

    [Benchmark]
    public double MaterializedEquivalentDynamicSlotOrderedLookupComparison() =>
        SumLookups(_dynamicSlotMaterialized, _data.OrderedTimestamps);

    private StepwiseTimeSeries<double> CreateStepwise(
        double initialValue,
        IReadOnlyList<double> changePointValues,
        Period period,
        DateTimeOffset start,
        DateTimeOffset end)
        => CreateStepwise(initialValue, changePointValues, period, start, end, _data.ChangePointStarts);

    private static StepwiseTimeSeries<double> CreateStepwise(
        double initialValue,
        IReadOnlyList<double> changePointValues,
        Period period,
        DateTimeOffset start,
        DateTimeOffset end,
        IReadOnlyList<int> changePointStarts)
    {
        var series = new StepwiseTimeSeries<double>(period, start, end, initialValue);
        var stepMinutes = period == Period.Hour ? 60 : 5;

        for (var i = 0; i < changePointStarts.Count; i++)
        {
            var startSlot = changePointStarts[i];
            var endExclusiveSlot = i + 1 < changePointStarts.Count ? changePointStarts[i + 1] : SlotCount(start, end, stepMinutes);
            series.SetSegment(
                start.AddMinutes(startSlot * (long)stepMinutes),
                start.AddMinutes(endExclusiveSlot * (long)stepMinutes),
                changePointValues[i]);
        }

        return series;
    }

    private FixedSlotTimeSeries<double> CreateFixedSlotMaterialized()
    {
        var series = new FixedSlotTimeSeries<double>(SourcePeriod, LogicalSlotCount);
        FillMaterialized(series, _data.DenseValuesA);
        return series;
    }

    private DynamicSlotTimeSeries<double> CreateDynamicSlotMaterialized()
    {
        var series = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, LogicalSlotCount);
        FillMaterialized(series, _data.DenseValuesA);
        return series;
    }

    private void FillMaterialized(ISparseTimeSeries<double> series, IReadOnlyList<double> values)
    {
        for (var i = 0; i < values.Count; i++)
            series[_data.OrderedTimestamps[i]] = values[i];
    }

    private void WriteSegment(StepwiseTimeSeries<double> series, int startSlot, int length, double value)
    {
        series.SetSegment(
            _data.OrderedTimestamps[startSlot],
            _data.OrderedTimestamps[startSlot].AddMinutes(length * 5L),
            value);
    }

    private int LongSegmentLength() =>
        Math.Clamp(LogicalSlotCount / 20, 64, Math.Max(64, LogicalSlotCount / 2));

    private static int SlotCount(DateTimeOffset start, DateTimeOffset end, int stepMinutes) =>
        checked((int)((end - start).Ticks / TimeSpan.FromMinutes(stepMinutes).Ticks) + 1);

    private static double SumLookups(
        IReadOnlyTimeSeries<double> series,
        IReadOnlyList<DateTimeOffset> timestamps)
    {
        var sum = 0d;
        for (var i = 0; i < timestamps.Count; i++)
            sum += series[timestamps[i]];

        return sum;
    }
}
