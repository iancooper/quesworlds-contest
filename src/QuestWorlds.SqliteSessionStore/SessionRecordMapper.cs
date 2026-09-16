namespace QuestWorlds.SqliteSessionStore;

using QuestWorlds.Framing;
using QuestWorlds.Session;

// QuestWorlds.Session is a namespace as well as a type, and this namespace sits beside it under
// QuestWorlds, so an unqualified Session binds to the namespace. The alias says which we mean, and
// it has to live inside the namespace declaration to win against the enclosing one.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Turns the columns of a stored row back into the types the domain uses, and says how an enum is
/// spelled on the way out.
/// </summary>
/// <remarks>
/// The mapping is hand-written, which is the point rather than an inconvenience: it is what an
/// adapter does, and an ORM would hide it (ADR-0009 D1). It decides nothing — every value is taken
/// as stored, and reconstruction goes through <see cref="Session.Rehydrate"/> and
/// <see cref="ContestFrame.Rehydrate"/> so that no domain rule is re-run to read a row.
/// </remarks>
internal static class SessionRecordMapper
{
    /// <summary>
    /// How an enum is written to a column: its name, never its ordinal.
    /// </summary>
    /// <remarks>
    /// A numeric enum value is positional, so inserting a <see cref="SessionState"/> in the middle of
    /// the workflow would silently reinterpret every stored row. A name survives reordering, and it
    /// makes the table readable (ADR-0009 D3).
    /// </remarks>
    internal static string NameOf<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();

    /// <summary>
    /// Reads an enum back from the name a column holds.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stored name is not a member of the enum. Loudly, and on the row that carries
    /// it: a store that fell back to the default would answer a question about a session it could not
    /// actually read (ADR-0009 D3).
    /// </exception>
    internal static TEnum StoredAs<TEnum>(string name) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(name, out var value) && Enum.IsDefined(value)
            ? value
            : throw new InvalidOperationException(
                $"'{name}' is not a known {typeof(TEnum).Name}. The stored value is one this build " +
                $"does not understand; known values are {string.Join(", ", Enum.GetNames<TEnum>())}.");

    /// <summary>
    /// Rebuilds a participant from its row.
    /// </summary>
    internal static Participant ParticipantFrom(string name, string role, string connectionId) =>
        new(name, StoredAs<ParticipantRole>(role), connectionId);

    /// <summary>
    /// Rebuilds a session from its row and its participants, in stored order.
    /// </summary>
    /// <remarks>
    /// The GM is a participant row like any other, distinguished by its <see cref="ParticipantRole"/>
    /// rather than by a column of its own, so loading splits the rows by role — which is exactly the
    /// shape <see cref="Session.Rehydrate"/> takes (ADR-0009 D2).
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no row holds the session's GM.</exception>
    internal static Session SessionFrom(string id, string state, IReadOnlyList<Participant> participants)
    {
        var gm = participants.SingleOrDefault(participant => participant.Role == ParticipantRole.GM)
                 ?? throw new InvalidOperationException(
                     $"Session '{id}' has no GM among its stored participants. A session cannot exist " +
                     "without one, so the rows it was read from are incomplete.");

        var players = participants.Where(participant => participant.Role == ParticipantRole.Player).ToList();

        return Session.Rehydrate(id, gm, players, StoredAs<SessionState>(state));
    }

    /// <summary>
    /// Rebuilds a modifier from its row.
    /// </summary>
    internal static Modifier ModifierFrom(string type, int value) =>
        new(StoredAs<ModifierType>(type), value);

    /// <summary>
    /// Rebuilds a contest frame from its row and its modifiers, in stored order.
    /// </summary>
    /// <remarks>
    /// The player's ability and rating are nullable because a frame exists from the moment the GM
    /// sets a prize and resistance, and the player's side arrives later: the nullability in the
    /// schema is the nullability already on <see cref="ContestFrame"/> (ADR-0009 D2).
    /// </remarks>
    internal static ContestFrame FrameFrom(
        string prize,
        int resistanceBase,
        int resistanceMasteries,
        int resistanceModifier,
        string? playerAbilityName,
        int? playerRatingBase,
        int? playerRatingMasteries,
        IEnumerable<Modifier> modifiers) =>
        ContestFrame.Rehydrate(
            prize,
            new TargetNumber(resistanceBase, resistanceMasteries, resistanceModifier),
            playerAbilityName,
            playerRatingBase is null ? null : new Rating(playerRatingBase.Value, playerRatingMasteries ?? 0),
            modifiers);
}
