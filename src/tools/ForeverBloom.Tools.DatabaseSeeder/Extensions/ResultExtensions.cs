using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Tools.DatabaseSeeder.Exceptions;

namespace ForeverBloom.Tools.DatabaseSeeder.Extensions;

/// <summary>
/// Extension methods for working with Result types in seeding operations.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Extracts the value from a Result or throws a SeedingException if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to extract the value from.</param>
    /// <param name="seedOperation">Seeding operation that returned a failure result (for error messages).</param>
    /// <returns>The value if the result is successful.</returns>
    /// <exception cref="SeedingException">Thrown when the result is a failure.</exception>
    public static T ValueOrThrow<T>(this Result<T> result, string seedOperation)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new SeedingException(seedOperation, result.Error);
    }
}
