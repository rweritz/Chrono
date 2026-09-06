using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesOperationPolicyTest
{
    public static TheoryData<object, object, Period, object>
        FamilyTargetPeriodMatrix => new()
        {
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.FixedSlot },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.DynamicSlot },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.DynamicSlot },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Sparse, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None },

            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.BoundedStepwise },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.BoundedStepwise },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.BoundedStepwise },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.BoundedStepwise },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.BoundedStepwise },
            { TimeSeriesSemanticFamily.BoundedStepwise, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None },

            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.Hour, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.Month, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.Compatibility, Period.NonStandard, TimeSeriesResultAdapter.SortedArray },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.Hour, TimeSeriesResultAdapter.FixedSlot },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.FixedSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.Hour, TimeSeriesResultAdapter.DynamicSlot },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.Month, TimeSeriesResultAdapter.DynamicSlot },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.DynamicSlot, Period.NonStandard, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.Hour, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.Month, TimeSeriesResultAdapter.None },
            { TimeSeriesSemanticFamily.Mixed, TimeSeriesResultTarget.BoundedStepwise, Period.NonStandard, TimeSeriesResultAdapter.None }
        };

    [Theory]
    [MemberData(nameof(FamilyTargetPeriodMatrix))]
    public void Decide_ShouldApplyTheSingleFamilyTargetPeriodMatrix(
        object familyValue,
        object targetValue,
        Period period,
        object expectedAdapterValue)
    {
        var family = (TimeSeriesSemanticFamily)familyValue;
        var target = (TimeSeriesResultTarget)targetValue;
        var expectedAdapter = (TimeSeriesResultAdapter)expectedAdapterValue;
        var decision = TimeSeriesOperationPolicy.Decide(family, target, period);

        decision.SemanticFamily.Should().Be(family);
        decision.Target.Should().Be(target);
        decision.Adapter.Should().Be(expectedAdapter);
        decision.IsValid.Should().Be(expectedAdapter != TimeSeriesResultAdapter.None);
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
    public void ResamplingAdapter_ShouldRejectAFixedSlotTargetForCalendarGeometry()
    {
        IReadOnlyTimeSeries<int> sparse = new SortedArrayTimeSeries<int>(Period.Month);

        var success = TimeSeriesAggregation.TryResampleAsFixedSlotTimeSeries(sparse, Period.Month, out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }
}
