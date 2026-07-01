using System.Security.Cryptography;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Interfaces;

namespace AspNetOcr.Infrastructure.Ocr;

public sealed class MockPdfPageImagePipeline : IPdfPageImagePipeline
{
    public Task<IReadOnlyList<OcrPageImage>> RenderAsync(
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        IReadOnlyList<OcrPageImage> pages =
        [
            new(1, contentType, content, hash, contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                ? "mock-pdf-rasterization"
                : "mock-image-pass-through")
        ];

        return Task.FromResult(pages);
    }
}
