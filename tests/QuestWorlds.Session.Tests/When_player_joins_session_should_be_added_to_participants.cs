using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_player_joins_session_should_be_added_to_participants
{
    [Fact]
    public async Task Player_should_be_added_to_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        var playerName = "Player One";
        var playerConnectionId = "player-connection-1";

        // Act
        await coordinator.JoinSessionAsync(session.Id, playerName, playerConnectionId);

        // Assert
        Assert.Single(session.Players);
    }

    [Fact]
    public async Task Player_should_have_correct_name()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        var playerName = "Player One";

        // Act
        await coordinator.JoinSessionAsync(session.Id, playerName, "player-connection");

        // Assert
        Assert.Equal(playerName, session.Players[0].Name);
    }

    [Fact]
    public async Task Player_should_have_player_role()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        await coordinator.JoinSessionAsync(session.Id, "Player One", "player-connection");

        // Assert
        Assert.Equal(ParticipantRole.Player, session.Players[0].Role);
    }

    [Fact]
    public async Task Player_should_have_correct_connection_id()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        var connectionId = "player-connection-123";

        // Act
        await coordinator.JoinSessionAsync(session.Id, "Player One", connectionId);

        // Assert
        Assert.Equal(connectionId, session.Players[0].ConnectionId);
    }

    [Fact]
    public async Task Multiple_players_can_join_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        await coordinator.JoinSessionAsync(session.Id, "Player One", "connection-1");
        await coordinator.JoinSessionAsync(session.Id, "Player Two", "connection-2");
        await coordinator.JoinSessionAsync(session.Id, "Player Three", "connection-3");

        // Assert
        Assert.Equal(3, session.Players.Count);
        Assert.Contains(session.Players, p => p.Name == "Player One");
        Assert.Contains(session.Players, p => p.Name == "Player Two");
        Assert.Contains(session.Players, p => p.Name == "Player Three");
    }

    [Fact]
    public async Task GetParticipantConnectionIds_should_include_all_participants()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = await coordinator.CreateSessionAsync("GM Name", "gm-connection");
        await coordinator.JoinSessionAsync(session.Id, "Player One", "player-connection-1");
        await coordinator.JoinSessionAsync(session.Id, "Player Two", "player-connection-2");

        // Act
        var connectionIds = (await coordinator.GetParticipantConnectionIdsAsync(session.Id)).ToList();

        // Assert
        Assert.Equal(3, connectionIds.Count);
        Assert.Contains("gm-connection", connectionIds);
        Assert.Contains("player-connection-1", connectionIds);
        Assert.Contains("player-connection-2", connectionIds);
    }
}
