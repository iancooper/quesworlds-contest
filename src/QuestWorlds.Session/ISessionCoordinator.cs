namespace QuestWorlds.Session;

/// <summary>
/// Coordinates session creation, joining, and management for QuestWorlds contests.
/// </summary>
public interface ISessionCoordinator
{
    /// <summary>
    /// Creates a new session with the specified GM.
    /// </summary>
    /// <param name="gmName">The name of the Game Master.</param>
    /// <param name="connectionId">The SignalR connection ID of the GM.</param>
    /// <returns>The newly created session.</returns>
    Session CreateSession(string gmName, string connectionId);

    /// <summary>
    /// Gets an existing session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID to look up.</param>
    /// <returns>The session if found, or null if not found.</returns>
    Session? GetSession(string sessionId);

    /// <summary>
    /// Adds a player to an existing session.
    /// </summary>
    /// <param name="sessionId">The session ID to join.</param>
    /// <param name="playerName">The name of the player joining.</param>
    /// <param name="connectionId">The SignalR connection ID of the player.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session is not found.</exception>
    void JoinSession(string sessionId, string playerName, string connectionId);

    /// <summary>
    /// Gets all participant connection IDs for a session (GM and all players).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>An enumerable of connection IDs, or empty if the session is not found.</returns>
    IEnumerable<string> GetParticipantConnectionIds(string sessionId);
}
