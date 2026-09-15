namespace QuestWorlds.InMemorySessionStore;

using System.Collections.Concurrent;
using QuestWorlds.Session;

// QuestWorlds.Session is a sibling namespace, so an unqualified Session inside this one
// binds to the namespace rather than the type. The alias says which we mean.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Holds sessions in memory, for a deployment that does not need them to survive a restart.
/// </summary>
/// <remarks>
/// Register it as a singleton: it is the state, so a second instance is a second set of sessions.
/// <see cref="GetAsync"/> returns a copy, exactly as a database-backed store must, so that a caller
/// which forgets to save fails here too and not only in the deployment that persists (ADR-0008 D7).
/// </remarks>
public class InMemorySessionStore : IAmASessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

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
        return Task.CompletedTask;
    }

    private static Session CopyOf(Session session) =>
        Session.Rehydrate(session.Id, session.GM, session.Players, session.State);
}
