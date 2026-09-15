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

    public async Task<Session> CreateSessionAsync(string gmName, string connectionId, CancellationToken cancellationToken = default)
    {
        var sessionId = _idGenerator.Generate();
        var gm = new Participant(gmName, ParticipantRole.GM, connectionId);
        var session = new Session(sessionId, gm);
        await _store.SaveAsync(session, cancellationToken);
        return session;
    }

    public Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _store.GetAsync(sessionId, cancellationToken);

    public async Task JoinSessionAsync(string sessionId, string playerName, string connectionId, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
            throw new InvalidOperationException($"Session '{sessionId}' not found");

        var player = new Participant(playerName, ParticipantRole.Player, connectionId);
        session.AddPlayer(player);
        await _store.SaveAsync(session, cancellationToken);
    }

    public async Task TransitionSessionStateAsync(string sessionId, SessionState newState, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
            throw new InvalidOperationException($"Session '{sessionId}' not found");

        session.TransitionTo(newState);
        await _store.SaveAsync(session, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetParticipantConnectionIdsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
            return Enumerable.Empty<string>();

        return new[] { session.GM.ConnectionId }
            .Concat(session.Players.Select(p => p.ConnectionId));
    }
}
