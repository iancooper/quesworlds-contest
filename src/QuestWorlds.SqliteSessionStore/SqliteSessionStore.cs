namespace QuestWorlds.SqliteSessionStore;

using Microsoft.Data.Sqlite;
using QuestWorlds.Framing;
using QuestWorlds.Session;

// QuestWorlds.Session is a namespace as well as a type, and this namespace sits beside it under
// QuestWorlds, so an unqualified Session binds to the namespace. The alias says which we mean, and
// it has to live inside the namespace declaration to win against the enclosing one.
using Session = QuestWorlds.Session.Session;

/// <summary>
/// Holds sessions and their contest frames in a SQLite database, so that they survive a restart.
/// </summary>
/// <remarks>
/// One class behind both ports, so that a session and its frame reach the same database over the
/// same connection and cannot end up in different files (ADR-0010 D4). Register it once, as a
/// singleton, forwarding both interfaces to it.
/// It holds a connection string and nothing else, opening a connection per operation: the provider
/// pools them, so that is cheap, and it keeps concurrent hub callbacks off a shared connection
/// (ADR-0009 D5).
/// Every get builds new objects out of rows, so a caller that mutates what it was handed and forgets
/// to save changes nothing — the obligation the in-memory store has to imitate (ADR-0008 D7).
/// </remarks>
public class SqliteSessionStore : IAmASessionStore, IAmAContestFrameStore
{
    private const string UPSERT_SESSION = """
        INSERT INTO Sessions (Id, State) VALUES (@sessionId, @state)
        ON CONFLICT(Id) DO UPDATE SET State = excluded.State;
        """;

    private const string DELETE_PARTICIPANTS = "DELETE FROM Participants WHERE SessionId = @sessionId;";

    private const string INSERT_PARTICIPANT = """
        INSERT INTO Participants (SessionId, Ordinal, Name, Role, ConnectionId)
        VALUES (@sessionId, @ordinal, @name, @role, @connectionId);
        """;

    private const string SELECT_SESSION = "SELECT State FROM Sessions WHERE Id = @sessionId;";

    private const string SELECT_PARTICIPANTS = """
        SELECT Name, Role, ConnectionId FROM Participants WHERE SessionId = @sessionId ORDER BY Ordinal;
        """;

    private const string DELETE_SESSION = "DELETE FROM Sessions WHERE Id = @sessionId;";

    private const string UPSERT_FRAME = """
        INSERT INTO ContestFrames (
            SessionId, Prize, ResistanceBase, ResistanceMasteries, ResistanceModifier,
            PlayerAbilityName, PlayerRatingBase, PlayerRatingMasteries)
        VALUES (
            @sessionId, @prize, @resistanceBase, @resistanceMasteries, @resistanceModifier,
            @playerAbilityName, @playerRatingBase, @playerRatingMasteries)
        ON CONFLICT(SessionId) DO UPDATE SET
            Prize                 = excluded.Prize,
            ResistanceBase        = excluded.ResistanceBase,
            ResistanceMasteries   = excluded.ResistanceMasteries,
            ResistanceModifier    = excluded.ResistanceModifier,
            PlayerAbilityName     = excluded.PlayerAbilityName,
            PlayerRatingBase      = excluded.PlayerRatingBase,
            PlayerRatingMasteries = excluded.PlayerRatingMasteries;
        """;

    private const string DELETE_MODIFIERS = "DELETE FROM ContestModifiers WHERE SessionId = @sessionId;";

    private const string INSERT_MODIFIER = """
        INSERT INTO ContestModifiers (SessionId, Ordinal, Type, Value)
        VALUES (@sessionId, @ordinal, @type, @value);
        """;

    private const string SELECT_FRAME = """
        SELECT Prize, ResistanceBase, ResistanceMasteries, ResistanceModifier,
               PlayerAbilityName, PlayerRatingBase, PlayerRatingMasteries
        FROM ContestFrames WHERE SessionId = @sessionId;
        """;

