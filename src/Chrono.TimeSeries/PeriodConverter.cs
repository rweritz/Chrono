using System.Diagnostics;

namespace Chrono.TimeSeries;

public class PeriodConverter
{
    public static Func<DateTimeOffset, DateTimeOffset, bool> GetValidationFunc(Period period) => period switch
    {
        Period.FiveMinutes => IsValidFiveMinutesDateTimeOffset,
        Period.QuaterHour => IsValidQuarterHourDateTimeOffset,
        Period.HalfHour => IsValidHalfHourDateTimeOffset,
        Period.Hour => IsValidHourDateTimeOffset,
        _ => throw new ArgumentException($"Unsupported {nameof(period)} value: {period}")
    };
    
    public static bool IsValidFiveMinutesDateTimeOffset(DateTimeOffset dateTimeOffset, 
        DateTimeOffset referenceDateTimeOffset)
    {
        return IsValidMinutesDateTimeOffset(dateTimeOffset, referenceDateTimeOffset, 5);
    }
    
    public static bool IsValidQuarterHourDateTimeOffset(DateTimeOffset dateTimeOffset, 
        DateTimeOffset referenceDateTimeOffset)
    {
        return IsValidMinutesDateTimeOffset(dateTimeOffset, referenceDateTimeOffset, 15);
    }
    
    public static bool IsValidHalfHourDateTimeOffset(DateTimeOffset dateTimeOffset, 
        DateTimeOffset referenceDateTimeOffset)
    {
        return IsValidMinutesDateTimeOffset(dateTimeOffset, referenceDateTimeOffset, 30);
    }

    public static bool IsValidHourDateTimeOffset(DateTimeOffset dateTimeOffset,
        DateTimeOffset referenceDateTimeOffset)
    {
        return dateTimeOffset.Minute == referenceDateTimeOffset.Minute
               && dateTimeOffset.Second == referenceDateTimeOffset.Second
               && dateTimeOffset.Millisecond == referenceDateTimeOffset.Millisecond
               && dateTimeOffset.Microsecond == referenceDateTimeOffset.Microsecond
               && dateTimeOffset.Nanosecond == referenceDateTimeOffset.Nanosecond;
    }
    
    public static bool IsValidMinutesDateTimeOffset(DateTimeOffset dateTimeOffset,
        DateTimeOffset referenceDateTimeOffset, int minutes)
    {
        return dateTimeOffset.Minute % minutes == referenceDateTimeOffset.Minute % minutes
               && dateTimeOffset.Second == referenceDateTimeOffset.Second
               && dateTimeOffset.Millisecond == referenceDateTimeOffset.Millisecond
               && dateTimeOffset.Microsecond == referenceDateTimeOffset.Microsecond
               && dateTimeOffset.Nanosecond == referenceDateTimeOffset.Nanosecond;
    }
}