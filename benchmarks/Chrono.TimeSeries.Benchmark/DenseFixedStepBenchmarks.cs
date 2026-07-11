using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public enum DenseFixedStepImplementation
{
    FixedSlot,
    DynamicSlot,
    SortedArray
}

[MemoryDiagnoser]
public class DenseFixedStepBenchmarks
{
    private const Period DenseSourcePeriod = Period.FiveMinutes;
    private const Period AggregationTargetPeriod = Period.Hour;
    private const Period ResampleSourcePeriod = Period.Hour;
    private const Period ResampleTargetPeriod = Period.FiveMinutes;

    private DenseFixedStepBenchmarkData _data = null!;
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
        DenseFixedStepImplementation.FixedSlot,
        DenseFixedStepImplementation.DynamicSlot,
        DenseFixedStepImplementation.SortedArray)]
    public DenseFixedStepImplementation Implementation { get; set; }

    [Params(1_000, 10_000, 100_000)]
    public int PointCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = DenseFixedStepBenchmarkDataFactory.Create(PointCount);

        _fixedSlotA = new FixedSlotTimeSeries<double>(DenseSourcePeriod, PointCount);
        _fixedSlotB = new FixedSlotTimeSeries<double>(DenseSourcePeriod, PointCount);
        _dynamicSlotA = new DynamicSlotTimeSeries<double>(DenseSourcePeriod, AlignMode.Strict, PointCount);
        _dynamicSlotB = new DynamicSlotTimeSeries<double>(DenseSourcePeriod, AlignMode.Strict, PointCount);
        _sortedArrayA = new SortedArrayTimeSeries<double>(DenseSourcePeriod, PointCount);
        _sortedArrayB = new SortedArrayTimeSeries<double>(DenseSourcePeriod, PointCount);
        _fixedSlotResample = new FixedSlotTimeSeries<double>(ResampleSourcePeriod, PointCount);
        _dynamicSlotResample = new DynamicSlotTimeSeries<double>(ResampleSourcePeriod, AlignMode.Strict, PointCount);
        _sortedArrayResample = new SortedArrayTimeSeries<double>(ResampleSourcePeriod, PointCount);

        FillSeries(_fixedSlotA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_fixedSlotB, _data.OrderedTimestamps, _data.Values, 1d);
        FillSeries(_dynamicSlotA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_dynamicSlotB, _data.OrderedTimestamps, _data.Values, 1d);
        FillSeries(_sortedArrayA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_sortedArrayB, _data.OrderedTimestamps, _data.Values, 1d);
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
    public double OrderedLookup() => SumLookups(ActiveSeriesA(), _data.OrderedTimestamps);

    [Benchmark]
    public double RandomLookup() => SumLookups(ActiveSeriesA(), _data.RandomLookupTimestamps);

    [Benchmark]
    public object ScalarAdd() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesMath.Add(_fixedSlotA, 5d),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, 5d),
            DenseFixedStepImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, 5d),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object ScalarMultiply() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesMath.Multiply(_fixedSlotA, 1.5d),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesMath.Multiply(_dynamicSlotA, 1.5d),
            DenseFixedStepImplementation.SortedArray => TimeSeriesMath.Multiply(_sortedArrayA, 1.5d),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryAdd() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesMath.Add(_fixedSlotA, _fixedSlotB),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB),
            DenseFixedStepImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object BinaryMultiply() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesMath.Multiply(_fixedSlotA, _fixedSlotB),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesMath.Multiply(_dynamicSlotA, _dynamicSlotB),
            DenseFixedStepImplementation.SortedArray => TimeSeriesMath.Multiply(_sortedArrayA, _sortedArrayB),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateSum() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Sum(_fixedSlotA, AggregationTargetPeriod),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Sum(_dynamicSlotA, AggregationTargetPeriod),
            DenseFixedStepImplementation.SortedArray => TimeSeriesAggregation.Sum(_sortedArrayA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object AggregateAverage() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Average(_fixedSlotA, AggregationTargetPeriod),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Average(_dynamicSlotA, AggregationTargetPeriod),
            DenseFixedStepImplementation.SortedArray => TimeSeriesAggregation.Average(_sortedArrayA, AggregationTargetPeriod),
            _ => throw new NotSupportedException()
        };

    [Benchmark]
    public object Resample() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => TimeSeriesAggregation.Resample((IReadOnlySparseTimeSeries<double>)_fixedSlotResample, ResampleTargetPeriod),
            DenseFixedStepImplementation.DynamicSlot => TimeSeriesAggregation.Resample(_dynamicSlotResample, ResampleTargetPeriod),
            DenseFixedStepImplementation.SortedArray => TimeSeriesAggregation.Resample(_sortedArrayResample, ResampleTargetPeriod),
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> ActiveSeriesA() =>
        Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => _fixedSlotA,
            DenseFixedStepImplementation.DynamicSlot => _dynamicSlotA,
            DenseFixedStepImplementation.SortedArray => _sortedArrayA,
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> CreateEmptySeries(int capacity, bool preWindowSlotSeries = false)
    {
        ISparseTimeSeries<double> series = Implementation switch
        {
            DenseFixedStepImplementation.FixedSlot => new FixedSlotTimeSeries<double>(DenseSourcePeriod, capacity),
            DenseFixedStepImplementation.DynamicSlot => new DynamicSlotTimeSeries<double>(DenseSourcePeriod, AlignMode.Strict, capacity),
            DenseFixedStepImplementation.SortedArray => new SortedArrayTimeSeries<double>(DenseSourcePeriod, capacity),
            _ => throw new NotSupportedException()
        };

        if (preWindowSlotSeries && Implementation != DenseFixedStepImplementation.SortedArray)
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
