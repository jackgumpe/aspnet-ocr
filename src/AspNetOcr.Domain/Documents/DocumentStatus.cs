namespace AspNetOcr.Domain.Documents;

public enum DocumentStatus
{
    Received = 0,
    Ingested = 1,
    Preprocessed = 2,
    Recognizing = 3,
    Validating = 4,
    Validated = 5,
    Exporting = 6,
    Exported = 7,
    DeadLettered = 8
}
