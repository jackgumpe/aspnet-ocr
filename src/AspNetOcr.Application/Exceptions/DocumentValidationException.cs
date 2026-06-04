namespace AspNetOcr.Application.Exceptions;

public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(IReadOnlyList<string> errors)
        : base("Document validation failed.")
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
