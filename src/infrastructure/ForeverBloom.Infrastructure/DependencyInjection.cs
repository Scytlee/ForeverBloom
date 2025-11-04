using System.Reflection;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Assembly marker for reflection-based registrations.
    /// </summary>
    public static Assembly InfrastructureAssembly { get; } = typeof(DependencyInjection).Assembly;

    /// <summary>
    /// Registers Infrastructure layer services including time provider, messaging, email, etc.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register time provider
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        return services;
    }
}
