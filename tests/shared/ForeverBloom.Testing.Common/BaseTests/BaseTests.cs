using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.TestHelper;

namespace ForeverBloom.Testing.Common.BaseTests;

public static class BaseTests
{
    /// <summary>
    /// Provides a base validation test for a specific property of a request.
    /// It checks if the validation for the given property results in the expected error code,
    /// or no error if the expected error code is null.
    /// </summary>
    /// <typeparam name="TValidator">The type of the FluentValidation validator.</typeparam>
    /// <typeparam name="TRequest">The type of the request object being validated.</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <param name="validator">The validator instance.</param>
    /// <param name="request">The request object populated with data for the test case.</param>
    /// <param name="memberAccessor">An expression pointing to the member being validated.</param>
    /// <param name="expectedErrorCode">The expected error code if validation fails for the member.
    /// If null, the test asserts that there is no validation error for this member.</param>
    public static void BaseValidationTest<TValidator, TRequest, TProperty>(
      TValidator validator,
      TRequest request,
      Expression<Func<TRequest, TProperty>> memberAccessor,
      string? expectedErrorCode)
      where TValidator : AbstractValidator<TRequest>
    {
        var result = validator.TestValidate(request);

        if (expectedErrorCode is null)
        {
            result.ShouldNotHaveValidationErrorFor(memberAccessor);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(memberAccessor)
              .WithErrorCode(expectedErrorCode);
        }
    }
}
