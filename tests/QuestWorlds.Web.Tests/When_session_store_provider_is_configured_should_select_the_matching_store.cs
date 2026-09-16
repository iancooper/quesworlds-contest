namespace QuestWorlds.Web.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestWorlds.Session;

// Both store types name a namespace as well as a type, and from this sibling namespace an
// unqualified use of either is CS0118. The aliases have to sit inside the namespace declaration:
// at global scope the enclosing QuestWorlds namespace is searched first and wins.
using InMemorySessionStore = QuestWorlds.InMemorySessionStore.InMemorySessionStore;
using SqliteSessionStore = QuestWorlds.SqliteSessionStore.SqliteSessionStore;

/// <summary>
/// One setting decides which store the application runs on, and a setting nobody recognises stops
/// it starting rather than quietly losing sessions on the next restart (AC7, AC7a, AC7b).
/// </summary>
/// <remarks>
/// The cases go through <see cref="WebApplicationFactory{TEntryPoint}"/> rather than calling the
/// registration directly, because the choice is the host's and the host is what is being tested —
/// and because the extension that makes it is internal to <c>QuestWorlds.Web</c>, while tests here
/// exercise exports only.
/// <para>
/// Configuration is overridden with <c>UseSetting</c>, not <c>ConfigureAppConfiguration</c>: under
/// minimal hosting the latter is applied when the host is built, which is *after* the top-level
/// statements have read configuration and chosen a store.
/// </para>
/// </remarks>
public class When_session_store_provider_is_configured_should_select_the_matching_store : IDisposable
{
    private const string PROVIDER = "SessionStore:Provider";
    private const string CONNECTION_STRING = "ConnectionStrings:SessionStore";

    private readonly List<WebApplicationFactory<Program>> _applications = [];

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"questworlds-configured-{Guid.NewGuid():N}.db");

    [Fact]
    public void No_provider_setting_should_give_the_in_memory_store()
    {
        //Arrange
        // Nothing configured at all, which is what a deployment that has changed nothing has: only
        // appsettings.Development.json names a provider, and outside Development it is not read, so
        // this is the code's own default rather than the settings file's.
        var application = ApplicationOutsideDevelopment();

        //Act
        var store = application.Services.GetRequiredService<IAmASessionStore>();

        //Assert
        Assert.IsType<InMemorySessionStore>(store);
    }

    [Fact]
    public void The_sqlite_provider_should_give_the_sqlite_store()
    {
        //Arrange
        var application = ApplicationConfiguredWith(
            (PROVIDER, "Sqlite"),
            (CONNECTION_STRING, $"Data Source={_databasePath}"));

        //Act
        var store = application.Services.GetRequiredService<IAmASessionStore>();

        //Assert
        Assert.IsType<SqliteSessionStore>(store);
    }

    [Fact]
    public void An_unrecognised_provider_should_stop_the_application_starting()
    {
        //Arrange
        var application = ApplicationConfiguredWith((PROVIDER, "Postgres"));

        //Act
        var exception = Assert.Throws<InvalidOperationException>(() => application.Services);

        //Assert
        // The message has to name the typo and the way out of it; a reader who only learns that
        // something is wrong still has to go and find the two values that are right.
        Assert.Contains("Postgres", exception.Message);
        Assert.Contains("InMemory", exception.Message);
        Assert.Contains("Sqlite", exception.Message);
    }

    [Fact]
    public void The_sqlite_provider_without_a_connection_string_should_stop_the_application_starting()
    {
        //Arrange
        // SQLite asked for, but nothing saying where the database lives. Outside Development there
        // is no connection string in any settings file to fall back on.
        var application = ApplicationOutsideDevelopment((PROVIDER, "Sqlite"));

        //Act
        // Asking for the services is what starts the host, so throwing here is throwing at startup.
        // A store that accepted the missing string would instead fail on the first save, mid-game.
        var exception = Record.Exception(() => application.Services);

        //Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    private WebApplicationFactory<Program> ApplicationConfiguredWith(params (string Key, string? Value)[] settings) =>
        Application(builder =>
        {
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
        });

    private WebApplicationFactory<Program> ApplicationOutsideDevelopment(params (string Key, string? Value)[] settings) =>
        Application(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
        });

    private WebApplicationFactory<Program> Application(Action<IWebHostBuilder> configure)
    {
        var application = new WebApplicationFactory<Program>().WithWebHostBuilder(configure);

        _applications.Add(application);
        return application;
    }

    public void Dispose()
    {
        foreach (var application in _applications) application.Dispose();

        // A pooled connection still holds the file open, so the pools go before the files do.
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (File.Exists(file)) File.Delete(file);
        }

        GC.SuppressFinalize(this);
    }
}
