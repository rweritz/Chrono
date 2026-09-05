using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public enum CalendarPeriodImplementation
{
    DynamicSlot,
    SortedArray,
    DynamicSlotTruncate
}

public enum CalendarPeriodOperation
{
    MonthOrderedInsert,
    MonthRandomInsert,
    MonthOrderedLookup,
    MonthRandomLookup,
    ScalarMultiply,
    BinaryAdd,
    AggregateSumMonthToYear,
    AggregateCountMonthToYear,
    ResampleMonthToYear,
    MisalignedInsert,
    MisalignedLookup
}

public sealed class CalendarPeriodBenchmarkCase
{
    public CalendarPeriodBenchmarkCase(CalendarPeriodImplementation implementation, CalendarPeriodOperation operation)
    {
        Implementation = implementation;
        Operation = operation;
    }

    public CalendarPeriodImplementation Implementation { get; }

    public CalendarPeriodOperation Operation { get; }

    public override string ToString() => $"{Implementation}/{Operation}";
}

public sealed record CalendarPeriodBenchmarkData(
    DateTimeOffset[] OrderedTimestamps,
    DateTimeOffset[] MisalignedTimestamps,
    DateTimeOffset[] ResampleSourceTimestamps,
    int[] RandomInsertIndices,
    DateTimeOffset[] RandomLookupTimestamps,
    DateTimeOffset[] MisalignedRandomLookupTimestamps,
    double[] Values,
    double[] ResampleSourceValues);

public static class CalendarPeriodBenchmarkDataFactory
{
    private const int InsertOrderSeed = 41041;
    private const int LookupOrderSeed = 41042;
    private const int ValueSeed = 41043;
    private const int ResampleValueSeed = 41044;

