namespace ForeverBloom.SharedKernel.Result;

/// <summary>
/// Marks a property as the primary attempted/invalid value in an error.
/// This property will be serialized as "attemptedValue" in error responses.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class AttemptedValueAttribute : Attribute;
