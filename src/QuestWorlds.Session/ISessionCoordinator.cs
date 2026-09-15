namespace QuestWorlds.Session;

/// <summary>
/// Coordinates session creation, joining, and management for QuestWorlds contests.
/// </summary>
public interface ISessionCoordinator
{
    /// <summary>
    /// Creates a new session with the specified GM and stores it.
    /// </summary>
    /// <param name="gmName">The name of the Game Master.</param>
    /// <param name="connectionId">The SignalR connection ID of the GM.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the storage of the session. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is the newly created <see cref="Session"/>.</returns>
    Task<Session> CreateSessionAsync(string gmName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID to look up.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the lookup. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is the <see cref="Session"/> if found, or null if not found.</returns>
    Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a player to an existing session.
    /// </summary>
    /// <param name="sessionId">The session ID to join.</param>
    /// <param name="playerName">The name of the player joining.</param>
    /// <param name="connectionId">The SignalR connection ID of the player.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the join. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the player has joined.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not found.</exception>
    Task JoinSessionAsync(string sessionId, string playerName, string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a session to a new state and stores the result.
    /// </summary>
    /// <param name="sessionId">The session ID to transition.</param>
    /// <param name="newState">The <see cref="SessionState"/> to move the session to.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the transition. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the new state has been stored.</returns>
    /// <remarks>
    /// Transition through here rather than calling <see cref="Session.TransitionTo"/> on a session you were handed.
    /// A store may return a copy, so a transition applied to that copy changes nothing that anyone will read back.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the session is not found.</exception>
    Task TransitionSessionStateAsync(string sessionId, SessionState newState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all participant connection IDs for a session (GM and all players).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the lookup. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is an enumerable of connection IDs, or empty if the session is not found.</returns>
    Task<IEnumerable<string>> GetParticipantConnectionIdsAsync(string sessionId, CancellationToken cancellationToken = default);
}
