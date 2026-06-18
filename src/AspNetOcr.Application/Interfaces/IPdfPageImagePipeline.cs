using AspNetOcr.Application.Contracts;

namespace AspNetOcr.Application.Interfaces;

public interface IPdfPageImagePipeline
{
    Task<IReadOnlyList<OcrPageImage>> RenderAsync(
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken);
}
