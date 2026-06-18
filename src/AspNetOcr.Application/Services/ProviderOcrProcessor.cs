using System.Security.Cryptography;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Application.Options;
using FluentValidation;

namespace AspNetOcr.Application.Services;

public sealed class ProviderOcrProcessor
{
    private readonly IValidator<DocumentUploadRequest> _uploadValidator;
    private readonly IOcrProvider _ocrProvider;
    private readonly IPdfPageImagePipeline _pageImagePipeline;
    private readonly IArtifactStore _artifactStore;
    private readonly PipelineOptions _options;
    private readonly TimeProvider _clock;

    public ProviderOcrProcessor(
        IValidator<DocumentUploadRequest> uploadValidator,
        IOcrProvider ocrProvider,
        IPdfPageImagePipeline pageImagePipeline,
        IArtifactStore artifactStore,
        PipelineOptions options,
        TimeProvider clock)
    {
        _uploadValidator = uploadValidator;
        _ocrProvider = ocrProvider;
        _pageImagePipeline = pageImagePipeline;
        _artifactStore = artifactStore;
        _options = options;
        _clock = clock;
    }

    public async Task<ProviderOcrProcessResult> ProcessAsync(DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        var validation = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DocumentRejectedException(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        var bytes = await ReadAllBytesAsync(request.Content, cancellationToken);
        if (bytes.Length == 0 || bytes.LongLength > _options.MaxUploadBytes)
        {
            throw new DocumentRejectedException("Uploaded file is empty or exceeds the Phase 2 OCR size limit.");
        }

        var documentId = Guid.NewGuid();
        var originalPath = await _artifactStore.SaveBytesAsync(documentId, $"original/{request.FileName}", bytes, cancellationToken);
        var pages = await _pageImagePipeline.RenderAsync(request.FileName, request.ContentType, bytes, cancellationToken);
        if (pages.Count == 0)
        {
            throw new InvalidOperationException("PDF page image pipeline produced no pages.");
        }

        var providerRequest = new OcrProviderRequest(
            documentId,
            request.CorrelationId,
            request.FileName,
            request.ContentType,
            pages,
            _clock.GetUtcNow());
        var normalized = await _ocrProvider.RecognizeAsync(providerRequest, cancellationToken);
        var normalizedPath = await _artifactStore.SaveJsonAsync(documentId, "ocr/normalized.json", normalized, cancellationToken);

        return new ProviderOcrProcessResult(
            documentId,
            request.CorrelationId,
            request.FileName,
            originalPath,
            normalizedPath,
            normalized.PageCount,
            normalized.MeanConfidence,
            normalized.Fields);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
