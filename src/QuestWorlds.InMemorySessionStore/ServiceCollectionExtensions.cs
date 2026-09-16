namespace QuestWorlds.InMemorySessionStore;

using Microsoft.Extensions.DependencyInjection;
using QuestWorlds.Framing;
using QuestWorlds.Session;

/// <summary>
/// Extension methods for registering the in-memory session store with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="InMemorySessionStore"/> as the deployment's <see cref="IAmASessionStore"/>
    /// and <see cref="IAmAContestFrameStore"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Call exactly one store registration. A second one wins, because the container takes the last
    /// registration of a service type, and nothing will tell you the first was discarded (ADR-0008 D2).
    /// </remarks>
    public static IServiceCollection AddInMemorySessionStore(this IServiceCollection services)
    {
        services.AddSingleton<IAmASessionStore, InMemorySessionStore>();
        services.AddSingleton<IAmAContestFrameStore, InMemorySessionStore>();
        return services;
    }
}
