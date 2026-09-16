using Microsoft.Extensions.DependencyInjection;
using QuestWorlds.Framing;
using QuestWorlds.Session;

namespace QuestWorlds.InMemorySessionStore.Tests;

public class When_a_store_is_registered_should_serve_both_ports_from_one_instance
{
    private const string SESSION_ID = "ABC123";
    private const string PRIZE = "Sneak past the guards";

    private static readonly TargetNumber Resistance = new(14);

    private readonly ServiceProvider _container;

    public When_a_store_is_registered_should_serve_both_ports_from_one_instance()
    {
        //Arrange
        var services = new ServiceCollection();
        services.AddInMemorySessionStore();
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
            "Registering the store class against each port separately yields one instance per port, " +
            "and a frame the session store cannot see. Forward both ports to one concrete singleton.");
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
        var frame = await ((IAmAContestFrameStore)storeTheHubWouldUseForSessions).GetFrameAsync(SESSION_ID);
        Assert.NotNull(frame);
        Assert.Equal(PRIZE, frame.Prize);
    }
}
