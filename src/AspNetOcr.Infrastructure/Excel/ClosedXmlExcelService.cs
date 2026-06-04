using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Domain.Products;
using ClosedXML.Excel;

namespace AspNetOcr.Infrastructure.Excel;

public sealed class ClosedXmlExcelService : IExcelService
{
    public Task<string> ExportAsync(
        IReadOnlyList<ProductSheet> products,
        DocumentManifest manifest,
        string exportPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifest.ManifestPath))
        {
            throw new InvalidOperationException("Cannot export workbook before manifest is persisted.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath) ?? ".");

        using var workbook = new XLWorkbook();
        var productSheet = workbook.Worksheets.Add("Products");
        productSheet.Cell(1, 1).Value = "SKU";
        productSheet.Cell(1, 2).Value = "Name";
        productSheet.Cell(1, 3).Value = "Category";
        productSheet.Cell(1, 4).Value = "Quantity";
        productSheet.Cell(1, 5).Value = "UnitPrice";
        productSheet.Cell(1, 6).Value = "Confidence";

        for (var index = 0; index < products.Count; index++)
        {
            var row = index + 2;
            var product = products[index];
            productSheet.Cell(row, 1).Value = product.Sku;
            productSheet.Cell(row, 2).Value = product.Name;
            productSheet.Cell(row, 3).Value = product.Category;
            productSheet.Cell(row, 4).Value = product.Quantity;
            productSheet.Cell(row, 5).Value = product.UnitPrice;
            productSheet.Cell(row, 6).Value = product.Confidence.Value;
        }

        productSheet.Columns().AdjustToContents();

        var manifestSheet = workbook.Worksheets.Add("Manifest");
        manifestSheet.Cell(1, 1).Value = "DocumentId";
        manifestSheet.Cell(1, 2).Value = manifest.DocumentId.ToString();
        manifestSheet.Cell(2, 1).Value = "CorrelationId";
        manifestSheet.Cell(2, 2).Value = manifest.CorrelationId;
        manifestSheet.Cell(3, 1).Value = "ManifestPath";
        manifestSheet.Cell(3, 2).Value = manifest.ManifestPath;
        manifestSheet.Cell(4, 1).Value = "RawOcrPath";
        manifestSheet.Cell(4, 2).Value = manifest.RawOcrArtifactPath;
        manifestSheet.Cell(5, 1).Value = "ValidationPath";
        manifestSheet.Cell(5, 2).Value = manifest.ValidationArtifactPath;
        manifestSheet.Cell(6, 1).Value = "ProductRowCount";
        manifestSheet.Cell(6, 2).Value = manifest.ProductRowCount;
        manifestSheet.Columns().AdjustToContents();

        workbook.SaveAs(exportPath);
        return Task.FromResult(exportPath);
    }
}
