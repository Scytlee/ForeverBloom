using System.Reflection;
using ForeverBloom.Application.Abstractions.Behaviors;
using ForeverBloom.Domain.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Assembly marker for reflection-based registrations.
    /// </summary>
    public static Assembly ApplicationAssembly { get; } = typeof(DependencyInjection).Assembly;

    /// <summary>
    /// Registers Application layer services including MediatR and pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR with all handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(ApplicationAssembly);

            cfg.AddOpenBehavior(typeof(TransactionalCommandBehavior<,>));
        });

        // Register domain services
        services.AddTransient<CategoryHierarchyService>();

        return services;
    }
}
