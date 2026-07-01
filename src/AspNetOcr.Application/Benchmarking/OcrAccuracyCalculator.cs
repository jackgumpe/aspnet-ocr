namespace AspNetOcr.Application.Benchmarking;

public sealed record OcrAccuracyMetrics(decimal CharacterErrorRate, decimal WordErrorRate);

public static class OcrAccuracyCalculator
{
    public static OcrAccuracyMetrics Calculate(string expectedText, string observedText)
    {
        var expected = NormalizeWhitespace(expectedText);
        var observed = NormalizeWhitespace(observedText);

        var characterErrorRate = ErrorRate(
            expected.ToCharArray(),
            observed.ToCharArray());
        var wordErrorRate = ErrorRate(
            SplitWords(expected),
            SplitWords(observed));

        return new OcrAccuracyMetrics(characterErrorRate, wordErrorRate);
    }

    private static decimal ErrorRate<T>(IReadOnlyList<T> expected, IReadOnlyList<T> observed)
        where T : IEquatable<T>
    {
        if (expected.Count == 0)
        {
            return observed.Count == 0 ? 0m : 1m;
        }

        var distance = LevenshteinDistance(expected, observed);
        return Math.Round((decimal)distance / expected.Count, 6, MidpointRounding.AwayFromZero);
    }

    private static int LevenshteinDistance<T>(IReadOnlyList<T> expected, IReadOnlyList<T> observed)
        where T : IEquatable<T>
    {
        var previous = new int[observed.Count + 1];
        var current = new int[observed.Count + 1];

        for (var index = 0; index <= observed.Count; index++)
        {
            previous[index] = index;
        }

        for (var expectedIndex = 1; expectedIndex <= expected.Count; expectedIndex++)
        {
            current[0] = expectedIndex;

            for (var observedIndex = 1; observedIndex <= observed.Count; observedIndex++)
            {
                var substitutionCost = expected[expectedIndex - 1].Equals(observed[observedIndex - 1]) ? 0 : 1;
                current[observedIndex] = Math.Min(
                    Math.Min(
                        current[observedIndex - 1] + 1,
                        previous[observedIndex] + 1),
                    previous[observedIndex - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[observed.Count];
    }

    private static string NormalizeWhitespace(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static IReadOnlyList<string> SplitWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
