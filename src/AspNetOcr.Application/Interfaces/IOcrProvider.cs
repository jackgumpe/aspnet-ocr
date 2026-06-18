using AspNetOcr.Application.Contracts;

namespace AspNetOcr.Application.Interfaces;

public interface IOcrProvider
{
    OcrProviderDescriptor Descriptor { get; }

    Task<NormalizedOcrResult> RecognizeAsync(OcrProviderRequest request, CancellationToken cancellationToken);
}
