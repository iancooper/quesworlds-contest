namespace QuestWorlds.Web;

using QuestWorlds.InMemorySessionStore;
using QuestWorlds.SqliteSessionStore;

/// <summary>
/// The composition root's store switch: one setting decides which store the application runs on.
/// </summary>
/// <remarks>
/// This is <c>internal</c> on purpose. Choosing a store is this host's business, not a reusable
/// abstraction — a shared "store chooser" would have to reference every store that exists, which is
/// how a composition root leaks into a library (ADR-0008 D4).
/// </remarks>
internal static class ServiceCollectionExtensions
{
    private const string PROVIDER_SETTING = "SessionStore:Provider";
    private const string CONNECTION_STRING_NAME = "SessionStore";
    private const string IN_MEMORY = "InMemory";
    private const string SQLITE = "Sqlite";

    /// <summary>
    /// Registers the session store named by <c>SessionStore:Provider</c>, defaulting to the
    /// in-memory store when the setting is absent.
    /// </summary>
    /// <param name="services">The service collection to add the store to.</param>
    /// <param name="configuration">
    /// The host's configuration, read for the provider and, for SQLite, the
    /// <c>ConnectionStrings:SessionStore</c> the module needs (ADR-0009 D7).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the provider is set to a value that is neither <c>InMemory</c> nor <c>Sqlite</c>.
    /// </exception>
    /// <remarks>
    /// An unrecognised value throws rather than falling back to in-memory. A typo must not silently
    /// produce a server that loses sessions on restart while appearing to be configured not to;
    /// failing loudly at startup is the whole point of having the switch in one place.
    /// </remarks>
    internal static IServiceCollection AddConfiguredSessionStore(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue(PROVIDER_SETTING, IN_MEMORY)!;

        return provider switch
        {
            IN_MEMORY => services.AddInMemorySessionStore(),

            // A missing connection string is the module's own guard, and it throws here — while the
            // host is starting — rather than on the first save, mid-game (ADR-0008 D3).
            SQLITE => services.AddSqliteSessionStore(configuration.GetConnectionString(CONNECTION_STRING_NAME)!),

            _ => throw new InvalidOperationException(
                $"Unknown {PROVIDER_SETTING} '{provider}'. Expected '{IN_MEMORY}' or '{SQLITE}'.")
        };
    }
}
