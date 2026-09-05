namespace Chrono.TimeSeries;

/// <summary>
/// Defines the canonical UTC geometry shared by every standard period.
/// </summary>
internal static class PeriodGeometry
{
    private const int _epochYear = 1970;
    private static readonly long _unixEpochUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks;
    private static readonly long _mondayEpochUtcTicks =
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

        if (!TryGetCalendarMonths(period, out var monthsPerSlot))
            return false;

        var utc = timestamp.ToUniversalTime();
        var month = MonthsSinceEpoch(utc);
        return utc.Day == 1 && IsMidnight(utc) && month % monthsPerSlot == 0;
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

        if (!TryGetCalendarMonths(period, out var monthsPerSlot))
            throw new NotSupportedException($"Period {period} does not have canonical slots.");

        return MonthsSinceEpoch(timestamp.ToUniversalTime()) / monthsPerSlot;
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

        if (!TryGetCalendarMonths(period, out var monthsPerSlot))
            throw new NotSupportedException($"Period {period} does not have canonical slots.");

        return CalendarFromSlot(slot, monthsPerSlot);
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

        if (!TryGetCalendarMonths(period, out var monthsPerSlot))
            throw new NotSupportedException($"Period {period} does not have canonical buckets.");

        var calendarMonth = MonthsSinceEpoch(timestamp.ToUniversalTime());
        var (calendarSlot, _) = DivideWithPositiveRemainder(calendarMonth, monthsPerSlot);
        return CalendarFromSlot(calendarSlot, monthsPerSlot);
    }

    public static DateTimeOffset AddPeriods(DateTimeOffset timestamp, Period period, long count = 1)
    {
        var slot = ToSlot(timestamp, period);
        return FromSlot(checked(slot + count), period);
    }

    private static long GetAnchorUtcTicks(Period period) =>
        period == Period.Week ? _mondayEpochUtcTicks : _unixEpochUtcTicks;

    private static bool TryGetCalendarMonths(Period period, out int monthsPerSlot)
    {
        monthsPerSlot = period switch
        {
            Period.Month => 1,
            Period.QuaterYear => 3,
            Period.HalfYear => 6,
            Period.Year => 12,
            _ => 0
        };

        return monthsPerSlot != 0;
    }

    private static DateTimeOffset CalendarFromSlot(long slot, int monthsPerSlot)
    {
        var month = checked(slot * monthsPerSlot);
        var (yearOffset, monthOffset) = DivideWithPositiveRemainder(month, 12);
        var year = checked(_epochYear + checked((int)yearOffset));
        return UtcStart(year, checked((int)monthOffset + 1));
    }

    private static long MonthsSinceEpoch(DateTimeOffset utc) =>
        checked((utc.Year - _epochYear) * 12L + utc.Month - 1);

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
