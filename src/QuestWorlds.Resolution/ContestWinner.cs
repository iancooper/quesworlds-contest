namespace QuestWorlds.Resolution;

/// <summary>
/// The possible outcomes for who wins a contest.
/// </summary>
public enum ContestWinner
{
    /// <summary>
    /// The player won the contest.
    /// </summary>
    Player,

    /// <summary>
    /// The resistance won the contest.
    /// </summary>
    Resistance,

    /// <summary>
    /// The contest ended in a tie (both sides have equal successes and equal rolls).
    /// </summary>
    Tie
}
