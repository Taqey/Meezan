using System.Net;

namespace Meezan.Application.Common.Exceptions;

public class ConflictException : ApplicationExceptionBase
{
    public ConflictException(string message)
        : base(HttpStatusCode.Conflict, message, new { status = 409, title = "Conflict", message })
    {
    }

    public ConflictException(string message, object customPayload)
        : base(HttpStatusCode.Conflict, message, customPayload)
    {
    }
}
