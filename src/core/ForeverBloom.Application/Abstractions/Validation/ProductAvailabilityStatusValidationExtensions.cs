using FluentValidation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// FluentValidation extensions for ProductAvailabilityStatus value object validation.
/// </summary>
internal static class ProductAvailabilityStatusValidationExtensions
{
    /// <summary>
    /// Validates that an int is a valid ProductAvailabilityStatus by delegating to domain validation.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, int> MustBeValidProductAvailabilityStatus<T>(
        this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = ProductAvailabilityStatus.FromCode(value);
            if (result.IsSuccess)
            {
                return;
            }

            var composite = (CompositeError)result.Error;

            foreach (var error in composite.Errors)
            {
                switch (error)
                {
                    case ProductAvailabilityStatusErrors.InvalidCode e:
                        context.AddInvalidCodeFailure(e, value);
                        break;
                }
            }
        });
    }

    private static void AddInvalidCodeFailure<T>(this ValidationContext<T> context, ProductAvailabilityStatusErrors.InvalidCode error, int attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        failure.CustomState = new { error.ValidCodes };
        context.AddFailure(failure);
    }
}
