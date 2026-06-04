namespace AspNetOcr.Application.Exceptions;

public sealed class OcrEngineUnavailableException : Exception
{
    public OcrEngineUnavailableException(string message)
        : base(message)
    {
    }
}
