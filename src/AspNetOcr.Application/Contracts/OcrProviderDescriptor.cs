namespace AspNetOcr.Application.Contracts;

public sealed record OcrProviderDescriptor(
    string ProviderId,
    string Runtime,
    string Status,
    string AccelerationStatus,
    string Notes);
