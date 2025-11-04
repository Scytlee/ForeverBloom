using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Validation extensions for Optional&lt;T&gt; properties.
/// </summary>
internal static class OptionalValidationExtensions
{
    /// <summary>
    /// Creates a validation rule for an Optional&lt;T&gt; property that automatically handles the IsSet check and value validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method simplifies validation of Optional properties by:
    /// </para>
    /// <list type="number">
    /// <item><description>Only validating when the Optional is set (IsSet == true)</description></item>
    /// <item><description>Validating the inner value, not the Optional wrapper</description></item>
    /// <item><description>Using clean property names in error messages (e.g., "Name" instead of "Name.Value")</description></item>
    /// <item><description>Preserving nested property paths (e.g., "Address.City" instead of "Address.Value.City")</description></item>
    /// </list>
    /// <para>
    /// The method uses async validation internally to support validators that perform async operations
    /// (e.g., database lookups, external API calls). Standard synchronous validators work seamlessly.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type being validated</typeparam>
    /// <typeparam name="TValue">The type wrapped by Optional</typeparam>
    /// <param name="validator">The validator instance</param>
    /// <param name="expression">Expression selecting the Optional property (e.g., x => x.Name)</param>
    /// <param name="configureRule">Action to configure validation rules for the inner value</param>
    /// <example>
    /// <code>
    /// // Single rule
    /// RuleForOptional(x => x.Name, name => name.MaximumLength(100));
    ///  <br />
    /// // Multiple rules
    /// RuleForOptional(x => x.Description, desc =>
    /// {
    ///     desc.NotEmpty();
    ///     desc.MaximumLength(500);
    /// });
    ///  <br />
    /// // In RuleForEach with ChildRules
    /// RuleForEach(x => x.Items)
    ///     .ChildRules(item =>
    ///     {
    ///         item.RuleForOptional(i => i.Name, name => name.MaximumLength(100));
    ///     });
    /// </code>
    /// </example>
    internal static IRuleBuilderOptionsConditions<T, Optional<TValue>> RuleForOptional<T, TValue>(
        this AbstractValidator<T> validator,
        Expression<Func<T, Optional<TValue>>> expression,
        Action<IRuleBuilderInitial<TValue, TValue>> configureRule)
    {
        // Extract property name for clean error messages (without ".Value")
        var propertyName = GetPropertyName(expression);

        // Build a temporary validator to collect the rules
        var valueValidator = new InlineValidator<TValue>();
        configureRule(valueValidator.RuleFor(x => x));

        var get = expression.Compile();

        // Create the rule on the Optional itself, using a custom validator
        return validator.RuleFor(expression)
            .CustomAsync(async (optional, context, cancellationToken) =>
            {
                if (!optional.IsSet)
                {
                    return;
                }

                // FluentValidation cannot validate null instances, so skip validation if value is null
                if (optional.Value is null)
                {
                    return;
                }

                var result = await valueValidator.ValidateAsync(optional.Value, cancellationToken);

                // Modify property names in failures to use the Optional's property name
                // This ensures we get "Property.NestedField" instead of "Value.NestedField"
                foreach (var failure in result.Errors)
                {
                    var path = string.IsNullOrWhiteSpace(failure.PropertyName)
                        ? propertyName
                        : $"{propertyName}.{failure.PropertyName}";

                    context.AddFailure(new ValidationFailure(path, failure.ErrorMessage)
                    {
                        ErrorCode = failure.ErrorCode,
                        Severity = failure.Severity,
                        CustomState = failure.CustomState,
                        AttemptedValue = failure.AttemptedValue
                    });
                }
            })
            .When(x => get(x).IsSet);
    }

    /// <summary>
    /// Extracts the property name from the expression.
    /// </summary>
    private static string GetPropertyName<T, TValue>(Expression<Func<T, Optional<TValue>>> expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            _ => throw new ArgumentException(
                "Expression must be a simple property access (e.g., x => x.Property).",
                nameof(expression))
        };
    }
}
