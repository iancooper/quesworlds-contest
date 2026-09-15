using QuestWorlds.Session;

namespace QuestWorlds.Session.Tests;

// The store's namespace and its type share a name, so an unqualified InMemorySessionStore
// under QuestWorlds binds to the namespace. The alias says which we mean.
using InMemorySessionStore = QuestWorlds.InMemorySessionStore.InMemorySessionStore;

public class When_creating_session_should_generate_unique_id
{
    private const string VALID_CHARACTERS = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int EXPECTED_ID_LENGTH = 6;

    [Fact]
    public async Task Session_should_have_non_empty_id()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session = await coordinator.CreateSessionAsync("GM Name", "connection-1");

        // Assert
        Assert.False(string.IsNullOrEmpty(session.Id));
    }

    [Fact]
    public async Task Session_id_should_be_six_characters()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session = await coordinator.CreateSessionAsync("GM Name", "connection-1");

        // Assert
        Assert.Equal(EXPECTED_ID_LENGTH, session.Id.Length);
    }

    [Fact]
    public async Task Session_id_should_contain_only_valid_characters()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session = await coordinator.CreateSessionAsync("GM Name", "connection-1");

        // Assert
        Assert.All(session.Id, c => Assert.Contains(c, VALID_CHARACTERS));
    }

    [Fact]
    public async Task Multiple_sessions_should_have_different_ids()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session1 = await coordinator.CreateSessionAsync("GM One", "connection-1");
        var session2 = await coordinator.CreateSessionAsync("GM Two", "connection-2");
        var session3 = await coordinator.CreateSessionAsync("GM Three", "connection-3");

        // Assert
        var ids = new[] { session1.Id, session2.Id, session3.Id };
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task Session_should_have_gm_as_participant()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();
        var gmName = "Test GM";
        var connectionId = "connection-1";

        // Act
        var session = await coordinator.CreateSessionAsync(gmName, connectionId);

        // Assert
        Assert.NotNull(session.GM);
        Assert.Equal(gmName, session.GM.Name);
        Assert.Equal(ParticipantRole.GM, session.GM.Role);
        Assert.Equal(connectionId, session.GM.ConnectionId);
    }

    [Fact]
    public async Task New_session_should_be_in_waiting_for_players_state()
    {
        // Arrange
        var coordinator = new SessionCoordinatorBuilder().Build();

        // Act
        var session = await coordinator.CreateSessionAsync("GM Name", "connection-1");

        // Assert
        Assert.Equal(SessionState.WaitingForPlayers, session.State);
    }
}

/// <summary>
/// Builder for creating ISessionCoordinator instances in tests.
/// Encapsulates the construction details, allowing tests to focus on behavior.
/// </summary>
internal class SessionCoordinatorBuilder
{
    public ISessionCoordinator Build()
    {
        return SessionModule.CreateCoordinator(new InMemorySessionStore());
    }
}
