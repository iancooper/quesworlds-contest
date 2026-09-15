using System.Collections.Concurrent;

namespace QuestWorlds.Session;

internal class InMemorySessionStore : IAmASessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? CopyOf(session) : null);

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    private static Session CopyOf(Session session) =>
        Session.Rehydrate(session.Id, session.GM, session.Players, session.State);
}
