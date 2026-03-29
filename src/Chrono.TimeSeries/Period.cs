using System.Diagnostics;

namespace Chrono.TimeSeries;

public enum Period
{
    NonStandard,
    FiveMinutes,
    QuaterHour,
    HalfHour,
    Hour,
    HalfDay,
    Day,
    Week,
    Month,
    QuaterYear,
    HalfYear,
    Year,
}