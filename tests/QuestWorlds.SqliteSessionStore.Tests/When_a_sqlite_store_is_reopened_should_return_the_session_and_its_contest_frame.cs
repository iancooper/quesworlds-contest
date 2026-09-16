namespace QuestWorlds.SqliteSessionStore.Tests;

using Microsoft.Data.Sqlite;
using QuestWorlds.Framing;
using QuestWorlds.Session;

// The alias has to sit inside the namespace declaration: at global scope the enclosing QuestWorlds
// namespace is searched first, finds the QuestWorlds.Session namespace, and an unqualified Session
// is CS0118.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// A game is saved, the process stops, and a new store over the same database file gives the game
/// back — the criterion the in-memory store cannot satisfy, and the one this spec's title rests on
/// (AC17).
/// </summary>
/// <remarks>
/// The cases share their set-up, because the set-up *is* the condition being tested: one store
/// writes, everything in memory goes away, and a second store reads. Splitting it across files would
/// mean writing the game three times.
/// </remarks>
public class When_a_sqlite_store_is_reopened_should_return_the_session_and_its_contest_frame : IAsyncLifetime
{
    private const string SESSION_ID = "ABC123";
    private const string PRIZE = "Sneak past the guards";
    private const string PLAYER_ABILITY = "Thief of Nochet";

    private static readonly Participant Gm = new("The GM", ParticipantRole.GM, "gm-connection");
    private static readonly Participant FirstPlayer = new("First Player", ParticipantRole.Player, "player-connection-1");
    private static readonly Participant SecondPlayer = new("Second Player", ParticipantRole.Player, "player-connection-2");

    private static readonly TargetNumber Resistance = new(14);
    private static readonly Rating PlayerRating = new(15, 1);

    // Two modifiers that differ in both type and value, so that an order they came back in wrongly
    // would show up rather than being hidden by a matching pair.
    private static readonly Modifier FirstModifier = new(ModifierType.Augment, 10);
    private static readonly Modifier SecondModifier = new(ModifierType.Stretch, -5);

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-restart-{Guid.NewGuid():N}.db");

    private SqliteSessionStore _storeAfterTheRestart = null!;

    private string ConnectionString => $"Data Source={_databasePath}";

    public async Task InitializeAsync()
    {
        //Arrange
        await SqliteSessionStoreInitialiser.InitialiseAsync(ConnectionString);

        // A game mid-contest: two players have joined, the GM has framed a contest, the player has
        // answered with an ability, and two modifiers have been applied.
        var storeBeforeTheRestart = new SqliteSessionStore(ConnectionString);

        await storeBeforeTheRestart.SaveAsync(Session.Rehydrate(
            SESSION_ID, Gm, new[] { FirstPlayer, SecondPlayer }, SessionState.AwaitingPlayerAbility));

        var frame = new ContestFrame(PRIZE, Resistance);
        frame.SetPlayerAbility(PLAYER_ABILITY, PlayerRating);
        frame.ApplyModifier(FirstModifier);
        frame.ApplyModifier(SecondModifier);
        await storeBeforeTheRestart.SaveFrameAsync(SESSION_ID, frame);

        // The process stops. The store instance is unreachable and its pooled connections are
        // returned, so nothing of the game is in memory any more: the database file is all that is
        // left, which is the only thing a restart has to read from.
        SqliteConnection.ClearAllPools();

        _storeAfterTheRestart = new SqliteSessionStore(ConnectionString);
    }

    [Fact]
    public async Task The_session_should_come_back_with_its_gm_players_and_state()
    {
        //Act
        var session = await _storeAfterTheRestart.GetAsync(SESSION_ID);

        //Assert
        Assert.NotNull(session);
        Assert.Equal(SESSION_ID, session.Id);
        Assert.Equal(Gm, session.GM);
        Assert.Equal(new[] { FirstPlayer, SecondPlayer }, session.Players);
        Assert.Equal(SessionState.AwaitingPlayerAbility, session.State);
    }

    [Fact]
    public async Task The_contest_frame_should_come_back_with_its_ability_and_modifiers()
    {
        //Act
        var frame = await _storeAfterTheRestart.GetFrameAsync(SESSION_ID);

        //Assert
        Assert.NotNull(frame);
        Assert.Equal(PRIZE, frame.Prize);
        Assert.Equal(Resistance, frame.Resistance);
        Assert.Equal(PLAYER_ABILITY, frame.PlayerAbilityName);
        Assert.Equal(PlayerRating, frame.PlayerRating);
        Assert.Equal(new[] { FirstModifier, SecondModifier }, frame.Modifiers);
    }

    [Fact]
    public async Task The_restored_contest_should_still_be_resolvable()
    {
        //Act
        var frame = await _storeAfterTheRestart.GetFrameAsync(SESSION_ID);

        //Assert
        // Before ADR-0010 a restored session could only ever answer "No contest has been framed".
        // The game state coming back whole is the difference, so it is asserted as the game sees it
        // and not only field by field.
        Assert.NotNull(frame);
        Assert.True(frame.IsReadyForResolution);
        Assert.Equal(TargetNumber.FromRating(PlayerRating, FirstModifier.Value + SecondModifier.Value),
            frame.GetPlayerTargetNumber());
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