    private const string SELECT_MODIFIERS = """
        SELECT Type, Value FROM ContestModifiers WHERE SessionId = @sessionId ORDER BY Ordinal;
        """;

    private const string DELETE_FRAME = "DELETE FROM ContestFrames WHERE SessionId = @sessionId;";

    private readonly string _connectionString;

    /// <summary>
    /// Creates a store over the database the supplied connection string names.
    /// </summary>
    /// <param name="connectionString">Where the database lives.</param>
    /// <remarks>
    /// The schema is not created here: a hosted service does it at startup, and tests call
    /// <see cref="SqliteSessionStoreInitialiser.InitialiseAsync"/> directly (ADR-0009 D6).
    /// </remarks>
    public SqliteSessionStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        // Between the delete and the last insert the session has no participants, and a concurrent
        // get must not see that. The upsert is what makes a save indifferent to whether the session
        // is already held, which is the obligation the port took on when Add and Update became one
        // method (ADR-0009 D4, ADR-0007 D4).
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var upsert = Command(connection, transaction, UPSERT_SESSION);
        upsert.Parameters.AddWithValue("@sessionId", session.Id);
        upsert.Parameters.AddWithValue("@state", SessionRecordMapper.NameOf(session.State));
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        // Delete and reinsert rather than diff: a Participant is a value object with no identity to
        // diff on, and change tracking is the coordinator's job, not a store's (ADR-0009 D4).
        var clear = Command(connection, transaction, DELETE_PARTICIPANTS);
        clear.Parameters.AddWithValue("@sessionId", session.Id);
        await clear.ExecuteNonQueryAsync(cancellationToken);

        var insert = Command(connection, transaction, INSERT_PARTICIPANT);
        insert.Parameters.AddWithValue("@sessionId", session.Id);
        var ordinal = insert.Parameters.Add("@ordinal", SqliteType.Integer);
        var name = insert.Parameters.Add("@name", SqliteType.Text);
        var role = insert.Parameters.Add("@role", SqliteType.Text);
        var connectionId = insert.Parameters.Add("@connectionId", SqliteType.Text);

