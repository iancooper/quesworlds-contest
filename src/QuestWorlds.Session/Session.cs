namespace QuestWorlds.Session;

/// <summary>
/// Represents a contest session with a GM and players.
/// Tracks participants and the current state of the session.
/// </summary>
public class Session
{
    private readonly List<Participant> _players = new();

    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the Game Master participant for this session.
    /// </summary>
    public Participant GM { get; }

    /// <summary>
    /// Gets the list of players in this session.
    /// </summary>
    public IReadOnlyList<Participant> Players => _players.AsReadOnly();

    /// <summary>
    /// Gets the current state of the session.
    /// </summary>
    public SessionState State { get; private set; }

    /// <summary>
    /// Creates a new session with the specified ID and GM.
    /// </summary>
    /// <param name="id">The unique session identifier.</param>
    /// <param name="gm">The Game Master participant.</param>
    public Session(string id, Participant gm)
    {
        Id = id;
        GM = gm;
        State = SessionState.WaitingForPlayers;
    }

    /// <summary>
    /// Adds a player to this session.
    /// </summary>
    /// <param name="player">The player to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when the participant is not a player.</exception>
    public void AddPlayer(Participant player)
    {
        if (player.Role != ParticipantRole.Player)
            throw new InvalidOperationException("Only players can join as participants");
        _players.Add(player);
    }

    /// <summary>
    /// Transitions the session to a new state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public void TransitionTo(SessionState newState) => State = newState;
}
