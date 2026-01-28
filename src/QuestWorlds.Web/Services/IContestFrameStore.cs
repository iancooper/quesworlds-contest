using QuestWorlds.Framing;

namespace QuestWorlds.Web.Services;

/// <summary>
/// Stores contest frames associated with sessions during the contest workflow.
/// </summary>
public interface IContestFrameStore
{
    /// <summary>
    /// Stores a contest frame for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="frame">The contest frame to store.</param>
    void SetFrame(string sessionId, ContestFrame frame);

    /// <summary>
    /// Gets the contest frame for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The contest frame, or null if not found.</returns>
    ContestFrame? GetFrame(string sessionId);

    /// <summary>
    /// Clears the contest frame for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    void ClearFrame(string sessionId);
}

/// <summary>
/// In-memory implementation of IContestFrameStore.
/// Suitable for single-server deployments.
/// </summary>
public class InMemoryContestFrameStore : IContestFrameStore
{
    private readonly Dictionary<string, ContestFrame> _frames = new();

    /// <inheritdoc />
    public void SetFrame(string sessionId, ContestFrame frame)
    {
        _frames[sessionId] = frame;
    }

    /// <inheritdoc />
    public ContestFrame? GetFrame(string sessionId)
    {
        return _frames.TryGetValue(sessionId, out var frame) ? frame : null;
    }

    /// <inheritdoc />
    public void ClearFrame(string sessionId)
    {
        _frames.Remove(sessionId);
    }
}