        // The GM goes in first, then the players in join order. Ordinal is what preserves that order
        // across a round trip, because Players is a list and a table is a set (ADR-0009 D2).
        var participants = session.Players.Prepend(session.GM).ToList();
        for (var position = 0; position < participants.Count; position++)
        {
            ordinal.Value = position;
            name.Value = participants[position].Name;
            role.Value = SessionRecordMapper.NameOf(participants[position].Role);
            connectionId.Value = participants[position].ConnectionId;
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        var select = Command(connection, transaction: null, SELECT_SESSION);
        select.Parameters.AddWithValue("@sessionId", sessionId);
        if (await select.ExecuteScalarAsync(cancellationToken) is not string state) return null;

        var participants = new List<Participant>();

        var selectParticipants = Command(connection, transaction: null, SELECT_PARTICIPANTS);
        selectParticipants.Parameters.AddWithValue("@sessionId", sessionId);
        await using var reader = await selectParticipants.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            participants.Add(SessionRecordMapper.ParticipantFrom(
                reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return SessionRecordMapper.SessionFrom(sessionId, state, participants);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        // A frame has no life outside its session, so removing the session takes the frame with it.
        // Leaving one behind is an orphan nothing can reach and nothing will clean up.
        var deleteFrame = Command(connection, transaction, DELETE_FRAME);
        deleteFrame.Parameters.AddWithValue("@sessionId", sessionId);
        await deleteFrame.ExecuteNonQueryAsync(cancellationToken);

        // Participants and modifiers go with their parents, by ON DELETE CASCADE. Removing a session
        // that is not held deletes nothing and succeeds, which is the port's idempotency obligation.
        var deleteSession = Command(connection, transaction, DELETE_SESSION);
        deleteSession.Parameters.AddWithValue("@sessionId", sessionId);
        await deleteSession.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFrameAsync(string sessionId, ContestFrame frame, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        // The same shape as SaveAsync, and for the same reason. It is a separate transaction, though:
        // the hub saves a session and a frame at different moments for different reasons, so the two
        // calls are not atomic with each other, and inventing a unit of work to span them would be a
        // larger thing than the exposure deserves (ADR-0009 D4).
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var upsert = Command(connection, transaction, UPSERT_FRAME);
        upsert.Parameters.AddWithValue("@sessionId", sessionId);
        upsert.Parameters.AddWithValue("@prize", frame.Prize);
        upsert.Parameters.AddWithValue("@resistanceBase", frame.Resistance.Base);
        upsert.Parameters.AddWithValue("@resistanceMasteries", frame.Resistance.Masteries);
        upsert.Parameters.AddWithValue("@resistanceModifier", frame.Resistance.Modifier);
        upsert.Parameters.AddWithValue("@playerAbilityName", frame.PlayerAbilityName ?? (object)DBNull.Value);
        upsert.Parameters.AddWithValue("@playerRatingBase", frame.PlayerRating?.Base ?? (object)DBNull.Value);
        upsert.Parameters.AddWithValue("@playerRatingMasteries", frame.PlayerRating?.Masteries ?? (object)DBNull.Value);
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        var clear = Command(connection, transaction, DELETE_MODIFIERS);
        clear.Parameters.AddWithValue("@sessionId", sessionId);
        await clear.ExecuteNonQueryAsync(cancellationToken);

        var insert = Command(connection, transaction, INSERT_MODIFIER);
        insert.Parameters.AddWithValue("@sessionId", sessionId);
        var ordinal = insert.Parameters.Add("@ordinal", SqliteType.Integer);
        var type = insert.Parameters.Add("@type", SqliteType.Text);
        var value = insert.Parameters.Add("@value", SqliteType.Integer);

        for (var position = 0; position < frame.Modifiers.Count; position++)
        {
            ordinal.Value = position;
            type.Value = SessionRecordMapper.NameOf(frame.Modifiers[position].Type);
            value.Value = frame.Modifiers[position].Value;
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContestFrame?> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        var select = Command(connection, transaction: null, SELECT_FRAME);
        select.Parameters.AddWithValue("@sessionId", sessionId);

        string prize;
        int resistanceBase, resistanceMasteries, resistanceModifier;
        string? playerAbilityName;
        int? playerRatingBase, playerRatingMasteries;

        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;

            prize = reader.GetString(0);
            resistanceBase = reader.GetInt32(1);
            resistanceMasteries = reader.GetInt32(2);
            resistanceModifier = reader.GetInt32(3);
            playerAbilityName = reader.IsDBNull(4) ? null : reader.GetString(4);
            playerRatingBase = reader.IsDBNull(5) ? null : reader.GetInt32(5);
            playerRatingMasteries = reader.IsDBNull(6) ? null : reader.GetInt32(6);
        }

        var modifiers = new List<Modifier>();

        var selectModifiers = Command(connection, transaction: null, SELECT_MODIFIERS);
        selectModifiers.Parameters.AddWithValue("@sessionId", sessionId);
        await using var modifierReader = await selectModifiers.ExecuteReaderAsync(cancellationToken);
        while (await modifierReader.ReadAsync(cancellationToken))
        {
            modifiers.Add(SessionRecordMapper.ModifierFrom(modifierReader.GetString(0), modifierReader.GetInt32(1)));
        }

        return SessionRecordMapper.FrameFrom(
            prize, resistanceBase, resistanceMasteries, resistanceModifier,
            playerAbilityName, playerRatingBase, playerRatingMasteries, modifiers);
    }

    /// <inheritdoc />
    public async Task ClearFrameAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        // The frame's modifiers go with it, by ON DELETE CASCADE. Clearing a frame for a session that
        // has none deletes nothing and succeeds.
        var delete = Command(connection, transaction: null, DELETE_FRAME);
        delete.Parameters.AddWithValue("@sessionId", sessionId);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    // Microsoft.Data.Sqlite requires a command to name the transaction it runs in when one is open,
    // so the two are handed over together rather than left to be remembered.
    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }
}
