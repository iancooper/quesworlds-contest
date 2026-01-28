using Microsoft.AspNetCore.SignalR;
using QuestWorlds.DiceRoller;
using QuestWorlds.Framing;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;
using QuestWorlds.Session;
using QuestWorlds.Web.Services;

namespace QuestWorlds.Web.Hubs;

/// <summary>
/// SignalR hub for real-time contest coordination between GM and players.
/// Handles session management, contest framing, ability submission, and resolution.
/// </summary>
public class ContestHub : Hub<IContestHubClient>
{
    private readonly ISessionCoordinator _sessionCoordinator;
    private readonly IDiceRoller _diceRoller;
    private readonly IContestResolver _contestResolver;
    private readonly IOutcomeInterpreter _outcomeInterpreter;
    private readonly IContestFrameStore _frameStore;

    /// <summary>
    /// Creates a new instance of the ContestHub with the required dependencies.
    /// </summary>
    /// <param name="sessionCoordinator">The session coordinator for managing sessions.</param>
    /// <param name="diceRoller">The dice roller for generating random rolls.</param>
    /// <param name="contestResolver">The contest resolver for calculating results.</param>
    /// <param name="outcomeInterpreter">The outcome interpreter for creating display outcomes.</param>
    /// <param name="frameStore">The frame store for persisting contest frames.</param>
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

    /// <summary>
    /// Creates a new session with the caller as the GM.
    /// </summary>
    /// <param name="gmName">The name of the Game Master.</param>
    /// <returns>The unique session ID that players can use to join.</returns>
    public async Task<string> CreateSession(string gmName)
    {
        var session = _sessionCoordinator.CreateSession(gmName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.Caller.SessionCreated(session.Id);
        return session.Id;
    }

    /// <summary>
    /// Joins an existing session as a player.
    /// </summary>
    /// <param name="sessionId">The session ID to join.</param>
    /// <param name="playerName">The name of the player joining.</param>
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

    /// <summary>
    /// Frames a new contest with the specified prize and resistance.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="prize">The prize at stake in the contest.</param>
    /// <param name="resistanceTn">The resistance target number in QuestWorlds notation.</param>
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

    /// <summary>
    /// Submits the player's ability for the current contest.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="abilityName">The name of the ability being used.</param>
    /// <param name="rating">The ability rating in QuestWorlds notation.</param>
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

    /// <summary>
    /// Applies a modifier to the current contest.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="type">The modifier type (Stretch, Situational, Augment, Hindrance, or BenefitConsequence).</param>
    /// <param name="value">The modifier value (must be ±5 or ±10).</param>
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

    /// <summary>
    /// Resolves the current contest by rolling dice and determining the outcome.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
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

    /// <summary>
    /// Starts a new contest in the session, transitioning to the framing state.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
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
