namespace Chrono.Testing;

/// <summary>
/// Selects the Chrono time-series implementation materialized by a deterministic builder.
/// </summary>
public enum ChronoSeriesShape
{
    /// <summary>
    /// Materializes a <c>SortedArrayTimeSeries&lt;T&gt;</c>.
    /// </summary>
    SortedArray,

    /// <summary>
    /// Materializes a <c>FixedSlotTimeSeries&lt;T&gt;</c>.
    /// </summary>
    FixedSlot,

    /// <summary>
    /// Materializes a <c>DynamicSlotTimeSeries&lt;T&gt;</c>.
    /// </summary>
    DynamicSlot,
}
