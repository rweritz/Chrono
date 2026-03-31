using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesAggregationBenchmarks
{
    private const int N = 10000;

    private readonly FixedSlotTimeSeries<double> _fixedSlot;
    private readonly SortedArrayTimeSeries<double> _sortedArray;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlot;

    public TimeSeriesAggregationBenchmarks()
    {
        // N five-minute points = ~35 hours of data, aggregates into ~35 hourly buckets
        var start = new DateTimeOffset(2022, 2, 6, 0, 0, 0, TimeSpan.Zero);

        _fixedSlot = new FixedSlotTimeSeries<double>(Period.FiveMinutes, N);
        _sortedArray = new SortedArrayTimeSeries<double>(Period.FiveMinutes, N);

        for (var i = 0; i < N; i++)
        {
            var t = start.AddMinutes(i * 5);
            var v = (i % 100) + 1.0;
            _fixedSlot[t] = v;
            _sortedArray[t] = v;
        }

        // N hourly points for calendar aggregation (Hour → Month)
        _dynamicSlot = new DynamicSlotTimeSeries<double>(Period.Hour, AlignMode.Strict, N);

        for (var i = 0; i < N; i++)
        {
            var t = start.AddHours(i);
            var v = (i % 100) + 1.0;
            _dynamicSlot[t] = v;
        }
    }

    // ── FixedSlot aggregations ─────────────────────────────────────────────

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotSum()
        => TimeSeriesAggregation.Sum(_fixedSlot, Period.Hour);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotAverage()
        => TimeSeriesAggregation.Average(_fixedSlot, Period.Hour);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotMin()
        => TimeSeriesAggregation.Min(_fixedSlot, Period.Hour);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotMax()
        => TimeSeriesAggregation.Max(_fixedSlot, Period.Hour);

    [Benchmark]
    public FixedSlotTimeSeries<int> FixedSlotCount()
        => TimeSeriesAggregation.Count(_fixedSlot, Period.Hour);

    // ── SortedArray aggregations ──────────────────────────────────────────────

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArraySum()
        => TimeSeriesAggregation.Sum(_sortedArray, Period.Hour);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayAverage()
        => TimeSeriesAggregation.Average(_sortedArray, Period.Hour);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayMin()
        => TimeSeriesAggregation.Min(_sortedArray, Period.Hour);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayMax()
        => TimeSeriesAggregation.Max(_sortedArray, Period.Hour);

    [Benchmark]
    public SortedArrayTimeSeries<int> SortedArrayCount()
        => TimeSeriesAggregation.Count(_sortedArray, Period.Hour);

    // ── DynamicSlot aggregations (Hour → Month) ──────────────────────────

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotSum()
        => TimeSeriesAggregation.Sum(_dynamicSlot, Period.Month);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotAverage()
        => TimeSeriesAggregation.Average(_dynamicSlot, Period.Month);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotMin()
        => TimeSeriesAggregation.Min(_dynamicSlot, Period.Month);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotMax()
        => TimeSeriesAggregation.Max(_dynamicSlot, Period.Month);

    [Benchmark]
    public DynamicSlotTimeSeries<int> DynamicSlotCount()
        => TimeSeriesAggregation.Count(_dynamicSlot, Period.Month);
}
