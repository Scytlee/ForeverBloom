using FluentAssertions;
using FluentAssertions.Equivalency;
using ForeverBloom.Testing.Common.Time;

namespace ForeverBloom.Testing.Common.Extensions;

public static class EquivalencyAssertionOptionsExtensions
{
    // Applies a tolerance suitable for persisted timestamps (e.g., PostgreSQL timestamp(6)).
    public static EquivalencyAssertionOptions<T> WithTimestampTolerance<T>(
      this EquivalencyAssertionOptions<T> options, TimeSpan? tolerance = null)
    {
        var tol = tolerance ?? TemporalTolerances.DatabaseTimestamp;

        options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, tol))
          .WhenTypeIs<DateTime>();

        options.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, tol))
          .WhenTypeIs<DateTimeOffset>();

        return options;
    }
}
