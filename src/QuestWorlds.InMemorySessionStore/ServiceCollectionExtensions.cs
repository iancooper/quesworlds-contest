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
        // One concrete singleton, both ports forwarded to it. Registering the class against each port
        // instead gives an instance per port — two sets of state, and a frame the session store cannot
        // see. It fails silently, and only in composition (ADR-0010 D4).
        services.AddSingleton<InMemorySessionStore>();
        services.AddSingleton<IAmASessionStore>(sp => sp.GetRequiredService<InMemorySessionStore>());
        services.AddSingleton<IAmAContestFrameStore>(sp => sp.GetRequiredService<InMemorySessionStore>());
        return services;
    }
}
