using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

public class When_joining_with_invalid_session_id_should_throw
{
    [Fact]
    public async Task JoinSession_with_nonexistent_id_should_throw()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.JoinSessionAsync(nonExistentSessionId, "Player", "connection-1")
        );
        Assert.Contains(nonExistentSessionId, exception.Message);
    }

    [Fact]
    public async Task GetSession_with_nonexistent_id_should_return_null()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act
        var session = await coordinator.GetSessionAsync(nonExistentSessionId);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task GetSession_with_valid_id_should_return_session()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var createdSession = await coordinator.CreateSessionAsync("GM Name", "gm-connection");

        // Act
        var retrievedSession = await coordinator.GetSessionAsync(createdSession.Id);

        // Assert
        Assert.NotNull(retrievedSession);
        Assert.Equal(createdSession.Id, retrievedSession.Id);
    }

    [Fact]
    public async Task GetParticipantConnectionIds_with_nonexistent_id_should_return_empty()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var nonExistentSessionId = "XXXXXX";

        // Act
        var connectionIds = await coordinator.GetParticipantConnectionIdsAsync(nonExistentSessionId);

        // Assert
        Assert.Empty(connectionIds);
    }
}
