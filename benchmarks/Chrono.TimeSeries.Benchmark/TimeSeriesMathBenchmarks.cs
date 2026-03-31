using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesMathBenchmarks
{
    private const int N = 10000;

    private readonly FixedSlotTimeSeries<double> _fixedSlotA;
    private readonly FixedSlotTimeSeries<double> _fixedSlotB;
    private readonly SortedArrayTimeSeries<double> _sortedArrayA;
    private readonly SortedArrayTimeSeries<double> _sortedArrayB;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlotA;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlotB;

    public TimeSeriesMathBenchmarks()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);

        _fixedSlotA = new FixedSlotTimeSeries<double>(Period.FiveMinutes, N);
        _fixedSlotB = new FixedSlotTimeSeries<double>(Period.FiveMinutes, N);
        _sortedArrayA = new SortedArrayTimeSeries<double>(Period.FiveMinutes, N);
        _sortedArrayB = new SortedArrayTimeSeries<double>(Period.FiveMinutes, N);

        for (var i = 0; i < N; i++)
        {
            var t = start.AddMinutes(i * 5);
            _fixedSlotA[t] = i + 1.0;
            _fixedSlotB[t] = (i % 10) + 1.0;
            _sortedArrayA[t] = i + 1.0;
            _sortedArrayB[t] = (i % 10) + 1.0;
        }

        _dynamicSlotA = new DynamicSlotTimeSeries<double>(Period.Month, AlignMode.Strict, N);
        _dynamicSlotB = new DynamicSlotTimeSeries<double>(Period.Month, AlignMode.Strict, N);

        var monthStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < N; i++)
        {
            var t = monthStart.AddMonths(i);
            _dynamicSlotA[t] = i + 1.0;
            _dynamicSlotB[t] = (i % 10) + 1.0;
        }
    }

    // ── FixedSlot binary operations ────────────────────────────────────────

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotAdd()
        => TimeSeriesMath.Add(_fixedSlotA, _fixedSlotB);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotSubtract()
        => TimeSeriesMath.Subtract(_fixedSlotA, _fixedSlotB);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotMultiply()
        => TimeSeriesMath.Multiply(_fixedSlotA, _fixedSlotB);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotDivide()
        => TimeSeriesMath.Divide(_fixedSlotA, _fixedSlotB);

    // ── FixedSlot scalar operations ────────────────────────────────────────

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotScalarAdd()
        => TimeSeriesMath.Add(_fixedSlotA, 5.0);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotScalarMultiply()
        => TimeSeriesMath.Multiply(_fixedSlotA, 1.5);

    [Benchmark]
    public FixedSlotTimeSeries<double> FixedSlotScalarDivide()
        => TimeSeriesMath.Divide(_fixedSlotA, 2.0);

    // ── SortedArray binary operations ─────────────────────────────────────────

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayAdd()
        => TimeSeriesMath.Add(_sortedArrayA, _sortedArrayB);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArraySubtract()
        => TimeSeriesMath.Subtract(_sortedArrayA, _sortedArrayB);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayMultiply()
        => TimeSeriesMath.Multiply(_sortedArrayA, _sortedArrayB);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayDivide()
        => TimeSeriesMath.Divide(_sortedArrayA, _sortedArrayB);

    // ── SortedArray scalar operations ─────────────────────────────────────────

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayScalarAdd()
        => TimeSeriesMath.Add(_sortedArrayA, 5.0);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayScalarMultiply()
        => TimeSeriesMath.Multiply(_sortedArrayA, 1.5);

    [Benchmark]
    public SortedArrayTimeSeries<double> SortedArrayScalarDivide()
        => TimeSeriesMath.Divide(_sortedArrayA, 2.0);

    // ── DynamicSlot binary operations ─────────────────────────────────────

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotAdd()
        => TimeSeriesMath.Add(_dynamicSlotA, _dynamicSlotB);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotSubtract()
        => TimeSeriesMath.Subtract(_dynamicSlotA, _dynamicSlotB);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotMultiply()
        => TimeSeriesMath.Multiply(_dynamicSlotA, _dynamicSlotB);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotDivide()
        => TimeSeriesMath.Divide(_dynamicSlotA, _dynamicSlotB);

    // ── DynamicSlot scalar operations ─────────────────────────────────────

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotScalarAdd()
        => TimeSeriesMath.Add(_dynamicSlotA, 5.0);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotScalarMultiply()
        => TimeSeriesMath.Multiply(_dynamicSlotA, 1.5);

    [Benchmark]
    public DynamicSlotTimeSeries<double> DynamicSlotScalarDivide()
        => TimeSeriesMath.Divide(_dynamicSlotA, 2.0);
}
