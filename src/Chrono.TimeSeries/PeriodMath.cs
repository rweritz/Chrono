namespace Chrono.TimeSeries;

internal static class PeriodMath
{
    public static bool TryGetFixedTicks(Period period, out long ticks)
    {
        switch (period)
        {
            case Period.FiveMinutes:
                ticks = TimeSpan.TicksPerMinute * 5;
                return true;
            case Period.QuaterHour:
                ticks = TimeSpan.TicksPerMinute * 15;
                return true;
            case Period.HalfHour:
                ticks = TimeSpan.TicksPerMinute * 30;
                return true;
            case Period.Hour:
                ticks = TimeSpan.TicksPerHour;
                return true;
            case Period.HalfDay:
                ticks = TimeSpan.TicksPerHour * 12;
                return true;
            case Period.Day:
                ticks = TimeSpan.TicksPerDay;
                return true;
            case Period.Week:
                ticks = TimeSpan.TicksPerDay * 7;
                return true;
            default:
                ticks = 0;
                return false;
        }
    }

    public static long GetFixedTicks(Period period)
    {
        if (!TryGetFixedTicks(period, out var ticks))
            throw new NotSupportedException($"Period {period} is not fixed-length.");

        return ticks;
    }

    public static long AnchorUtcTicks(Period period)
    {
        if (period == Period.Week)
            return new DateTimeOffset(1970, 1, 5, 0, 0, 0, TimeSpan.Zero).UtcTicks;

        return DateTimeOffset.UnixEpoch.UtcTicks;
    }

    public static long ToAbsoluteSlot(DateTimeOffset timestamp, Period period)
    {
        var step = GetFixedTicks(period);
        var utcTicks = timestamp.UtcTicks;
        var delta = utcTicks - AnchorUtcTicks(period);
        var slot = Math.DivRem(delta, step, out var remainder);

        if (remainder != 0)
            throw new ArgumentException($"Timestamp {timestamp:O} is not aligned to {period}.", nameof(timestamp));

        return slot;
    }

    public static DateTimeOffset FromAbsoluteSlot(long slot, Period period)
    {
        var ticks = AnchorUtcTicks(period) + slot * GetFixedTicks(period);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    public static DateTimeOffset FloorToCalendarBucket(DateTimeOffset timestamp, Period period)
    {
        var ts = timestamp.ToUniversalTime();

        return period switch
        {
            Period.Day => new DateTimeOffset(ts.Year, ts.Month, ts.Day, 0, 0, 0, TimeSpan.Zero),
            Period.Week => new DateTimeOffset(ts.Year, ts.Month, ts.Day, 0, 0, 0, TimeSpan.Zero)
                .AddDays(-(((int)ts.DayOfWeek + 6) % 7)),
            Period.Month => new DateTimeOffset(ts.Year, ts.Month, 1, 0, 0, 0, TimeSpan.Zero),
            Period.QuaterYear => new DateTimeOffset(ts.Year, ((ts.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0,
                TimeSpan.Zero),
            Period.HalfYear => new DateTimeOffset(ts.Year, ts.Month <= 6 ? 1 : 7, 1, 0, 0, 0, TimeSpan.Zero),
            Period.Year => new DateTimeOffset(ts.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => throw new NotSupportedException($"Period {period} is not calendar-bucket compatible.")
        };
    }

    public static DateTimeOffset TruncateToFixedBucket(DateTimeOffset timestamp, Period target)
    {
        var utcTicks = timestamp.UtcTicks;
        var size = GetFixedTicks(target);
        var bucketTicks = utcTicks - (utcTicks % size);
        return new DateTimeOffset(bucketTicks, TimeSpan.Zero);
    }
}