    public static CalendarPeriodBenchmarkData Create(int pointCount)
    {
        var start = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var orderedTimestamps = new DateTimeOffset[pointCount];
        var misalignedTimestamps = new DateTimeOffset[pointCount];
        var resampleSourceTimestamps = new DateTimeOffset[pointCount];
        var values = new double[pointCount];
        var resampleSourceValues = new double[pointCount];
        var valueRandom = new Random(ValueSeed);
        var resampleValueRandom = new Random(ResampleValueSeed);

        for (var i = 0; i < pointCount; i++)
        {
            orderedTimestamps[i] = start.AddMonths(i);
            misalignedTimestamps[i] = orderedTimestamps[i].AddDays(14).AddHours(6);
            // Explicit resample uses one month-period point per target year; aggregate sum/count cover many-month-to-year workloads.
            resampleSourceTimestamps[i] = new DateTimeOffset(start.Year + i, 1, 1, 0, 0, 0, TimeSpan.Zero);
            values[i] = valueRandom.NextDouble() * 1000d;
            resampleSourceValues[i] = resampleValueRandom.NextDouble() * 1000d;
        }

        var randomInsertIndices = ShuffleIndices(pointCount, InsertOrderSeed);
        var randomLookupTimestamps = Shuffle(orderedTimestamps, LookupOrderSeed);
        var misalignedRandomLookupTimestamps = Shuffle(misalignedTimestamps, LookupOrderSeed);

        return new CalendarPeriodBenchmarkData(
            orderedTimestamps,
            misalignedTimestamps,
            resampleSourceTimestamps,
            randomInsertIndices,
            randomLookupTimestamps,
            misalignedRandomLookupTimestamps,
            values,
            resampleSourceValues);
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

    private static DateTimeOffset[] Shuffle(DateTimeOffset[] source, int seed)
    {
        var shuffled = (DateTimeOffset[])source.Clone();
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
public class CalendarPeriodBenchmarks
{
    private const Period SourcePeriod = Period.Month;
    private const Period TargetPeriod = Period.Year;

    // FixedSlotTimeSeries is intentionally excluded: it only supports fixed-tick periods,
    // while this scenario exercises variable-length calendar month/year slot math.
    private static readonly CalendarPeriodBenchmarkCase[] AllCases =
    [
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.MonthOrderedInsert),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.MonthRandomInsert),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.MonthOrderedLookup),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.MonthRandomLookup),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.ScalarMultiply),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.BinaryAdd),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.AggregateSumMonthToYear),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.AggregateCountMonthToYear),
        new(CalendarPeriodImplementation.DynamicSlot, CalendarPeriodOperation.ResampleMonthToYear),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.MonthOrderedInsert),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.MonthRandomInsert),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.MonthOrderedLookup),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.MonthRandomLookup),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.ScalarMultiply),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.BinaryAdd),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.AggregateSumMonthToYear),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.AggregateCountMonthToYear),
        new(CalendarPeriodImplementation.SortedArray, CalendarPeriodOperation.ResampleMonthToYear),
        new(CalendarPeriodImplementation.DynamicSlotTruncate, CalendarPeriodOperation.MisalignedInsert),
        new(CalendarPeriodImplementation.DynamicSlotTruncate, CalendarPeriodOperation.MisalignedLookup)
    ];

    private CalendarPeriodBenchmarkData _data = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotA = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotB = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotTruncate = null!;
    private DynamicSlotTimeSeries<double> _dynamicSlotResample = null!;
    private SortedArrayTimeSeries<double> _sortedArrayA = null!;
    private SortedArrayTimeSeries<double> _sortedArrayB = null!;
    private SortedArrayTimeSeries<double> _sortedArrayResample = null!;

    public IEnumerable<CalendarPeriodBenchmarkCase> Cases => AllCases;

    [ParamsSource(nameof(Cases))]
    public CalendarPeriodBenchmarkCase Case { get; set; } = null!;

    [Params(1_000, 5_000, 8_000)]
    public int PointCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = CalendarPeriodBenchmarkDataFactory.Create(PointCount);

        _dynamicSlotA = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _dynamicSlotB = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _dynamicSlotTruncate = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Truncate, PointCount);
        _dynamicSlotResample = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, PointCount);
        _sortedArrayA = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayB = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);
        _sortedArrayResample = new SortedArrayTimeSeries<double>(SourcePeriod, PointCount);

        FillSeries(_dynamicSlotA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_dynamicSlotB, _data.OrderedTimestamps, _data.Values, 1d);
        FillSeries(_dynamicSlotTruncate, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_dynamicSlotResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
        FillSeries(_sortedArrayA, _data.OrderedTimestamps, _data.Values, 0d);
        FillSeries(_sortedArrayB, _data.OrderedTimestamps, _data.Values, 1d);
        FillSeries(_sortedArrayResample, _data.ResampleSourceTimestamps, _data.ResampleSourceValues, 0d);
    }

    [Benchmark]
    public object Run() =>
        Case.Operation switch
        {
            CalendarPeriodOperation.MonthOrderedInsert => MonthOrderedInsert(),
            CalendarPeriodOperation.MonthRandomInsert => MonthRandomInsert(),
            CalendarPeriodOperation.MonthOrderedLookup => MonthOrderedLookup(),
            CalendarPeriodOperation.MonthRandomLookup => MonthRandomLookup(),
            CalendarPeriodOperation.ScalarMultiply => ScalarMultiply(),
            CalendarPeriodOperation.BinaryAdd => BinaryAdd(),
            CalendarPeriodOperation.AggregateSumMonthToYear => AggregateSumMonthToYear(),
            CalendarPeriodOperation.AggregateCountMonthToYear => AggregateCountMonthToYear(),
            CalendarPeriodOperation.ResampleMonthToYear => ResampleMonthToYear(),
            CalendarPeriodOperation.MisalignedInsert => MisalignedInsert(),
            CalendarPeriodOperation.MisalignedLookup => MisalignedLookup(),
            _ => throw new NotSupportedException()
        };

    private int MonthOrderedInsert()
    {
        var series = CreateEmptySeries(PointCount);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, 0d);
        return series.ExplicitPointCount;
    }

    private int MonthRandomInsert()
    {
        var series = CreateEmptySeries(PointCount, preWindowSlotSeries: true);
        FillSeries(series, _data.OrderedTimestamps, _data.Values, _data.RandomInsertIndices, 0d);
        return series.ExplicitPointCount;
    }

    private double MonthOrderedLookup() => SumLookups(ActiveSeriesA(), _data.OrderedTimestamps);

    private double MonthRandomLookup() => SumLookups(ActiveSeriesA(), _data.RandomLookupTimestamps);

    private object ScalarMultiply() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => TimeSeriesMath.Multiply(_dynamicSlotA, 1.5d),
            CalendarPeriodImplementation.SortedArray => TimeSeriesMath.Multiply(_sortedArrayA, 1.5d),
            _ => throw new NotSupportedException()
        };

    private object BinaryAdd() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB),
            CalendarPeriodImplementation.SortedArray => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB),
            _ => throw new NotSupportedException()
        };

    private object AggregateSumMonthToYear() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => TimeSeriesAggregation.Sum(_dynamicSlotA, TargetPeriod),
            CalendarPeriodImplementation.SortedArray => TimeSeriesAggregation.Sum(_sortedArrayA, TargetPeriod),
            _ => throw new NotSupportedException()
        };

    private object AggregateCountMonthToYear() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => TimeSeriesAggregation.Count(_dynamicSlotA, TargetPeriod),
            CalendarPeriodImplementation.SortedArray => TimeSeriesAggregation.Count(_sortedArrayA, TargetPeriod),
            _ => throw new NotSupportedException()
        };

    private object ResampleMonthToYear() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => TimeSeriesAggregation.Resample(_dynamicSlotResample, TargetPeriod),
            CalendarPeriodImplementation.SortedArray => TimeSeriesAggregation.Resample(_sortedArrayResample, TargetPeriod),
            _ => throw new NotSupportedException()
        };

    private int MisalignedInsert()
    {
        var series = new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Truncate, PointCount);
        FillSeries(series, _data.MisalignedTimestamps, _data.Values, 0d);
        return series.ExplicitPointCount;
    }

    private double MisalignedLookup() =>
        SumLookups(_dynamicSlotTruncate, _data.MisalignedRandomLookupTimestamps);

    private ISparseTimeSeries<double> ActiveSeriesA() =>
        Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => _dynamicSlotA,
            CalendarPeriodImplementation.SortedArray => _sortedArrayA,
            _ => throw new NotSupportedException()
        };

    private ISparseTimeSeries<double> CreateEmptySeries(int capacity, bool preWindowSlotSeries = false)
    {
        ISparseTimeSeries<double> series = Case.Implementation switch
        {
            CalendarPeriodImplementation.DynamicSlot => new DynamicSlotTimeSeries<double>(SourcePeriod, AlignMode.Strict, capacity),
            CalendarPeriodImplementation.SortedArray => new SortedArrayTimeSeries<double>(SourcePeriod, capacity),
            _ => throw new NotSupportedException()
        };

        if (preWindowSlotSeries && Case.Implementation == CalendarPeriodImplementation.DynamicSlot)
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
