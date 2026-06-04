using AspNetOcr.Application.Interfaces;
using AspNetOcr.Infrastructure.Artifacts;
using AspNetOcr.Infrastructure.Excel;
using AspNetOcr.Infrastructure.Ocr;
using AspNetOcr.Infrastructure.Persistence;
using AspNetOcr.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetOcr.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAspNetOcrInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var artifactRoot = configuration["AspNetOcr:ArtifactRoot"] ?? Path.Combine(AppContext.BaseDirectory, "artifacts");
        var databasePath = configuration["AspNetOcr:DatabasePath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "aspnetocr.sqlite");
        var tessdataPath = configuration["AspNetOcr:TessdataPath"] ?? Path.Combine(AppContext.BaseDirectory, "tessdata");

        services.AddSingleton<IArtifactStore>(_ => new FileSystemArtifactStore(artifactRoot));
        services.AddSingleton<IDocumentRepository>(_ => new SqliteDocumentRepository(databasePath));
        services.AddSingleton<IExcelService, ClosedXmlExcelService>();
        services.AddSingleton<IOcrService>(_ => new TesseractOcrService(tessdataPath));
        services.AddSingleton<ITelemetrySink, SerilogTelemetrySink>();

        return services;
    }
}
