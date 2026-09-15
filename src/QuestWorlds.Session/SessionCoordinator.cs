namespace QuestWorlds.Session;

internal class SessionCoordinator : ISessionCoordinator
{
    private readonly ISessionIdGenerator _idGenerator;
    private readonly IAmASessionStore _store;

    public SessionCoordinator(ISessionIdGenerator idGenerator, IAmASessionStore store)
    {
        _idGenerator = idGenerator;
        _store = store;
    }

    public Session CreateSession(string gmName, string connectionId)
    {
        var sessionId = _idGenerator.Generate();
        var gm = new Participant(gmName, ParticipantRole.GM, connectionId);
        var session = new Session(sessionId, gm);
        _store.Save(session);
        return session;
    }

    public Session? GetSession(string sessionId) => _store.Get(sessionId);

    public void JoinSession(string sessionId, string playerName, string connectionId)
    {
        var session = _store.Get(sessionId);
        if (session is null)
            throw new InvalidOperationException($"Session '{sessionId}' not found");

        var player = new Participant(playerName, ParticipantRole.Player, connectionId);
        session.AddPlayer(player);
    }

    public IEnumerable<string> GetParticipantConnectionIds(string sessionId)
    {
        var session = _store.Get(sessionId);
        if (session is null)
            return Enumerable.Empty<string>();

        return new[] { session.GM.ConnectionId }
            .Concat(session.Players.Select(p => p.ConnectionId));
    }
}
