namespace QuestWorlds.Framing;

/// <summary>
/// Types of modifiers that can be applied to a contest.
/// </summary>
public enum ModifierType
{
    /// <summary>
    /// Penalty when an ability doesn't quite fit the situation (-5 or -10).
    /// </summary>
    Stretch,

    /// <summary>
    /// Bonus or penalty based on circumstances (±5 or ±10).
    /// </summary>
    Situational,

    /// <summary>
    /// Bonus from supporting abilities or help (+5 or +10).
    /// </summary>
    Augment,

    /// <summary>
    /// Penalty from obstacles or opposition (-5 or -10).
    /// </summary>
    Hindrance,

    /// <summary>
    /// Modifier carried forward from a previous contest outcome (±5 or ±10).
    /// </summary>
    BenefitConsequence
}
