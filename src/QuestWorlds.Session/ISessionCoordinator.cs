namespace QuestWorlds.Session;

public interface ISessionCoordinator
{
    Session CreateSession(string gmName, string connectionId);
    Session? GetSession(string sessionId);
    void JoinSession(string sessionId, string playerName, string connectionId);
    IEnumerable<string> GetParticipantConnectionIds(string sessionId);
}
