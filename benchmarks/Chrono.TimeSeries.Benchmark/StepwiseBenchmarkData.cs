namespace Chrono.TimeSeries.Benchmark;

public sealed record StepwiseBenchmarkData(
    DateTimeOffset Start,
    DateTimeOffset End,
    DateTimeOffset ResampleStart,
    DateTimeOffset ResampleEnd,
    DateTimeOffset[] OrderedTimestamps,
    DateTimeOffset[] RandomLookupTimestamps,
    double InitialValueA,
    double InitialValueB,
    int[] ChangePointStarts,
    double[] ChangePointValuesA,
    double[] ChangePointValuesB,
    double[] DenseValuesA,
    double[] DenseValuesB,
    int[] RandomSingleSlotIndices,
    double[] RandomSingleSlotValues,
    int[] ShortSegmentStarts,
    double[] ShortSegmentValues,
    int[] LongSegmentStarts,
    double[] LongSegmentValues,
    double[] LeftExpansionValues,
    double[] RightExpansionValues,
    int[] ResampleChangePointStarts,
    double[] ResampleChangePointValues);

public static class StepwiseBenchmarkDataFactory
{
    private const int ChangePointSeed = 45041;
    private const int LookupOrderSeed = 45042;
    private const int SingleSlotOrderSeed = 45043;
    private const int ShortSegmentSeed = 45044;
    private const int LongSegmentSeed = 45045;
    private const int ValueSeed = 45046;
    private const int ResampleChangePointSeed = 45047;

    public static StepwiseBenchmarkData Create(int logicalSlotCount, double changePointDensity)
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes((logicalSlotCount - 1) * 5L);
        var orderedTimestamps = CreateFiveMinuteTimestamps(start, logicalSlotCount);
        var changePointStarts = CreateChangePointStarts(logicalSlotCount, changePointDensity, ChangePointSeed);
        var valueRandom = new Random(ValueSeed);
        var initialValueA = NextValue(valueRandom);
        var initialValueB = initialValueA + 11d;
        var changePointValuesA = CreateValues(changePointStarts.Length, valueRandom, 0d);
        var changePointValuesB = CreateValues(changePointStarts.Length, valueRandom, 11d);
        var denseValuesA = MaterializeDenseValues(logicalSlotCount, initialValueA, changePointStarts, changePointValuesA);
        var denseValuesB = MaterializeDenseValues(logicalSlotCount, initialValueB, changePointStarts, changePointValuesB);

        // Resample uses the same logical slot scale as the primary benchmark data so
        // LogicalSlotCount=100_000 exercises 100k source slots instead of a capped subset.
        var resampleSlotCount = logicalSlotCount;
        var resampleStart = start;
        var resampleEnd = resampleStart.AddHours(resampleSlotCount - 1L);
        var resampleChangePointStarts = CreateChangePointStarts(
            resampleSlotCount,
            changePointDensity,
            ResampleChangePointSeed);
        var resampleChangePointValues = CreateValues(resampleChangePointStarts.Length, valueRandom, 23d);

        return new StepwiseBenchmarkData(
            start,
            end,
            resampleStart,
            resampleEnd,
            orderedTimestamps,
            Shuffle(orderedTimestamps, LookupOrderSeed),
            initialValueA,
            initialValueB,
            changePointStarts,
            changePointValuesA,
            changePointValuesB,
            denseValuesA,
            denseValuesB,
            ShuffleIndices(logicalSlotCount, SingleSlotOrderSeed, Math.Min(512, logicalSlotCount)),
            CreateValues(Math.Min(512, logicalSlotCount), valueRandom, 31d),
            CreateSegmentStarts(logicalSlotCount, segmentLength: 4, count: 256, ShortSegmentSeed),
            CreateValues(Math.Min(256, Math.Max(1, logicalSlotCount - 4)), valueRandom, 47d),
            CreateSegmentStarts(logicalSlotCount, LongSegmentLength(logicalSlotCount), count: 64, LongSegmentSeed),
            CreateValues(Math.Min(64, Math.Max(1, logicalSlotCount - LongSegmentLength(logicalSlotCount))), valueRandom, 59d),
            CreateValues(Math.Min(256, logicalSlotCount - 1), valueRandom, 71d),
            CreateValues(Math.Min(256, logicalSlotCount - 1), valueRandom, 83d),
            resampleChangePointStarts,
            resampleChangePointValues);
    }

    private static DateTimeOffset[] CreateFiveMinuteTimestamps(DateTimeOffset start, int count)
    {
        var timestamps = new DateTimeOffset[count];
        for (var i = 0; i < timestamps.Length; i++)
            timestamps[i] = start.AddMinutes(i * 5L);

        return timestamps;
    }

    private static int[] CreateChangePointStarts(int logicalSlotCount, double changePointDensity, int seed)
    {
        if (logicalSlotCount <= 2)
            return Array.Empty<int>();

        var targetChangePointCount = Math.Clamp(
            (int)Math.Round(logicalSlotCount * changePointDensity),
            2,
            logicalSlotCount);
        var interiorChangePointCount = Math.Min(targetChangePointCount - 2, logicalSlotCount - 2);
        if (interiorChangePointCount <= 0)
            return Array.Empty<int>();

        var candidates = ShuffleIndices(logicalSlotCount - 2, seed, logicalSlotCount - 2);
        var starts = new int[interiorChangePointCount];
        for (var i = 0; i < starts.Length; i++)
            starts[i] = candidates[i] + 1;

        Array.Sort(starts);
        return starts;
    }

    private static int[] CreateSegmentStarts(int logicalSlotCount, int segmentLength, int count, int seed)
    {
        var maxStartExclusive = Math.Max(1, logicalSlotCount - segmentLength);
        var sampleCount = Math.Min(count, maxStartExclusive);
        return ShuffleIndices(maxStartExclusive, seed, sampleCount);
    }

    private static int LongSegmentLength(int logicalSlotCount) =>
        Math.Clamp(logicalSlotCount / 20, 64, Math.Max(64, logicalSlotCount / 2));

    private static double[] CreateValues(int count, Random random, double offset)
    {
        var values = new double[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = NextValue(random) + offset;

        return values;
    }

    private static double[] MaterializeDenseValues(
        int logicalSlotCount,
        double initialValue,
        IReadOnlyList<int> changePointStarts,
        IReadOnlyList<double> changePointValues)
    {
        var values = new double[logicalSlotCount];
        var current = initialValue;
        var changePointIndex = 0;

        for (var slot = 0; slot < values.Length; slot++)
        {
            if (changePointIndex < changePointStarts.Count && slot == changePointStarts[changePointIndex])
            {
                current = changePointValues[changePointIndex];
                changePointIndex++;
            }

            values[slot] = current;
        }

        return values;
    }

    private static double NextValue(Random random) =>
        Math.Round(random.NextDouble() * 1000d, 6);

    private static int[] ShuffleIndices(int count, int seed, int take)
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

        if (take == shuffled.Length)
            return shuffled;

        var result = new int[take];
        Array.Copy(shuffled, result, result.Length);
        return result;
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
