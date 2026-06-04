using System.Text.Json;
using AspNetOcr.Application.Parsing;
using Xunit;

namespace AspNetOcr.Tests;

public sealed class GoldDatasetTests
{
    [Fact]
    public void ProductSheetParser_MatchesGoldDataset()
    {
        var root = FindRepositoryRoot();
        var groundTruthPath = Path.Combine(root, "gold", "ground_truth.json");
        var groundTruth = JsonSerializer.Deserialize<List<GoldCase>>(
            File.ReadAllText(groundTruthPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(groundTruth);
        Assert.Equal(10, groundTruth.Count);

        var parser = new ProductSheetParser();
        foreach (var testCase in groundTruth)
        {
            var sourcePath = Path.Combine(root, "gold", "product-sheets", testCase.SourceFile);
            var products = parser.Parse(File.ReadAllText(sourcePath));

            Assert.Equal(testCase.Products.Count, products.Count);
            for (var index = 0; index < products.Count; index++)
            {
                Assert.Equal(testCase.Products[index].Sku, products[index].Sku);
                Assert.Equal(testCase.Products[index].Name, products[index].Name);
                Assert.Equal(testCase.Products[index].Category, products[index].Category);
                Assert.Equal(testCase.Products[index].Quantity, products[index].Quantity);
                Assert.Equal(testCase.Products[index].UnitPrice, products[index].UnitPrice);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AspNetOcr.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find AspNetOcr repository root.");
    }

    private sealed record GoldCase(string SourceFile, List<ExpectedProduct> Products);

    private sealed record ExpectedProduct(
        string Sku,
        string Name,
        string Category,
        int Quantity,
        decimal UnitPrice);
}
