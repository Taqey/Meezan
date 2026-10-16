using System.Net;

namespace Meezan.Application.Common.Exceptions;

public class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, message, new { status = 404, title = "Not Found", message })
    {
    }

    public NotFoundException(string message, object customPayload)
        : base(HttpStatusCode.NotFound, message, customPayload)
    {
    }
}
