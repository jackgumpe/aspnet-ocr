using System.Text;
using AspNetOcr.Api.Phase2;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Infrastructure.Ocr;
using Xunit;

namespace AspNetOcr.Tests;

public sealed class AspOcr003Phase2ApiTests
{
    [Fact]
    public async Task MockOcrProvider_ReturnsNormalizedHarnessResult()
    {
        var provider = new MockOcrProvider();
        var content = Encoding.UTF8.GetBytes("mock pdf bytes");
        var request = new OcrProviderRequest(
            Guid.NewGuid(),
            "asp-ocr-003-test",
            "sample.pdf",
            "application/pdf",
            [new OcrPageImage(1, "application/pdf", content, "content-hash", "mock-pdf-rasterization")],
            DateTimeOffset.UtcNow);

        var result = await provider.RecognizeAsync(request, CancellationToken.None);

        Assert.Equal("mock-ocr", result.ProviderId);
        Assert.Equal("sample.pdf", result.SourceFileName);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(0m, result.CharacterErrorRate);
        Assert.Equal(0m, result.WordErrorRate);
        Assert.Contains(result.Fields, field => field.Name == "sku" && field.Value == "A-100");
    }

    [Fact]
    public async Task MockPdfPageImagePipeline_RendersSingleHarnessPage()
    {
        var pipeline = new MockPdfPageImagePipeline();
        var pages = await pipeline.RenderAsync(
            "sample.pdf",
            "application/pdf",
            Encoding.UTF8.GetBytes("mock pdf bytes"),
            CancellationToken.None);

        var page = Assert.Single(pages);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal("mock-pdf-rasterization", page.RenderMode);
        Assert.False(string.IsNullOrWhiteSpace(page.ContentHash));
    }

    [Fact]
    public void Phase2OcrJobStore_TracksProcessingAndCompletion()
    {
        var store = new Phase2OcrJobStore();
        var now = DateTimeOffset.UtcNow;
        var job = store.Create("sample.pdf", now);
        var normalized = new NormalizedOcrResult(
            job.Id,
            "asp-ocr-003-test",
            "sample.pdf",
            "mock-ocr",
            1,
            0.96m,
            0m,
            0m,
            [new NormalizedOcrPage(1, "SKU: A-100", 0.96m, "hash", "mock")],
            [],
            new OcrProviderTelemetry("mock-ocr", "harness", 25, 0m, null, 10, 128, 0, "test"),
            now);

        store.MarkProcessing(job.Id, 0, 1, now.AddSeconds(1));
        store.MarkComplete(job.Id, normalized, "{\"providerId\":\"mock-ocr\"}", now.AddSeconds(2));

        var completed = store.Find(job.Id);
        Assert.NotNull(completed);
        Assert.Equal("complete", completed.Status);
        Assert.Equal(1, completed.PagesProcessed);
        Assert.Equal(0.96m, completed.Confidence);
        Assert.Contains("SKU: A-100", completed.Result?.Text);
        Assert.Contains("mock-ocr", completed.Result?.RawJson);
    }
}
