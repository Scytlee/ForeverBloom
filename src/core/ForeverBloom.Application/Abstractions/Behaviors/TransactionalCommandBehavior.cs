using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ForeverBloom.Application.Abstractions.Behaviors;

public sealed class TransactionalCommandBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : IBaseCommand
    where TResponse : IResult, ICreatesFailure<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionalCommandBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TCommand request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var settings = request is IWithTransactionOverrides commandWithOverrides
            ? commandWithOverrides.TransactionSettings
            : TransactionSettings.Default;

        try
        {
            return await _unitOfWork.ExecuteTransactionalAsync(
                async _ =>
                {
                    var result = await next(cancellationToken);
                    return result;
                },
                commitCondition: result => result.IsSuccess,
                settings.Isolation,
                settings.LockTimeout,
                settings.StatementTimeout,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TResponse.Failure(new ApplicationErrors.ConcurrencyConflict());
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return TResponse.Failure(new ApplicationErrors.ConcurrencyConflict());
        }
    }
}
