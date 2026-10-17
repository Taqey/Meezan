namespace Meezan.Application.Common.Results;

public record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Failure,
    object? ExtraPayload = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.Failure, extraPayload);

    public static Error Validation(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.Validation, extraPayload);

    public static Error NotFound(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.NotFound, extraPayload);

    public static Error Conflict(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.Conflict, extraPayload);

    public static Error Unauthorized(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.Unauthorized, extraPayload);

    public static Error Forbidden(string code, string message, object? extraPayload = null) =>
        new(code, message, ErrorType.Forbidden, extraPayload);
}
