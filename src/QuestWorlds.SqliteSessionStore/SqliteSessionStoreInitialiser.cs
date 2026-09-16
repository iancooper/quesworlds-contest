namespace QuestWorlds.SqliteSessionStore;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Creates the schema the store reads and writes, once, when the application starts.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IHostedService"/> by
/// <see cref="ServiceCollectionExtensions.AddSqliteSessionStore"/>, so choosing the store is the only
/// decision a host makes and initialisation cannot be left out (ADR-0009 D6).
/// Tests do not run a host, so <see cref="InitialiseAsync"/> is callable directly.
/// </remarks>
public class SqliteSessionStoreInitialiser : IHostedService
{
    // Write-ahead logging is a property of the database file, not of a connection, so setting it once
    // here holds for every connection the store opens afterwards (ADR-0009 D5). It cannot run inside a
    // transaction, which is why it is a statement of its own rather than part of the batch below.
    private const string JOURNAL_MODE = "PRAGMA journal_mode = WAL;";

    // Four tables, not a JSON blob: the mapping an adapter does is the point of this module, and a
    // reader should be able to open the database and see the session (ADR-0009 D2).
    //
    // Enums are stored as their names, never their ordinals. A numeric enum value is positional, so
    // inserting a SessionState in the middle of the workflow would silently reinterpret every stored
    // row (ADR-0009 D3).
    //
    // Ordinal columns exist because Session.Players and ContestFrame.Modifiers are ordered lists and a
    // table is a set; they are what makes a reloaded aggregate equal to the one that was stored.
    private const string SCHEMA = """
        CREATE TABLE IF NOT EXISTS Sessions (
            Id     TEXT PRIMARY KEY NOT NULL,
            State  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Participants (
            SessionId    TEXT NOT NULL,
            Ordinal      INTEGER NOT NULL,
            Name         TEXT NOT NULL,
            Role         TEXT NOT NULL,
            ConnectionId TEXT NOT NULL,
            PRIMARY KEY (SessionId, Ordinal),
            FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS ContestFrames (
            SessionId             TEXT PRIMARY KEY NOT NULL,
            Prize                 TEXT NOT NULL,
            ResistanceBase        INTEGER NOT NULL,
            ResistanceMasteries   INTEGER NOT NULL,
            ResistanceModifier    INTEGER NOT NULL,
            PlayerAbilityName     TEXT NULL,
            PlayerRatingBase      INTEGER NULL,
            PlayerRatingMasteries INTEGER NULL,
            FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS ContestModifiers (
            SessionId TEXT NOT NULL,
            Ordinal   INTEGER NOT NULL,
            Type      TEXT NOT NULL,
            Value     INTEGER NOT NULL,
            PRIMARY KEY (SessionId, Ordinal),
            FOREIGN KEY (SessionId) REFERENCES ContestFrames(SessionId) ON DELETE CASCADE
        );
        """;

    private readonly string _connectionString;

    /// <summary>
    /// Creates an initialiser for the database the supplied connection string names.
    /// </summary>
    /// <param name="connectionString">The connection string of the database to initialise.</param>
    public SqliteSessionStoreInitialiser(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates the schema if it is absent and puts the database into write-ahead logging mode.
    /// </summary>
    /// <param name="connectionString">The connection string of the database to initialise.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Safe to call on a database that is already initialised: every statement is
    /// <c>IF NOT EXISTS</c> and the journal mode is idempotent. Available directly because tests do
    /// not run a host (ADR-0009 D6).
    /// </remarks>
    public static async Task InitialiseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var journalMode = connection.CreateCommand();
        journalMode.CommandText = JOURNAL_MODE;
        await journalMode.ExecuteNonQueryAsync(cancellationToken);

        var schema = connection.CreateCommand();
        schema.CommandText = SCHEMA;
        await schema.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        InitialiseAsync(_connectionString, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
