# Implementation Tasks

> **Workflow**: Tests → Code (TDD)
>
> Each task uses `/test-first` to write tests before implementation.

## Phase 0: Project Setup

- [x] **SETUP: Create solution structure**
  - Create `QuestWorlds.sln`
  - Create projects:
    - `src/QuestWorlds.Session/`
    - `src/QuestWorlds.Framing/`
    - `src/QuestWorlds.Resolution/`
    - `src/QuestWorlds.Outcome/`
    - `src/QuestWorlds.Web/`
    - `tests/QuestWorlds.Session.Tests/`
    - `tests/QuestWorlds.Framing.Tests/`
    - `tests/QuestWorlds.Resolution.Tests/`
    - `tests/QuestWorlds.Outcome.Tests/`
  - Add project references between modules
  - Add `InternalsVisibleTo` for test projects

---

## Phase 1: Framing Module

### Rating Value Object

- [x] **TEST + IMPLEMENT: Rating parses simple numeric notation**
  - **USE COMMAND**: `/test-first when parsing rating "15" should create rating with base 15 and no masteries`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_parsing_rating_15_should_create_rating_with_base_15_and_no_masteries.cs`
  - Test should verify:
    - `Rating.Parse("15")` returns Rating with Base = 15
    - Rating has Masteries = 0
    - ToString() returns "15"
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `Rating` readonly record struct in `QuestWorlds.Framing`
    - Implement `Parse(string notation)` static method
    - Use regex to parse numeric notation

- [x] **TEST + IMPLEMENT: Rating parses mastery notation with single mastery**
  - **USE COMMAND**: `/test-first when parsing rating "5M" should create rating with base 5 and one mastery`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_parsing_rating_5M_should_create_rating_with_base_5_and_one_mastery.cs`
  - Test should verify:
    - `Rating.Parse("5M")` returns Rating with Base = 5
    - Rating has Masteries = 1
    - ToString() returns "5M"
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Extend regex pattern to handle "M" suffix
    - Default mastery count to 1 when M present without number

- [ ] **TEST + IMPLEMENT: Rating parses mastery notation with multiple masteries**
  - **USE COMMAND**: `/test-first when parsing rating "6M2" should create rating with base 6 and two masteries`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_parsing_rating_6M2_should_create_rating_with_base_6_and_two_masteries.cs`
  - Test should verify:
    - `Rating.Parse("6M2")` returns Rating with Base = 6
    - Rating has Masteries = 2
    - ToString() returns "6M2"
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Extend regex pattern to handle "M{n}" suffix

- [ ] **TEST + IMPLEMENT: Rating rejects invalid base values**
  - **USE COMMAND**: `/test-first when creating rating with base 0 should throw argument exception`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_creating_rating_with_invalid_base_should_throw.cs`
  - Test should verify:
    - `new Rating(0)` throws ArgumentOutOfRangeException
    - `new Rating(21)` throws ArgumentOutOfRangeException
    - `new Rating(-1)` throws ArgumentOutOfRangeException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add validation in Rating constructor for base 1-20

### Modifier Value Object

- [ ] **TEST + IMPLEMENT: Modifier validates stretch must be negative**
  - **USE COMMAND**: `/test-first when creating stretch modifier with positive value should throw`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_creating_stretch_modifier_with_positive_value_should_throw.cs`
  - Test should verify:
    - `new Modifier(ModifierType.Stretch, -5)` succeeds
    - `new Modifier(ModifierType.Stretch, -10)` succeeds
    - `new Modifier(ModifierType.Stretch, 5)` throws ArgumentException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `Modifier` readonly record struct
    - Create `ModifierType` enum
    - Validate sign based on modifier type

- [ ] **TEST + IMPLEMENT: Modifier validates allowed values**
  - **USE COMMAND**: `/test-first when creating modifier with invalid value should throw`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_creating_modifier_with_invalid_value_should_throw.cs`
  - Test should verify:
    - Values ±5 and ±10 are allowed
    - Value of 3 throws ArgumentOutOfRangeException
    - Value of 15 throws ArgumentOutOfRangeException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add validation for allowed modifier values

### TargetNumber Value Object

- [ ] **TEST + IMPLEMENT: TargetNumber calculates effective base with modifier**
  - **USE COMMAND**: `/test-first when target number has modifier should calculate effective base`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_target_number_has_modifier_should_calculate_effective_base.cs`
  - Test should verify:
    - TN(10, 0, +5) has EffectiveBase = 15
    - TN(10, 0, -5) has EffectiveBase = 5
    - TN(18, 0, +5) has EffectiveBase = 20 (clamped)
    - TN(3, 0, -5) has EffectiveBase = 1 (clamped)
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `TargetNumber` readonly record struct
    - Implement EffectiveBase with clamping to 1-20

- [ ] **TEST + IMPLEMENT: TargetNumber creates from Rating**
  - **USE COMMAND**: `/test-first when creating target number from rating should preserve masteries`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_creating_target_number_from_rating_should_preserve_masteries.cs`
  - Test should verify:
    - `TargetNumber.FromRating(new Rating(5, 2))` has Base = 5, Masteries = 2
    - `TargetNumber.FromRating(new Rating(10, 1), -5)` has modifier applied
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `FromRating` static factory method

