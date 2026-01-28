using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Outcome;

/// <summary>
/// Extension methods for registering the Outcome module with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Outcome module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutcomeModule(this IServiceCollection services)
    {
        services.AddSingleton<IOutcomeInterpreter, OutcomeInterpreter>();
        return services;
    }
}
