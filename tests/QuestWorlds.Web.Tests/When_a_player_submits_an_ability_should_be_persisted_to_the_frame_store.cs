using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using QuestWorlds.Framing;
using QuestWorlds.Outcome;

namespace QuestWorlds.Web.Tests;

public class When_a_player_submits_an_ability_should_be_persisted_to_the_frame_store
    : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private const string PRIZE = "Sneak past the guards";
    private const string RESISTANCE = "14";
    private const string ABILITY = "Thief of Nochet";
    private const string RATING = "15";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly IAmAContestFrameStore _frameStore;
    private HubConnection _gmConnection = null!;
    private HubConnection _playerConnection = null!;

    public When_a_player_submits_an_ability_should_be_persisted_to_the_frame_store(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        // The store the hub writes through. Reading the frame back from it is the only way to see
        // whether a mutation was saved; the pushed events say nothing about what was stored.
        _frameStore = _factory.Services.GetRequiredService<IAmAContestFrameStore>();
    }

    public async Task InitializeAsync()
    {
        var client = _factory.CreateClient();
        var hubUrl = new Uri(client.BaseAddress!, "/contestHub");

        _gmConnection = Connect(hubUrl);
        _playerConnection = Connect(hubUrl);

        await _gmConnection.StartAsync();
        await _playerConnection.StartAsync();

        SubscribeToEverything(_gmConnection);
        SubscribeToEverything(_playerConnection);
    }

    public async Task DisposeAsync()
    {
        await _gmConnection.DisposeAsync();
        await _playerConnection.DisposeAsync();
    }

    [Fact]
    public async Task The_ability_the_player_submitted_should_be_in_the_frame_read_back()
    {
        //Arrange
        var sessionId = await AContestHasBeenFramed();

        //Act
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, ABILITY, RATING);
        await Task.Delay(100);

        //Assert
        var frame = await _frameStore.GetFrameAsync(sessionId);
        Assert.NotNull(frame);
        Assert.Equal(ABILITY, frame.PlayerAbilityName);
        Assert.Equal(Rating.Parse(RATING), frame.PlayerRating);
    }

    [Fact]
    public async Task A_modifier_the_gm_applied_should_be_in_the_frame_read_back()
    {
        //Arrange
        var sessionId = await AContestHasBeenFramed();
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, ABILITY, RATING);
        await Task.Delay(100);

        //Act
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Augment", 5);
        await Task.Delay(100);

        //Assert
        var frame = await _frameStore.GetFrameAsync(sessionId);
        Assert.NotNull(frame);
        Assert.Equal(new Modifier(ModifierType.Augment, 5), Assert.Single(frame.Modifiers));
    }

    [Fact]
    public async Task Two_modifiers_the_gm_applied_should_both_be_in_the_frame_read_back_in_order()
    {
        //Arrange
        var sessionId = await AContestHasBeenFramed();
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, ABILITY, RATING);
        await Task.Delay(100);

        //Act
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Augment", 5);
        await Task.Delay(50);
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Stretch", -5);
        await Task.Delay(100);

        //Assert
        var frame = await _frameStore.GetFrameAsync(sessionId);
        Assert.NotNull(frame);
        Assert.Equal(
            new[] { new Modifier(ModifierType.Augment, 5), new Modifier(ModifierType.Stretch, -5) },
            frame.Modifiers);
    }

    [Fact]
    public async Task A_contest_framed_answered_modified_and_resolved_should_still_reach_an_outcome()
    {
        //Arrange
        ContestOutcome? outcome = null;
        _playerConnection.On<ContestOutcome>("ContestResolved", resolved => outcome = resolved);

        var sessionId = await AContestHasBeenFramed();
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, ABILITY, RATING);
        await Task.Delay(100);
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Augment", 5);
        await Task.Delay(100);

        //Act
        await _gmConnection.InvokeAsync("ResolveContest", sessionId);
        await Task.Delay(200);

        //Assert
        Assert.NotNull(outcome);
        Assert.Equal(PRIZE, outcome.Prize);
        Assert.Equal(ABILITY, outcome.PlayerAbilityName);
    }

    private async Task<string> AContestHasBeenFramed()
    {
        var sessionId = await _gmConnection.InvokeAsync<string>("CreateSession", "Test GM");
        await _playerConnection.InvokeAsync("JoinSession", sessionId, "Test Player");
        await _gmConnection.InvokeAsync("FrameContest", sessionId, PRIZE, RESISTANCE);
        await Task.Delay(100);
        return sessionId;
    }

    private HubConnection Connect(Uri hubUrl) =>
        new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
            .Build();

    private static void SubscribeToEverything(HubConnection connection)
    {
        connection.On<string>("SessionCreated", _ => { });
        connection.On<string>("PlayerJoined", _ => { });
        connection.On<string, string>("ContestFramed", (_, _) => { });
        connection.On<string, string>("AbilitySubmitted", (_, _) => { });
        connection.On<string, int>("ModifierApplied", (_, _) => { });
        connection.On<ContestOutcome>("ContestResolved", _ => { });
        connection.On<string>("Error", _ => { });
    }
}
