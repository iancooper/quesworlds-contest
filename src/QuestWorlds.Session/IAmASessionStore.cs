namespace QuestWorlds.Session;

internal interface IAmASessionStore
{
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);
    Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
