using Chrono.Testing;
using Chrono.TimeSeries;
using FluentAssertions;
using System.Numerics;

namespace Chrono.Testing.Test;

public sealed class ChronoTimeSeriesGeneratorTest
{
    [Fact]
    public void ConstantBuildsRequestedShapeWithConfiguredPeriodStartCountAndValue()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Day)
            .WithStart(start)
            .WithCount(3)
            .Constant(12.5)
            .AsFixedSlot()
            .Build();

        series.Should().BeOfType<FixedSlotTimeSeries<double>>();
        series.Period.Should().Be(Period.Day);
        series.ExplicitPointCount.Should().Be(3);
        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<double>(start, 12.5),
            new TimeSeriesPoint<double>(start.AddDays(1), 12.5),
            new TimeSeriesPoint<double>(start.AddDays(2), 12.5));
    }

    [Fact]
    public void RandomWalkBuildsSamePointsForSameSeed()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var first = ChronoTimeSeriesGenerator
            .For<decimal>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(42)
            .RandomWalk(100m, 2m)
            .AsSortedArray()
            .Build();

        var second = ChronoTimeSeriesGenerator
            .For<decimal>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(42)
            .RandomWalk(100m, 2m)
            .AsSortedArray()
            .Build();

        first.Should().BeOfType<SortedArrayTimeSeries<decimal>>();
        first.Period.Should().Be(Period.Hour);
        first.ExplicitPointCount.Should().Be(5);
        first.GetPoints().Select(point => point.Timestamp).Should().Equal(
            start,
            start.AddHours(1),
            start.AddHours(2),
            start.AddHours(3),
            start.AddHours(4));
        first.GetPoints().Should().Equal(second.GetPoints());
    }

    [Fact]
    public void LinearTrendBuildsArithmeticSequence()
    {
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var series = ChronoTimeSeriesGenerator
            .For<int>()
            .WithPeriod(Period.Month)
            .WithStart(start)
            .WithCount(4)
            .LinearTrend(10, 3)
            .AsDynamicSlot()
            .Build();

        series.Should().BeOfType<DynamicSlotTimeSeries<int>>();
        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 10),
            new TimeSeriesPoint<int>(start.AddMonths(1), 13),
            new TimeSeriesPoint<int>(start.AddMonths(2), 16),
            new TimeSeriesPoint<int>(start.AddMonths(3), 19));
    }

    [Fact]
    public void StepFunctionBuildsConfiguredLevelsForConfiguredStepLength()
    {
        var start = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        var series = ChronoTimeSeriesGenerator
            .For<int>()
            .WithPeriod(Period.Day)
            .WithStart(start)
            .WithCount(5)
            .StepFunction(2, 1, 5, 2)
            .AsSortedArray()
            .Build();

        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 1),
            new TimeSeriesPoint<int>(start.AddDays(1), 1),
            new TimeSeriesPoint<int>(start.AddDays(2), 5),
            new TimeSeriesPoint<int>(start.AddDays(3), 5),
            new TimeSeriesPoint<int>(start.AddDays(4), 2));
    }

    [Fact]
    public void SparseGeneratorBuildsSameGapsForSameSeedAndLeavesActualMissingPoints()
    {
        var start = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        var first = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(12)
            .WithSeed(99)
            .Constant(7.0)
            .Sparse(0.45)
            .AsSortedArray()
            .Build();

        var second = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(12)
            .WithSeed(99)
            .Constant(7.0)
            .Sparse(0.45)
            .AsSortedArray()
            .Build();

        var differentSeed = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(12)
            .WithSeed(100)
            .Constant(7.0)
            .Sparse(0.45)
            .AsSortedArray()
            .Build();

        first.GetPoints().Should().Equal(second.GetPoints());
        first.GetPoints().Should().NotEqual(differentSeed.GetPoints());
        first.ExplicitPointCount.Should().BeGreaterThan(0).And.BeLessThan(12);

        var explicitTimestamps = first.GetPoints().Select(point => point.Timestamp).ToHashSet();
        var missingTimestamp = Enumerable.Range(0, 12)
            .Select(offset => start.AddHours(offset))
            .First(timestamp => !explicitTimestamps.Contains(timestamp));

        first.TryGetValue(missingTimestamp, out _).Should().BeFalse();
    }

    [Fact]
    public void SeasonalBuildsPeriodAlignedDeterministicCycle()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var first = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(17)
            .Seasonal(amplitude: 10.0, cycleLength: 4, baseline: 50.0)
            .Build();

        var second = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(17)
            .Seasonal(amplitude: 10.0, cycleLength: 4, baseline: 50.0)
            .Build();

        first.GetPoints().Should().Equal(second.GetPoints());
        first.GetPoints().Select(point => point.Timestamp).Should().Equal(
            start,
            start.AddHours(1),
            start.AddHours(2),
            start.AddHours(3),
            start.AddHours(4));
        var values = first.GetPoints().Select(point => point.Value).ToArray();
        values[0].Should().BeApproximately(50.0, 0.000001);
        values[1].Should().BeApproximately(60.0, 0.000001);
        values[2].Should().BeApproximately(50.0, 0.000001);
        values[3].Should().BeApproximately(40.0, 0.000001);
        values[4].Should().BeApproximately(50.0, 0.000001);
    }

    [Fact]
    public void SeasonalFacadeBuildsDeterministicConfigurableCycle()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var first = TimeSeriesGenerator
            .Seasonal<double>(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(17)
            .WithAmplitude(10.0)
            .WithCycleLength(4)
            .WithBaseline(50.0)
            .AsDynamicSlot()
            .Build();

        var second = TimeSeriesGenerator
            .Seasonal<double>(Period.Hour)
            .WithStart(start)
            .WithCount(5)
            .WithSeed(17)
            .WithAmplitude(10.0)
            .WithCycleLength(4)
            .WithBaseline(50.0)
            .AsDynamicSlot()
            .Build();

        first.Should().BeOfType<DynamicSlotTimeSeries<double>>();
        first.GetPoints().Should().Equal(second.GetPoints());
        var values = first.GetPoints().Select(point => point.Value).ToArray();
        values[0].Should().BeApproximately(50.0, 0.000001);
        values[1].Should().BeApproximately(60.0, 0.000001);
        values[2].Should().BeApproximately(50.0, 0.000001);
        values[3].Should().BeApproximately(40.0, 0.000001);
        values[4].Should().BeApproximately(50.0, 0.000001);
    }

    [Fact]
    public void SawtoothFacadeBuildsRepeatingRamp()
    {
        var start = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var series = TimeSeriesGenerator
            .Sawtooth<int>(Period.Day)
            .WithStart(start)
            .WithCount(5)
            .WithAmplitude(6)
            .WithCycleLength(3)
            .Build();

        series.Period.Should().Be(Period.Day);
        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 0),
            new TimeSeriesPoint<int>(start.AddDays(1), 2),
            new TimeSeriesPoint<int>(start.AddDays(2), 4),
            new TimeSeriesPoint<int>(start.AddDays(3), 0),
            new TimeSeriesPoint<int>(start.AddDays(4), 2));
    }

    [Fact]
    public void ImpulseBuildsBaselineWithConfiguredSpikes()
    {
        var start = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var series = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.HalfHour)
            .WithStart(start)
            .WithCount(5)
            .Impulse(baseline: 1.5, (2, 9.0), (4, -2.0))
            .Build();

        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<double>(start, 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(30), 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(60), 9.0),
            new TimeSeriesPoint<double>(start.AddMinutes(90), 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(120), -2.0));
    }

    [Fact]
    public void ImpulseFacadeBuildsRequestedShapeWithConfiguredSpikes()
    {
        var start = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var series = TimeSeriesGenerator
            .Impulse<double>(Period.HalfHour)
            .WithStart(start)
            .WithCount(5)
            .WithBaseline(1.5)
            .WithSpikes((2, 9.0), (4, -2.0))
            .AsFixedSlot()
            .Build();

        series.Should().BeOfType<FixedSlotTimeSeries<double>>();
        series.Period.Should().Be(Period.HalfHour);
        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<double>(start, 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(30), 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(60), 9.0),
            new TimeSeriesPoint<double>(start.AddMinutes(90), 1.5),
            new TimeSeriesPoint<double>(start.AddMinutes(120), -2.0));
    }

    [Fact]
    public void CompositeFacadeCombinesAlignedGeneratorOutputs()
    {
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var baseline = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(4)
            .Constant(10.0);
        var spikes = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithStart(start)
            .WithCount(4)
            .Impulse(0.0, (1, 5.0), (3, -2.0));

        var series = TimeSeriesGenerator
            .Composite(baseline, spikes, static (left, right) => left + right)
            .Build();

        series.Period.Should().Be(Period.Hour);
        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<double>(start, 10.0),
            new TimeSeriesPoint<double>(start.AddHours(1), 15.0),
            new TimeSeriesPoint<double>(start.AddHours(2), 10.0),
            new TimeSeriesPoint<double>(start.AddHours(3), 8.0));
    }

    [Fact]
    public void BuildMaterializesDeterministicSortedArraySeries()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var first = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithCount(4)
            .StartingAt(start)
            .WithSeed(1234)
            .As(ChronoSeriesShape.SortedArray)
            .Build();

        var second = ChronoTimeSeriesGenerator
            .For<double>()
            .WithPeriod(Period.Hour)
            .WithCount(4)
            .StartingAt(start)
            .WithSeed(1234)
            .As(ChronoSeriesShape.SortedArray)
            .Build();

        first.Should().BeOfType<SortedArrayTimeSeries<double>>();
        first.Period.Should().Be(Period.Hour);
        first.ExplicitPointCount.Should().Be(4);
        first.MinDate.Should().Be(start);
        first.MaxDate.Should().Be(start.AddHours(3));
        first.GetPoints().Should().Equal(second.GetPoints());
    }

    [Fact]
    public void BuildMaterializesSupportedSparseShapesAcrossNumericTypes()
    {
        BuildAndAssert<int>(ChronoSeriesShape.SortedArray).Should().BeOfType<SortedArrayTimeSeries<int>>();
        BuildAndAssert<int>(ChronoSeriesShape.FixedSlot).Should().BeOfType<FixedSlotTimeSeries<int>>();
        BuildAndAssert<int>(ChronoSeriesShape.DynamicSlot).Should().BeOfType<DynamicSlotTimeSeries<int>>();
        BuildAndAssert<long>(ChronoSeriesShape.SortedArray).Should().BeOfType<SortedArrayTimeSeries<long>>();
        BuildAndAssert<float>(ChronoSeriesShape.FixedSlot).Should().BeOfType<FixedSlotTimeSeries<float>>();
        BuildAndAssert<decimal>(ChronoSeriesShape.DynamicSlot).Should().BeOfType<DynamicSlotTimeSeries<decimal>>();
    }

    [Fact]
    public void BuildMaterializesBoundaryNumericTypesReproducibly()
    {
        BuildAndAssert<byte>(ChronoSeriesShape.SortedArray).Should().BeOfType<SortedArrayTimeSeries<byte>>();
        BuildAndAssert<sbyte>(ChronoSeriesShape.SortedArray).Should().BeOfType<SortedArrayTimeSeries<sbyte>>();
        BuildAndAssert<byte>(ChronoSeriesShape.FixedSlot).Should().BeOfType<FixedSlotTimeSeries<byte>>();
        BuildAndAssert<sbyte>(ChronoSeriesShape.DynamicSlot).Should().BeOfType<DynamicSlotTimeSeries<sbyte>>();
    }

    private static ISparseTimeSeries<T> BuildAndAssert<T>(ChronoSeriesShape shape)
        where T : struct, INumber<T>
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var series = ChronoTimeSeriesGenerator
            .For<T>()
            .WithPeriod(Period.Hour)
            .WithCount(3)
            .StartingAt(start)
            .WithSeed(5678)
            .As(shape)
            .Build();

        series.GetPoints().Should().HaveCount(3);
        series.GetPoints().Select(point => point.Timestamp).Should().Equal(
            start,
            start.AddHours(1),
            start.AddHours(2));

        var repeated = ChronoTimeSeriesGenerator
            .For<T>()
            .WithPeriod(Period.Hour)
            .WithCount(3)
            .StartingAt(start)
            .WithSeed(5678)
            .As(shape)
            .Build();

        series.GetPoints().Should().Equal(repeated.GetPoints());
        return series;
    }
}
