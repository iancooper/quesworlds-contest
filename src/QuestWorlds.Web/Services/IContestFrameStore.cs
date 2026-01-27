using QuestWorlds.Framing;

namespace QuestWorlds.Web.Services;

/// <summary>
/// Stores contest frames associated with sessions.
/// </summary>
public interface IContestFrameStore
{
    void SetFrame(string sessionId, ContestFrame frame);
    ContestFrame? GetFrame(string sessionId);
    void ClearFrame(string sessionId);
}

public class InMemoryContestFrameStore : IContestFrameStore
{
    private readonly Dictionary<string, ContestFrame> _frames = new();

    public void SetFrame(string sessionId, ContestFrame frame)
    {
        _frames[sessionId] = frame;
    }

    public ContestFrame? GetFrame(string sessionId)
    {
        return _frames.TryGetValue(sessionId, out var frame) ? frame : null;
    }

    public void ClearFrame(string sessionId)
    {
        _frames.Remove(sessionId);
    }
}
