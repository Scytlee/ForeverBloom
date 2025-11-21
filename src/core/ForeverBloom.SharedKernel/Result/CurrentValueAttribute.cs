namespace ForeverBloom.SharedKernel.Result;

/// <summary>
/// Marks a property as the current/existing value for context in an error.
/// This property will be serialized as "currentValue" in error responses.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class CurrentValueAttribute : Attribute;
