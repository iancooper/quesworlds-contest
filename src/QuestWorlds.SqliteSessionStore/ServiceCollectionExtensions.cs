namespace QuestWorlds.SqliteSessionStore;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the SQLite session store with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQLite store over the database the supplied connection string names, together
    /// with the hosted service that creates its schema at startup.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">
    /// Where the database lives. The module takes it and nothing else: choosing a path, or reading
    /// configuration, is the host's decision, not this module's (ADR-0009 D7).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or whitespace.</exception>
    /// <remarks>
    /// Call exactly one store registration. A second one wins, because the container takes the last
    /// registration of a service type, and nothing will tell you the first was discarded (ADR-0008 D2).
    /// </remarks>
    public static IServiceCollection AddSqliteSessionStore(this IServiceCollection services, string connectionString)
    {
        // A missing connection string is a deployment mistake, and it should be one the host hears
        // about while it is starting rather than on the first save (ADR-0008 D3).
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // The initialiser runs at start, not at registration: registering services should not open a
        // database (ADR-0009 D6).
        services.AddHostedService(_ => new SqliteSessionStoreInitialiser(connectionString));

        // The store itself, forwarded to from both ports by one concrete singleton, arrives with
        // task 7.2. There is nothing to register behind IAmASessionStore yet, so a host that calls
        // this today fails its ValidateOnBuild check — loudly, which is the intent.
        return services;
    }
}
