namespace QuestWorlds.Session;

/// <summary>
/// Factory for creating Session module components.
/// </summary>
public static class SessionModule
{
    /// <summary>
    /// Creates an <see cref="ISessionCoordinator"/> over the supplied store.
    /// </summary>
    /// <param name="store">The <see cref="IAmASessionStore"/> the coordinator keeps sessions in.</param>
    /// <returns>A coordinator with this module's internal dependencies supplied.</returns>
    /// <remarks>
    /// The store is a parameter because this module contains no implementation of the port and so
    /// cannot supply a default (ADR-0008 D6).
    /// </remarks>
    public static ISessionCoordinator CreateCoordinator(IAmASessionStore store)
    {
        var idGenerator = new SessionIdGenerator();
        return new SessionCoordinator(idGenerator, store);
    }
}
