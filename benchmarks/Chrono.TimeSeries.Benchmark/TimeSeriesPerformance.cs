using BenchmarkDotNet.Attributes;

namespace Chrono.TimeSeries.Benchmark;

public class TimeSeriesPerformance
{
    private const int N = 10000;
    private readonly IList<DateTimeOffset> _dates;

    private readonly IList<DateTimeOffset> _hourDates;
    private readonly IList<DateTimeOffset> _misalignedHourDates;
    private readonly IList<DateTimeOffset> _monthDates;

    private readonly SortedArrayTimeSeries<double> _sortedArray;
    private readonly FixedSlotTimeSeries<double> _fixedSlot;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlotStrict;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlotTruncate;
    private readonly DynamicSlotTimeSeries<double> _dynamicSlotMonth;

    public TimeSeriesPerformance()
    {
        var initDate = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        _dates = new List<DateTimeOffset>(N);
        for (var i = 0; i < N; i++)
            _dates.Add(initDate.AddMinutes(i * 5));

        _sortedArray = new SortedArrayTimeSeries<double>(Period.FiveMinutes);
        _fixedSlot = new FixedSlotTimeSeries<double>(Period.FiveMinutes);

        foreach (var date in _dates)
        {
            _sortedArray[date] = 1;
            _fixedSlot[date] = 1;
        }

        var hourStart = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _hourDates = new List<DateTimeOffset>(N);
        _misalignedHourDates = new List<DateTimeOffset>(N);
        for (var i = 0; i < N; i++)
        {
            _hourDates.Add(hourStart.AddHours(i));
            _misalignedHourDates.Add(hourStart.AddHours(i).AddMinutes(17));
        }

        var monthStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _monthDates = new List<DateTimeOffset>(N);
        for (var i = 0; i < N; i++)
            _monthDates.Add(monthStart.AddMonths(i));

        _dynamicSlotStrict = new DynamicSlotTimeSeries<double>(Period.Hour);
        _dynamicSlotTruncate = new DynamicSlotTimeSeries<double>(Period.Hour, AlignMode.Truncate);
        _dynamicSlotMonth = new DynamicSlotTimeSeries<double>(Period.Month);

        foreach (var date in _hourDates)
            _dynamicSlotStrict[date] = 1;
        foreach (var date in _misalignedHourDates)
            _dynamicSlotTruncate[date] = 1;
        foreach (var date in _monthDates)
            _dynamicSlotMonth[date] = 1;
    }

    [Benchmark]
    public void SortedArrayOrderedInsert()
    {
        var series = new SortedArrayTimeSeries<double>(Period.FiveMinutes);
        foreach (var date in _dates)
            series[date] = 1;
    }

    [Benchmark]
    public void FixedSlotOrderedInsert()
    {
        var series = new FixedSlotTimeSeries<double>(Period.FiveMinutes);
        foreach (var date in _dates)
            series[date] = 1;
    }

    [Benchmark]
    public void SortedArrayOrderedAccess()
    {
        var sum = 0d;
        foreach (var date in _dates)
            sum += _sortedArray[date];
    }

    [Benchmark]
    public void FixedSlotOrderedAccess()
    {
        var sum = 0d;
        foreach (var date in _dates)
            sum += _fixedSlot[date];
    }

    [Benchmark]
    public void SortedArrayScalarMultiply()
    {
        var result = TimeSeriesMath.Multiply(_sortedArray, 1.5d);
    }

    [Benchmark]
    public void FixedSlotScalarMultiply()
    {
        var result = TimeSeriesMath.Multiply(_fixedSlot, 1.5d);
    }

    // ── DynamicSlot storage benchmarks ────────────────────────────────────

    [Benchmark]
    public void DynamicSlotStrictInsert()
    {
        var series = new DynamicSlotTimeSeries<double>(Period.Hour);
        foreach (var date in _hourDates)
            series[date] = 1;
    }

    [Benchmark]
    public void DynamicSlotTruncateInsert()
    {
        var series = new DynamicSlotTimeSeries<double>(Period.Hour, AlignMode.Truncate);
        foreach (var date in _misalignedHourDates)
            series[date] = 1;
    }

    [Benchmark]
    public void DynamicSlotMonthInsert()
    {
        var series = new DynamicSlotTimeSeries<double>(Period.Month);
        foreach (var date in _monthDates)
            series[date] = 1;
    }

    [Benchmark]
    public void DynamicSlotStrictAccess()
    {
        var sum = 0d;
        foreach (var date in _hourDates)
            sum += _dynamicSlotStrict[date];
    }

    [Benchmark]
    public void DynamicSlotTruncateAccess()
    {
        var sum = 0d;
        foreach (var date in _misalignedHourDates)
            sum += _dynamicSlotTruncate[date];
    }

    [Benchmark]
    public void DynamicSlotMonthAccess()
    {
        var sum = 0d;
        foreach (var date in _monthDates)
            sum += _dynamicSlotMonth[date];
    }

    [Benchmark]
    public void DynamicSlotScalarMultiply()
    {
        var result = TimeSeriesMath.Multiply(_dynamicSlotStrict, 1.5d);
    }
}
