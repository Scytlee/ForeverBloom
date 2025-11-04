using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Pagination;

internal static class PaginationValidationExtensions
{
    internal static IRuleBuilderOptionsConditions<T, int> MustBeValidPageNumber<T>(
        this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value <= 0)
            {
                context.AddInvalidPageNumberFailure(new PaginationErrors.InvalidPageNumber(value), value);
            }
        });
    }

    private static void AddInvalidPageNumberFailure<T>(
        this ValidationContext<T> context,
        PaginationErrors.InvalidPageNumber error,
        int attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    internal static IRuleBuilderOptionsConditions<T, int> MustBeValidPageSize<T>(
        this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            if (value is < PaginationConstants.MinimumPageSize or > PaginationConstants.MaximumPageSize)
            {
                context.AddInvalidPageSizeFailure(new PaginationErrors.InvalidPageSize(value), value);
            }
        });
    }

    private static void AddInvalidPageSizeFailure<T>(
        this ValidationContext<T> context,
        PaginationErrors.InvalidPageSize error,
        int attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.MinimumPageSize, error.MaximumPageSize };
        context.AddFailure(failure);
    }
}