### ContestFrame Aggregate

- [ ] **TEST + IMPLEMENT: ContestFrame requires prize and resistance**
  - **USE COMMAND**: `/test-first when creating contest frame should require prize and resistance`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_creating_contest_frame_should_require_prize_and_resistance.cs`
  - Test should verify:
    - `new ContestFrame("Sneak past guards", new TargetNumber(10))` succeeds
    - Empty prize throws ArgumentException
    - Null prize throws ArgumentException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `ContestFrame` class
    - Add constructor validation

- [ ] **TEST + IMPLEMENT: ContestFrame tracks player ability**
  - **USE COMMAND**: `/test-first when setting player ability should update frame`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_setting_player_ability_should_update_frame.cs`
  - Test should verify:
    - SetPlayerAbility("Stealth", new Rating(15)) updates PlayerAbilityName and PlayerRating
    - Empty ability name throws ArgumentException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `SetPlayerAbility` method
    - Add `PlayerAbilityName` and `PlayerRating` properties

- [ ] **TEST + IMPLEMENT: ContestFrame calculates player target number with modifiers**
  - **USE COMMAND**: `/test-first when getting player target number should include all modifiers`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_getting_player_target_number_should_include_all_modifiers.cs`
  - Test should verify:
    - Frame with no modifiers returns TN from rating
    - Frame with +5 augment and -5 stretch returns TN with net 0 modifier
    - Frame with multiple modifiers sums them correctly
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `ApplyModifier` method
    - Add `GetPlayerTargetNumber` method that sums modifiers

- [ ] **TEST + IMPLEMENT: ContestFrame knows when ready for resolution**
  - **USE COMMAND**: `/test-first when frame is complete should be ready for resolution`
  - Test location: `tests/QuestWorlds.Framing.Tests/`
  - Test file: `When_frame_is_complete_should_be_ready_for_resolution.cs`
  - Test should verify:
    - Frame with only prize/resistance is NOT ready
    - Frame with prize, resistance, and player ability IS ready
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `IsReadyForResolution` property

---

## Phase 2: Resolution Module

### Success Calculator

- [ ] **TEST + IMPLEMENT: Success calculator returns 1 success when roll below TN**
  - **USE COMMAND**: `/test-first when roll is below target number should return one success`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_roll_is_below_target_number_should_return_one_success.cs`
  - Test should verify:
    - Roll 5 vs TN 10 = 1 success
    - Roll 1 vs TN 10 = 1 success
    - Roll 9 vs TN 10 = 1 success
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `ISuccessCalculator` internal interface
    - Create `SuccessCalculator` internal class
    - Create `RollResult` internal record

- [ ] **TEST + IMPLEMENT: Success calculator returns 2 successes when roll equals TN (big success)**
  - **USE COMMAND**: `/test-first when roll equals target number should return two successes`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_roll_equals_target_number_should_return_two_successes.cs`
  - Test should verify:
    - Roll 10 vs TN 10 = 2 successes
    - RollResult.IsBigSuccess = true
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add big success check in Calculate method
    - Set IsBigSuccess flag

- [ ] **TEST + IMPLEMENT: Success calculator returns 0 successes when roll above TN**
  - **USE COMMAND**: `/test-first when roll is above target number should return zero successes`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_roll_is_above_target_number_should_return_zero_successes.cs`
  - Test should verify:
    - Roll 11 vs TN 10 = 0 successes
    - Roll 20 vs TN 10 = 0 successes
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add failure case in Calculate method

- [ ] **TEST + IMPLEMENT: Success calculator adds mastery successes**
  - **USE COMMAND**: `/test-first when target number has masteries should add to success total`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_target_number_has_masteries_should_add_to_success_total.cs`
  - Test should verify:
    - Roll 5 vs TN 10 with 1 mastery = 2 successes (1 base + 1 mastery)
    - Roll 10 vs TN 10 with 2 masteries = 4 successes (2 big + 2 masteries)
    - Roll 15 vs TN 10 with 1 mastery = 1 success (0 base + 1 mastery)
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add TotalSuccesses = BaseSuccesses + Masteries

### Winner Decider

- [ ] **TEST + IMPLEMENT: Winner decider determines winner by success count**
  - **USE COMMAND**: `/test-first when one side has more successes should win`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_one_side_has_more_successes_should_win.cs`
  - Test should verify:
    - Player 2 successes vs Resistance 1 success → Player wins with degree 1
    - Player 1 success vs Resistance 3 successes → Resistance wins with degree 2
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `IWinnerDecider` internal interface
    - Create `WinnerDecider` internal class
    - Create `ContestWinner` public enum

