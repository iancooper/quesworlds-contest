using Microsoft.Extensions.DependencyInjection;

namespace QuestWorlds.Session;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.AddSingleton<ISessionCoordinator, SessionCoordinator>();
        return services;
    }
}
