using AspNetOcr.Application.Contracts;
using AspNetOcr.Domain.Products;

namespace AspNetOcr.Application.Interfaces;

public interface IExcelService
{
    Task<string> ExportAsync(
        IReadOnlyList<ProductSheet> products,
        DocumentManifest manifest,
        string exportPath,
        CancellationToken cancellationToken);
}
