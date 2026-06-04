namespace AspNetOcr.Application.Exceptions;

public sealed class DocumentRejectedException : Exception
{
    public DocumentRejectedException(string message)
        : base(message)
    {
    }
}
