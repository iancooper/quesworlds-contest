namespace QuestWorlds.Session;

public record Participant(
    string Name,
    ParticipantRole Role,
    string ConnectionId
);
