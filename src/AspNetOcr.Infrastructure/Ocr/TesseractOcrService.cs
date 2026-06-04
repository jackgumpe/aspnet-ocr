using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Domain.Ocr;
using Tesseract;
using UglyToad.PdfPig;

namespace AspNetOcr.Infrastructure.Ocr;

public sealed class TesseractOcrService : IOcrService
{
    private readonly string _tessdataPath;

    public TesseractOcrService(string tessdataPath)
    {
        _tessdataPath = tessdataPath;
    }

    public Task<OcrHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trainedDataPath = Path.Combine(_tessdataPath, "eng.traineddata");
        var available = File.Exists(trainedDataPath);
        var detail = available
            ? "eng.traineddata found."
            : $"Missing Tesseract trained data at {trainedDataPath}.";
        return Task.FromResult(new OcrHealth(available, "tesseract", detail));
    }

    public Task<OcrResult> RecognizeAsync(OcrInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var extension = Path.GetExtension(input.FileName);
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ExtractTextPdf(input));
        }

        var trainedDataPath = Path.Combine(_tessdataPath, "eng.traineddata");
        if (!File.Exists(trainedDataPath))
        {
            throw new OcrEngineUnavailableException($"Missing Tesseract trained data at {trainedDataPath}.");
        }

        using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
        using var pix = Pix.LoadFromMemory(input.Content);
        using var page = engine.Process(pix);
        var rawText = page.GetText();
        var confidence = ConfidenceScore.FromRatio((decimal)page.GetMeanConfidence());
        return Task.FromResult(new OcrResult(rawText, confidence, "tesseract", input.FileName));
    }

    private static OcrResult ExtractTextPdf(OcrInput input)
    {
        using var stream = new MemoryStream(input.Content);
        using var document = PdfDocument.Open(stream);
        var rawText = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new OcrEngineUnavailableException(
                "PDF did not contain extractable text. Scanned PDF rasterization is not implemented in Phase 1.");
        }

        return new OcrResult(rawText, ConfidenceScore.FromRatio(0.90m), "pdfpig-text", input.FileName);
    }
}
