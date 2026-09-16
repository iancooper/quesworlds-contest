namespace QuestWorlds.SqliteSessionStore.Tests;

using System.Collections;
using System.Reflection;
using Microsoft.Data.Sqlite;
using QuestWorlds.Framing;
using QuestWorlds.Session;

// The alias has to sit inside the namespace declaration: at global scope the enclosing QuestWorlds
// namespace is searched first, finds the QuestWorlds.Session namespace, and an unqualified Session
// is CS0118.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Nothing is lost on the way to a row and back: every property of a stored session and its frame
/// reads back as it was written, and every enum value is stored as its name.
/// </summary>
/// <remarks>
/// The comparison is over **every public property**, found by reflection rather than listed, because
/// the risk this test exists for is a field added to <see cref="Session"/> or
/// <see cref="ContestFrame"/> later and silently not persisted. A list of spot checks would still
/// pass the day that happens; a walk over the properties fails, which is the point (ADR-0009 risks).
/// </remarks>
public class When_a_fully_populated_session_is_stored_and_read_should_be_unchanged : IAsyncLifetime
{
    private const string SESSION_ID = "ABC123";
    private const string PRIZE = "Sneak past the guards at the Duke's gate";
    private const string PLAYER_ABILITY = "Thief of Nochet";

    private static readonly Participant Gm = new("Greybeard the GM", ParticipantRole.GM, "gm-connection");
    private static readonly Participant FirstPlayer = new("Vasana", ParticipantRole.Player, "player-connection-1");
    private static readonly Participant SecondPlayer = new("Yanioth", ParticipantRole.Player, "player-connection-2");

    // Every part of both value types carries a value that is not its default, so a column that is
    // never written, or written into the wrong place, cannot pass by accident.
    private static readonly TargetNumber Resistance = new(17, 2, -5);
    private static readonly Rating PlayerRating = new(15, 3);

    private static readonly Modifier[] Modifiers =
    [
        new(ModifierType.Augment, 10),
        new(ModifierType.Stretch, -5),
        new(ModifierType.BenefitConsequence, -10)
    ];

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-roundtrip-{Guid.NewGuid():N}.db");

    private SqliteSessionStore _store = null!;

    private string ConnectionString => $"Data Source={_databasePath}";

    public async Task InitializeAsync()
    {
        //Arrange
        await SqliteSessionStoreInitialiser.InitialiseAsync(ConnectionString);
        _store = new SqliteSessionStore(ConnectionString);
    }

    [Fact]
    public async Task Every_property_of_a_session_should_survive_the_round_trip()
    {
        //Arrange
        var session = Session.Rehydrate(
            SESSION_ID, Gm, new[] { FirstPlayer, SecondPlayer }, SessionState.ResolvingContest);

        //Act
        await _store.SaveAsync(session);

        //Assert
        var stored = await _store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        AssertEveryPropertyMatches(session, stored);
    }

    [Fact]
    public async Task Every_property_of_a_contest_frame_should_survive_the_round_trip()
    {
        //Arrange
        var frame = new ContestFrame(PRIZE, Resistance);
        frame.SetPlayerAbility(PLAYER_ABILITY, PlayerRating);
        foreach (var modifier in Modifiers) frame.ApplyModifier(modifier);

        //Act
        await _store.SaveFrameAsync(SESSION_ID, frame);

        //Assert
        var stored = await _store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(stored);
        AssertEveryPropertyMatches(frame, stored);
    }

    [Fact]
    public async Task A_frame_the_player_has_not_answered_yet_should_survive_with_its_nulls()
    {
        //Arrange
        // The state between framing and submission. The nullable columns have to come back null
        // rather than as a zero rating, which would read as an ability of base 0.
        var framedButUnanswered = new ContestFrame(PRIZE, Resistance);

        //Act
        await _store.SaveFrameAsync(SESSION_ID, framedButUnanswered);

        //Assert
        var stored = await _store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(stored);
        AssertEveryPropertyMatches(framedButUnanswered, stored);
    }

    [Theory]
    [MemberData(nameof(EverySessionState))]
    public async Task Every_session_state_should_be_stored_as_its_name(SessionState state)
    {
        //Arrange
        var session = Session.Rehydrate(SESSION_ID, Gm, Array.Empty<Participant>(), state);

        //Act
        await _store.SaveAsync(session);

        //Assert
        // Both halves matter. That it reads back is the round trip; that the column holds the *name*
        // is what makes reordering the enum safe, because an ordinal would be reinterpreted in
        // silence by the next build (ADR-0009 D3).
        var stored = await _store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(state, stored.State);
        Assert.Equal(state.ToString(),
            await ScalarAsync("SELECT State FROM Sessions WHERE Id = @sessionId;", ("@sessionId", SESSION_ID)));
    }

