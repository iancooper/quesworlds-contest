using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Resolution;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResolutionModule(this IServiceCollection services)
    {
        services.AddSingleton<IContestResolver, ContestResolver>();
        return services;
    }
}
