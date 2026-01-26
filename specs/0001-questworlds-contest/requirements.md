# QuestWorlds Contest Specification

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

## Problem Statement

As a **GM (Game Master)** I would like to **run a QuestWorlds contest with my players online**, so that **we can resolve conflicts and obstacles in our tabletop RPG sessions remotely**.

As a **Player** I would like to **participate in QuestWorlds contests initiated by my GM**, so that **I can contribute my character's abilities to overcome obstacles**.

## Proposed Solution

A web-based application that facilitates QuestWorlds simple contest resolution between a GM and players:

1. **Session Management**: GM generates a unique session ID via the QuestWorlds website that can be shared with players
2. **Contest Framing**: GM creates contests by defining the prize (what's at stake) and setting the resistance's Target Number
3. **Player Participation**: Players join the session and submit their chosen ability and its rating
4. **Modifier Application**: GM applies any relevant modifiers (stretch, situational modifiers, hindrances, augments)
5. **Dice Resolution**: The system rolls D20 for both sides and evaluates the results
6. **Outcome Determination**: The system adjudicates the winner and determines consequences or benefits
7. **Result Sharing**: Both GM and Player see the contest outcome

## Requirements

### Functional Requirements

#### Session Management

- GM can generate a unique session ID
- Session ID can be shared with players (e.g., via URL or code)
- Players can join an existing session using the session ID
- Sessions should persist for the duration of the game

#### Contest Creation (GM)

- GM can create a new contest within a session
- GM must enter the prize (narrative description of what's at stake)
- GM must set the Target Number (TN) for the resistance
- GM can apply modifiers:
  - Stretch (penalty when ability doesn't quite fit)
  - Situational modifiers (bonuses/penalties based on circumstances)
  - Hindrances (penalties from obstacles or opposition)
  - Augments (bonuses from supporting abilities or help)

#### Player Participation

- Player can view the current contest (prize and any public information)
- Player can enter their chosen ability name
- Player can enter their ability rating
- Player can submit their contest entry

### Target Numbers and Masteries

- An ability is rated from 1-20
- An ability over 20 has one or more masteries (each mastery = +20)
- Notation: 5M = 5 with one mastery (25), 6M2 = 6 with two masteries (46)
- When rolling, use only the base value (1-20); masteries add successes

### Resistance Table

The GM sets resistance based on difficulty:

| Resistance       | TN  |
|------------------|-----|
| Simple           | 0   |
| Easy             | 0   |
| Routine          | 0   |
| Straightforward  | 5   |
| Base             | 10  |
| Challenging      | 15  |
| Hard             | 20  |
| Punishing        | 5M  |
| Exceptional      | 10M |

### Modifiers

GM can apply the following modifiers to the player's TN:

| Modifier Type        | Values      |
|----------------------|-------------|
| Stretch              | -5 or -10   |
| Situational modifier | ±5 or ±10   |
| Augment              | +5 or +10   |
| Hindrance            | -5 or -10   |
| Benefit/Consequence  | ±5 or ±10   |

#### Dice Resolution

- System rolls 1D20 for the player's ability check
- System rolls 1D20 for the resistance check
- Each roll is compared against its respective Target Number (base value only, excluding masteries)

#### Result Calculation

**Determining Successes:**

- Roll below TN = 1 success
- Roll equal to TN = 2 successes (big success)
- Roll above TN = 0 successes
- Each mastery = 1 additional success

**Determining Winner:**

- The participant with more successes wins the prize
- If successes are tied, the higher roll wins
- The degree of victory/defeat = difference between success totals

### Benefits and Consequences

Based on the degree of victory or defeat, the GM may assign benefits (bonuses) or consequences (penalties) that affect future contests:

| Outcome          | Modifier |
|------------------|----------|
| 3 degree defeat  | -20      |
| 2 degree defeat  | -15      |
| 1 degree defeat  | -10      |
| 0 degree defeat  | -5       |
| Tie              | 0        |
| 0 degree victory | +5       |
| 1 degree victory | +10      |
| 2 degree victory | +15      |
| 3 degree victory | +20      |

#### Result Display

- Both GM and Player see the dice rolls
- Both see the target numbers used
- Both see the number of successes achieved (including from masteries)
- Both see the final outcome:
  - Winner (player or resistance)
  - Degree of victory/defeat (0-3)
- Both see suggested benefits/consequences modifier based on degree

### Non-functional Requirements

#### Performance

- Dice rolls and calculations should be near-instantaneous
- Session state updates should propagate to all participants within 1 second

#### Scalability

- Support multiple concurrent sessions
- Support at least 6 players per session (typical tabletop group size)

#### Security

- Session IDs should be sufficiently random to prevent guessing
- Players should only see information appropriate to their role

#### Usability

- Interface should be simple and focused on the contest workflow
- Mobile-friendly for players who may be using phones/tablets
- Clear visual distinction between GM and Player views

#### Availability

- Web application should be accessible via modern browsers
- No installation required for end users

### Constraints and Assumptions

#### Constraints

- Must follow QuestWorlds SRD rules for simple contests
- Web-based solution (no native app required initially)

#### Assumptions

- Users have basic familiarity with QuestWorlds contest mechanics
- GM and players have internet connectivity
- One contest runs at a time within a session

### Out of Scope

- Extended contests (scored sequences, wagered sequences, chained sequences)
- Group simple contests (multiple players in single contest)
- Story points (spending for additional successes)
- Character sheet management
- Campaign/session history persistence
- User accounts and authentication
- Automated consequence tracking on character sheets
- Integration with virtual tabletop platforms
- Dice rolling for non-contest purposes

## Acceptance Criteria

### Definition of Done

- [ ] GM can generate and share a session ID
- [ ] Players can join a session using the ID
- [ ] GM can create a contest with prize and resistance TN (using resistance table or custom value)
- [ ] Player can submit ability name and rating (including masteries, e.g., "15" or "5M" or "6M2")
- [ ] GM can apply modifiers: stretch, situational, augment, hindrance, benefit/consequence
- [ ] System correctly rolls D20 for both player and resistance
- [ ] System correctly calculates successes:
  - [ ] Roll < TN = 1 success
  - [ ] Roll = TN = 2 successes (big success)
  - [ ] Roll > TN = 0 successes
  - [ ] Each mastery adds 1 success
- [ ] System correctly determines winner (more successes; high roll breaks ties)
- [ ] System correctly calculates degree of victory/defeat (difference in successes)
- [ ] Results displayed to both GM and Player showing rolls, successes, winner, and degree
- [ ] Benefits/consequences modifier shown based on degree (-20 to +20)

### Success Metrics

- Contests can be resolved in under 2 minutes from setup to outcome
- No manual dice rolling or calculation required
- Results match what would be calculated manually using QuestWorlds rules

### Testing Approach

- Unit tests for dice rolling and result calculation logic
- Unit tests for success level determination
- Integration tests for session management
- End-to-end tests for complete contest workflow
- Manual testing with actual QuestWorlds scenarios
