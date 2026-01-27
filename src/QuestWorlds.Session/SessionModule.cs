namespace QuestWorlds.Session;

/// <summary>
/// Factory for creating Session module components.
/// Use this for testing or when not using dependency injection.
/// </summary>
public static class SessionModule
{
    /// <summary>
    /// Creates an ISessionCoordinator with default internal dependencies.
    /// </summary>
    public static ISessionCoordinator CreateCoordinator()
    {
        var idGenerator = new SessionIdGenerator();
        var repository = new InMemorySessionRepository();
        return new SessionCoordinator(idGenerator, repository);
    }
}
