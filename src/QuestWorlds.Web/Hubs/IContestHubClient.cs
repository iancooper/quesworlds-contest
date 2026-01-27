using QuestWorlds.Outcome;
using QuestWorlds.Session;

namespace QuestWorlds.Web.Hubs;

/// <summary>
/// Client methods that can be called from the server.
/// </summary>
public interface IContestHubClient
{
    Task SessionCreated(string sessionId);
    Task PlayerJoined(string playerName);
    Task SessionStateChanged(SessionState state);
    Task ContestFramed(string prize, string resistanceTn);
    Task AbilitySubmitted(string abilityName, string rating);
    Task ModifierApplied(string type, int value);
    Task ContestResolved(ContestOutcome outcome);
    Task Error(string message);
}
