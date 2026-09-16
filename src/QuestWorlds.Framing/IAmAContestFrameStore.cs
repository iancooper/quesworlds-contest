namespace QuestWorlds.Framing;

/// <summary>
/// Stores the contest frame a session has in progress. Implemented by a store module;
/// QuestWorlds.Framing declares this port and implements none of it.
/// </summary>
/// <remarks>
/// A frame is keyed by session id and has no life outside one session, so a store that holds
/// sessions holds their frames too (ADR-0010 D4). Hub callbacks for one session can arrive
/// concurrently, so an implementation must tolerate concurrent calls.
/// </remarks>
public interface IAmAContestFrameStore
{
    /// <summary>
    /// Stores <paramref name="frame"/> as the contest the session has in progress, replacing any frame already held for it.
    /// </summary>
    /// <param name="sessionId">The id of the session the frame belongs to.</param>
    /// <param name="frame">The <see cref="ContestFrame"/> to store.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the save. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the frame has been stored.</returns>
    /// <remarks>
    /// This is an upsert, for the same reason <c>IAmASessionStore.SaveAsync</c> is: the caller does
    /// not know whether the session already has a frame, and must not have to.
    /// </remarks>
    Task SaveFrameAsync(string sessionId, ContestFrame frame, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the contest frame the session has in progress, if there is one.
    /// </summary>
    /// <param name="sessionId">The id of the session to look up.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the lookup. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> whose result is the <see cref="ContestFrame"/> if the session has one, or null if it does not.</returns>
    /// <remarks>
    /// A session between contests has no frame, and that is not an error: the answer is null.
    /// </remarks>
    Task<ContestFrame?> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the contest frame the session has in progress.
    /// </summary>
    /// <param name="sessionId">The id of the session whose frame is to be discarded.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels the removal. Optional; defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the session no longer has a frame.</returns>
    /// <remarks>
    /// This is idempotent. Clearing a frame for a session that has none is not an error.
    /// </remarks>
    Task ClearFrameAsync(string sessionId, CancellationToken cancellationToken = default);
}
