using System.Collections.Concurrent;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Domain.Ocr;

namespace AspNetOcr.Api.Phase2;

public sealed class Phase2OcrJobStore
{
    private readonly ConcurrentDictionary<Guid, Phase2OcrJob> _jobs = new();

    public Phase2OcrJob Create(string sourceFileName, DateTimeOffset now)
    {
        var job = new Phase2OcrJob(
            Guid.NewGuid(),
            sourceFileName,
            "queued",
            Confidence: null,
            PagesProcessed: 0,
            TotalPages: 0,
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            CompletedAtUtc: null,
            Error: null,
            Result: null);

        _jobs[job.Id] = job;
        return job;
    }

    public IReadOnlyList<Phase2OcrJob> List()
    {
        return _jobs.Values
            .OrderByDescending(job => job.CreatedAtUtc)
            .ToArray();
    }

    public Phase2OcrJob? Find(Guid id)
    {
        return _jobs.TryGetValue(id, out var job) ? job : null;
    }

    public void MarkProcessing(Guid id, int pagesProcessed, int totalPages, DateTimeOffset now)
    {
        _jobs.AddOrUpdate(
            id,
            static _ => throw new KeyNotFoundException("OCR job was not created before processing started."),
            (_, existing) => existing with
            {
                Status = "processing",
                PagesProcessed = pagesProcessed,
                TotalPages = totalPages,
                UpdatedAtUtc = now
            });
    }

    public void MarkComplete(Guid id, NormalizedOcrResult normalized, string rawJson, DateTimeOffset now)
    {
        var result = new Phase2OcrJobResult(
            string.Join(Environment.NewLine + Environment.NewLine, normalized.Pages.Select(page => page.Text)),
            normalized.Fields,
            normalized.MeanConfidence,
            normalized.CharacterErrorRate,
            normalized.WordErrorRate,
            rawJson);

        _jobs.AddOrUpdate(
            id,
            static _ => throw new KeyNotFoundException("OCR job was not created before completion."),
            (_, existing) => existing with
            {
                Status = "complete",
                Confidence = normalized.MeanConfidence,
                PagesProcessed = normalized.PageCount,
                TotalPages = normalized.PageCount,
                UpdatedAtUtc = now,
                CompletedAtUtc = now,
                Result = result
            });
    }

    public void MarkFailed(Guid id, string error, DateTimeOffset now)
    {
        _jobs.AddOrUpdate(
            id,
            static _ => throw new KeyNotFoundException("OCR job was not created before failure."),
            (_, existing) => existing with
            {
                Status = "failed",
                UpdatedAtUtc = now,
                CompletedAtUtc = now,
                Error = error
            });
    }
}

public sealed record Phase2OcrJob(
    Guid Id,
    string SourceFileName,
    string Status,
    decimal? Confidence,
    int PagesProcessed,
    int TotalPages,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Error,
    Phase2OcrJobResult? Result);

public sealed record Phase2OcrJobResult(
    string Text,
    IReadOnlyList<OcrFieldResult> Fields,
    decimal Confidence,
    decimal? CharacterErrorRate,
    decimal? WordErrorRate,
    string RawJson);
