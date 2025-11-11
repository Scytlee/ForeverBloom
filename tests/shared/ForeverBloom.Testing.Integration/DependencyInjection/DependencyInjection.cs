using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Testing.Integration.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ForeverBloom.Testing.Integration.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the minimal infrastructure services required for integration seeding.
    /// </summary>
    public static IServiceCollection AddTestInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<ITimeProvider, TestTimeProvider>();
        return services;
    }
}
