using System.Collections;
using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class DynamicSlotTimeSeries<T> : ISparseTimeSeries<T>, IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    private readonly SlotWindow<T> _window;

    public DynamicSlotTimeSeries(Period period, AlignMode alignMode = AlignMode.Strict, int capacity = 0)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} is not supported.");

        Period = period;
        AlignMode = alignMode;
        _window = new SlotWindow<T>(capacity);
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

    internal long StartSlot => _window.StartSlot;

    internal int SlotLength => _window.Length;

    internal bool IsDense => _window.IsDense;

    internal ReadOnlySpan<T> ValueSpan => _window.ValueSpan;

    internal Span<T> MutableValueSpan => _window.MutableValueSpan;

    internal ReadOnlySpan<ulong> PresenceBits => _window.PresenceBits;

    internal bool TryGetSlotValue(long slot, out T value) => _window.TryGetValue(slot, out value);

    internal void InitializeWindow(long startSlot, int length) => _window.InitializeWindow(startSlot, length);

    internal void MarkPresentAt(int index) => _window.MarkPresentAt(index);

    private DateTimeOffset Normalize(DateTimeOffset timestamp) =>
        AlignMode == AlignMode.Truncate
            ? PeriodGeometry.FloorToBucket(timestamp, Period)
            : timestamp;
}
