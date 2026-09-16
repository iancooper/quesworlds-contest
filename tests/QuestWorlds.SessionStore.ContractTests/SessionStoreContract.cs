using QuestWorlds.Session;

namespace QuestWorlds.SessionStore.ContractTests;

/// <summary>
/// The obligations every <see cref="IAmASessionStore"/> owes its callers, written once and run
/// against each store (ADR-0008 D5).
/// </summary>
/// <remarks>
/// Derive one class per store in that store's own test project and implement
/// <see cref="CreateStore"/>; xUnit discovers the inherited cases on the subclass, so a failure
/// names the store that broke.
/// </remarks>
public abstract class SessionStoreContract
{
    /// <summary>
    /// Returns the store under test, empty, holding no sessions.
    /// </summary>
    /// <remarks>
    /// Called once per case. A store that owns external state — a file, a connection — should
    /// give each call its own, so that cases cannot see each other's sessions.
    /// </remarks>
    protected abstract IAmASessionStore CreateStore();
}
