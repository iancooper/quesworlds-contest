namespace QuestWorlds.SqliteSessionStore.Tests;

using Microsoft.Data.Sqlite;
using QuestWorlds.Framing;
using QuestWorlds.Session;

// The alias has to sit inside the namespace declaration: at global scope the enclosing QuestWorlds
// namespace is searched first, finds the QuestWorlds.Session namespace, and an unqualified Session
// is CS0118.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Removing a session takes every row that belonged to it, leaving nothing behind that nothing can
/// reach (AC18).
/// </summary>
/// <remarks>
/// The contract suite already proves a removed session's frame is no longer gettable, on both stores.
/// It cannot prove this: a frame that is unreachable through the port may still exist as rows, and
/// the modifiers hanging off it certainly can. Only a store that has tables can be asked, so the
/// question is asked here, in table terms, by counting.
/// <para>
/// It matters because the deletion is not all written down in one place. <c>RemoveAsync</c> deletes
/// the frame explicitly — the <c>ContestFrames</c> foreign key was dropped in an amendment to
/// ADR-0009 D2 — while participants and modifiers go by <c>ON DELETE CASCADE</c>, which SQLite only
/// honours when foreign keys are enforced on the connection. Three mechanisms, one obligation, and
/// nothing guarding the seams between them.
/// </para>
/// </remarks>
public class When_a_session_is_removed_should_leave_no_orphaned_rows : IAsyncLifetime
{
    private const string SESSION_ID = "ABC123";
    private const string PRIZE = "Sneak past the guards";
    private const string PLAYER_ABILITY = "Thief of Nochet";

    private static readonly Participant Gm = new("The GM", ParticipantRole.GM, "gm-connection");
    private static readonly Participant Player = new("The Player", ParticipantRole.Player, "player-connection");

    // Two modifiers, so a cascade that deletes one row and stops would still be caught.
    private static readonly Modifier[] Modifiers =
    [
        new(ModifierType.Augment, 10),
        new(ModifierType.Stretch, -5)
    ];

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-removal-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_databasePath}";

    public async Task InitializeAsync()
    {
        //Arrange
        // A session with every kind of row it can own: a participant list, a frame, and the frame's
        // modifiers. Removing anything less would not exercise all three deletion mechanisms.
        await SqliteSessionStoreInitialiser.InitialiseAsync(ConnectionString);

        var store = new SqliteSessionStore(ConnectionString);
        await store.SaveAsync(Session.Rehydrate(
            SESSION_ID, Gm, new[] { Player }, SessionState.AwaitingPlayerAbility));

        var frame = new ContestFrame(PRIZE, new TargetNumber(14));
        frame.SetPlayerAbility(PLAYER_ABILITY, new Rating(15, 1));
        foreach (var modifier in Modifiers) frame.ApplyModifier(modifier);
        await store.SaveFrameAsync(SESSION_ID, frame);

        //Act
        await store.RemoveAsync(SESSION_ID);
    }

    // The session's own table keys on Id; every table that hangs off it keys on SessionId.
    [Theory]
    [InlineData("Sessions", "Id")]
    [InlineData("Participants", "SessionId")]
    [InlineData("ContestFrames", "SessionId")]
    [InlineData("ContestModifiers", "SessionId")]
    public async Task No_table_should_still_hold_a_row_for_the_removed_session(string table, string keyColumn)
    {
        //Act
        var remaining = await RowsFor(table, keyColumn);

        //Assert
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Foreign_keys_should_be_enforced_on_the_connection()
    {
        //Act
        // SQLite defaults foreign keys to off, and with them off the cascades above delete nothing
        // while every other test in the suite still passes. Asserting the pragma says out loud what
        // the deletions are relying on.
        var enforced = await ScalarAsync("PRAGMA foreign_keys;");

        //Assert
        Assert.Equal(1L, Convert.ToInt64(enforced));
    }

    private async Task<long> RowsFor(string table, string keyColumn) => Convert.ToInt64(
        await ScalarAsync($"SELECT COUNT(*) FROM {table} WHERE {keyColumn} = @sessionId;", SESSION_ID));

    private async Task<object?> ScalarAsync(string sql, string? sessionId = null)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (sessionId is not null) command.Parameters.AddWithValue("@sessionId", sessionId);
        return await command.ExecuteScalarAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (File.Exists(file)) File.Delete(file);
        }

        return Task.CompletedTask;
    }
}
