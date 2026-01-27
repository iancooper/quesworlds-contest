namespace QuestWorlds.Session;

public class Session
{
    private readonly List<Participant> _players = new();

    public string Id { get; }
    public Participant GM { get; }
    public IReadOnlyList<Participant> Players => _players.AsReadOnly();
    public SessionState State { get; private set; }

    public Session(string id, Participant gm)
    {
        Id = id;
        GM = gm;
        State = SessionState.WaitingForPlayers;
    }

    public void AddPlayer(Participant player)
    {
        if (player.Role != ParticipantRole.Player)
            throw new InvalidOperationException("Only players can join as participants");
        _players.Add(player);
    }

    public void TransitionTo(SessionState newState) => State = newState;
}