- [ ] **TEST + IMPLEMENT: Winner decider breaks ties with higher roll**
  - **USE COMMAND**: `/test-first when successes are tied should use higher roll as tiebreaker`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_successes_are_tied_should_use_higher_roll_as_tiebreaker.cs`
  - Test should verify:
    - Both 1 success, player rolls 8, resistance rolls 5 → Player wins degree 0
    - Both 1 success, player rolls 5, resistance rolls 8 → Resistance wins degree 0
    - Both 1 success, both roll 7 → Tie
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add tie-breaking logic in Decide method

### Contest Resolver

- [ ] **TEST + IMPLEMENT: Contest resolver orchestrates full resolution**
  - **USE COMMAND**: `/test-first when resolving contest should return complete result`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_resolving_contest_should_return_complete_result.cs`
  - Test should verify:
    - Given known dice rolls (via fake IDiceRoller)
    - ResolutionResult contains both rolls
    - ResolutionResult contains both success counts
    - ResolutionResult contains winner and degree
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `IContestResolver` public interface
    - Create `ContestResolver` internal class
    - Create `IDiceRoller` internal interface
    - Create `ResolutionResult` public record

- [ ] **TEST + IMPLEMENT: Contest resolver rejects incomplete frames**
  - **USE COMMAND**: `/test-first when frame is not ready should throw`
  - Test location: `tests/QuestWorlds.Resolution.Tests/`
  - Test file: `When_frame_is_not_ready_should_throw.cs`
  - Test should verify:
    - Resolving frame without player ability throws InvalidOperationException
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add IsReadyForResolution check at start of Resolve

---

## Phase 3: Outcome Module

- [ ] **TEST + IMPLEMENT: Outcome interpreter maps degree to description**
  - **USE COMMAND**: `/test-first when interpreting result should map degree to description`
  - Test location: `tests/QuestWorlds.Outcome.Tests/`
  - Test file: `When_interpreting_result_should_map_degree_to_description.cs`
  - Test should verify:
    - Degree 0 → Marginal
    - Degree 1 → Minor
    - Degree 2 → Major
    - Degree 3+ → Complete
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `IOutcomeInterpreter` public interface
    - Create `OutcomeInterpreter` internal class
    - Create `DegreeDescription` public enum
    - Create `ContestOutcome` public record

- [ ] **TEST + IMPLEMENT: Outcome interpreter looks up benefit/consequence modifier**
  - **USE COMMAND**: `/test-first when interpreting victory should return positive modifier`
  - Test location: `tests/QuestWorlds.Outcome.Tests/`
  - Test file: `When_interpreting_victory_should_return_positive_modifier.cs`
  - Test should verify:
    - Victory degree 0 → +5
    - Victory degree 1 → +10
    - Victory degree 2 → +15
    - Victory degree 3 → +20
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `IBenefitConsequenceLookup` internal interface
    - Create `BenefitConsequenceLookup` internal class

- [ ] **TEST + IMPLEMENT: Outcome interpreter returns negative modifier for defeat**
  - **USE COMMAND**: `/test-first when interpreting defeat should return negative modifier`
  - Test location: `tests/QuestWorlds.Outcome.Tests/`
  - Test file: `When_interpreting_defeat_should_return_negative_modifier.cs`
  - Test should verify:
    - Defeat degree 0 → -5
    - Defeat degree 1 → -10
    - Defeat degree 2 → -15
    - Defeat degree 3 → -20
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add defeat case to BenefitConsequenceLookup

- [ ] **TEST + IMPLEMENT: Outcome includes complete contest context**
  - **USE COMMAND**: `/test-first when interpreting should include contest context`
  - Test location: `tests/QuestWorlds.Outcome.Tests/`
  - Test file: `When_interpreting_should_include_contest_context.cs`
  - Test should verify:
    - ContestOutcome.Prize matches frame
    - ContestOutcome.PlayerAbilityName matches frame
    - ContestOutcome.PlayerRating formatted correctly
    - ContestOutcome.Summary provides human-readable description
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Complete ContestOutcome with all context fields

---

## Phase 4: Session Module

