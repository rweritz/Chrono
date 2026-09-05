namespace Chrono.TimeSeries.Benchmark;

public sealed record DenseFixedStepBenchmarkData(
    DateTimeOffset[] OrderedTimestamps,
    DateTimeOffset[] ResampleSourceTimestamps,
    int[] RandomInsertIndices,
    DateTimeOffset[] RandomLookupTimestamps,
    double[] Values,
    double[] ResampleSourceValues);

public static class DenseFixedStepBenchmarkDataFactory
{
    private const int InsertOrderSeed = 41041;
    private const int LookupOrderSeed = 41042;
    private const int ValueSeed = 41043;
    private const int ResampleValueSeed = 41044;

    public static DenseFixedStepBenchmarkData Create(int pointCount)
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var orderedTimestamps = new DateTimeOffset[pointCount];
        var resampleTimestamps = new DateTimeOffset[pointCount];
        var values = new double[pointCount];
        var valueRandom = new Random(ValueSeed);
        var resampleValueRandom = new Random(ResampleValueSeed);

        for (var i = 0; i < pointCount; i++)
        {
            orderedTimestamps[i] = start.AddMinutes(i * 5);
            values[i] = valueRandom.NextDouble() * 1000d;
            resampleTimestamps[i] = start.AddHours(i);
        }

        var resampleValues = new double[pointCount];
        for (var i = 0; i < resampleValues.Length; i++)
            resampleValues[i] = resampleValueRandom.NextDouble() * 1000d;

        var randomInsertIndices = ShuffleIndices(pointCount, InsertOrderSeed);
        var randomLookupTimestamps = Shuffle(orderedTimestamps, LookupOrderSeed);

        return new DenseFixedStepBenchmarkData(
            orderedTimestamps,
            resampleTimestamps,
            randomInsertIndices,
            randomLookupTimestamps,
            values,
            resampleValues);
    }

    private static int[] ShuffleIndices(int count, int seed)
    {
        var shuffled = new int[count];
        for (var i = 0; i < shuffled.Length; i++)
            shuffled[i] = i;

        var random = new Random(seed);

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    private static DateTimeOffset[] Shuffle(DateTimeOffset[] source, int seed)
    {
        var shuffled = (DateTimeOffset[])source.Clone();
        var random = new Random(seed);

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}
