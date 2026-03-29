using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesMathBenchmarks
{
    private const int N = 10000;

    private readonly RegularTimeSeries<double> _regularA;
    private readonly RegularTimeSeries<double> _regularB;
    private readonly SparseTimeSeries<double> _sparseA;
    private readonly SparseTimeSeries<double> _sparseB;

    public TimeSeriesMathBenchmarks()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);

        _regularA = new RegularTimeSeries<double>(Period.FiveMinutes, N);
        _regularB = new RegularTimeSeries<double>(Period.FiveMinutes, N);
        _sparseA = new SparseTimeSeries<double>(Period.FiveMinutes, N);
        _sparseB = new SparseTimeSeries<double>(Period.FiveMinutes, N);

        for (var i = 0; i < N; i++)
        {
            var t = start.AddMinutes(i * 5);
            _regularA[t] = i + 1.0;
            _regularB[t] = (i % 10) + 1.0;
            _sparseA[t] = i + 1.0;
            _sparseB[t] = (i % 10) + 1.0;
        }
    }

    // ── Regular binary operations ────────────────────────────────────────

    [Benchmark]
    public RegularTimeSeries<double> RegularAdd()
        => TimeSeriesMath.Add(_regularA, _regularB);

    [Benchmark]
    public RegularTimeSeries<double> RegularSubtract()
        => TimeSeriesMath.Subtract(_regularA, _regularB);

    [Benchmark]
    public RegularTimeSeries<double> RegularMultiply()
        => TimeSeriesMath.Multiply(_regularA, _regularB);

    [Benchmark]
    public RegularTimeSeries<double> RegularDivide()
        => TimeSeriesMath.Divide(_regularA, _regularB);

    // ── Regular scalar operations ────────────────────────────────────────

    [Benchmark]
    public RegularTimeSeries<double> RegularScalarAdd()
        => TimeSeriesMath.Add(_regularA, 5.0);

    [Benchmark]
    public RegularTimeSeries<double> RegularScalarMultiply()
        => TimeSeriesMath.Multiply(_regularA, 1.5);

    [Benchmark]
    public RegularTimeSeries<double> RegularScalarDivide()
        => TimeSeriesMath.Divide(_regularA, 2.0);

    // ── Sparse binary operations ─────────────────────────────────────────

    [Benchmark]
    public SparseTimeSeries<double> SparseAdd()
        => TimeSeriesMath.Add(_sparseA, _sparseB);

    [Benchmark]
    public SparseTimeSeries<double> SparseSubtract()
        => TimeSeriesMath.Subtract(_sparseA, _sparseB);

    [Benchmark]
    public SparseTimeSeries<double> SparseMultiply()
        => TimeSeriesMath.Multiply(_sparseA, _sparseB);

    [Benchmark]
    public SparseTimeSeries<double> SparseDivide()
        => TimeSeriesMath.Divide(_sparseA, _sparseB);

    // ── Sparse scalar operations ─────────────────────────────────────────

    [Benchmark]
    public SparseTimeSeries<double> SparseScalarAdd()
        => TimeSeriesMath.Add(_sparseA, 5.0);

    [Benchmark]
    public SparseTimeSeries<double> SparseScalarMultiply()
        => TimeSeriesMath.Multiply(_sparseA, 1.5);

    [Benchmark]
    public SparseTimeSeries<double> SparseScalarDivide()
        => TimeSeriesMath.Divide(_sparseA, 2.0);
}
