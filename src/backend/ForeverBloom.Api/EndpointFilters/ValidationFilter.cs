using FluentValidation;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.EndpointFilters;

internal sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    private readonly ILogger<ValidationFilter<T>> _logger;

    public ValidationFilter(IValidator<T> validator, ILogger<ValidationFilter<T>> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argToValidate = context.Arguments.OfType<T>().First();
        var validationResult = await _validator.ValidateAsync(argToValidate);

        if (!validationResult.IsValid)
        {
            var errorDictionary = validationResult.Errors
              .GroupBy(x => x.PropertyName)
              .ToDictionary(
                k => k.Key,
                v => v.Select(x => x.ErrorCode).ToArray());

            _logger.LogWarning("Validation failed for {RequestType}: {@Errors}", typeof(T).Name, errorDictionary);
            return ApiResults.ValidationProblem(errorDictionary);
        }

        return await next(context);
    }
}

public sealed record ValidationApplied<T> where T : class;

public static class ValidationEndpointFilterExtensions
{
    public static RouteHandlerBuilder ValidateRequest<TRequest>(this RouteHandlerBuilder builder)
      where TRequest : class
    {
        builder.AddEndpointFilter<ValidationFilter<TRequest>>()
          .WithMetadata(new ValidationApplied<TRequest>());

        return builder;
    }
}
