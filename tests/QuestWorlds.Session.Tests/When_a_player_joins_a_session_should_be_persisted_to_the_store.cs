using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_a_player_joins_a_session_should_be_persisted_to_the_store
{
    private const string FIRST_PLAYER = "First Player";
    private const string SECOND_PLAYER = "Second Player";

    [Fact]
    public async Task A_player_who_joined_should_be_in_the_session_read_back()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var created = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        await coordinator.JoinSessionAsync(created.Id, FIRST_PLAYER, "player-connection-1");

        // Assert
        var readBack = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(readBack);
        Assert.Equal(FIRST_PLAYER, Assert.Single(readBack.Players).Name);
    }

    [Fact]
    public async Task Both_players_who_joined_should_be_in_the_session_read_back_in_join_order()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var created = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        await coordinator.JoinSessionAsync(created.Id, FIRST_PLAYER, "player-connection-1");
        await coordinator.JoinSessionAsync(created.Id, SECOND_PLAYER, "player-connection-2");

        // Assert
        var readBack = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(readBack);
        Assert.Equal(new[] { FIRST_PLAYER, SECOND_PLAYER }, readBack.Players.Select(p => p.Name));
    }

    [Fact]
    public async Task A_state_transition_should_survive_being_read_back()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var created = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        await coordinator.TransitionSessionStateAsync(created.Id, SessionState.AwaitingPlayerAbility);

        // Assert
        var readBack = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(readBack);
        Assert.Equal(SessionState.AwaitingPlayerAbility, readBack.State);
    }

    [Fact]
    public async Task A_state_transition_should_not_discard_the_players_already_stored()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var created = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        await coordinator.JoinSessionAsync(created.Id, FIRST_PLAYER, "player-connection-1");

        // Act
        await coordinator.TransitionSessionStateAsync(created.Id, SessionState.AwaitingPlayerAbility);

        // Assert
        var readBack = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(readBack);
        Assert.Equal(FIRST_PLAYER, Assert.Single(readBack.Players).Name);
    }

    [Fact]
    public async Task Mutating_a_session_read_back_should_not_change_what_is_stored()
    {
        // Arrange - the store hands back a copy, so a caller who mutates without
        // saving changes nothing; this is what makes a missing save visible
        var coordinator = new SessionCoordinatorBuilder().Build();
        var created = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        var firstRead = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(firstRead);

        // Act
        firstRead.AddPlayer(new Participant("Never Saved", ParticipantRole.Player, "player-connection-1"));

        // Assert
        var secondRead = await coordinator.GetSessionAsync(created.Id);
        Assert.NotNull(secondRead);
        Assert.Empty(secondRead.Players);
    }
}
