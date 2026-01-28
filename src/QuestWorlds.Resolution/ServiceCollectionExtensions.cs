using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Resolution;

/// <summary>
/// Extension methods for registering the Resolution module with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Resolution module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResolutionModule(this IServiceCollection services)
    {
        services.AddSingleton<IContestResolver, ContestResolver>();
        return services;
    }
}
