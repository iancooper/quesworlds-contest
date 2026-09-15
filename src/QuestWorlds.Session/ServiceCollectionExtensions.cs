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
    /// <remarks>
    /// No store is registered here: which store a deployment uses is the host's choice, and this
    /// module implements none of them (ADR-0008 D2). A host that registers none gets a container
    /// that cannot construct <see cref="ISessionCoordinator"/>.
    /// </remarks>
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
        services.AddSingleton<ISessionCoordinator, SessionCoordinator>();
        return services;
    }
}
