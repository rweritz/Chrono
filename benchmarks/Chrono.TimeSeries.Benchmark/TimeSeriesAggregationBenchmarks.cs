using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesAggregationBenchmarks
{
    private const int N = 10000;

    private readonly RegularTimeSeries<double> _regular;
    private readonly SparseTimeSeries<double> _sparse;

    public TimeSeriesAggregationBenchmarks()
    {
        // N five-minute points = ~35 hours of data, aggregates into ~35 hourly buckets
        var start = new DateTimeOffset(2022, 2, 6, 0, 0, 0, TimeSpan.Zero);

        _regular = new RegularTimeSeries<double>(Period.FiveMinutes, N);
        _sparse = new SparseTimeSeries<double>(Period.FiveMinutes, N);

        for (var i = 0; i < N; i++)
        {
            var t = start.AddMinutes(i * 5);
            var v = (i % 100) + 1.0;
            _regular[t] = v;
            _sparse[t] = v;
        }
    }

    // ── Regular aggregations ─────────────────────────────────────────────

    [Benchmark]
    public RegularTimeSeries<double> RegularSum()
        => TimeSeriesAggregation.Sum(_regular, Period.Hour);

    [Benchmark]
    public RegularTimeSeries<double> RegularAverage()
        => TimeSeriesAggregation.Average(_regular, Period.Hour);

    [Benchmark]
    public RegularTimeSeries<double> RegularMin()
        => TimeSeriesAggregation.Min(_regular, Period.Hour);

    [Benchmark]
    public RegularTimeSeries<double> RegularMax()
        => TimeSeriesAggregation.Max(_regular, Period.Hour);

    [Benchmark]
    public RegularTimeSeries<int> RegularCount()
        => TimeSeriesAggregation.Count(_regular, Period.Hour);

    // ── Sparse aggregations ──────────────────────────────────────────────

    [Benchmark]
    public SparseTimeSeries<double> SparseSum()
        => TimeSeriesAggregation.Sum(_sparse, Period.Hour);

    [Benchmark]
    public SparseTimeSeries<double> SparseAverage()
        => TimeSeriesAggregation.Average(_sparse, Period.Hour);

    [Benchmark]
    public SparseTimeSeries<double> SparseMin()
        => TimeSeriesAggregation.Min(_sparse, Period.Hour);

    [Benchmark]
    public SparseTimeSeries<double> SparseMax()
        => TimeSeriesAggregation.Max(_sparse, Period.Hour);

    [Benchmark]
    public SparseTimeSeries<int> SparseCount()
        => TimeSeriesAggregation.Count(_sparse, Period.Hour);
}