- [ ] **TEST + IMPLEMENT: Session coordinator creates session with unique ID**
  - **USE COMMAND**: `/test-first when creating session should generate unique ID`
  - Test location: `tests/QuestWorlds.Session.Tests/`
  - Test file: `When_creating_session_should_generate_unique_id.cs`
  - Test should verify:
    - CreateSession returns Session with non-empty ID
    - Session ID is 6 characters
    - Session ID contains only valid characters (A-Z excluding ambiguous, 2-9)
    - Multiple calls generate different IDs
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `ISessionCoordinator` public interface
    - Create `SessionCoordinator` internal class
    - Create `ISessionIdGenerator` internal interface
    - Create `SessionIdGenerator` internal class
    - Create `Session` public class

- [ ] **TEST + IMPLEMENT: Session coordinator allows players to join**
  - **USE COMMAND**: `/test-first when player joins session should be added to participants`
  - Test location: `tests/QuestWorlds.Session.Tests/`
  - Test file: `When_player_joins_session_should_be_added_to_participants.cs`
  - Test should verify:
    - JoinSession adds player to session
    - Session.Players contains the new participant
    - Participant has correct name and role
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add JoinSession method to coordinator
    - Create `Participant` public record
    - Create `ParticipantRole` public enum

- [ ] **TEST + IMPLEMENT: Session coordinator rejects invalid session ID**
  - **USE COMMAND**: `/test-first when joining with invalid session ID should throw`
  - Test location: `tests/QuestWorlds.Session.Tests/`
  - Test file: `When_joining_with_invalid_session_id_should_throw.cs`
  - Test should verify:
    - JoinSession with non-existent ID throws exception
    - GetSession with non-existent ID returns null
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `ISessionRepository` internal interface
    - Create `InMemorySessionRepository` internal class
    - Add validation in JoinSession

- [ ] **TEST + IMPLEMENT: Session tracks state transitions**
  - **USE COMMAND**: `/test-first when session state changes should track correctly`
  - Test location: `tests/QuestWorlds.Session.Tests/`
  - Test file: `When_session_state_changes_should_track_correctly.cs`
  - Test should verify:
    - New session starts in WaitingForPlayers state
    - TransitionTo updates State property
    - State enum has all required values
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `SessionState` public enum
    - Add State property and TransitionTo method to Session

---

## Phase 5: Web Integration

- [ ] **TEST + IMPLEMENT: DI registration for all modules**
  - **USE COMMAND**: `/test-first when registering services should resolve all interfaces`
  - Test location: `tests/QuestWorlds.Web.Tests/`
  - Test file: `When_registering_services_should_resolve_all_interfaces.cs`
  - Test should verify:
    - ISessionCoordinator resolves
    - IContestFramer resolves
    - IContestResolver resolves
    - IOutcomeInterpreter resolves
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create ServiceCollectionExtensions in each module
    - Register all services in Program.cs

- [ ] **IMPLEMENT: SignalR ContestHub**
  - Create ContestHub with methods:
    - CreateSession(gmName) → sessionId
    - JoinSession(sessionId, playerName)
    - FrameContest(prize, resistanceTn)
    - SubmitAbility(abilityName, rating)
    - ApplyModifier(type, value)
    - ResolveContest()
  - Wire up SignalR group management
  - Broadcast state changes to participants

- [ ] **IMPLEMENT: GM Razor Pages**
  - Create `/GM/Index` - Session creation page
  - Create `/GM/Contest` - Contest framing and resolution page
  - Add Bootstrap styling for responsive layout

- [ ] **IMPLEMENT: Player Razor Pages**
  - Create `/Player/Join` - Session join page
  - Create `/Player/Contest` - Ability submission and results page
  - Add Bootstrap styling for mobile-friendly layout

---

## Phase 6: End-to-End Testing

- [ ] **TEST: Complete contest workflow**
  - Test location: `tests/QuestWorlds.Web.Tests/`
  - Test file: `Complete_contest_workflow_tests.cs`
  - Test should verify:
    - GM creates session
    - Player joins session
    - GM frames contest with prize and resistance
    - Player submits ability
    - GM applies modifiers
    - Resolution produces valid outcome
    - Both participants receive outcome

---

## Dependencies

```
Phase 0 (Setup)
    ↓
Phase 1 (Framing) ──→ Phase 2 (Resolution) ──→ Phase 3 (Outcome)
                                                      ↓
Phase 4 (Session) ────────────────────────────→ Phase 5 (Web)
                                                      ↓
                                              Phase 6 (E2E)
```

## Risk Mitigation

- [ ] **RISK: SignalR connection management**
  - Mitigation: Implement connection tracking and reconnection handling
  - Test disconnection and reconnection scenarios

- [ ] **RISK: Concurrent session access**
  - Mitigation: Use ConcurrentDictionary for session storage
  - Test concurrent operations

- [ ] **RISK: Mobile browser compatibility**
  - Mitigation: Test on multiple mobile browsers
  - Use Bootstrap responsive utilities
