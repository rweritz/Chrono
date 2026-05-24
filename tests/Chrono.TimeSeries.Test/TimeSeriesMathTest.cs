using System.Linq;
using System.Reflection;
using FluentAssertions;

namespace Chrono.TimeSeries.Test;

public class TimeSeriesMathTest
{
    [Theory]
    [InlineData(nameof(TimeSeriesMath.Add))]
    [InlineData(nameof(TimeSeriesMath.Subtract))]
    [InlineData(nameof(TimeSeriesMath.Multiply))]
    [InlineData(nameof(TimeSeriesMath.Divide))]
    public void SparseBinaryOverloads_ShouldExposeExpectedResultMatrix(string methodName)
    {
        GetSparseBinaryOverload(methodName, typeof(IReadOnlySparseTimeSeries<int>), typeof(IReadOnlySparseTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));

        GetSparseBinaryOverload(methodName, typeof(SortedArrayTimeSeries<int>), typeof(SortedArrayTimeSeries<int>))
            .ReturnType.Should().Be(typeof(SortedArrayTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(FixedSlotTimeSeries<int>), typeof(FixedSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(FixedSlotTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(DynamicSlotTimeSeries<int>), typeof(DynamicSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(DynamicSlotTimeSeries<int>));

        GetSparseBinaryOverload(methodName, typeof(SortedArrayTimeSeries<int>), typeof(FixedSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(SortedArrayTimeSeries<int>), typeof(DynamicSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(FixedSlotTimeSeries<int>), typeof(DynamicSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(FixedSlotTimeSeries<int>), typeof(SortedArrayTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(DynamicSlotTimeSeries<int>), typeof(SortedArrayTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(DynamicSlotTimeSeries<int>), typeof(FixedSlotTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(IReadOnlySparseTimeSeries<int>), typeof(IBoundedStepwiseTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
        GetSparseBinaryOverload(methodName, typeof(IBoundedStepwiseTimeSeries<int>), typeof(IReadOnlySparseTimeSeries<int>))
            .ReturnType.Should().Be(typeof(IReadOnlySparseTimeSeries<int>));
    }

    [Fact]
    public void SortedArray_Add_Subtract_Multiply_Divide_ShouldWorkWithIntersection()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);
        var t2 = t1.AddMinutes(5);

        var a = new SortedArrayTimeSeries<double>(Period.FiveMinutes);
        var b = new SortedArrayTimeSeries<double>(Period.FiveMinutes);

        a[t0] = 10;
        a[t1] = 20;
        b[t1] = 4;
        b[t2] = 8;

        var add = TimeSeriesMath.Add(a, b, MissingValuePolicy.Intersection);
        var sub = TimeSeriesMath.Subtract(a, b, MissingValuePolicy.Intersection);
        var mul = TimeSeriesMath.Multiply(a, b, MissingValuePolicy.Intersection);
        var div = TimeSeriesMath.Divide(a, b, MissingValuePolicy.Intersection);

        add.ExplicitPointCount.Should().Be(1);
        add[t1].Should().Be(24);
        sub[t1].Should().Be(16);
        mul[t1].Should().Be(80);
        div[t1].Should().Be(5);
    }

    [Fact]
    public void SortedArray_UnionWithZero_ShouldIncludeAllKeys()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);

        var a = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        var b = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        a[t0] = 2;
        b[t1] = 3;

        var add = TimeSeriesMath.Add(a, b, MissingValuePolicy.UnionWithZero);

        add.ExplicitPointCount.Should().Be(2);
        add[t0].Should().Be(2);
        add[t1].Should().Be(3);
    }

    [Fact]
    public void ScalarOperations_ShouldWork()
    {
        var t0 = new DateTimeOffset(2022, 2, 6, 5, 6, 7, 8, TimeSpan.FromHours(1));
        var t1 = t0.AddMinutes(5);

        var source = new SortedArrayTimeSeries<decimal>(Period.FiveMinutes);
        source[t0] = 2m;
        source[t1] = 8m;

        var multiplied = TimeSeriesMath.Multiply(source, 2m);
        var added = TimeSeriesMath.Add(source, 3m);
        var divided = TimeSeriesMath.Divide(source, 2m);

        multiplied[t0].Should().Be(4m);
        multiplied[t1].Should().Be(16m);
        added[t0].Should().Be(5m);
        divided[t1].Should().Be(4m);
    }

    [Fact]
    public void RegularSeries_BinaryAndScalarOperations_ShouldWork()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);

        var a = new FixedSlotTimeSeries<double>(Period.FiveMinutes);
        var b = new FixedSlotTimeSeries<double>(Period.FiveMinutes);

        a[start] = 1;
        a[start.AddMinutes(5)] = 2;
        b[start] = 10;
        b[start.AddMinutes(5)] = 20;

        var add = TimeSeriesMath.Add(a, b);
        var scaled = TimeSeriesMath.Multiply(a, 3d);

        add[start].Should().Be(11);
        add[start.AddMinutes(5)].Should().Be(22);
        scaled[start].Should().Be(3);
        scaled[start.AddMinutes(5)].Should().Be(6);
    }

    [Fact]
    public void SparseFamilyMath_ShouldWorkAcrossImplementationsThroughSparseContract()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);

        IReadOnlySparseTimeSeries<int> left = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        var rightSeries = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        IReadOnlySparseTimeSeries<int> right = rightSeries;

        ((ITimeSeries<int>)left)[start] = 2;
        ((ITimeSeries<int>)left)[middle] = 4;
        rightSeries[middle] = 10;
        rightSeries[end] = 20;

        var union = TimeSeriesMath.Add(left, right, MissingValuePolicy.UnionWithZero);

        union.ExplicitPointCount.Should().Be(3);
        union[start].Should().Be(2);
        union[middle].Should().Be(14);
        union[end].Should().Be(20);
        union.GetPoints().Select(point => point.Timestamp).Should().Equal(start, middle, end);
    }

    [Fact]
    public void SparseFamilyMath_MixedConcreteInputs_ShouldRouteThroughCompatibilityResult()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        var left = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        var right = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);

        left[start] = 2;
        left[middle] = 4;
        right[middle] = 10;
        right[end] = 20;

        IReadOnlySparseTimeSeries<int> union = TimeSeriesMath.Add(left, right, MissingValuePolicy.UnionWithZero);

        union.Should().BeOfType<SortedArrayTimeSeries<int>>();
        union.ExplicitPointCount.Should().Be(3);
        union.MinDate.Should().Be(start);
        union.MaxDate.Should().Be(end);
        union[start].Should().Be(2);
        union[middle].Should().Be(14);
        union[end].Should().Be(20);
    }

    [Fact]
    public void SparseFamilyMath_MixedConcreteInputs_ShouldKeepMissingValuePolicyExplicit()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        var left = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        var right = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);

        left[start] = 7;
        left[middle] = 11;
        right[middle] = 3;
        right[end] = 5;

        var intersection = TimeSeriesMath.Subtract(left, right, MissingValuePolicy.Intersection);
        Action throwOnMissing = () => TimeSeriesMath.Subtract(left, right, MissingValuePolicy.Throw);

        intersection.Should().BeOfType<SortedArrayTimeSeries<int>>();
        intersection.ExplicitPointCount.Should().Be(1);
        intersection.MinDate.Should().Be(middle);
        intersection.MaxDate.Should().Be(middle);
        intersection[middle].Should().Be(8);
        throwOnMissing.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SparseAndBoundedStepwiseMath_Intersection_ShouldPreserveSparsePoints()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        var after = start.AddMinutes(15);
        var sparse = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        IBoundedStepwiseTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.FiveMinutes, middle, after, 10);

