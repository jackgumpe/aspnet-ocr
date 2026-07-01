using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AspNetOcr.Application.Benchmarking;
using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Exceptions;
using AspNetOcr.Application.Interfaces;
using AspNetOcr.Domain.Ocr;
using AspNetOcr.Infrastructure.Ocr;
using Xunit;

namespace AspNetOcr.Tests;

public sealed class AspOcr002BBenchmarkTests
{
    [Fact]
    public void OcrPipelineStages_DefaultPlans_CoversSixRequiredStages()
    {
        var stages = OcrPipelineStages.DefaultPlans;

        Assert.Equal(
            new[]
            {
                "PDF load",
                "rasterization",
                "preprocessing",
                "OCR",
                "normalization",
                "serialization"
            },
            stages.Select(stage => stage.StageName));
        Assert.Equal("CPU-bound", stages.Single(stage => stage.StageName == OcrPipelineStages.PdfLoad).Classification);
        Assert.Equal("CPU-bound", stages.Single(stage => stage.StageName == OcrPipelineStages.Rasterization).Classification);
        Assert.Equal("GPU-candidate", stages.Single(stage => stage.StageName == OcrPipelineStages.Preprocessing).Classification);
        Assert.Equal("CPU-bound", stages.Single(stage => stage.StageName == OcrPipelineStages.Ocr).Classification);
        Assert.Equal("not worth", stages.Single(stage => stage.StageName == OcrPipelineStages.Normalization).Classification);
        Assert.Equal("not worth", stages.Single(stage => stage.StageName == OcrPipelineStages.Serialization).Classification);
    }

    [Fact]
    public async Task TesseractLocalProvider_ThrowsWorkstationDeferredInsteadOfFallback()
    {
        var provider = new TesseractLocalProvider();

        var exception = await Assert.ThrowsAsync<OcrEngineUnavailableException>(
            () => provider.RecognizeAsync(BuildRequest(), CancellationToken.None));

        Assert.Equal("workstation_deferred", provider.Descriptor.Status);
        Assert.Contains("workstation_deferred", exception.Message);
        Assert.Contains("ASP-OCR-002B", exception.Message);
    }

    [Fact]
    public void OcrAccuracyCalculator_ReportsCerAndWer()
    {
        var exact = OcrAccuracyCalculator.Calculate("Alpha beta gamma", "Alpha beta gamma");
        var changed = OcrAccuracyCalculator.Calculate("Alpha beta gamma", "Alpha gamma");

        Assert.Equal(0m, exact.CharacterErrorRate);
        Assert.Equal(0m, exact.WordErrorRate);
        Assert.True(changed.CharacterErrorRate > 0m);
        Assert.True(changed.WordErrorRate > 0m);
    }

    [Fact]
    public async Task OcrBenchmarkScaffold_RunHarnessAsync_LabelsMockRunAsHarnessOnly()
    {
        var request = BuildRequest();
        var expectedText = string.Join(
            Environment.NewLine,
            new[]
            {
                "Synthetic complaint page one.",
                "Synthetic complaint page two."
            });
        var scaffold = new OcrBenchmarkScaffold(new HarnessMockOcrProvider());

        var run = await scaffold.RunHarnessAsync(request, expectedText, CancellationToken.None);

        Assert.False(run.IsOcrBenchmark);
        Assert.Equal(OcrBenchmarkScaffold.HarnessOnlyLabel, run.RunLabel);
        Assert.Equal(6, run.Stages.Count);
        Assert.All(run.Stages, stage =>
        {
            Assert.False(stage.IsMeasuredBenchmark);
            Assert.Equal(OcrBenchmarkScaffold.HarnessOnlyLabel, stage.Notes);
            Assert.NotNull(stage.CpuPercent);
            Assert.NotNull(stage.MemoryMegabytes);
            Assert.True(stage.DurationMs >= 0);
            Assert.True(stage.CpuPercent >= 0);
            Assert.True(stage.MemoryMegabytes > 0);
        });
        Assert.Equal(0m, run.Accuracy.CharacterErrorRate);
        Assert.Equal(0m, run.Accuracy.WordErrorRate);
        Assert.Null(run.Result.Telemetry.GpuPercent);
        Assert.True(run.PagesPerMinute > 0m);
    }

