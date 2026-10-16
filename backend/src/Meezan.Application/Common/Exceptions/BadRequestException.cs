using System.Net;

namespace Meezan.Application.Common.Exceptions;

public class BadRequestException : ApplicationExceptionBase
{
    public BadRequestException(string message)
        : base(HttpStatusCode.BadRequest, message, new { message })
    {
    }

    public BadRequestException(string message, object customPayload)
        : base(HttpStatusCode.BadRequest, message, customPayload)
    {
    }
}
