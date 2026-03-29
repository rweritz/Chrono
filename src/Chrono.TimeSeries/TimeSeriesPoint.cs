namespace Chrono.TimeSeries;

public readonly record struct TimeSeriesPoint<T>(DateTimeOffset Timestamp, T Value);
