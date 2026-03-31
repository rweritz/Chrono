namespace Chrono.TimeSeries;

internal static class ReferencePeriodValidator
{
    public static bool IsAligned(Period period, DateTimeOffset timestamp, DateTimeOffset reference)
    {
        if (period == Period.NonStandard)
            return true;

        if (PeriodMath.TryGetFixedTicks(period, out var ticks))
            return IsAlignedFixed(ticks, timestamp, reference);

        return period switch
        {
            Period.Day => timestamp.TimeOfDay == reference.TimeOfDay,
            Period.Week => timestamp.DayOfWeek == reference.DayOfWeek && timestamp.TimeOfDay == reference.TimeOfDay,
            Period.Month => timestamp.Day == reference.Day && timestamp.TimeOfDay == reference.TimeOfDay,
            Period.QuaterYear => timestamp.Day == reference.Day &&
                                 timestamp.TimeOfDay == reference.TimeOfDay &&
                                 (timestamp.Month - 1) % 3 == (reference.Month - 1) % 3,
            Period.HalfYear => timestamp.Day == reference.Day &&
                               timestamp.TimeOfDay == reference.TimeOfDay &&
                               (timestamp.Month <= 6 ? 0 : 1) == (reference.Month <= 6 ? 0 : 1),
            Period.Year => timestamp.Month == reference.Month &&
                           timestamp.Day == reference.Day &&
                           timestamp.TimeOfDay == reference.TimeOfDay,
            _ => false
        };
    }

    private static bool IsAlignedFixed(long ticks, DateTimeOffset timestamp, DateTimeOffset reference)
    {
        var delta = timestamp.UtcTicks - reference.UtcTicks;
        Math.DivRem(delta, ticks, out var remainder);
        return remainder == 0;
    }
}