    [Fact]
    public void OcrCostModel_Estimate_ReturnsLocalCloudComponentsAndDeferredStatus()
    {
        var estimate = OcrCostModel.Estimate(
            pageCount: 250,
            wattHours: 125m,
            electricityUsdPerKwh: 0.16m,
            hardwareAmortizationUsd: 1.50m,
            maintenanceUsd: 0.25m,
            cloudUsd: 3.75m,
            transferAndRetryUsd: 0.20m);

        Assert.Equal("workstation_inputs_required", estimate.Status);
        Assert.Equal(0.02m, estimate.MarginalLocalUsd);
        Assert.Equal(1.77m, estimate.FullyLoadedLocalUsd);
        Assert.Equal(3.95m, estimate.CloudUsd);
        Assert.Equal(-2.18m, estimate.LocalVsCloudDeltaUsd);
    }

    [Fact]
    public void SyntheticCorpusManifest_IsDeterministicAndHasContentHashes()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "testdata", "phase2", "manifests", "ground_truth.jsonl");
        var lines = File.ReadAllLines(manifestPath);

        Assert.Equal(50, lines.Length);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var totalPages = 0;
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            var fixture = document.RootElement;
            var fixtureId = fixture.GetProperty("fixtureId").GetString();
            var sourcePath = fixture.GetProperty("sourcePath").GetString();
            var contentHash = fixture.GetProperty("contentHash").GetString();
            var pageCount = fixture.GetProperty("pageCount").GetInt32();

            Assert.NotNull(fixtureId);
            Assert.StartsWith("asp-ocr-002b-", fixtureId, StringComparison.Ordinal);
            Assert.True(ids.Add(fixtureId));
            Assert.True(fixture.GetProperty("seed").GetInt32() >= 200200);
            Assert.True(pageCount > 0);

            var absoluteSourcePath = Path.Combine(
                root,
                sourcePath!.Replace('/', Path.DirectorySeparatorChar));
            var expectedHash = "sha256:" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absoluteSourcePath))).ToLowerInvariant();
            Assert.Equal(expectedHash, contentHash);
            totalPages += pageCount;
        }

        Assert.True(totalPages >= 250);
    }

    private static OcrProviderRequest BuildRequest()
    {
        var pages = new[]
        {
            BuildPage(1, "page-one-image"),
            BuildPage(2, "page-two-image")
        };

        return new OcrProviderRequest(
            Guid.NewGuid(),
            "asp-ocr-002b-test",
            "synthetic-complaint.pdf",
            "application/pdf",
            pages,
            DateTimeOffset.UtcNow);
    }

    private static OcrPageImage BuildPage(int pageNumber, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new OcrPageImage(
            pageNumber,
            "image/png",
            bytes,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            "mock-page-image");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AspNetOcr.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find AspNetOcr repository root.");
    }

    private sealed class HarnessMockOcrProvider : IOcrProvider
    {
        public OcrProviderDescriptor Descriptor { get; } = new(
            "mock-ocr",
            "test",
            "available",
            "not_applicable",
            "Harness-only mock provider for ASP-OCR-002B scaffold verification.");

        public Task<NormalizedOcrResult> RecognizeAsync(OcrProviderRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pages = request.Pages
                .Select(page => new NormalizedOcrPage(
                    page.PageNumber,
                    page.PageNumber == 1
                        ? "Synthetic complaint page one."
                        : "Synthetic complaint page two.",
                    0.99m,
                    page.ContentHash,
                    page.RenderMode))
                .ToArray();
            var telemetry = new OcrProviderTelemetry(
                Descriptor.ProviderId,
                "CPU-bound",
                DurationMs: 8,
                CostPerPageUsdEstimate: 0m,
                WattHoursEstimate: null,
                CpuPercent: null,
                RamMegabytes: null,
                GpuPercent: null,
                Notes: "Harness-only mock provider; no real OCR benchmark.");

            return Task.FromResult(new NormalizedOcrResult(
                request.DocumentId,
                request.CorrelationId,
                request.SourceFileName,
                Descriptor.ProviderId,
                pages.Length,
                0.99m,
                0m,
                0m,
                pages,
                [],
                telemetry,
                DateTimeOffset.UtcNow));
        }
    }
}