    [Theory]
    [MemberData(nameof(EveryModifierType))]
    public async Task Every_modifier_type_should_be_stored_as_its_name(ModifierType type)
    {
        //Arrange
        // -5 is legal for every type, including Stretch, which may only be negative.
        var frame = new ContestFrame(PRIZE, Resistance);
        frame.ApplyModifier(new Modifier(type, -5));

        //Act
        await _store.SaveFrameAsync(SESSION_ID, frame);

        //Assert
        var stored = await _store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(type, Assert.Single(stored.Modifiers).Type);
        Assert.Equal(type.ToString(),
            await ScalarAsync("SELECT Type FROM ContestModifiers WHERE SessionId = @sessionId;", ("@sessionId", SESSION_ID)));
    }

    [Fact]
    public async Task A_session_state_this_build_does_not_know_should_fail_loudly()
    {
        //Arrange
        // A row written by a build that had a state this one does not, which is what makes the
        // failure mode worth pinning: the alternative to a loud failure is answering a question
        // about a session nobody can actually read.
        await ExecuteAsync("""
            INSERT INTO Sessions (Id, State) VALUES (@sessionId, 'Bewildered');
            INSERT INTO Participants (SessionId, Ordinal, Name, Role, ConnectionId)
            VALUES (@sessionId, 0, @name, 'GM', @connectionId);
            """,
            ("@sessionId", SESSION_ID), ("@name", Gm.Name), ("@connectionId", Gm.ConnectionId));

        //Act
        var readingAStateThisBuildDoesNotHave = () => _store.GetAsync(SESSION_ID);

        //Assert
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(readingAStateThisBuildDoesNotHave);
        Assert.Contains("Bewildered", failure.Message);
        Assert.Contains(nameof(SessionState.ResolvingContest), failure.Message);
    }

    [Fact]
    public async Task A_modifier_type_this_build_does_not_know_should_fail_loudly()
    {
        //Arrange
        await ExecuteAsync("""
            INSERT INTO ContestFrames (
                SessionId, Prize, ResistanceBase, ResistanceMasteries, ResistanceModifier,
                PlayerAbilityName, PlayerRatingBase, PlayerRatingMasteries)
            VALUES (@sessionId, @prize, 17, 2, -5, NULL, NULL, NULL);
            INSERT INTO ContestModifiers (SessionId, Ordinal, Type, Value)
            VALUES (@sessionId, 0, 'Brilliance', 5);
            """,
            ("@sessionId", SESSION_ID), ("@prize", PRIZE));

        //Act
        var readingAModifierThisBuildDoesNotHave = () => _store.GetFrameAsync(SESSION_ID);

        //Assert
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(readingAModifierThisBuildDoesNotHave);
        Assert.Contains("Brilliance", failure.Message);
    }

    public static IEnumerable<object[]> EverySessionState() =>
        Enum.GetValues<SessionState>().Select(state => new object[] { state });

    public static IEnumerable<object[]> EveryModifierType() =>
        Enum.GetValues<ModifierType>().Select(type => new object[] { type });

    // Compares what was stored with what came back over every public property of the type, so that a
    // property added tomorrow is compared tomorrow without anyone remembering to add it here.
    private static void AssertEveryPropertyMatches(object stored, object readBack)
    {
        var properties = stored.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.NotEmpty(properties);

        var lost = properties
            .Where(property => !Matches(property.GetValue(stored), property.GetValue(readBack)))
            .Select(property => $"{property.Name}: stored '{Describe(property.GetValue(stored))}', " +
                                $"read back '{Describe(property.GetValue(readBack))}'")
            .ToList();

        Assert.True(lost.Count == 0,
            $"{stored.GetType().Name} did not survive the round trip unchanged. The mapping and the " +
            $"schema have to carry every property, and these did not arrive:{Environment.NewLine}" +
            string.Join(Environment.NewLine, lost));
    }

    private static bool Matches(object? stored, object? readBack) =>
        stored is IEnumerable storedItems and not string
            ? readBack is IEnumerable readBackItems and not string &&
              storedItems.Cast<object>().SequenceEqual(readBackItems.Cast<object>())
            : Equals(stored, readBack);

    private static string Describe(object? value) =>
        value is IEnumerable items and not string
            ? string.Join(", ", items.Cast<object>())
            : value?.ToString() ?? "null";

    // Raw SQL, because what is being asserted is what the column holds, and because a row written by
    // another build is the condition two of the cases need. Parameterised even here: the prize
    // carries an apostrophe on purpose, so that the store's own path is shown handling one.
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var command = Command(connection, sql, parameters);
        return await command.ExecuteScalarAsync();
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var command = Command(connection, sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static SqliteCommand Command(
        SqliteConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);

        return command;
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
