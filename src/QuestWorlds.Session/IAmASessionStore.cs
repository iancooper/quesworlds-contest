namespace QuestWorlds.Session;

/// <summary>
/// Stores the sessions a deployment knows about. Implemented by a store module;
/// QuestWorlds.Session declares this port and implements none of it.
/// </summary>
/// <remarks>
/// Hub callbacks for one session can arrive concurrently, so an implementation must
/// tolerate concurrent calls. It is not asked to arbitrate two concurrent writes to
/// the same session; nothing here promises that the later write wins.
/// </remarks>
public interface IAmASessionStore
{
    /// <summary>
    /// Stores <paramref name="session"/>, replacing any session already held under the same id.
    /// </summary>
    /// <param name="session">The <see cref="Session"/> to store.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the save. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the session has been stored.</returns>
    /// <remarks>
    /// This is an upsert. Implementations must not distinguish between a first save and a later
    /// one, and must not fail because a session does or does not already exist: the caller does
    /// not know which it is.
    /// </remarks>
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the session held under <paramref name="sessionId"/>, if there is one.
    /// </summary>
    /// <param name="sessionId">The session ID to look up.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the lookup. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is the <see cref="Session"/> if found, or null if not found.</returns>
    /// <remarks>
    /// The result may be a copy. Callers must not assume that mutating it changes what is stored;
    /// they must call <see cref="SaveAsync"/>. Any store that is not in-process returns a copy, so
    /// a caller that relies on a live reference works only by accident of configuration.
    /// </remarks>
    Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the session held under <paramref name="sessionId"/>.
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the removal. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the session is no longer held.</returns>
    /// <remarks>
    /// This is idempotent. Removing a session that is not held is not an error.
    /// </remarks>
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
