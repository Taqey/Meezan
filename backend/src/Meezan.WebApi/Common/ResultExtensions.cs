using System.Net;
using Meezan.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        return MapError(result.Error);
    }

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return MapError(result.Error);
    }

    public static IActionResult ToNoContentResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return MapError(result.Error);
    }

    public static IActionResult ToNoContentResult<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return MapError(result.Error);
    }

    public static IActionResult ToCreatedResult<TValue>(
        this Result<TValue> result,
        string actionName,
        object? routeValues)
    {
        if (result.IsSuccess)
        {
            return new CreatedAtActionResult(actionName, null, routeValues, result.Value);
        }

        return MapError(result.Error);
    }

    public static IActionResult ToCreatedResult<TValue>(
        this Result<TValue> result,
        string uri)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(uri, result.Value);
        }

        return MapError(result.Error);
    }

    private static IActionResult MapError(Error error)
    {
        if (error.ExtraPayload != null)
        {
            return error.Type switch
            {
                ErrorType.Validation => new BadRequestObjectResult(error.ExtraPayload),
                ErrorType.NotFound => new NotFoundObjectResult(error.ExtraPayload),
                ErrorType.Conflict => new ConflictObjectResult(error.ExtraPayload),
                ErrorType.Unauthorized => new UnauthorizedObjectResult(error.ExtraPayload),
                ErrorType.Forbidden => new ObjectResult(error.ExtraPayload) { StatusCode = (int)HttpStatusCode.Forbidden },
                _ => new ObjectResult(error.ExtraPayload) { StatusCode = (int)HttpStatusCode.InternalServerError }
            };
        }

        return error.Type switch
        {
            ErrorType.Validation => new BadRequestObjectResult(new { message = error.Message }),
            ErrorType.NotFound => string.IsNullOrWhiteSpace(error.Message)
                ? new NotFoundResult()
                : new NotFoundObjectResult(new
                {
                    status = 404,
                    title = "Not Found",
                    message = error.Message
                }),
            ErrorType.Conflict => new ConflictObjectResult(new
            {
                status = 409,
                title = "Conflict",
                message = error.Message
            }),
            ErrorType.Unauthorized => new UnauthorizedResult(),
            ErrorType.Forbidden => new ForbidResult(),
            _ => new ObjectResult(new
            {
                status = 500,
                title = "Server Error",
                message = "An unexpected error occurred."
            })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };
    }
}
