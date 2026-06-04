using System.Globalization;
using AspNetOcr.Domain.Ocr;
using AspNetOcr.Domain.Products;

namespace AspNetOcr.Application.Parsing;

public sealed class ProductSheetParser
{
    public IReadOnlyList<ProductSheet> Parse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [];
        }

        var products = new List<ProductSheet>();
        var sections = rawText.Split(["---"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var section in sections)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in section.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                fields[key] = value;
            }

            if (fields.Count == 0)
            {
                continue;
            }

            products.Add(new ProductSheet(
                Require(fields, "SKU"),
                Require(fields, "Name"),
                Require(fields, "Category"),
                int.Parse(Require(fields, "Quantity"), CultureInfo.InvariantCulture),
                decimal.Parse(Require(fields, "UnitPrice"), CultureInfo.InvariantCulture),
                ParseConfidence(fields.TryGetValue("Confidence", out var confidence) ? confidence : "0.90")));
        }

        return products;
    }

    private static string Require(IReadOnlyDictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Product sheet is missing required field '{key}'.");
        }

        return value;
    }

    private static ConfidenceScore ParseConfidence(string value)
    {
        var parsed = decimal.Parse(value, CultureInfo.InvariantCulture);
        return parsed > 1m ? ConfidenceScore.FromPercent(parsed) : ConfidenceScore.FromRatio(parsed);
    }
}
