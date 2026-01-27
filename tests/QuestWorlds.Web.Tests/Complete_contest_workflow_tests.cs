using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;
using QuestWorlds.Session;

namespace QuestWorlds.Web.Tests;

public class Complete_contest_workflow_tests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HubConnection _gmConnection = null!;
    private HubConnection _playerConnection = null!;

    public Complete_contest_workflow_tests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var client = _factory.CreateClient();
        var hubUrl = new Uri(client.BaseAddress!, "/contestHub");

        _gmConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        _playerConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await _gmConnection.StartAsync();
        await _playerConnection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _gmConnection.DisposeAsync();
        await _playerConnection.DisposeAsync();
    }

    [Fact]
    public async Task When_running_complete_contest_workflow_should_produce_valid_outcome()
    {
        // Arrange
        string? sessionId = null;
        string? playerJoinedName = null;
        string? framedPrize = null;
        string? framedResistance = null;
        string? submittedAbility = null;
        string? submittedRating = null;
        ContestOutcome? gmOutcome = null;
        ContestOutcome? playerOutcome = null;
        SessionState? gmSessionState = null;
        SessionState? playerSessionState = null;

        // Set up GM event handlers
        _gmConnection.On<string>("SessionCreated", id => sessionId = id);
        _gmConnection.On<string>("PlayerJoined", name => playerJoinedName = name);
        _gmConnection.On<string, string>("ContestFramed", (prize, resistance) =>
        {
            framedPrize = prize;
            framedResistance = resistance;
        });
        _gmConnection.On<string, string>("AbilitySubmitted", (ability, rating) =>
        {
            submittedAbility = ability;
            submittedRating = rating;
        });
        _gmConnection.On<ContestOutcome>("ContestResolved", outcome => gmOutcome = outcome);
        _gmConnection.On<SessionState>("SessionStateChanged", state => gmSessionState = state);

        // Set up Player event handlers
        _playerConnection.On<string>("PlayerJoined", _ => { });
        _playerConnection.On<string, string>("ContestFramed", (_, _) => { });
        _playerConnection.On<string, string>("AbilitySubmitted", (_, _) => { });
        _playerConnection.On<ContestOutcome>("ContestResolved", outcome => playerOutcome = outcome);
        _playerConnection.On<SessionState>("SessionStateChanged", state => playerSessionState = state);

        // Act - Step 1: GM creates session
        var createdSessionId = await _gmConnection.InvokeAsync<string>("CreateSession", "Test GM");
        await Task.Delay(100); // Allow events to propagate

        // Assert - Session created
        Assert.NotNull(sessionId);
        Assert.Equal(6, sessionId.Length);
        Assert.Equal(createdSessionId, sessionId);

        // Act - Step 2: Player joins session
        await _playerConnection.InvokeAsync("JoinSession", sessionId, "Test Player");
        await Task.Delay(100);

        // Assert - Player joined
        Assert.Equal("Test Player", playerJoinedName);

        // Act - Step 3: GM frames contest
        await _gmConnection.InvokeAsync("FrameContest", sessionId, "Sneak past guards", "14");
        await Task.Delay(100);

        // Assert - Contest framed
        Assert.Equal("Sneak past guards", framedPrize);
        Assert.Equal("14", framedResistance);
        Assert.Equal(SessionState.AwaitingPlayerAbility, gmSessionState);

        // Act - Step 4: Player submits ability
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, "Stealth", "15");
        await Task.Delay(100);

        // Assert - Ability submitted
        Assert.Equal("Stealth", submittedAbility);
        Assert.Equal("15", submittedRating);
        Assert.Equal(SessionState.ResolvingContest, gmSessionState);

        // Act - Step 5: GM resolves contest
        await _gmConnection.InvokeAsync("ResolveContest", sessionId);
        await Task.Delay(100);

        // Assert - Both participants received outcome
        Assert.NotNull(gmOutcome);
        Assert.NotNull(playerOutcome);

        // Assert - Outcome contains correct context
        Assert.Equal("Sneak past guards", gmOutcome.Prize);
        Assert.Equal("Stealth", gmOutcome.PlayerAbilityName);
        Assert.Equal("15", gmOutcome.PlayerRating);

        // Assert - Outcome has valid resolution data
        Assert.InRange(gmOutcome.PlayerRoll, 1, 20);
        Assert.InRange(gmOutcome.ResistanceRoll, 1, 20);
        Assert.True(gmOutcome.PlayerSuccesses >= 0);
        Assert.True(gmOutcome.ResistanceSuccesses >= 0);

        // Assert - Winner is determined
        Assert.True(gmOutcome.Winner == ContestWinner.Player ||
                    gmOutcome.Winner == ContestWinner.Resistance ||
                    gmOutcome.Winner == ContestWinner.Tie);

        // Assert - Benefit/consequence modifier is valid
        var validModifiers = new[] { -20, -15, -10, -5, 5, 10, 15, 20 };
        Assert.Contains(gmOutcome.BenefitConsequenceModifier, validModifiers);

        // Assert - Both participants see same outcome
        Assert.Equal(gmOutcome.Winner, playerOutcome.Winner);
        Assert.Equal(gmOutcome.PlayerRoll, playerOutcome.PlayerRoll);
        Assert.Equal(gmOutcome.ResistanceRoll, playerOutcome.ResistanceRoll);

        // Assert - Session state is showing outcome
        Assert.Equal(SessionState.ShowingOutcome, gmSessionState);
    }

    [Fact]
    public async Task When_gm_applies_modifiers_should_be_reflected_in_contest()
    {
        // Arrange
        var modifiersApplied = new List<(string Type, int Value)>();

        _gmConnection.On<string>("SessionCreated", _ => { });
        _gmConnection.On<string>("PlayerJoined", _ => { });
        _gmConnection.On<string, string>("ContestFramed", (_, _) => { });
        _gmConnection.On<string, string>("AbilitySubmitted", (_, _) => { });
        _gmConnection.On<string, int>("ModifierApplied", (type, value) =>
            modifiersApplied.Add((type, value)));
        _gmConnection.On<ContestOutcome>("ContestResolved", _ => { });
        _gmConnection.On<SessionState>("SessionStateChanged", _ => { });

        _playerConnection.On<string>("PlayerJoined", _ => { });
        _playerConnection.On<string, string>("ContestFramed", (_, _) => { });
        _playerConnection.On<string, string>("AbilitySubmitted", (_, _) => { });
        _playerConnection.On<string, int>("ModifierApplied", (type, value) =>
            modifiersApplied.Add((type, value)));
        _playerConnection.On<ContestOutcome>("ContestResolved", _ => { });
        _playerConnection.On<SessionState>("SessionStateChanged", _ => { });

        // Act - Setup contest
        var sessionId = await _gmConnection.InvokeAsync<string>("CreateSession", "Test GM");
        await _playerConnection.InvokeAsync("JoinSession", sessionId, "Test Player");
        await _gmConnection.InvokeAsync("FrameContest", sessionId, "Climb the wall", "10");
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, "Athletics", "12");
        await Task.Delay(100);

        // Act - Apply modifiers
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Augment", 5);
        await Task.Delay(50);
        await _gmConnection.InvokeAsync("ApplyModifier", sessionId, "Stretch", -5);
        await Task.Delay(100);

        // Assert - Modifiers were applied and broadcast
        Assert.Contains(("Augment", 5), modifiersApplied);
        Assert.Contains(("Stretch", -5), modifiersApplied);
    }

    [Fact]
    public async Task When_joining_invalid_session_should_receive_error()
    {
        // Arrange
        string? errorMessage = null;
        _playerConnection.On<string>("Error", msg => errorMessage = msg);
        _playerConnection.On<string>("PlayerJoined", _ => { });

        // Act
        await _playerConnection.InvokeAsync("JoinSession", "XXXXXX", "Test Player");
        await Task.Delay(100);

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("not found", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task When_resolving_without_player_ability_should_receive_error()
    {
        // Arrange
        string? errorMessage = null;
        _gmConnection.On<string>("SessionCreated", _ => { });
        _gmConnection.On<string, string>("ContestFramed", (_, _) => { });
        _gmConnection.On<string>("Error", msg => errorMessage = msg);
        _gmConnection.On<SessionState>("SessionStateChanged", _ => { });

        // Act - Create session and frame contest but don't submit ability
        var sessionId = await _gmConnection.InvokeAsync<string>("CreateSession", "Test GM");
        await _gmConnection.InvokeAsync("FrameContest", sessionId, "Test prize", "10");
        await Task.Delay(100);

        // Try to resolve without player ability
        await _gmConnection.InvokeAsync("ResolveContest", sessionId);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("player ability", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task When_starting_new_contest_should_reset_to_framing_state()
    {
        // Arrange
        SessionState? finalState = null;
        _gmConnection.On<string>("SessionCreated", _ => { });
        _gmConnection.On<string>("PlayerJoined", _ => { });
        _gmConnection.On<string, string>("ContestFramed", (_, _) => { });
        _gmConnection.On<string, string>("AbilitySubmitted", (_, _) => { });
        _gmConnection.On<ContestOutcome>("ContestResolved", _ => { });
        _gmConnection.On<SessionState>("SessionStateChanged", state => finalState = state);

        _playerConnection.On<string>("PlayerJoined", _ => { });
        _playerConnection.On<string, string>("ContestFramed", (_, _) => { });
        _playerConnection.On<string, string>("AbilitySubmitted", (_, _) => { });
        _playerConnection.On<ContestOutcome>("ContestResolved", _ => { });
        _playerConnection.On<SessionState>("SessionStateChanged", _ => { });

        // Act - Complete a contest
        var sessionId = await _gmConnection.InvokeAsync<string>("CreateSession", "Test GM");
        await _playerConnection.InvokeAsync("JoinSession", sessionId, "Test Player");
        await _gmConnection.InvokeAsync("FrameContest", sessionId, "Test", "10");
        await _playerConnection.InvokeAsync("SubmitAbility", sessionId, "Test", "10");
        await _gmConnection.InvokeAsync("ResolveContest", sessionId);
        await Task.Delay(100);

        // Act - Start new contest
        await _gmConnection.InvokeAsync("StartNewContest", sessionId);
        await Task.Delay(100);

        // Assert
        Assert.Equal(SessionState.FramingContest, finalState);
    }
}
