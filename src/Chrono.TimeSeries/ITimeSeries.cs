using System.Numerics;

namespace Chrono.TimeSeries;

public interface IReadOnlyTimeSeries<T>
    where T : struct, INumber<T>
{
    Period Period { get; }

    bool TryGetValue(DateTimeOffset timestamp, out T value);

    T this[DateTimeOffset timestamp] { get; }
}

public interface ITimeSeries<T> : IReadOnlyTimeSeries<T>
    where T : struct, INumber<T>
{
    new T this[DateTimeOffset timestamp] { get; set; }

    void Set(DateTimeOffset timestamp, T value);

    void Clear();
}

public interface IReadOnlySparseTimeSeries<T> : IReadOnlyTimeSeries<T>
    where T : struct, INumber<T>
{
    int ExplicitPointCount { get; }

    DateTimeOffset MinDate { get; }

    DateTimeOffset MaxDate { get; }

    IEnumerable<TimeSeriesPoint<T>> GetPoints();
}

public interface ISparseTimeSeries<T> : ITimeSeries<T>, IReadOnlySparseTimeSeries<T>
    where T : struct, INumber<T>
{
    void SetSegment(DateTimeOffset startInclusive, DateTimeOffset endExclusive, T value);

    bool Remove(DateTimeOffset timestamp);
}

public interface IBoundedStepwiseTimeSeries<T> : ITimeSeries<T>
    where T : struct, INumber<T>
{
    DateTimeOffset LogicalRangeStart { get; }

    DateTimeOffset LogicalRangeEnd { get; }

    int LogicalSlotCount { get; }

    int ChangePointCount { get; }

    void SetSegment(DateTimeOffset startInclusive, DateTimeOffset endExclusive, T value);

    IEnumerable<TimeSeriesPoint<T>> GetChangePoints();
}
