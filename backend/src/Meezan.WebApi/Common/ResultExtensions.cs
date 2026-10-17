using Meezan.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private static IActionResult JsonContent(object payload, int statusCode)
    {
        return new ContentResult
        {
            Content = System.Text.Json.JsonSerializer.Serialize(payload, JsonOptions),
            ContentType = "application/json",
            StatusCode = statusCode
        };
    }

    private static IActionResult MapError(Error error)
    {
        if (error.ExtraPayload != null)
        {
            return error.Type switch
            {
                ErrorType.Validation => JsonContent(error.ExtraPayload, (int)HttpStatusCode.BadRequest),
                ErrorType.NotFound => JsonContent(error.ExtraPayload, (int)HttpStatusCode.NotFound),
                ErrorType.Conflict => JsonContent(error.ExtraPayload, (int)HttpStatusCode.Conflict),
                ErrorType.Unauthorized => JsonContent(error.ExtraPayload, (int)HttpStatusCode.Unauthorized),
                ErrorType.Forbidden => JsonContent(error.ExtraPayload, (int)HttpStatusCode.Forbidden),
                _ => JsonContent(error.ExtraPayload, (int)HttpStatusCode.InternalServerError)
            };
        }

        return error.Type switch
        {
            ErrorType.Validation => JsonContent(new { message = error.Message }, (int)HttpStatusCode.BadRequest),
            ErrorType.NotFound => string.IsNullOrWhiteSpace(error.Message)
                ? new NotFoundResult()
                : JsonContent(new
                {
                    status = 404,
                    title = "Not Found",
                    message = error.Message
                }, (int)HttpStatusCode.NotFound),
            ErrorType.Conflict => JsonContent(new
            {
                status = 409,
                title = "Conflict",
                message = error.Message
            }, (int)HttpStatusCode.Conflict),
            ErrorType.Unauthorized => new UnauthorizedResult(),
            ErrorType.Forbidden => new ForbidResult(),
            _ => JsonContent(new
            {
                status = 500,
                title = "Server Error",
                message = "An unexpected error occurred."
            }, (int)HttpStatusCode.InternalServerError)
        };
    }
}
