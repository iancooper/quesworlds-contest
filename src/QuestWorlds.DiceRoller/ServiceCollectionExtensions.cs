using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.DiceRoller;

/// <summary>
/// Extension methods for registering the DiceRoller module with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the DiceRoller module services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDiceRollerModule(this IServiceCollection services)
    {
        services.AddSingleton<IDiceRoller, DiceRoller>();
        return services;
    }
}
