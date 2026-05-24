using FluentAssertions;
using System.Reflection;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesAggregationRoutingTest
{
    [Fact]
    public void AggregationSpecializationApis_ShouldExposeMajorTargetFamilies()
    {
        var methods = typeof(TimeSeriesAggregation)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Try", StringComparison.Ordinal))
            .ToList();

        methods.Should().Contain(method => HasAggregateSpecializationSignature(method, "TryAggregateAsFixedSlotTimeSeries",
            typeof(FixedSlotTimeSeries<>)));
        methods.Should().Contain(method => HasAggregateSpecializationSignature(method, "TryAggregateAsDynamicSlotTimeSeries",
            typeof(DynamicSlotTimeSeries<>)));
        methods.Should().Contain(method => HasAggregateSpecializationSignature(method, "TryAggregateAsBoundedStepwiseTimeSeries",
            typeof(StepwiseTimeSeries<>)));
        methods.Should().Contain(method => HasResampleSpecializationSignature(method, "TryResampleAsFixedSlotTimeSeries",
            typeof(FixedSlotTimeSeries<>)));
        methods.Should().Contain(method => HasResampleSpecializationSignature(method, "TryResampleAsDynamicSlotTimeSeries",
            typeof(DynamicSlotTimeSeries<>)));
        methods.Should().Contain(method => HasResampleSpecializationSignature(method, "TryResampleAsBoundedStepwiseTimeSeries",
            typeof(StepwiseTimeSeries<>)));
    }

    [Fact]
    public void TryAggregateAsDynamicSlotTimeSeries_ShouldSucceedForSemanticallySparseAggregation()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryAggregateAsDynamicSlotTimeSeries<int, int, SumAggregator<int>>(
            source,
            Period.Hour,
            out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        result!.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 6));
    }

    [Fact]
    public void TryAggregateAsFixedSlotTimeSeries_ShouldSucceedForFixedPeriodSparseAggregation()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryAggregateAsFixedSlotTimeSeries<int, int, SumAggregator<int>>(
            source,
            Period.Hour,
            out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeOfType<FixedSlotTimeSeries<int>>();
        result!.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 6));
    }

    [Fact]
    public void TryAggregateAsBoundedStepwiseTimeSeries_ShouldFailForSparseAggregationSemantics()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryAggregateAsBoundedStepwiseTimeSeries<int, int, SumAggregator<int>>(
            source,
            Period.Hour,
            out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryResampleAsDynamicSlotTimeSeries_ShouldSucceedForSemanticallySparseExplicitResampling()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        ((ITimeSeries<int>)source)[start] = 2;
        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryResampleAsDynamicSlotTimeSeries(source, Period.FiveMinutes, out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        result!.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 2),
            new TimeSeriesPoint<int>(start.AddMinutes(5), 4));
    }

    [Fact]
    public void TryResampleAsBoundedStepwiseTimeSeries_ShouldSucceedForSemanticallyBoundedStepwiseExplicitResampling()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, start.AddMinutes(10), 2);

        ((ITimeSeries<int>)source)[start.AddMinutes(5)] = 4;

        var success = TimeSeriesAggregation.TryResampleAsBoundedStepwiseTimeSeries(source, Period.FiveMinutes, out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeOfType<StepwiseTimeSeries<int>>();
        result!.LogicalRangeStart.Should().Be(start);
        result.LogicalRangeEnd.Should().Be(start.AddMinutes(10));
        result.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 2),
            new TimeSeriesPoint<int>(start.AddMinutes(5), 4),
            new TimeSeriesPoint<int>(start.AddMinutes(10), 2));
    }

    [Fact]
    public void TryResampleAsFixedSlotTimeSeries_ShouldFailWhenTheTargetPeriodCannotUseFixedSlots()
    {
        var january = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyTimeSeries<int> source = new SortedArrayTimeSeries<int>(Period.Month);

        ((ITimeSeries<int>)source)[january] = 2;
        ((ITimeSeries<int>)source)[january.AddMonths(1)] = 4;

        var success = TimeSeriesAggregation.TryResampleAsFixedSlotTimeSeries(source, Period.Month, out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Aggregate_ExactConcreteTypes_ShouldPreserveConcreteFamilies()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var fixedSlot = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        fixedSlot[start] = 3;
        fixedSlot[start.AddMinutes(5)] = 5;

        var dynamicSlot = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        dynamicSlot[start] = 7;
        dynamicSlot[start.AddMinutes(30)] = 11;

        var sparse = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        sparse[start] = 13;
        sparse[start.AddMinutes(5)] = 17;

        var stepwise = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, start.AddMinutes(55), 1);
        stepwise.SetSegment(start.AddMinutes(30), start.AddHours(1), 2);

        var fixedSlotSum = TimeSeriesAggregation.Sum(fixedSlot, Period.Hour);
        var fixedSlotAverage = TimeSeriesAggregation.Average(fixedSlot, Period.Hour);
        var fixedSlotMin = TimeSeriesAggregation.Min(fixedSlot, Period.Hour);
        var fixedSlotMax = TimeSeriesAggregation.Max(fixedSlot, Period.Hour);
        var fixedSlotCount = TimeSeriesAggregation.Count(fixedSlot, Period.Hour);
        var dynamicSlotSum = TimeSeriesAggregation.Sum(dynamicSlot, Period.Hour);
        var dynamicSlotAverage = TimeSeriesAggregation.Average(dynamicSlot, Period.Hour);
        var dynamicSlotMin = TimeSeriesAggregation.Min(dynamicSlot, Period.Hour);
        var dynamicSlotMax = TimeSeriesAggregation.Max(dynamicSlot, Period.Hour);
        var dynamicSlotCount = TimeSeriesAggregation.Count(dynamicSlot, Period.Hour);
        var sparseSum = TimeSeriesAggregation.Sum(sparse, Period.Hour);
        var sparseAverage = TimeSeriesAggregation.Average(sparse, Period.Hour);
        var sparseMin = TimeSeriesAggregation.Min(sparse, Period.Hour);
        var sparseMax = TimeSeriesAggregation.Max(sparse, Period.Hour);
        var sparseCount = TimeSeriesAggregation.Count(sparse, Period.Hour);
        var stepwiseSum = TimeSeriesAggregation.Sum(stepwise, Period.Hour);
        var stepwiseAverage = TimeSeriesAggregation.Average(stepwise, Period.Hour);
        var stepwiseMin = TimeSeriesAggregation.Min(stepwise, Period.Hour);
        var stepwiseMax = TimeSeriesAggregation.Max(stepwise, Period.Hour);
        var stepwiseCount = TimeSeriesAggregation.Count(stepwise, Period.Hour);

        fixedSlotSum.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedSlotSum.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 8));
        fixedSlotAverage.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedSlotAverage.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 4));
        fixedSlotMin.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedSlotMin.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 3));
        fixedSlotMax.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedSlotMax.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 5));
        fixedSlotCount.Should().BeOfType<FixedSlotTimeSeries<int>>();
        fixedSlotCount.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));

        dynamicSlotSum.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicSlotSum.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 18));
        dynamicSlotAverage.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicSlotAverage.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 9));
        dynamicSlotMin.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicSlotMin.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 7));
        dynamicSlotMax.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicSlotMax.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 11));
        dynamicSlotCount.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        dynamicSlotCount.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));

        sparseSum.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseSum.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 30));
        sparseAverage.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseAverage.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 15));
        sparseMin.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseMin.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 13));
        sparseMax.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseMax.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 17));
        sparseCount.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseCount.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));

        stepwiseSum.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseSum.LogicalRangeStart.Should().Be(start);
        stepwiseSum.LogicalRangeEnd.Should().Be(start);
        stepwiseSum.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 18));
        stepwiseAverage.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseAverage.LogicalRangeStart.Should().Be(start);
        stepwiseAverage.LogicalRangeEnd.Should().Be(start);
        stepwiseAverage.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 1));
        stepwiseMin.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseMin.LogicalRangeStart.Should().Be(start);
        stepwiseMin.LogicalRangeEnd.Should().Be(start);
        stepwiseMin.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 1));
        stepwiseMax.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseMax.LogicalRangeStart.Should().Be(start);
        stepwiseMax.LogicalRangeEnd.Should().Be(start);
        stepwiseMax.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));

        stepwiseCount.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseCount.LogicalRangeStart.Should().Be(start);
        stepwiseCount.LogicalRangeEnd.Should().Be(start);
        stepwiseCount.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 12));
    }

    [Fact]
    public void Resample_ExactConcreteTypes_ShouldPreserveConcreteFamilies()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var fixedSlot = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        fixedSlot[start] = 3;
        fixedSlot[start.AddMinutes(5)] = 5;

        var dynamicSlot = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        dynamicSlot[start] = 7;
        dynamicSlot[start.AddMinutes(5)] = 11;

        var sparse = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        sparse[start] = 13;
        sparse[start.AddMinutes(5)] = 17;

        var stepwise = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, start.AddMinutes(5), 19);
        stepwise[start.AddMinutes(5)] = 23;

        TimeSeriesAggregation.Resample(fixedSlot, Period.FiveMinutes).Should().BeOfType<FixedSlotTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(fixedSlot.GetPoints());
        TimeSeriesAggregation.Resample(dynamicSlot, Period.FiveMinutes).Should().BeOfType<DynamicSlotTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(dynamicSlot.GetPoints());
        TimeSeriesAggregation.Resample(sparse, Period.FiveMinutes).Should().BeOfType<SortedArrayTimeSeries<int>>()
            .Which.GetPoints().Should().Equal(sparse.GetPoints());
        TimeSeriesAggregation.Resample(stepwise, Period.FiveMinutes).Should().BeOfType<StepwiseTimeSeries<int>>()
            .Which.GetChangePoints().Should().Equal(stepwise.GetChangePoints());
    }

    [Fact]
    public void SharedContractAggregationAndResampling_ShouldAdvertiseSharedContractsAndUseCanonicalResults()
    {
        FindGenericOverload(nameof(TimeSeriesAggregation.Sum), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Average), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Min), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Max), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Count), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Sum), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Average), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Min), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Max), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Count), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Resample), typeof(IReadOnlySparseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IReadOnlySparseTimeSeries<>));
        FindGenericOverload(nameof(TimeSeriesAggregation.Resample), typeof(IBoundedStepwiseTimeSeries<>)).ReturnType
            .GetGenericTypeDefinition().Should().Be(typeof(IBoundedStepwiseTimeSeries<>));

        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var dynamicSlot = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        dynamicSlot[start] = 2;
        dynamicSlot[start.AddMinutes(30)] = 4;
        IReadOnlySparseTimeSeries<int> sparse = dynamicSlot;

        var sparseAggregate = TimeSeriesAggregation.Sum(sparse, Period.Hour);
        var sparseAverage = TimeSeriesAggregation.Average(sparse, Period.Hour);
        var sparseMin = TimeSeriesAggregation.Min(sparse, Period.Hour);
        var sparseMax = TimeSeriesAggregation.Max(sparse, Period.Hour);
        var sparseCount = TimeSeriesAggregation.Count(sparse, Period.Hour);
        var sparseResample = TimeSeriesAggregation.Resample(sparse, Period.FiveMinutes);

        sparseAggregate.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseAverage.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseMin.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseMax.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseCount.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseResample.Should().BeOfType<SortedArrayTimeSeries<int>>();
        sparseAggregate.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 6));
        sparseAverage.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 3));
        sparseMin.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));
        sparseMax.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 4));
        sparseCount.GetPoints().Should().Equal(new TimeSeriesPoint<int>(start, 2));
        sparseResample.GetPoints().Should().Equal(dynamicSlot.GetPoints());

        IBoundedStepwiseTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, start.AddMinutes(55), 3);
        stepwise.SetSegment(start.AddMinutes(30), start.AddHours(1), 5);

        var stepwiseAggregate = TimeSeriesAggregation.Sum(stepwise, Period.Hour);
        var stepwiseAverage = TimeSeriesAggregation.Average(stepwise, Period.Hour);
        var stepwiseMin = TimeSeriesAggregation.Min(stepwise, Period.Hour);
        var stepwiseMax = TimeSeriesAggregation.Max(stepwise, Period.Hour);
        var stepwiseCount = TimeSeriesAggregation.Count(stepwise, Period.Hour);
        var stepwiseResample = TimeSeriesAggregation.Resample(stepwise, Period.FiveMinutes);

        stepwiseAggregate.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseAverage.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseMin.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseMax.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseCount.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseResample.Should().BeOfType<StepwiseTimeSeries<int>>();
        stepwiseAggregate.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 48));
        stepwiseAverage.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 4));
        stepwiseMin.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 3));
        stepwiseMax.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 5));
        stepwiseCount.GetChangePoints().Should().Equal(new TimeSeriesPoint<int>(start, 12));
        stepwiseResample.GetChangePoints().Should().Equal(stepwise.GetChangePoints());
    }

    private static MethodInfo FindGenericOverload(string name, Type firstParameterTypeDefinition) =>
        typeof(TimeSeriesAggregation)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == name &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType: var parameterType }, ..] &&
                parameterType.IsGenericType &&
                parameterType.GetGenericTypeDefinition() == firstParameterTypeDefinition);

    private static bool HasAggregateSpecializationSignature(MethodInfo method, string methodName, Type outTypeDefinition)
    {
        if (method.Name != methodName || !method.IsGenericMethodDefinition || method.ReturnType != typeof(bool))
            return false;

        var genericArguments = method.GetGenericArguments();
        if (genericArguments.Length != 3)
            return false;

        var inputType = genericArguments[0];
        var outputType = genericArguments[1];
        var aggregatorType = genericArguments[2];
        var parameters = method.GetParameters();
        if (parameters.Length != 4 ||
            parameters[0].ParameterType != typeof(IReadOnlyTimeSeries<>).MakeGenericType(inputType) ||
            parameters[1].ParameterType != typeof(Period) ||
            !parameters[2].IsOut ||
            parameters[3].ParameterType != aggregatorType)
        {
            return false;
        }

        return parameters[2].ParameterType.GetElementType() == outTypeDefinition.MakeGenericType(outputType);
    }

    private static bool HasResampleSpecializationSignature(MethodInfo method, string methodName, Type outTypeDefinition)
    {
        if (method.Name != methodName || !method.IsGenericMethodDefinition || method.ReturnType != typeof(bool))
            return false;

        var genericArguments = method.GetGenericArguments();
        if (genericArguments.Length != 1)
            return false;

        var genericType = genericArguments[0];
        var parameters = method.GetParameters();
        if (parameters.Length != 3 ||
            parameters[0].ParameterType != typeof(IReadOnlyTimeSeries<>).MakeGenericType(genericType) ||
            parameters[1].ParameterType != typeof(Period) ||
            !parameters[2].IsOut)
        {
            return false;
        }

        return parameters[2].ParameterType.GetElementType() == outTypeDefinition.MakeGenericType(genericType);
    }
}
