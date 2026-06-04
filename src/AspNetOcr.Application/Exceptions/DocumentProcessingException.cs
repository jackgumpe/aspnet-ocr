using AspNetOcr.Application.Contracts;

namespace AspNetOcr.Application.Exceptions;

public sealed class DocumentProcessingException : Exception
{
    public DocumentProcessingException(string message, DocumentProcessResult result, Exception innerException)
        : base(message, innerException)
    {
        Result = result;
    }

    public DocumentProcessResult Result { get; }
}
