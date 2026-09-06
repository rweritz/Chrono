using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesOperationPolicyTest
{
    private static readonly PolicyCase[] _familyTargetPeriodMatrix =
    [
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.FixedSlot),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.DynamicSlot),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.DynamicSlot),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None),

        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.BoundedStepwise),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.BoundedStepwise),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.BoundedStepwise),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.BoundedStepwise),
        new(TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None),

        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.SortedArray),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.FixedSlot),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.DynamicSlot),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.DynamicSlot),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.None),
        new(TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None)
    ];

    [Fact]
    public void Decide_ShouldApplyTheSingleFamilyTargetPeriodMatrix()
    {
        foreach (var testCase in _familyTargetPeriodMatrix)
        {
            var decision = TimeSeriesOperationPolicy.Decide(testCase.Family, testCase.Target, testCase.Period);

            decision.SemanticFamily.Should().Be(testCase.Family, testCase.ToString());
            decision.Target.Should().Be(testCase.Target, testCase.ToString());
            decision.Adapter.Should().Be(testCase.ExpectedAdapter, testCase.ToString());
            decision.IsValid.Should().Be(testCase.ExpectedAdapter != TimeSeriesResultAdapter.None, testCase.ToString());
        }
    }

    [Fact]
    public void Classify_ShouldDistinguishSparseBoundedStepwiseAndMixedFamilies()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> sparse = new SortedArrayTimeSeries<int>(Period.Hour);
        IReadOnlyTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.Hour, start, start, 1);

        TimeSeriesOperationPolicy.Classify(sparse).Should().Be(TimeSeriesSemanticFamily.Sparse);
        TimeSeriesOperationPolicy.Classify(stepwise).Should().Be(TimeSeriesSemanticFamily.BoundedStepwise);
        TimeSeriesOperationPolicy.Classify(sparse, stepwise).Should().Be(TimeSeriesSemanticFamily.Mixed);
    }

    [Fact]
    public void ExactConcreteSelection_ShouldIncludeResultPeriodRepresentability()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> fixedSlot = new FixedSlotTimeSeries<int>(Period.Hour);
        IReadOnlyTimeSeries<int> dynamicSlot = new DynamicSlotTimeSeries<int>(Period.Hour);
        IReadOnlyTimeSeries<int> sortedArray = new SortedArrayTimeSeries<int>(Period.Hour);
        IReadOnlyTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.Hour, start, start, 1);

        TimeSeriesOperationPolicy.SelectExactConcreteAdapter(fixedSlot, Period.Month)
            .Should().Be(TimeSeriesResultAdapter.None);
        TimeSeriesOperationPolicy.SelectExactConcreteAdapter(dynamicSlot, Period.NonStandard)
            .Should().Be(TimeSeriesResultAdapter.None);
        TimeSeriesOperationPolicy.SelectExactConcreteAdapter(sortedArray, Period.NonStandard)
            .Should().Be(TimeSeriesResultAdapter.SortedArray);
        TimeSeriesOperationPolicy.SelectExactConcreteAdapter(stepwise, Period.NonStandard)
            .Should().Be(TimeSeriesResultAdapter.None);
    }

    [Fact]
    public void ArithmeticAdapter_ShouldRouteMixedFamiliesToSparseSpecialization()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> sparse = new SortedArrayTimeSeries<int>(Period.Hour);
        IReadOnlyTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.Hour, start, start, 3);
        ((ITimeSeries<int>)sparse)[start] = 2;

        var success = TimeSeriesMath.TryAddAsDynamicSlotTimeSeries(sparse, stepwise, out var result);

        success.Should().BeTrue();
        result.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        result![start].Should().Be(5);
    }

    [Fact]
    public void AggregationAdapter_ShouldRejectSemanticallyInvalidStepwiseTarget()
    {
        IReadOnlyTimeSeries<int> sparse = new SortedArrayTimeSeries<int>(Period.Hour);

        var success = TimeSeriesAggregation.TryAggregateAsBoundedStepwiseTimeSeries<int, int, SumAggregator<int>>(
            sparse, Period.Day, out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void AggregationAdapters_ShouldRouteSuccessfulExplicitAndCompatibilityResults()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlySparseTimeSeries<int> source = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryAggregateAsFixedSlotTimeSeries<int, int, SumAggregator<int>>(
            source, Period.Hour, out var explicitResult);
        var compatibilityResult = TimeSeriesAggregation.Sum(source, Period.Hour);

        success.Should().BeTrue();
        explicitResult.Should().BeOfType<FixedSlotTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 6));
        compatibilityResult.Should().BeOfType<SortedArrayTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 6));
    }

    [Fact]
    public void ExplicitResamplingAdapter_ShouldRouteSuccessfulSparseResult()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryResampleAsDynamicSlotTimeSeries(
            source, Period.FiveMinutes, out var result);

        success.Should().BeTrue();
        result.Should().BeOfType<DynamicSlotTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(
                new TimeSeriesPoint<int>(start, 2),
                new TimeSeriesPoint<int>(start.AddMinutes(5), 4));
    }

    [Fact]
    public void BoundedStepwiseOperations_ShouldRejectNonStandardResultsAtThePolicyBoundary()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new StepwiseTimeSeries<int>(Period.Hour, start, start, 1);

        var success = TimeSeriesAggregation.TryResampleAsBoundedStepwiseTimeSeries(
            source, Period.NonStandard, out var result);
        var exactOperation = () => TimeSeriesAggregation.Resample(
            (StepwiseTimeSeries<int>)source, Period.NonStandard);

        success.Should().BeFalse();
        result.Should().BeNull();
        exactOperation.Should().Throw<NotSupportedException>()
            .WithMessage("*cannot produce a BoundedStepwise result for period NonStandard*");
    }

    [Fact]
    public void ResamplingAdapter_ShouldRejectAFixedSlotTargetForCalendarGeometry()
    {
        IReadOnlyTimeSeries<int> sparse = new SortedArrayTimeSeries<int>(Period.Month);

        var success = TimeSeriesAggregation.TryResampleAsFixedSlotTimeSeries(sparse, Period.Month, out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    private sealed record PolicyCase(
        TimeSeriesSemanticFamily Family,
        TimeSeriesResultTarget Target,
        Period Period,
        TimeSeriesResultAdapter ExpectedAdapter);
}
