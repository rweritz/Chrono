using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesPerformance
{
    private const int N = 10000;
    private readonly IList<DateTimeOffset> _dates;

    private readonly SparseTimeSeries<double> _sparse;
    private readonly RegularTimeSeries<double> _regular;

    public TimeSeriesPerformance()
    {
        var initDate = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        _dates = new List<DateTimeOffset>(N);
        for (var i = 0; i < N; i++)
            _dates.Add(initDate.AddMinutes(i * 5));

        _sparse = new SparseTimeSeries<double>(Period.FiveMinutes);
        _regular = new RegularTimeSeries<double>(Period.FiveMinutes);

        foreach (var date in _dates)
        {
            _sparse[date] = 1;
            _regular[date] = 1;
        }
    }

    [Benchmark]
    public void SparseOrderedInsert()
    {
        var series = new SparseTimeSeries<double>(Period.FiveMinutes);
        foreach (var date in _dates)
            series[date] = 1;
    }

    [Benchmark]
    public void RegularOrderedInsert()
    {
        var series = new RegularTimeSeries<double>(Period.FiveMinutes);
        foreach (var date in _dates)
            series[date] = 1;
    }

    [Benchmark]
    public void SparseOrderedAccess()
    {
        var sum = 0d;
        foreach (var date in _dates)
            sum += _sparse[date];
    }

    [Benchmark]
    public void RegularOrderedAccess()
    {
        var sum = 0d;
        foreach (var date in _dates)
            sum += _regular[date];
    }

    [Benchmark]
    public void SparseScalarMultiply()
    {
        var result = TimeSeriesMath.Multiply(_sparse, 1.5d);
    }

    [Benchmark]
    public void RegularScalarMultiply()
    {
        var result = TimeSeriesMath.Multiply(_regular, 1.5d);
    }
}
