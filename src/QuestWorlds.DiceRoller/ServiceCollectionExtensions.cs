using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.DiceRoller;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiceRollerModule(this IServiceCollection services)
    {
        services.AddSingleton<IDiceRoller, DiceRoller>();
        return services;
    }
}