        sparse[start] = 2;
        sparse[middle] = 3;
        sparse[end] = 5;

        var result = TimeSeriesMath.Add(sparse, stepwise, MissingValuePolicy.Intersection);

        result.Should().BeAssignableTo<IReadOnlySparseTimeSeries<int>>();
        result.ExplicitPointCount.Should().Be(2);
        result.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(middle, 13),
            new TimeSeriesPoint<int>(end, 15));
    }

    [Fact]
    public void SparseAndBoundedStepwiseMath_UnionWithZero_ShouldNotDensifySparseResults()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var middle = start.AddMinutes(5);
        var end = start.AddMinutes(10);
        IBoundedStepwiseTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, end, 10);
        var sparse = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

        sparse[start] = 2;
        sparse[end] = 3;

        var result = TimeSeriesMath.Subtract(stepwise, sparse, MissingValuePolicy.UnionWithZero);

        result.Should().BeAssignableTo<IReadOnlySparseTimeSeries<int>>();
        result.ExplicitPointCount.Should().Be(2);
        result.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 8),
            new TimeSeriesPoint<int>(end, 7));
        result.TryGetValue(middle, out _).Should().BeFalse();
    }

    [Fact]
    public void SparseAndBoundedStepwiseMath_ShouldRequireExplicitPeriodCompatibility()
    {
        var timestamp = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sparse = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        IBoundedStepwiseTimeSeries<int> stepwise = new StepwiseTimeSeries<int>(Period.Hour, timestamp, timestamp.AddHours(1), 10);

        sparse[timestamp] = 2;

        Action act = () => TimeSeriesMath.Multiply(sparse, stepwise, MissingValuePolicy.UnionWithZero);

        act.Should().Throw<InvalidOperationException>().WithMessage("Periods must match.");
    }

    [Fact]
    public void SparseFamilyMath_ExactConcreteInputs_ShouldPreserveConcreteResultFamily()
    {
        var start = new DateTimeOffset(2022, 2, 6, 5, 0, 0, TimeSpan.Zero);
        var next = start.AddMinutes(5);

        var sortedLeft = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        var sortedRight = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
        sortedLeft[start] = 2;
        sortedRight[start] = 3;

        var fixedLeft = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        var fixedRight = new FixedSlotTimeSeries<int>(Period.FiveMinutes);
        fixedLeft[start] = 5;
        fixedRight[start] = 7;

        var dynamicLeft = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        var dynamicRight = new DynamicSlotTimeSeries<int>(Period.FiveMinutes);
        dynamicLeft[next] = 11;
        dynamicRight[next] = 13;

        TimeSeriesMath.Add(sortedLeft, sortedRight).Should().BeOfType<SortedArrayTimeSeries<int>>();
        TimeSeriesMath.Add(fixedLeft, fixedRight).Should().BeOfType<FixedSlotTimeSeries<int>>();
        TimeSeriesMath.Add(dynamicLeft, dynamicRight).Should().BeOfType<DynamicSlotTimeSeries<int>>();
    }

    private static MethodInfo GetSparseBinaryOverload(string methodName, Type leftType, Type rightType)
    {
        return typeof(TimeSeriesMath)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.IsGenericMethodDefinition ? method.MakeGenericMethod(typeof(int)) : method)
            .Single(method =>
            {
                if (method.Name != methodName)
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length == 3
                    && parameters[0].ParameterType == leftType
                    && parameters[1].ParameterType == rightType
                    && parameters[2].ParameterType == typeof(MissingValuePolicy);
            });
    }


    [Fact]
    public void BoundedStepwiseMath_ShouldUseDenseLogicalRangeSemantics()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = start.AddMinutes(5);
        var third = start.AddMinutes(10);
        var end = start.AddMinutes(15);

        IBoundedStepwiseTimeSeries<int> left = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, third, 1);
        IBoundedStepwiseTimeSeries<int> right = new StepwiseTimeSeries<int>(Period.FiveMinutes, second, end, 10);

        left[second] = 2;
        left[third] = 2;

        var sum = TimeSeriesMath.Add(left, right, MissingValuePolicy.UnionWithZero);

        sum.LogicalRangeStart.Should().Be(start);
        sum.LogicalRangeEnd.Should().Be(end);
        sum[start].Should().Be(1);
        sum[second].Should().Be(12);
        sum[third].Should().Be(12);
        sum[end].Should().Be(10);
        sum.GetChangePoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(second, 12),
            new TimeSeriesPoint<int>(end, 10));
    }

    [Fact]
    public void BoundedStepwiseUnionWithZero_ShouldRejectDisconnectedLogicalRanges()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var leftEnd = start.AddMinutes(10);
        var rightStart = start.AddMinutes(20);
        var rightEnd = start.AddMinutes(30);
        IBoundedStepwiseTimeSeries<int> left = new StepwiseTimeSeries<int>(Period.FiveMinutes, start, leftEnd, 1);
        IBoundedStepwiseTimeSeries<int> right = new StepwiseTimeSeries<int>(Period.FiveMinutes, rightStart, rightEnd, 2);

        Action act = () => TimeSeriesMath.Add(left, right, MissingValuePolicy.UnionWithZero);

        act.Should().Throw<InvalidOperationException>();
    }
}
