using System.Numerics;

namespace Chrono.TimeSeries;

public sealed class StepwiseTimeSeries<T> : IBoundedStepwiseTimeSeries<T>
    where T : struct, INumber<T>
{
    private readonly List<long> _changePointSlots;
    private readonly List<T> _changePointValues;
    private readonly T _initialValue;
    private long _logicalRangeStartSlot;
    private long _logicalRangeEndSlot;

    public StepwiseTimeSeries(Period period, DateTimeOffset logicalRangeStart, DateTimeOffset logicalRangeEnd, T initialValue)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} is not supported.");

        var startSlot = PeriodGeometry.ToSlot(logicalRangeStart, period);
        var endSlot = PeriodGeometry.ToSlot(logicalRangeEnd, period);
        if (endSlot < startSlot)
            throw new ArgumentOutOfRangeException(nameof(logicalRangeEnd), "Logical range end must not precede the logical range start.");

        Period = period;
        _initialValue = initialValue;
        _logicalRangeStartSlot = startSlot;
        _logicalRangeEndSlot = endSlot;
        _changePointSlots = [startSlot];
        _changePointValues = [initialValue];
        if (endSlot != startSlot)
        {
            _changePointSlots.Add(endSlot);
            _changePointValues.Add(initialValue);
        }
    }

    public Period Period { get; }

    public DateTimeOffset MinDate => PeriodGeometry.FromSlot(_changePointSlots[0], Period);

    public DateTimeOffset MaxDate => PeriodGeometry.FromSlot(_changePointSlots[^1], Period);

    public DateTimeOffset LogicalRangeStart => PeriodGeometry.FromSlot(_logicalRangeStartSlot, Period);

    public DateTimeOffset LogicalRangeEnd => PeriodGeometry.FromSlot(_logicalRangeEndSlot, Period);

    public int LogicalSlotCount => checked((int)(_logicalRangeEndSlot - _logicalRangeStartSlot + 1));

    public int ChangePointCount => _changePointSlots.Count;

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
        var slot = PeriodGeometry.ToSlot(timestamp, Period);
        SetSlotRange(slot, slot, value);
    }

    public void SetSegment(DateTimeOffset startInclusive, DateTimeOffset endExclusive, T value)
    {
        var startSlot = PeriodGeometry.ToSlot(startInclusive, Period);
        var endExclusiveSlot = PeriodGeometry.ToSlot(endExclusive, Period);
        if (endExclusiveSlot <= startSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Segment write end must be after the segment write start.");
        }

        SetSlotRange(startSlot, endExclusiveSlot - 1, value);
    }

    public void Clear()
    {
        _changePointSlots.Clear();
        _changePointValues.Clear();
        _changePointSlots.Add(_logicalRangeStartSlot);
        _changePointValues.Add(_initialValue);
        if (_logicalRangeEndSlot != _logicalRangeStartSlot)
        {
            _changePointSlots.Add(_logicalRangeEndSlot);
            _changePointValues.Add(_initialValue);
        }
    }

    public bool TryGetValue(DateTimeOffset timestamp, out T value)
    {
        var slot = PeriodGeometry.ToSlot(timestamp, Period);
        if (slot < _logicalRangeStartSlot || slot > _logicalRangeEndSlot)
        {
            value = T.Zero;
            return false;
        }

        value = GetValueAtSlot(slot);
        return true;
    }

    public IEnumerable<TimeSeriesPoint<T>> GetChangePoints()
    {
        for (var i = 0; i < _changePointSlots.Count; i++)
            yield return new TimeSeriesPoint<T>(PeriodGeometry.FromSlot(_changePointSlots[i], Period), _changePointValues[i]);
    }

    private void SetSlotRange(long startSlot, long endSlot, T value)
    {
        EnsureContiguousExpansion(startSlot, endSlot);

        var originalLogicalRangeEndSlot = _logicalRangeEndSlot;
        var afterExists = endSlot < originalLogicalRangeEndSlot;
        var afterValue = afterExists ? GetValueAtSlot(endSlot + 1) : value;

        _logicalRangeStartSlot = Math.Min(_logicalRangeStartSlot, startSlot);
        _logicalRangeEndSlot = Math.Max(_logicalRangeEndSlot, endSlot);

        RemoveRange(startSlot, endSlot);
        InsertOrReplace(startSlot, value);
        if (afterExists)
            InsertOrReplace(endSlot + 1, afterValue);
        else
            InsertOrReplace(_logicalRangeEndSlot, value);

        Canonicalize();
    }

    private void RemoveRange(long startSlot, long endSlot)
    {
        for (var i = _changePointSlots.Count - 1; i >= 0; i--)
        {
            var slot = _changePointSlots[i];
            if (slot >= startSlot && slot <= endSlot)
            {
                _changePointSlots.RemoveAt(i);
                _changePointValues.RemoveAt(i);
            }
        }
    }

    private void InsertOrReplace(long slot, T value)
    {
        var index = _changePointSlots.BinarySearch(slot);
        if (index >= 0)
        {
            _changePointValues[index] = value;
            return;
        }

        index = ~index;
        _changePointSlots.Insert(index, slot);
        _changePointValues.Insert(index, value);
    }

    private void Canonicalize()
    {
        if (_logicalRangeStartSlot == _logicalRangeEndSlot)
        {
            var singleValue = _changePointValues.Count == 0 ? T.Zero : _changePointValues[0];
            _changePointSlots.Clear();
            _changePointValues.Clear();
            _changePointSlots.Add(_logicalRangeStartSlot);
            _changePointValues.Add(singleValue);
            return;
        }

        RemovePointsOutsideLogicalRange();
        EnsureBoundaryAtStart();
        EnsureBoundaryAtEnd();

        for (var i = _changePointSlots.Count - 2; i >= 1; i--)
        {
            if (EqualityComparer<T>.Default.Equals(_changePointValues[i], _changePointValues[i - 1]))
            {
                _changePointSlots.RemoveAt(i);
                _changePointValues.RemoveAt(i);
            }
        }

        if (_changePointSlots.Count == 1)
        {
            _changePointSlots.Add(_logicalRangeEndSlot);
            _changePointValues.Add(_changePointValues[0]);
        }
        else if (_changePointSlots[^1] != _logicalRangeEndSlot)
        {
            _changePointSlots.Add(_logicalRangeEndSlot);
            _changePointValues.Add(GetValueAtSlot(_logicalRangeEndSlot));
        }
    }

    private void RemovePointsOutsideLogicalRange()
    {
        for (var i = _changePointSlots.Count - 1; i >= 0; i--)
        {
            var slot = _changePointSlots[i];
            if (slot < _logicalRangeStartSlot || slot > _logicalRangeEndSlot)
            {
                _changePointSlots.RemoveAt(i);
                _changePointValues.RemoveAt(i);
            }
        }
    }

    private void EnsureBoundaryAtStart()
    {
        if (_changePointSlots.BinarySearch(_logicalRangeStartSlot) >= 0)
            return;

        _changePointSlots.Insert(0, _logicalRangeStartSlot);
        _changePointValues.Insert(0, GetValueAtSlot(_logicalRangeStartSlot));
    }

    private void EnsureBoundaryAtEnd()
    {
        if (_changePointSlots.BinarySearch(_logicalRangeEndSlot) >= 0)
            return;

        _changePointSlots.Add(_logicalRangeEndSlot);
        _changePointValues.Add(GetValueAtSlot(_logicalRangeEndSlot));
    }

    private T GetValueAtSlot(long slot)
    {
        var index = _changePointSlots.BinarySearch(slot);
        if (index >= 0)
            return _changePointValues[index];

        index = ~index - 1;
        return _changePointValues[index];
    }

    private void EnsureContiguousExpansion(long startSlot, long endSlot)
    {
        if (endSlot < _logicalRangeStartSlot - 1 || startSlot > _logicalRangeEndSlot + 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startSlot),
                "Segment write must overlap the logical range or touch one of its boundaries.");
        }
    }
}
