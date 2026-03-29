using System.Numerics;

namespace Chrono.TimeSeries;

public interface IReadOnlyTimeSeries<T> : IEnumerable<TimeSeriesPoint<T>>
    where T : struct, INumber<T>
{
    Period Period { get; }

    int Count { get; }

    DateTimeOffset MinDate { get; }

    DateTimeOffset MaxDate { get; }

    bool TryGetValue(DateTimeOffset timestamp, out T value);

    T this[DateTimeOffset timestamp] { get; }
}

public interface ITimeSeries<T> : IReadOnlyTimeSeries<T>
    where T : struct, INumber<T>
{
    new T this[DateTimeOffset timestamp] { get; set; }

    void Set(DateTimeOffset timestamp, T value);

    bool Remove(DateTimeOffset timestamp);

    void Clear();
}
