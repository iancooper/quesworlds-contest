using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Outcome;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutcomeModule(this IServiceCollection services)
    {
        services.AddSingleton<IOutcomeInterpreter, OutcomeInterpreter>();
        return services;
    }
}
