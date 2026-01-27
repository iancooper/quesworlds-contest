using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_session_state_changes_should_track_correctly
{
    [Fact]
    public void New_session_should_start_in_waiting_for_players_state()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session = coordinator.CreateSession("GM Name", "gm-connection");

        // Assert
        Assert.Equal(SessionState.WaitingForPlayers, session.State);
    }

    [Fact]
    public void TransitionTo_should_update_state()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");

        // Act
        session.TransitionTo(SessionState.FramingContest);

        // Assert
        Assert.Equal(SessionState.FramingContest, session.State);
    }

    [Fact]
    public void Session_state_enum_should_have_all_required_values()
    {
        // Assert - verify all states from the ADR exist
        Assert.True(Enum.IsDefined(typeof(SessionState), SessionState.WaitingForPlayers));
        Assert.True(Enum.IsDefined(typeof(SessionState), SessionState.FramingContest));
        Assert.True(Enum.IsDefined(typeof(SessionState), SessionState.AwaitingPlayerAbility));
        Assert.True(Enum.IsDefined(typeof(SessionState), SessionState.ResolvingContest));
        Assert.True(Enum.IsDefined(typeof(SessionState), SessionState.ShowingOutcome));
    }

    [Fact]
    public void Session_can_transition_through_all_states()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");

        // Act & Assert - walk through the full state machine
        Assert.Equal(SessionState.WaitingForPlayers, session.State);

        session.TransitionTo(SessionState.FramingContest);
        Assert.Equal(SessionState.FramingContest, session.State);

        session.TransitionTo(SessionState.AwaitingPlayerAbility);
        Assert.Equal(SessionState.AwaitingPlayerAbility, session.State);

        session.TransitionTo(SessionState.ResolvingContest);
        Assert.Equal(SessionState.ResolvingContest, session.State);

        session.TransitionTo(SessionState.ShowingOutcome);
        Assert.Equal(SessionState.ShowingOutcome, session.State);

        // Can go back to framing for next contest
        session.TransitionTo(SessionState.FramingContest);
        Assert.Equal(SessionState.FramingContest, session.State);
    }
}
