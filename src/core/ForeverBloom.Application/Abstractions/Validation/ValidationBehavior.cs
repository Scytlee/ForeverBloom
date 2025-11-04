using FluentValidation;
using ForeverBloom.SharedKernel.Result;
using MediatR;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation.
/// Runs before all other behaviors to fail fast on invalid input.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The result type</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ICreatesFailure<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators registered, continue pipeline
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        // Run all validators
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // If validation failed, return error
        if (failures.Count > 0)
        {
            var error = new ValidationError(failures);
            return TResponse.Failure(error);
        }

        // Validation passed, continue pipeline
        return await next(cancellationToken);
    }
}
