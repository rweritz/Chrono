namespace Chrono.TimeSeries;

internal static class CalendarSlotMath
{
    private const int EpochYear = 1970;

    public static bool IsCalendarPeriod(Period period) =>
        period is Period.Month or Period.QuaterYear or Period.HalfYear or Period.Year;

    public static DateTimeOffset AlignToSlot(DateTimeOffset timestamp, Period period)
    {
        if (PeriodMath.TryGetFixedTicks(period, out _))
            return PeriodMath.TruncateToFixedBucket(timestamp, period);

        return PeriodMath.FloorToCalendarBucket(timestamp, period);
    }

    public static bool IsAligned(DateTimeOffset timestamp, Period period)
    {
        if (period == Period.NonStandard)
            return true;

        if (PeriodMath.TryGetFixedTicks(period, out _))
        {
            var utcTicks = timestamp.UtcTicks;
            var step = PeriodMath.GetFixedTicks(period);
            var delta = utcTicks - PeriodMath.AnchorUtcTicks(period);
            Math.DivRem(delta, step, out var remainder);
            return remainder == 0;
        }

        var utc = timestamp.ToUniversalTime();
        return period switch
        {
            Period.Month => utc.Day == 1 && IsMidnight(utc),
            Period.QuaterYear => utc.Day == 1 && IsMidnight(utc) && ((utc.Month - 1) % 3 == 0),
            Period.HalfYear => utc.Day == 1 && IsMidnight(utc) && (utc.Month is 1 or 7),
            Period.Year => utc.Month == 1 && utc.Day == 1 && IsMidnight(utc),
            _ => false
        };
    }

    public static long ToSlot(DateTimeOffset timestamp, Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} is not supported for slot mapping.");

        if (PeriodMath.TryGetFixedTicks(period, out _))
            return PeriodMath.ToAbsoluteSlot(timestamp, period);

        var utc = timestamp.ToUniversalTime();
        if (!IsAligned(utc, period))
            throw new ArgumentException($"Timestamp {timestamp:O} is not aligned to {period}.", nameof(timestamp));

        return period switch
        {
            Period.Month => MonthSlot(utc.Year, utc.Month),
            Period.QuaterYear => checked((utc.Year - EpochYear) * 4L + ((utc.Month - 1) / 3)),
            Period.HalfYear => checked((utc.Year - EpochYear) * 2L + (utc.Month <= 6 ? 0 : 1)),
            Period.Year => utc.Year - EpochYear,
            _ => throw new NotSupportedException($"Period {period} is not supported for slot mapping.")
        };
    }

    public static DateTimeOffset FromSlot(long slot, Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} is not supported for slot mapping.");

        if (PeriodMath.TryGetFixedTicks(period, out _))
            return PeriodMath.FromAbsoluteSlot(slot, period);

        return period switch
        {
            Period.Month => MonthFromSlot(slot),
            Period.QuaterYear => QuarterFromSlot(slot),
            Period.HalfYear => HalfYearFromSlot(slot),
            Period.Year => new DateTimeOffset(checked(EpochYear + (int)slot), 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => throw new NotSupportedException($"Period {period} is not supported for slot mapping.")
        };
    }

    private static long MonthSlot(int year, int month) =>
        checked((year - EpochYear) * 12L + (month - 1));

    private static DateTimeOffset MonthFromSlot(long slot)
    {
        var yearOffset = Math.DivRem(slot, 12, out var monthOffset);
        if (monthOffset < 0)
        {
            monthOffset += 12;
            yearOffset--;
        }

        var year = checked(EpochYear + (int)yearOffset);
        var month = checked((int)monthOffset + 1);
        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset QuarterFromSlot(long slot)
    {
        var yearOffset = Math.DivRem(slot, 4, out var quarterOffset);
        if (quarterOffset < 0)
        {
            quarterOffset += 4;
            yearOffset--;
        }

        var year = checked(EpochYear + (int)yearOffset);
        var month = checked((int)quarterOffset * 3 + 1);
        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset HalfYearFromSlot(long slot)
    {
        var yearOffset = Math.DivRem(slot, 2, out var halfOffset);
        if (halfOffset < 0)
        {
            halfOffset += 2;
            yearOffset--;
        }

        var year = checked(EpochYear + (int)yearOffset);
        var month = halfOffset == 0 ? 1 : 7;
        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private static bool IsMidnight(DateTimeOffset timestamp) =>
        timestamp.Hour == 0 &&
        timestamp.Minute == 0 &&
        timestamp.Second == 0 &&
        timestamp.Millisecond == 0 &&
        timestamp.Microsecond == 0 &&
        timestamp.Nanosecond == 0;
}
