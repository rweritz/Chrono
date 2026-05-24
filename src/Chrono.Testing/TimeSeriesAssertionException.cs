namespace Chrono.Testing;

/// <summary>
/// Represents a failed Chrono time-series assertion.
/// </summary>
public sealed class TimeSeriesAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSeriesAssertionException"/> class.
    /// </summary>
    public TimeSeriesAssertionException(string message)
        : base(message)
    {
    }
}
