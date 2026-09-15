namespace QuestWorlds.Session;

internal interface IAmASessionStore
{
    void Save(Session session);
    Session? Get(string sessionId);
    void Remove(string sessionId);
}
