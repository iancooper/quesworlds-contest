using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Session;

/// <summary>
/// Extension methods for registering the Session module with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Session module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.AddSingleton<ISessionCoordinator, SessionCoordinator>();
        return services;
    }
}
