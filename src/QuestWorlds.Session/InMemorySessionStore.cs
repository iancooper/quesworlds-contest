using System.Collections.Concurrent;

namespace QuestWorlds.Session;

internal class InMemorySessionStore : IAmASessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public void Save(Session session) => _sessions[session.Id] = session;

    public Session? Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session : null;

    public void Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);
}
