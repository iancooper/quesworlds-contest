using QuestWorlds.Outcome;
using QuestWorlds.Session;

namespace QuestWorlds.Web.Hubs;

/// <summary>
/// Defines the client methods that can be called from the ContestHub server.
/// Clients must implement these methods to receive real-time updates.
/// </summary>
public interface IContestHubClient
{
    /// <summary>
    /// Called when a new session has been created.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the created session.</param>
    Task SessionCreated(string sessionId);

    /// <summary>
    /// Called when a player joins the session.
    /// </summary>
    /// <param name="playerName">The name of the player who joined.</param>
    Task PlayerJoined(string playerName);

    /// <summary>
    /// Called when the session state changes.
    /// </summary>
    /// <param name="state">The new session state.</param>
    Task SessionStateChanged(SessionState state);

    /// <summary>
    /// Called when a contest has been framed by the GM.
    /// </summary>
    /// <param name="prize">The prize at stake.</param>
    /// <param name="resistanceTn">The resistance target number.</param>
    Task ContestFramed(string prize, string resistanceTn);

    /// <summary>
    /// Called when a player submits their ability.
    /// </summary>
    /// <param name="abilityName">The name of the submitted ability.</param>
    /// <param name="rating">The ability rating.</param>
    Task AbilitySubmitted(string abilityName, string rating);

    /// <summary>
    /// Called when a modifier is applied to the contest.
    /// </summary>
    /// <param name="type">The type of modifier.</param>
    /// <param name="value">The modifier value.</param>
    Task ModifierApplied(string type, int value);

    /// <summary>
    /// Called when a contest has been resolved.
    /// </summary>
    /// <param name="outcome">The complete contest outcome.</param>
    Task ContestResolved(ContestOutcome outcome);

    /// <summary>
    /// Called when an error occurs.
    /// </summary>
    /// <param name="message">The error message.</param>
    Task Error(string message);
}
