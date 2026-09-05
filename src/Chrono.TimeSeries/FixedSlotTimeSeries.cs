using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class FixedSlotTimeSeries<T> : ISparseTimeSeries<T>, IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    private readonly SlotWindow<T> _window;

    public FixedSlotTimeSeries(Period period, int capacity = 0)
    {
        if (!PeriodMath.TryGetFixedTicks(period, out _))
            throw new NotSupportedException($"Use SortedArrayTimeSeries for period {period}.");

        Period = period;
        _window = new SlotWindow<T>(capacity);
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

    internal long StartSlot => _window.StartSlot;

    internal int SlotLength => _window.Length;

    internal bool IsDense => _window.IsDense;

    internal ReadOnlySpan<T> ValueSpan => _window.ValueSpan;

    internal Span<T> MutableValueSpan => _window.MutableValueSpan;

    internal ReadOnlySpan<ulong> PresenceBits => _window.PresenceBits;

    internal bool TryGetSlotValue(long slot, out T value) => _window.TryGetValue(slot, out value);

    internal void InitializeWindow(long startSlot, int length) => _window.InitializeWindow(startSlot, length);

    internal void MarkPresentAt(int index) => _window.MarkPresentAt(index);
}
