using System.Reflection;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace ForeverBloom.Testing.Common.Helpers.Assertion;

public static class AssertionHelpers
{
    public static void AssertAllPropertiesAreMapped<TSource, TDestination>(
      TSource sourceObject,
      TDestination destinationObject,
      Dictionary<string, string>? overridesMap = null,
      HashSet<string>? sourceExcludes = null,
      Func<EquivalencyAssertionOptions<object?>, EquivalencyAssertionOptions<object?>>? equivalencyConfig = null)
      where TSource : notnull
      where TDestination : notnull
    {
        var sourcePropertiesEnumerable = typeof(TSource)
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        if (sourceExcludes is not null && sourceExcludes.Count > 0)
        {
            sourcePropertiesEnumerable = sourcePropertiesEnumerable.Where(p => !sourceExcludes.Contains(p.Name));
        }

        var sourceProperties = sourcePropertiesEnumerable.ToList();

        foreach (var property in sourceProperties)
        {
            string? overrideName = null;
            var overridden = overridesMap is not null && overridesMap.TryGetValue(property.Name, out overrideName);

            var destinationName = overridden ? overrideName! : property.Name;
            var destinationProperty = typeof(TDestination).GetProperty(destinationName, BindingFlags.Public | BindingFlags.Instance);

            destinationProperty.Should().NotBeNull($"'{typeof(TDestination).Name}.{destinationName}' property must exist to match '{typeof(TSource).Name}.{property.Name}'");

            var sourceValue = property.GetValue(sourceObject);
            var destinationValue = destinationProperty.GetValue(destinationObject);

            destinationValue.Should().BeEquivalentTo(sourceValue, equivalencyConfig + (options => options.ComparingEnumsByValue()),
              $"'{typeof(TSource).Name}.{property.Name}' should map to '{typeof(TDestination).Name}.{destinationName}'");
        }
    }
}
