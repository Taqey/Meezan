using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only run transactions for commands explicitly opting in
        if (request is not ITransactionalCommand)
        {
            return await next();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response is IResult result && result.IsFailure)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return response;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
