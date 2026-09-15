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
    /// Rebuilds a session from parts that were previously stored, restoring it as it was rather than starting it.
    /// </summary>
    /// <param name="id">The stored session identifier.</param>
    /// <param name="gm">The stored Game Master <see cref="Participant"/>.</param>
    /// <param name="players">The stored players, as an <see cref="IEnumerable{T}"/> of <see cref="Participant"/>. They are restored in the order supplied.</param>
    /// <param name="state">The stored <see cref="SessionState"/>.</param>
    /// <returns>A <see cref="Session"/> holding the supplied id, GM, players and state.</returns>
    /// <remarks>
    /// For use by a store loading a session, not by a caller starting one; use the constructor for that.
    /// The participant list is accepted as given: <see cref="AddPlayer"/>'s rule is not re-run, because it was
    /// satisfied when the player joined, and reading a row should not re-decide a decision already taken.
    /// </remarks>
    public static Session Rehydrate(string id, Participant gm, IEnumerable<Participant> players, SessionState state)
    {
        var session = new Session(id, gm);
        session._players.AddRange(players);
        session.State = state;
        return session;
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
