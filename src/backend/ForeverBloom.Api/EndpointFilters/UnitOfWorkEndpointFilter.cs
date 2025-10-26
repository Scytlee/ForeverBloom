using System.Diagnostics;
using ForeverBloom.Persistence.Abstractions;

namespace ForeverBloom.Api.EndpointFilters;

/// <summary>
/// An endpoint filter that wraps the endpoint handler in a transaction.
/// The transaction is committed if the endpoint returns a 2XX status code.
/// If the endpoint returns an error result, the transaction is rolled back.
/// <remarks>The endpoint must explicitly return a result with a defined status code.</remarks>
/// </summary>
internal sealed class UnitOfWorkEndpointFilter : IEndpointFilter
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkEndpointFilter(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<UnitOfWorkEndpointFilter>>();
        var stopwatch = Stopwatch.StartNew();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        logger.LogDebug("Transaction started");

        try
        {
            var result = await next(context);

            if (result is INestedHttpResult nestedResult)
            {
                result = nestedResult.Result;
            }

            if (result is not IStatusCodeHttpResult statusCodeResult || statusCodeResult.StatusCode is null)
            {
                // The endpoint must explicitly return a result with a defined status code.
                // This avoids ambiguity and unwanted transaction commits.
                throw new InvalidOperationException("Status code of endpoint result must be determined to finalize unit of work");
            }

            var statusCode = statusCodeResult.StatusCode.Value;
            if (statusCode is >= 200 and < 300)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);
                stopwatch.Stop();
                logger.LogInformation("Transaction committed ({StatusCode}) in {ElapsedMs} ms", statusCode, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                stopwatch.Stop();
                logger.LogWarning("Transaction rolled back ({StatusCode}) in {ElapsedMs} ms", statusCode, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            stopwatch.Stop();
            logger.LogError(ex, "Exception occurred in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

public sealed record UnitOfWorkApplied;

public static class UnitOfWorkEndpointFilterExtensions
{
    public static TBuilder UseUnitOfWork<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter<TBuilder, UnitOfWorkEndpointFilter>()
          .WithMetadata(new UnitOfWorkApplied());

        return builder;
    }
}
