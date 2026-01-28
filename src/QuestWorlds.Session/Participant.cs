namespace QuestWorlds.Session;

/// <summary>
/// Represents a participant in a session (either a GM or a Player).
/// </summary>
/// <param name="Name">The display name of the participant.</param>
/// <param name="Role">The role of the participant (GM or Player).</param>
/// <param name="ConnectionId">The SignalR connection ID for real-time communication.</param>
public record Participant(
    string Name,
    ParticipantRole Role,
    string ConnectionId
);
