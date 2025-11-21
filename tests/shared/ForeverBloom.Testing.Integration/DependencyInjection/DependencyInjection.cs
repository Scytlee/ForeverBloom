using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Testing.Integration.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the minimal infrastructure services required for integration seeding.
    /// </summary>
    public static IServiceCollection AddTestInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
