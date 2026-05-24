using Chrono.Testing;
using Chrono.TimeSeries;
using FluentAssertions;
using System.Numerics;

namespace Chrono.Testing.Test;

public sealed class ChronoTimeSeriesGeneratorTest
{
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
