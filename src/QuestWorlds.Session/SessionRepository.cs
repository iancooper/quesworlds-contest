using System.Collections.Concurrent;

namespace QuestWorlds.Session;

internal interface ISessionRepository
{
    void Add(Session session);
    Session? Get(string sessionId);
    void Update(Session session);
    void Remove(string sessionId);
}

internal class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public void Add(Session session) => _sessions[session.Id] = session;

    public Session? Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session : null;

    public void Update(Session session) => _sessions[session.Id] = session;

    public void Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);
}
