namespace QuestWorlds.SessionStore.ContractTests;

using QuestWorlds.Framing;
using QuestWorlds.Session;

// QuestWorlds.Session is a namespace as well as a type, and this namespace sits beside it under
// QuestWorlds, so an unqualified Session binds to the namespace. The alias says which we mean, and
// it has to live inside the namespace declaration to win against the enclosing one.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// The obligations every store owes its callers, across both ports, written once and run against
/// each store (ADR-0008 D5).
/// </summary>
/// <typeparam name="TStore">The store under test, which must satisfy both ports. One class implements
/// both, so that a session and its frame can be kept consistent (ADR-0010 D4), and the constraint
/// here is what lets a case save through one port and read through the other.</typeparam>
/// <remarks>
/// Derive one class per store in that store's own test project and implement
/// <see cref="CreateStore"/>; xUnit discovers the inherited cases on the subclass, so a failure
/// names the store that broke.
/// </remarks>
public abstract class SessionStoreContract<TStore> where TStore : IAmASessionStore, IAmAContestFrameStore
{
    private const string SESSION_ID = "ABC123";
    private const string UNKNOWN_SESSION_ID = "NOBODY";

    private static readonly Participant Gm = new("The GM", ParticipantRole.GM, "gm-connection");
    private static readonly Participant FirstPlayer = new("First Player", ParticipantRole.Player, "player-connection-1");
    private static readonly Participant SecondPlayer = new("Second Player", ParticipantRole.Player, "player-connection-2");
    private static readonly Participant ThirdPlayer = new("Third Player", ParticipantRole.Player, "player-connection-3");

    private const string OTHER_SESSION_ID = "XYZ789";
    private const string PRIZE = "Sneak past the guards";
    private const string OTHER_PRIZE = "Talk the Duke round";

    private static readonly TargetNumber Resistance = new(14);

    /// <summary>
    /// Returns the store under test, empty, holding no sessions.
    /// </summary>
    /// <remarks>
    /// Called once per case. A store that owns external state — a file, a connection — should
    /// give each call its own, so that cases cannot see each other's sessions.
    /// </remarks>
    protected abstract TStore CreateStore();

    [Fact]
    public async Task Saving_a_session_not_yet_held_should_store_it()
    {
        //Arrange
        var store = CreateStore();
        var session = new Session(SESSION_ID, Gm);

        //Act
        await store.SaveAsync(session);

        //Assert
        var stored = await store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(SESSION_ID, stored.Id);
        Assert.Equal(Gm, stored.GM);
    }

