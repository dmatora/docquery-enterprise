namespace Docquery.Server.Services;

public sealed class DocumentQaException : Exception
{
    public DocumentQaException(
        int statusCode,
        string title,
        string detail,
        Exception? innerException = null)
        : base(detail, innerException)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }

    public int StatusCode { get; }

    public string Title { get; }

    public string Detail { get; }
}