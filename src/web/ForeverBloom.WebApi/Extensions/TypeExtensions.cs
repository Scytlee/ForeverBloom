using System.Runtime.CompilerServices;

namespace ForeverBloom.WebApi.Extensions;

public static class TypeExtensions
{
    public static bool IsAnonymousType(this Type? type)
    {
        if (type is null)
        {
            return false;
        }

        // Must be compiler-generated
        var compilerGenerated =
            Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: false);

        // Name must start with "<>" and contain "AnonymousType"
        var nameLooksAnonymous =
            type.Name.StartsWith("<>", StringComparison.Ordinal) &&
            type.Name.Contains("AnonymousType", StringComparison.Ordinal);

        // Anonymous types are internal, sealed classes, and generic
        var isRightKindOfClass = type is { IsClass: true, IsSealed: true, IsGenericType: true };

        return compilerGenerated && nameLooksAnonymous && isRightKindOfClass;
    }
}
