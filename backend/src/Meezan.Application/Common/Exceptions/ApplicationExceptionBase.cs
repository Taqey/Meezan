using System.Net;

namespace Meezan.Application.Common.Exceptions;

/// <summary>
/// Base class for application-level domain/business exceptions that specify an HTTP status code
/// and an explicit response payload preserving endpoint response contracts.
/// </summary>
public abstract class ApplicationExceptionBase : Exception
{
    public HttpStatusCode StatusCode { get; }
    public object? Payload { get; }

    protected ApplicationExceptionBase(HttpStatusCode statusCode, string message, object? payload = null)
        : base(message)
    {
        StatusCode = statusCode;
        Payload = payload;
    }
}
