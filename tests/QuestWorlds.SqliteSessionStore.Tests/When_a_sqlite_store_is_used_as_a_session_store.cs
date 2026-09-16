using Microsoft.Data.Sqlite;
using QuestWorlds.SessionStore.ContractTests;

namespace QuestWorlds.SqliteSessionStore.Tests;

/// <summary>
/// Runs the store contract against SQLite. The cases are inherited, not restated: they are the
/// specification both stores answer to (ADR-0008 D5).
/// </summary>
/// <remarks>
/// Each case gets its own database file, created before it runs and deleted after it, so that cases
/// cannot see each other's sessions. A file rather than SQLite's in-memory mode, because the
/// requirement is about a session written by one store instance being readable by another over the
/// same database — the path a restart takes (ADR-0009 D7).
/// </remarks>
public class When_a_sqlite_store_is_used_as_a_session_store : SessionStoreContract<SqliteSessionStore>, IAsyncLifetime
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-contract-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_databasePath}";

    public Task InitializeAsync() => SqliteSessionStoreInitialiser.InitialiseAsync(ConnectionString);

    protected override SqliteSessionStore CreateStore() => new(ConnectionString);

    public Task DisposeAsync()
    {
        // The provider pools connections, and a pooled one still holds the file. Returning them
        // first is what makes the delete — and the write-ahead log beside it — actually go.
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (File.Exists(file)) File.Delete(file);
        }

        return Task.CompletedTask;
    }
}
