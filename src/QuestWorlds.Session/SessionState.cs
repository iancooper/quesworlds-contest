namespace QuestWorlds.Session;

/// <summary>
/// The possible states of a contest session.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// The session is waiting for players to join.
    /// </summary>
    WaitingForPlayers,

    /// <summary>
    /// The GM is framing the contest (setting prize and resistance).
    /// </summary>
    FramingContest,

    /// <summary>
    /// Waiting for the player to submit their ability.
    /// </summary>
    AwaitingPlayerAbility,

    /// <summary>
    /// The contest is being resolved (dice rolling and calculation).
    /// </summary>
    ResolvingContest,

    /// <summary>
    /// The outcome is being displayed to participants.
    /// </summary>
    ShowingOutcome
}
