namespace Chrono.TimeSeries;

/// <summary>
/// Defines the canonical UTC geometry shared by every standard period.
/// </summary>
internal static class PeriodGeometry
{
    private const int EpochYear = 1970;
    private static readonly long UnixEpochUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks;
    private static readonly long MondayEpochUtcTicks =
        new DateTimeOffset(1970, 1, 5, 0, 0, 0, TimeSpan.Zero).UtcTicks;

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

    public static bool IsAligned(DateTimeOffset timestamp, Period period)
    {
        if (period == Period.NonStandard)
            return true;

        if (TryGetFixedTicks(period, out var ticks))
        {
            var delta = timestamp.UtcTicks - GetAnchorUtcTicks(period);
            Math.DivRem(delta, ticks, out var remainder);
            return remainder == 0;
        }

        var utc = timestamp.ToUniversalTime();
        return period switch
        {
            Period.Month => utc.Day == 1 && IsMidnight(utc),
            Period.QuaterYear => utc.Day == 1 && IsMidnight(utc) && (utc.Month - 1) % 3 == 0,
            Period.HalfYear => utc.Day == 1 && IsMidnight(utc) && utc.Month is 1 or 7,
            Period.Year => utc.Month == 1 && utc.Day == 1 && IsMidnight(utc),
            _ => false
        };
    }

    public static void ValidateAligned(DateTimeOffset timestamp, Period period, string parameterName)
    {
        if (!IsAligned(timestamp, period))
        {
            throw new ArgumentException(
                $"Timestamp {timestamp:O} is not aligned to the canonical UTC grid for {period}.",
                parameterName);
        }
    }

    public static long ToSlot(DateTimeOffset timestamp, Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} does not have canonical slots.");

        ValidateAligned(timestamp, period, nameof(timestamp));

        if (TryGetFixedTicks(period, out var ticks))
            return (timestamp.UtcTicks - GetAnchorUtcTicks(period)) / ticks;

        var utc = timestamp.ToUniversalTime();
        return period switch
        {
            Period.Month => checked((utc.Year - EpochYear) * 12L + utc.Month - 1),
            Period.QuaterYear => checked((utc.Year - EpochYear) * 4L + (utc.Month - 1) / 3),
            Period.HalfYear => checked((utc.Year - EpochYear) * 2L + (utc.Month <= 6 ? 0 : 1)),
            Period.Year => utc.Year - EpochYear,
            _ => throw new NotSupportedException($"Period {period} does not have canonical slots.")
        };
    }

    public static DateTimeOffset FromSlot(long slot, Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} does not have canonical slots.");

        if (TryGetFixedTicks(period, out var ticks))
        {
            var utcTicks = checked(GetAnchorUtcTicks(period) + checked(slot * ticks));
            return new DateTimeOffset(utcTicks, TimeSpan.Zero);
        }

        return period switch
        {
            Period.Month => MonthFromSlot(slot),
            Period.QuaterYear => QuarterFromSlot(slot),
            Period.HalfYear => HalfYearFromSlot(slot),
            Period.Year => new DateTimeOffset(checked(EpochYear + checked((int)slot)), 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => throw new NotSupportedException($"Period {period} does not have canonical slots.")
        };
    }

    public static DateTimeOffset FloorToBucket(DateTimeOffset timestamp, Period period)
    {
        if (period == Period.NonStandard)
            throw new NotSupportedException($"Period {period} does not have canonical buckets.");

        if (TryGetFixedTicks(period, out var ticks))
        {
            var anchor = GetAnchorUtcTicks(period);
            var delta = timestamp.UtcTicks - anchor;
            var slot = Math.DivRem(delta, ticks, out var remainder);
            if (remainder < 0)
                slot--;

            return FromSlot(slot, period);
        }

        var utc = timestamp.ToUniversalTime();
        return period switch
        {
            Period.Month => UtcStart(utc.Year, utc.Month),
            Period.QuaterYear => UtcStart(utc.Year, (utc.Month - 1) / 3 * 3 + 1),
            Period.HalfYear => UtcStart(utc.Year, utc.Month <= 6 ? 1 : 7),
            Period.Year => UtcStart(utc.Year, 1),
            _ => throw new NotSupportedException($"Period {period} does not have canonical buckets.")
        };
    }

    public static DateTimeOffset AddPeriods(DateTimeOffset timestamp, Period period, long count = 1)
    {
        var slot = ToSlot(timestamp, period);
        return FromSlot(checked(slot + count), period);
    }

    private static long GetAnchorUtcTicks(Period period) =>
        period == Period.Week ? MondayEpochUtcTicks : UnixEpochUtcTicks;

    private static DateTimeOffset MonthFromSlot(long slot)
    {
        var (yearOffset, monthOffset) = DivideWithPositiveRemainder(slot, 12);
        var year = checked(EpochYear + checked((int)yearOffset));
        return UtcStart(year, checked((int)monthOffset + 1));
    }

    private static DateTimeOffset QuarterFromSlot(long slot)
    {
        var (yearOffset, quarterOffset) = DivideWithPositiveRemainder(slot, 4);
        var year = checked(EpochYear + checked((int)yearOffset));
        return UtcStart(year, checked((int)quarterOffset * 3 + 1));
    }

    private static DateTimeOffset HalfYearFromSlot(long slot)
    {
        var (yearOffset, halfOffset) = DivideWithPositiveRemainder(slot, 2);
        var year = checked(EpochYear + checked((int)yearOffset));
        return UtcStart(year, halfOffset == 0 ? 1 : 7);
    }

    private static (long Quotient, long Remainder) DivideWithPositiveRemainder(long value, long divisor)
    {
        var quotient = Math.DivRem(value, divisor, out var remainder);
        if (remainder < 0)
        {
            remainder += divisor;
            quotient--;
        }

        return (quotient, remainder);
    }

    private static DateTimeOffset UtcStart(int year, int month) =>
        new(year, month, 1, 0, 0, 0, TimeSpan.Zero);

    private static bool IsMidnight(DateTimeOffset timestamp) => timestamp.TimeOfDay == TimeSpan.Zero;
}
