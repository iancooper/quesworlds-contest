using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_joining_with_invalid_session_id_should_throw
{
    [Fact]
    public void JoinSession_with_nonexistent_id_should_throw()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => coordinator.JoinSession(nonExistentSessionId, "Player", "connection-1")
        );
        Assert.Contains(nonExistentSessionId, exception.Message);
    }

    [Fact]
    public void GetSession_with_nonexistent_id_should_return_null()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act
        var session = coordinator.GetSession(nonExistentSessionId);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void GetSession_with_valid_id_should_return_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var createdSession = coordinator.CreateSession("GM Name", "gm-connection");

        // Act
        var retrievedSession = coordinator.GetSession(createdSession.Id);

        // Assert
        Assert.NotNull(retrievedSession);
        Assert.Equal(createdSession.Id, retrievedSession.Id);
    }

    [Fact]
    public void GetParticipantConnectionIds_with_nonexistent_id_should_return_empty()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act
        var connectionIds = coordinator.GetParticipantConnectionIds(nonExistentSessionId);

        // Assert
        Assert.Empty(connectionIds);
    }
}
