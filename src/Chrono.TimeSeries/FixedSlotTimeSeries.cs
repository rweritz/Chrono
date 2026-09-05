using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class FixedSlotTimeSeries<T> : ISparseTimeSeries<T>, IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    private readonly SlotWindow<T> _window;

    public FixedSlotTimeSeries(Period period, int capacity = 0)
    {
        ValidatePeriod(period);
        Period = period;
        _window = new SlotWindow<T>(capacity);
    }

    internal FixedSlotTimeSeries(Period period, SlotWindow<T> window)
    {
        ValidatePeriod(period);
        Period = period;
        _window = window;
    }

    public Period Period { get; }

    public int ExplicitPointCount => _window.Count;

    public DateTimeOffset MinDate
    {
        get
        {
            return PeriodMath.FromAbsoluteSlot(_window.FirstPresentSlot(), Period);
        }
    }

    public DateTimeOffset MaxDate
    {
        get
        {
            return PeriodMath.FromAbsoluteSlot(_window.LastPresentSlot(), Period);
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
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        _window.Set(slot, value);
    }

    public void SetSegment(DateTimeOffset startInclusive, DateTimeOffset endExclusive, T value) =>
        SparseSegmentWriter.SetSegment(Period, startInclusive, endExclusive, value, Set);

    public bool Remove(DateTimeOffset timestamp)
    {
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        return _window.Remove(slot);
    }

    public void Clear()
    {
        _window.Clear();
    }

    public bool TryGetValue(DateTimeOffset timestamp, out T value)
    {
        var slot = PeriodMath.ToAbsoluteSlot(timestamp, Period);
        return _window.TryGetValue(slot, out value);
    }

    public IEnumerable<TimeSeriesPoint<T>> GetPoints()
    {
        foreach (var point in _window.GetPoints())
            yield return new TimeSeriesPoint<T>(PeriodMath.FromAbsoluteSlot(point.Slot, Period), point.Value);
    }

    public IEnumerator<TimeSeriesPoint<T>> GetEnumerator() => GetPoints().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal SlotWindow<T> Window => _window;

    private static void ValidatePeriod(Period period)
    {
        if (!PeriodMath.TryGetFixedTicks(period, out _))
            throw new NotSupportedException($"Use SortedArrayTimeSeries for period {period}.");
    }
}
