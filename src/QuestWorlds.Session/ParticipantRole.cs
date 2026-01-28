namespace QuestWorlds.Session;

/// <summary>
/// The role of a participant in a session.
/// </summary>
public enum ParticipantRole
{
    /// <summary>
    /// Game Master - creates sessions, frames contests, and applies modifiers.
    /// </summary>
    GM,

    /// <summary>
    /// Player - joins sessions and submits abilities for contests.
    /// </summary>
    Player
}
