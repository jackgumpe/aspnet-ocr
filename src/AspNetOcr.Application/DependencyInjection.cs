using AspNetOcr.Application.Contracts;
using AspNetOcr.Application.Options;
using AspNetOcr.Application.Parsing;
using AspNetOcr.Application.Services;
using AspNetOcr.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetOcr.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAspNetOcrApplication(this IServiceCollection services)
    {
        services.AddSingleton(new PipelineOptions());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IValidator<DocumentUploadRequest>, DocumentUploadValidator>();
        services.AddSingleton<ProductSheetParser>();
        services.AddScoped<PipelineOrchestrator>();
        services.AddScoped<ProviderOcrProcessor>();
        return services;
    }
}
