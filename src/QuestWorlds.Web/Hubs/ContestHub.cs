using Microsoft.AspNetCore.SignalR;
using QuestWorlds.DiceRoller;
using QuestWorlds.Framing;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;
using QuestWorlds.Session;
using QuestWorlds.Web.Services;

namespace QuestWorlds.Web.Hubs;

public class ContestHub : Hub<IContestHubClient>
{
    private readonly ISessionCoordinator _sessionCoordinator;
    private readonly IDiceRoller _diceRoller;
    private readonly IContestResolver _contestResolver;
    private readonly IOutcomeInterpreter _outcomeInterpreter;
    private readonly IContestFrameStore _frameStore;

    public ContestHub(
        ISessionCoordinator sessionCoordinator,
        IDiceRoller diceRoller,
        IContestResolver contestResolver,
        IOutcomeInterpreter outcomeInterpreter,
        IContestFrameStore frameStore)
    {
        _sessionCoordinator = sessionCoordinator;
        _diceRoller = diceRoller;
        _contestResolver = contestResolver;
        _outcomeInterpreter = outcomeInterpreter;
        _frameStore = frameStore;
    }

    public async Task<string> CreateSession(string gmName)
    {
        var session = _sessionCoordinator.CreateSession(gmName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.Caller.SessionCreated(session.Id);
        return session.Id;
    }

    public async Task JoinSession(string sessionId, string playerName)
    {
        try
        {
            _sessionCoordinator.JoinSession(sessionId, playerName, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Group(sessionId).PlayerJoined(playerName);
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task FrameContest(string sessionId, string prize, string resistanceTn)
    {
        try
        {
            var session = _sessionCoordinator.GetSession(sessionId);
            if (session is null)
            {
                await Clients.Caller.Error($"Session '{sessionId}' not found");
                return;
            }

            var resistance = Rating.Parse(resistanceTn);
            var frame = new ContestFrame(prize, TargetNumber.FromRating(resistance));
            _frameStore.SetFrame(sessionId, frame);

            session.TransitionTo(SessionState.AwaitingPlayerAbility);

            await Clients.Group(sessionId).ContestFramed(prize, resistanceTn);
            await Clients.Group(sessionId).SessionStateChanged(SessionState.AwaitingPlayerAbility);
        }
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task SubmitAbility(string sessionId, string abilityName, string rating)
    {
        try
        {
            var frame = _frameStore.GetFrame(sessionId);
            if (frame is null)
            {
                await Clients.Caller.Error("No contest has been framed");
                return;
            }

            var parsedRating = Rating.Parse(rating);
            frame.SetPlayerAbility(abilityName, parsedRating);

            var session = _sessionCoordinator.GetSession(sessionId);
            session?.TransitionTo(SessionState.ResolvingContest);

            await Clients.Group(sessionId).AbilitySubmitted(abilityName, rating);
            await Clients.Group(sessionId).SessionStateChanged(SessionState.ResolvingContest);
        }
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task ApplyModifier(string sessionId, string type, int value)
    {
        try
        {
            var frame = _frameStore.GetFrame(sessionId);
            if (frame is null)
            {
                await Clients.Caller.Error("No contest has been framed");
                return;
            }

            if (!Enum.TryParse<ModifierType>(type, true, out var modifierType))
            {
                await Clients.Caller.Error($"Invalid modifier type: {type}");
                return;
            }

            var modifier = new Modifier(modifierType, value);
            frame.ApplyModifier(modifier);

            await Clients.Group(sessionId).ModifierApplied(type, value);
        }
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task ResolveContest(string sessionId)
    {
        try
        {
            var frame = _frameStore.GetFrame(sessionId);
            if (frame is null)
            {
                await Clients.Caller.Error("No contest has been framed");
                return;
            }

            if (!frame.IsReadyForResolution)
            {
                await Clients.Caller.Error("Contest is not ready for resolution - player ability required");
                return;
            }

            // Roll dice
            var rolls = _diceRoller.Roll();

            // Resolve contest
            var result = _contestResolver.Resolve(frame, rolls);

            // Interpret outcome
            var outcome = _outcomeInterpreter.Interpret(result, frame);

            var session = _sessionCoordinator.GetSession(sessionId);
            session?.TransitionTo(SessionState.ShowingOutcome);

            await Clients.Group(sessionId).ContestResolved(outcome);
            await Clients.Group(sessionId).SessionStateChanged(SessionState.ShowingOutcome);

            // Clear the frame for potential new contest
            _frameStore.ClearFrame(sessionId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task StartNewContest(string sessionId)
    {
        var session = _sessionCoordinator.GetSession(sessionId);
        if (session is null)
        {
            await Clients.Caller.Error($"Session '{sessionId}' not found");
            return;
        }

        session.TransitionTo(SessionState.FramingContest);
        await Clients.Group(sessionId).SessionStateChanged(SessionState.FramingContest);
    }
}
