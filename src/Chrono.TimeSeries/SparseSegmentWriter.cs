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

        PeriodGeometry.ValidateAligned(startInclusive, period, nameof(startInclusive));
        PeriodGeometry.ValidateAligned(endExclusive, period, nameof(endExclusive));

        for (var current = startInclusive; current < endExclusive; current = PeriodGeometry.AddPeriods(current, period))
            set(current, value);
    }
}
