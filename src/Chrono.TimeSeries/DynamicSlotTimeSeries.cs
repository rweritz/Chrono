using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class DynamicSlotTimeSeries<T> : ISparseTimeSeries<T>, IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    private readonly SlotWindow<T> _window;

    public DynamicSlotTimeSeries(Period period, AlignMode alignMode = AlignMode.Strict, int capacity = 0)
    {
        ValidatePeriod(period);
        Period = period;
        AlignMode = alignMode;
        _window = new SlotWindow<T>(capacity);
    }

    internal DynamicSlotTimeSeries(Period period, AlignMode alignMode, SlotWindow<T> window)
    {
        ValidatePeriod(period);
        Period = period;
        AlignMode = alignMode;
        _window = window;
    }

    public Period Period { get; }

    public AlignMode AlignMode { get; }

    public int ExplicitPointCount => _window.Count;

    public DateTimeOffset MinDate
    {
        get
        {
            return PeriodGeometry.FromSlot(_window.FirstPresentSlot(), Period);
        }
    }

    public DateTimeOffset MaxDate
    {
        get
        {
            return PeriodGeometry.FromSlot(_window.LastPresentSlot(), Period);
        }
    }

    public T this[DateTimeOffset timestamp]
    {
        get
        {
            if (!TryGetValue(timestamp, out var value))
                throw new KeyNotFoundException($"No value exists at {timestamp:O}");

            return value;
        }
        set => Set(timestamp, value);
    }

    public void Set(DateTimeOffset timestamp, T value)
    {
        var normalized = Normalize(timestamp);
        var slot = PeriodGeometry.ToSlot(normalized, Period);
        _window.Set(slot, value);
    }

    public void SetSegment(DateTimeOffset startInclusive, DateTimeOffset endExclusive, T value)
    {
        SparseSegmentWriter.SetSegment(
            Period,
            Normalize(startInclusive),
            Normalize(endExclusive),
            value,
            Set);
    }

    public bool Remove(DateTimeOffset timestamp)
    {
        var normalized = Normalize(timestamp);
        var slot = PeriodGeometry.ToSlot(normalized, Period);
        return _window.Remove(slot);
    }

    public void Clear()
    {
        _window.Clear();
    }

    public bool TryGetValue(DateTimeOffset timestamp, out T value)
    {
        var normalized = Normalize(timestamp);
        var slot = PeriodGeometry.ToSlot(normalized, Period);
        return _window.TryGetValue(slot, out value);
    }

    public IEnumerable<TimeSeriesPoint<T>> GetPoints()
    {
        foreach (var point in _window.GetPoints())
            yield return new TimeSeriesPoint<T>(PeriodGeometry.FromSlot(point.Slot, Period), point.Value);
    }

    public IEnumerator<TimeSeriesPoint<T>> GetEnumerator() => GetPoints().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal DynamicSlotTimeSeries<TOut> AggregateSlots<TOut, TAggregator>(
        Period targetPeriod,
        int factor,
        TAggregator aggregator)
        where TOut : struct, INumber<TOut>
        where TAggregator : struct, IAggregator<T, TOut>
        => new(targetPeriod, AlignMode.Strict, _window.Aggregate<TOut, TAggregator>(factor, aggregator));

    internal DynamicSlotTimeSeries<T> AddSlots(
        DynamicSlotTimeSeries<T> other,
        MissingValuePolicy policy)
        => new(Period, AlignMode.Strict, _window.Add(other._window, policy));

    internal DynamicSlotTimeSeries<T> CombineSlots(
        DynamicSlotTimeSeries<T> other,
        MissingValuePolicy policy,
        Func<T, T, T> operation)
        => new(Period, AlignMode.Strict, _window.Combine(other._window, policy, operation));

    internal DynamicSlotTimeSeries<T> AddScalar(T scalar) =>
        new(Period, AlignMode.Strict, _window.Add(scalar));

    internal DynamicSlotTimeSeries<T> MultiplyScalar(T scalar) =>
        new(Period, AlignMode.Strict, _window.Multiply(scalar));

    internal DynamicSlotTimeSeries<T> DivideScalar(T scalar) =>
        new(Period, AlignMode.Strict, _window.Divide(scalar));

    private DateTimeOffset Normalize(DateTimeOffset timestamp) =>
        AlignMode == AlignMode.Truncate
            ? PeriodGeometry.FloorToBucket(timestamp, Period)
            : timestamp;

    private static void ValidatePeriod(Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} is not supported.");
    }
}