    [Fact]
    public async Task Saving_a_session_already_held_should_replace_it()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));

        var sameSessionFurtherOn = Session.Rehydrate(SESSION_ID, Gm, new[] { FirstPlayer }, SessionState.FramingContest);

        //Act
        await store.SaveAsync(sameSessionFurtherOn);

        //Assert
        var stored = await store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(SessionState.FramingContest, stored.State);
        Assert.Equal(FirstPlayer, Assert.Single(stored.Players));
    }

    [Fact]
    public async Task Getting_a_session_that_is_not_held_should_return_null()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));

        //Act
        var stored = await store.GetAsync(UNKNOWN_SESSION_ID);

        //Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task Removing_a_session_that_is_held_should_leave_nothing_to_get()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));

        //Act
        await store.RemoveAsync(SESSION_ID);

        //Assert
        Assert.Null(await store.GetAsync(SESSION_ID));
    }

    [Fact]
    public async Task Removing_a_session_that_is_not_held_should_succeed()
    {
        //Arrange
        var store = CreateStore();

        //Act
        var removingWhatIsNotThere = () => store.RemoveAsync(UNKNOWN_SESSION_ID);

        //Assert
        await removingWhatIsNotThere();
    }

    [Fact]
    public async Task Mutating_a_session_that_was_got_should_not_change_what_is_stored()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));
        var mine = await store.GetAsync(SESSION_ID);
        Assert.NotNull(mine);

        //Act
        mine.AddPlayer(FirstPlayer);
        mine.TransitionTo(SessionState.FramingContest);

        //Assert
        var stored = await store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Empty(stored.Players);
        Assert.Equal(SessionState.WaitingForPlayers, stored.State);
    }

    [Fact]
    public async Task A_session_with_no_players_should_round_trip()
    {
        //Arrange
        var store = CreateStore();
        var noOneHasJoinedYet = Session.Rehydrate(SESSION_ID, Gm, Array.Empty<Participant>(), SessionState.WaitingForPlayers);

        //Act
        await store.SaveAsync(noOneHasJoinedYet);

        //Assert
        var stored = await store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Empty(stored.Players);
    }

    [Fact]
    public async Task A_session_with_several_players_should_round_trip_in_order()
    {
        //Arrange
        var store = CreateStore();
        var joinedInOrder = new[] { FirstPlayer, SecondPlayer, ThirdPlayer };
        var session = Session.Rehydrate(SESSION_ID, Gm, joinedInOrder, SessionState.AwaitingPlayerAbility);

        //Act
        await store.SaveAsync(session);

        //Assert
        var stored = await store.GetAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(joinedInOrder, stored.Players);
    }

    [Fact]
    public async Task Saving_a_frame_should_make_it_gettable_for_that_session()
    {
        //Arrange
        var store = CreateStore();

        //Act
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));

        //Assert
        var stored = await store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Equal(PRIZE, stored.Prize);
        Assert.Equal(Resistance, stored.Resistance);
    }

    [Fact]
    public async Task Getting_a_frame_for_a_session_that_has_none_should_return_null()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));

        //Act
        var stored = await store.GetFrameAsync(SESSION_ID);

        //Assert
        Assert.Null(stored);
    }

    [Fact]
    public async Task Clearing_a_frame_should_leave_the_session_without_one()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));

        //Act
        await store.ClearFrameAsync(SESSION_ID);

        //Assert
        Assert.Null(await store.GetFrameAsync(SESSION_ID));
    }

    [Fact]
    public async Task Clearing_a_frame_for_a_session_that_has_none_should_succeed()
    {
        //Arrange
        var store = CreateStore();

        //Act
        var clearingWhatIsNotThere = () => store.ClearFrameAsync(SESSION_ID);

        //Assert
        await clearingWhatIsNotThere();
    }

    [Fact]
    public async Task A_session_and_its_frame_should_both_be_readable_after_both_are_saved()
    {
        //Arrange
        var store = CreateStore();
        var midContest = Session.Rehydrate(SESSION_ID, Gm, new[] { FirstPlayer }, SessionState.AwaitingPlayerAbility);

        //Act
        await store.SaveAsync(midContest);
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));

        //Assert
        var storedSession = await store.GetAsync(SESSION_ID);
        var storedFrame = await store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(storedSession);
        Assert.NotNull(storedFrame);
        Assert.Equal(SessionState.AwaitingPlayerAbility, storedSession.State);
        Assert.Equal(PRIZE, storedFrame.Prize);
    }

    [Fact]
    public async Task Removing_a_session_should_remove_its_frame_too()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveAsync(new Session(SESSION_ID, Gm));
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));

        //Act
        await store.RemoveAsync(SESSION_ID);

        //Assert
        Assert.Null(await store.GetFrameAsync(SESSION_ID));
    }

    [Fact]
    public async Task Mutating_a_frame_that_was_got_should_not_change_what_is_stored()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));
        var mine = await store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(mine);

        //Act
        mine.SetPlayerAbility("Thief of Nochet", new Rating(15));
        mine.ApplyModifier(new Modifier(ModifierType.Augment, 5));

        //Assert
        var stored = await store.GetFrameAsync(SESSION_ID);
        Assert.NotNull(stored);
        Assert.Null(stored.PlayerAbilityName);
        Assert.Empty(stored.Modifiers);
    }

    [Fact]
    public async Task Frames_for_two_sessions_should_not_collide()
    {
        //Arrange
        var store = CreateStore();
        await store.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));
        await store.SaveFrameAsync(OTHER_SESSION_ID, new ContestFrame(OTHER_PRIZE, Resistance));

        //Act
        await store.ClearFrameAsync(SESSION_ID);

        //Assert
        var survivor = await store.GetFrameAsync(OTHER_SESSION_ID);
        Assert.NotNull(survivor);
        Assert.Equal(OTHER_PRIZE, survivor.Prize);
    }
}
