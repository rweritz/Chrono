using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesAssertions : ReferenceTypeAssertions<IReadOnlyTimeSeries<double>, TimeSeriesAssertions>
{
    public TimeSeriesAssertions(IReadOnlyTimeSeries<double> subject) : base(subject)
    {
        
    }

    protected override string Identifier => "timeseries";
    
    public AndConstraint<TimeSeriesAssertions> ContainsValueAt(DateTimeOffset dateTimeOffset, double expectedValue)
    {
        Subject[dateTimeOffset].Should().Be(expectedValue);
        return new AndConstraint<TimeSeriesAssertions>(this);
    }
}
