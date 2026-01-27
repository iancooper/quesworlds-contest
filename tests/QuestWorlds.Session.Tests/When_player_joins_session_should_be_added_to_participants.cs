using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_player_joins_session_should_be_added_to_participants
{
    [Fact]
    public void Player_should_be_added_to_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");
        var playerName = "Player One";
        var playerConnectionId = "player-connection-1";

        // Act
        coordinator.JoinSession(session.Id, playerName, playerConnectionId);

        // Assert
        Assert.Single(session.Players);
    }

    [Fact]
    public void Player_should_have_correct_name()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");
        var playerName = "Player One";

        // Act
        coordinator.JoinSession(session.Id, playerName, "player-connection");

        // Assert
        Assert.Equal(playerName, session.Players[0].Name);
    }

    [Fact]
    public void Player_should_have_player_role()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");

        // Act
        coordinator.JoinSession(session.Id, "Player One", "player-connection");

        // Assert
        Assert.Equal(ParticipantRole.Player, session.Players[0].Role);
    }

    [Fact]
    public void Player_should_have_correct_connection_id()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");
        var connectionId = "player-connection-123";

        // Act
        coordinator.JoinSession(session.Id, "Player One", connectionId);

        // Assert
        Assert.Equal(connectionId, session.Players[0].ConnectionId);
    }

    [Fact]
    public void Multiple_players_can_join_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");

        // Act
        coordinator.JoinSession(session.Id, "Player One", "connection-1");
        coordinator.JoinSession(session.Id, "Player Two", "connection-2");
        coordinator.JoinSession(session.Id, "Player Three", "connection-3");

        // Assert
        Assert.Equal(3, session.Players.Count);
        Assert.Contains(session.Players, p => p.Name == "Player One");
        Assert.Contains(session.Players, p => p.Name == "Player Two");
        Assert.Contains(session.Players, p => p.Name == "Player Three");
    }

    [Fact]
    public void GetParticipantConnectionIds_should_include_all_participants()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var session = coordinator.CreateSession("GM Name", "gm-connection");
        coordinator.JoinSession(session.Id, "Player One", "player-connection-1");
        coordinator.JoinSession(session.Id, "Player Two", "player-connection-2");

        // Act
        var connectionIds = coordinator.GetParticipantConnectionIds(session.Id).ToList();

        // Assert
        Assert.Equal(3, connectionIds.Count);
        Assert.Contains("gm-connection", connectionIds);
        Assert.Contains("player-connection-1", connectionIds);
        Assert.Contains("player-connection-2", connectionIds);
    }
}
