using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;

namespace AspNetOcr.Infrastructure.Ocr;

public sealed class TesseractLocalProvider : IOcrProvider
{
    public OcrProviderDescriptor Descriptor { get; } = new(
        "tesseract-local",
        "local",
        "workstation_deferred",
        "cpu_baseline_deferred",
        "Local Tesseract benchmarking is deferred to ASP-OCR-002B on the workstation.");

    public Task<NormalizedOcrResult> RecognizeAsync(OcrProviderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new OcrEngineUnavailableException(
            "TesseractLocalProvider is a workstation_deferred stub for ASP-OCR-002B; install Tesseract 5 on the workstation before enabling local OCR.");
    }
}
