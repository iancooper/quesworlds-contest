using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_rehydrating_a_session_should_restore_id_gm_players_and_state
{
    private const string STORED_ID = "ABC123";
    private static readonly Participant StoredGm = new("Stored GM", ParticipantRole.GM, "gm-connection");
    private static readonly Participant FirstPlayer = new("First Player", ParticipantRole.Player, "player-connection-1");
    private static readonly Participant SecondPlayer = new("Second Player", ParticipantRole.Player, "player-connection-2");

    [Fact]
    public void Rehydrated_session_should_have_the_stored_id()
    {
        // Arrange
        var storedPlayers = new[] { FirstPlayer, SecondPlayer };

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, storedPlayers, SessionState.AwaitingPlayerAbility);

        // Assert
        Assert.Equal(STORED_ID, session.Id);
    }

    [Fact]
    public void Rehydrated_session_should_have_the_stored_gm()
    {
        // Arrange
        var storedPlayers = new[] { FirstPlayer, SecondPlayer };

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, storedPlayers, SessionState.AwaitingPlayerAbility);

        // Assert
        Assert.Equal(StoredGm, session.GM);
    }

    [Fact]
    public void Rehydrated_session_should_have_the_stored_state()
    {
        // Arrange
        var storedPlayers = new[] { FirstPlayer, SecondPlayer };

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, storedPlayers, SessionState.AwaitingPlayerAbility);

        // Assert
        Assert.Equal(SessionState.AwaitingPlayerAbility, session.State);
    }

    [Fact]
    public void Rehydrated_session_should_have_the_stored_players_in_the_order_supplied()
    {
        // Arrange
        var storedPlayers = new[] { FirstPlayer, SecondPlayer };

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, storedPlayers, SessionState.AwaitingPlayerAbility);

        // Assert
        Assert.Equal(new[] { FirstPlayer, SecondPlayer }, session.Players);
    }

    [Fact]
    public void Rehydrating_with_no_players_should_give_a_session_with_no_players()
    {
        // Arrange
        var noPlayers = Array.Empty<Participant>();

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, noPlayers, SessionState.WaitingForPlayers);

        // Assert
        Assert.Empty(session.Players);
    }

    [Fact]
    public void Rehydrating_should_accept_the_participant_list_as_stored()
    {
        // Arrange - a participant whose role AddPlayer would reject; the rule was
        // satisfied when the player joined, so loading a row must not re-run it
        var storedParticipants = new[] { new Participant("Stored As Given", ParticipantRole.GM, "connection-3") };

        // Act
        var session = Session.Rehydrate(STORED_ID, StoredGm, storedParticipants, SessionState.AwaitingPlayerAbility);

        // Assert
        Assert.Equal(storedParticipants, session.Players);
    }
}
