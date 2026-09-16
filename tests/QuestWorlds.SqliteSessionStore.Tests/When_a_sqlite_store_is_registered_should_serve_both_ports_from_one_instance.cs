using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestWorlds.Framing;
using QuestWorlds.Session;

namespace QuestWorlds.SqliteSessionStore.Tests;

/// <summary>
/// The registration obligation of ADR-0010 D4, asserted for this store as 6.3 asserts it for the
/// in-memory one: every store owes it, and it fails silently and only in composition.
/// </summary>
public class When_a_sqlite_store_is_registered_should_serve_both_ports_from_one_instance : IAsyncLifetime
{
    private const string SESSION_ID = "ABC123";
    private const string PRIZE = "Sneak past the guards";

    private static readonly TargetNumber Resistance = new(14);

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-registration-{Guid.NewGuid():N}.db");

    private ServiceProvider _container = null!;

    private string ConnectionString => $"Data Source={_databasePath}";

    public async Task InitializeAsync()
    {
        //Arrange
        await SqliteSessionStoreInitialiser.InitialiseAsync(ConnectionString);

        var services = new ServiceCollection();
        services.AddSqliteSessionStore(ConnectionString);
        _container = services.BuildServiceProvider();
    }

    [Fact]
    public void Both_ports_should_resolve_to_the_same_instance()
    {
        //Act
        var sessionStore = _container.GetRequiredService<IAmASessionStore>();
        var frameStore = _container.GetRequiredService<IAmAContestFrameStore>();

        //Assert
        Assert.True(ReferenceEquals(sessionStore, frameStore),
            "Registering the store class against each port separately yields one instance per port. " +
            "Forward both ports to one concrete singleton.");
    }

    [Fact]
    public async Task A_frame_saved_through_the_frame_port_should_be_visible_through_the_session_port()
    {
        //Arrange
        var frameStore = _container.GetRequiredService<IAmAContestFrameStore>();
        await frameStore.SaveFrameAsync(SESSION_ID, new ContestFrame(PRIZE, Resistance));

        //Act
        var storeTheHubWouldUseForSessions = _container.GetRequiredService<IAmASessionStore>();

        //Assert
        // Weaker here than it is for the in-memory store: two SQLite instances over one connection
        // string would both see this frame, because the database is the state rather than the object.
        // It says the ports reach the same database; the ReferenceEquals case above is the one that
        // catches a double registration.
        var frame = await ((IAmAContestFrameStore)storeTheHubWouldUseForSessions).GetFrameAsync(SESSION_ID);
        Assert.NotNull(frame);
        Assert.Equal(PRIZE, frame.Prize);
    }

    [Fact]
    public void The_schema_initialiser_should_be_registered_to_run_at_startup()
    {
        //Act
        var hostedServices = _container.GetServices<IHostedService>();

        //Assert
        // Choosing the store is the only decision a host makes; initialisation cannot be left out of
        // it (ADR-0009 D6).
        Assert.Single(hostedServices.OfType<SqliteSessionStoreInitialiser>());
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();

        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
