using Chrono.Testing;
using Chrono.TimeSeries;
using FluentAssertions;

namespace Chrono.Testing.Test;

public sealed class TimeSeriesGeneratorArchitectureTest
{
    [Fact]
    public void GeneratorPatternsAreImplementedAsInternalStrategies()
    {
        var assembly = typeof(TimeSeriesGenerator).Assembly;
        var strategyInterface = assembly.GetType("Chrono.Testing.IGeneratorStrategy`1");

        strategyInterface.Should().NotBeNull();
        strategyInterface!.IsInterface.Should().BeTrue();
        strategyInterface.IsNotPublic.Should().BeTrue();

        var expectedStrategyNames = new[]
        {
            "ConstantGeneratorStrategy`1",
            "RandomWalkGeneratorStrategy`1",
            "LinearTrendGeneratorStrategy`1",
            "StepFunctionGeneratorStrategy`1",
            "SeasonalGeneratorStrategy`1",
            "SawtoothGeneratorStrategy`1",
            "ImpulseGeneratorStrategy`1",
        };

        foreach (var strategyName in expectedStrategyNames)
        {
            var strategyType = assembly.GetType($"Chrono.Testing.{strategyName}");

            strategyType.Should().NotBeNull();
            strategyType!.IsNotPublic.Should().BeTrue();
            strategyType.GetInterfaces()
                .Should()
                .Contain(type => type.IsGenericType
                    && type.GetGenericTypeDefinition() == strategyInterface);
        }
    }

    [Fact]
    public void FacadeGeneratorsPreserveGeneratedValuesThroughStrategies()
    {
        var start = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var series = TimeSeriesGenerator
            .LinearTrend<int>(Period.Hour)
            .WithStart(start)
            .WithCount(4)
            .WithInitialValue(10)
            .WithStep(3)
            .Build();

        series.GetPoints().Should().Equal(
            new TimeSeriesPoint<int>(start, 10),
            new TimeSeriesPoint<int>(start.AddHours(1), 13),
            new TimeSeriesPoint<int>(start.AddHours(2), 16),
            new TimeSeriesPoint<int>(start.AddHours(3), 19));
    }
}
