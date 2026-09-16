namespace QuestWorlds.InMemorySessionStore;

using System.Collections.Concurrent;
using QuestWorlds.Framing;
using QuestWorlds.Session;

// QuestWorlds.Session is a sibling namespace, so an unqualified Session inside this one
// binds to the namespace rather than the type. The alias says which we mean.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Holds sessions and their contest frames in memory, for a deployment that does not need them to
/// survive a restart.
/// </summary>
/// <remarks>
/// Register it as a singleton, once, behind both ports: it is the state, so a second instance is a
/// second set of sessions and a frame the session store cannot see (ADR-0010 D4).
/// <see cref="GetAsync"/> returns a copy, exactly as a database-backed store must, so that a caller
/// which forgets to save fails here too and not only in the deployment that persists (ADR-0008 D7).
/// </remarks>
public class InMemorySessionStore : IAmASessionStore, IAmAContestFrameStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, ContestFrame> _frames = new();

    /// <inheritdoc />
    public Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? CopyOf(session) : null);

    /// <inheritdoc />
    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);

        // A frame has no life outside its session, so leaving one behind is an orphan nothing can
        // reach and nothing will clean up. The SQLite store gets this from ON DELETE CASCADE.
        _frames.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveFrameAsync(string sessionId, ContestFrame frame, CancellationToken cancellationToken = default)
    {
        _frames[sessionId] = frame;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ContestFrame?> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_frames.TryGetValue(sessionId, out var frame) ? CopyOf(frame) : null);

    /// <inheritdoc />
    public Task ClearFrameAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _frames.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    private static Session CopyOf(Session session) =>
        Session.Rehydrate(session.Id, session.GM, session.Players, session.State);

    private static ContestFrame CopyOf(ContestFrame frame) =>
        ContestFrame.Rehydrate(frame.Prize, frame.Resistance, frame.PlayerAbilityName, frame.PlayerRating, frame.Modifiers);
}
