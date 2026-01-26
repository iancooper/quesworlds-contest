namespace QuestWorlds.Framing;

/// <summary>
/// Represents a framed contest with a prize at stake and resistance to overcome.
/// </summary>
public class ContestFrame
{
    public string Prize { get; }
    public TargetNumber Resistance { get; }

    public ContestFrame(string prize, TargetNumber resistance)
    {
        if (string.IsNullOrWhiteSpace(prize))
            throw new ArgumentException("Prize cannot be empty", nameof(prize));

        Prize = prize;
        Resistance = resistance;
    }
}
