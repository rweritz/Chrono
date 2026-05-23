using System.Numerics;

namespace Chrono.TimeSeries;

internal static class SparseSegmentWriter
{
    public static void SetSegment<T>(
        Period period,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        T value,
        Action<DateTimeOffset, T> set)
        where T : struct, INumber<T>
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException("Segment writes are not supported for non-standard sparse time series.");

        if (endExclusive <= startInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Segment write end must be after the segment write start.");
        }

        var timestamps = new List<DateTimeOffset>();
        for (var current = startInclusive; current < endExclusive; current = AddPeriod(current, period))
            timestamps.Add(current);

        if (timestamps.Count == 0 || AddPeriod(timestamps[^1], period) != endExclusive)
        {
            throw new ArgumentException(
                "Segment write boundaries must align to the series period.",
                nameof(endExclusive));
        }

        foreach (var timestamp in timestamps)
            set(timestamp, value);
    }

    private static DateTimeOffset AddPeriod(DateTimeOffset timestamp, Period period) =>
        period switch
        {
            Period.FiveMinutes => timestamp.AddMinutes(5),
            Period.QuaterHour => timestamp.AddMinutes(15),
            Period.HalfHour => timestamp.AddMinutes(30),
            Period.Hour => timestamp.AddHours(1),
            Period.HalfDay => timestamp.AddHours(12),
            Period.Day => timestamp.AddDays(1),
            Period.Week => timestamp.AddDays(7),
            Period.Month => timestamp.AddMonths(1),
            Period.QuaterYear => timestamp.AddMonths(3),
            Period.HalfYear => timestamp.AddMonths(6),
            Period.Year => timestamp.AddYears(1),
            Period.NonStandard => throw new NotSupportedException("Segment writes are not supported for non-standard sparse time series."),
            _ => throw new NotSupportedException($"Period {period} is not supported.")
        };
}
