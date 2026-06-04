using AspNetOcr.Application.Contracts;
using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Application.Interfaces;

public interface IOcrService
{
    Task<OcrResult> RecognizeAsync(OcrInput input, CancellationToken cancellationToken);

    Task<OcrHealth> CheckHealthAsync(CancellationToken cancellationToken);
}
