using System.Numerics;
using Chrono.TimeSeries;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Chrono.Testing;

/// <summary>
/// Provides FluentAssertions assertions for Chrono time-series values.
/// </summary>
/// <typeparam name="T">The numeric value type stored by the time series.</typeparam>
public class TimeSeriesAssertions<T> : ReferenceTypeAssertions<IReadOnlyTimeSeries<T>, TimeSeriesAssertions<T>>
    where T : struct, INumber<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSeriesAssertions{T}"/> class.
    /// </summary>
    public TimeSeriesAssertions(IReadOnlyTimeSeries<T> subject)
        : base(subject)
    {
    }

    /// <inheritdoc />
    protected override string Identifier => "time series";

    /// <summary>
    /// Asserts that the sparse time series has the expected explicit point count.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveCount(int expectedCount)
    {
        RunAssertion(() => TimeSeriesAssert.HasCount(GetSparseSubject(), expectedCount));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the time series has the expected period.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HavePeriod(Period expectedPeriod)
    {
        RunAssertion(() => TimeSeriesAssert.HasPeriod(Subject, expectedPeriod));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the sparse time series has the expected minimum explicit point timestamp.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveMinDate(DateTimeOffset expectedMinDate)
    {
        var sparse = GetSparseSubject();
        RunAssertion(() => TimeSeriesAssert.HasDateRange(sparse, expectedMinDate, sparse.MaxDate));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the sparse time series has the expected maximum explicit point timestamp.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveMaxDate(DateTimeOffset expectedMaxDate)
    {
        var sparse = GetSparseSubject();
        RunAssertion(() => TimeSeriesAssert.HasDateRange(sparse, sparse.MinDate, expectedMaxDate));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the time series contains the expected value at the timestamp.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> ContainValueAt(DateTimeOffset timestamp, T expectedValue) =>
        ContainValueAt(timestamp, expectedValue, T.Zero);

    /// <summary>
    /// Asserts that the time series contains the expected value at the timestamp within the supplied tolerance.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> ContainValueAt(DateTimeOffset timestamp, T expectedValue, T tolerance)
    {
        RunAssertion(() => TimeSeriesAssert.ValueAtCloseTo(Subject, timestamp, expectedValue, tolerance));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the sparse time series has the same period, explicit timestamps, and values as another series.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> BeStructurallyEquivalentTo(
        IReadOnlySparseTimeSeries<T> expected,
        T tolerance)
    {
        RunAssertion(() => TimeSeriesAssert.Equal(expected, GetSparseSubject(), tolerance));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the sparse time series has the same period, explicit timestamps, and values as another series.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> BeEquivalentTo(
        IReadOnlySparseTimeSeries<T> expected,
        T tolerance) =>
        BeStructurallyEquivalentTo(expected, tolerance);

    /// <summary>
    /// Asserts that every adjacent explicit sparse time-series point is separated by exactly one period.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveNoGaps()
    {
        RunAssertion(() => TimeSeriesAssert.HasNoGaps(GetSparseSubject()));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the sum of explicit sparse time-series values is within the supplied tolerance.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveSumCloseTo(T expectedSum, T tolerance)
    {
        RunAssertion(() => TimeSeriesAssert.SumCloseTo(GetSparseSubject(), expectedSum, tolerance));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that every explicit sparse time-series value is within the inclusive range.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> OnlyContainValuesInRange(T min, T max)
    {
        RunAssertion(() => TimeSeriesAssert.AllValuesInRange(GetSparseSubject(), min, max));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that every explicit sparse time-series value is strictly greater than the supplied threshold.
    /// </summary>
    public AndConstraint<TimeSeriesAssertions<T>> HaveAllValuesGreaterThan(T threshold)
    {
        RunAssertion(() => TimeSeriesAssert.AllValuesGreaterThan(GetSparseSubject(), threshold));
        return new AndConstraint<TimeSeriesAssertions<T>>(this);
    }

    private IReadOnlySparseTimeSeries<T> GetSparseSubject()
    {
        Execute.Assertion
            .ForCondition(Subject is IReadOnlySparseTimeSeries<T>)
            .FailWith("Expected {context:time series} to be a sparse Chrono time series.");

        return (IReadOnlySparseTimeSeries<T>)Subject;
    }

    private static void RunAssertion(Action assertion)
    {
        try
        {
            assertion();
        }
        catch (TimeSeriesAssertionException ex)
        {
            Execute.Assertion.FailWith(ex.Message);
        }
    }
}
