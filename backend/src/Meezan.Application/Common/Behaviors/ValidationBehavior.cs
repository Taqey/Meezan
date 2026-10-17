using FluentValidation;
using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                if (typeof(IResult).IsAssignableFrom(typeof(TResponse)))
                {
                    var payload = new
                    {
                        status = 400,
                        title = "Validation Error",
                        errors = failures.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }).ToList()
                    };

                    var firstError = failures[0].ErrorMessage;
                    var error = Error.Validation("ValidationError", firstError, payload);
                    return ResultFactory.CreateFailure<TResponse>(error);
                }

                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
