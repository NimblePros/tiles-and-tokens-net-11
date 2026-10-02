# Plan: Tiles & Tokens Demo App (.NET 11 / C# 15)

## Context

This repo is the companion demo for a 5-part NimblePros blog series and a 30-minute conference talk:

> How do you model a game turn when a player can either roll dice, play a card, or pass without relying on vague string flags, null-heavy wrapper classes, or unsafe casting?

The app uses board game mechanics (player turns, tile placement validation, victory conditions) to show the C# 15 / .NET 11 domain modeling features:

| Post | Feature | Blog tracking issue |
|---|---|---|
| 1 | Intro to .NET 11, the "before" anti-patterns, series roadmap | NimblePros/nimblepros-blog-hugo#107 |
| 2 | Discriminated unions | NimblePros/nimblepros-blog-hugo#108 |
| 3 | Closed hierarchies | NimblePros/nimblepros-blog-hugo#109 |
| 4 | System.Text.Json + union state | NimblePros/nimblepros-blog-hugo#110 |
| 5 | EF Core 11 indexes on deeply nested complex types | NimblePros/nimblepros-blog-hugo#111 |

Series tracking issue: NimblePros/nimblepros-blog-hugo#106

**Audience:** Blog readers and talk attendees who clone the repo. The code is teaching material: favor clarity over cleverness, keep files small, and make each feature easy to find.

---

## Ground rules for Claude

1. **Verify before you write.** C# 15 / .NET 11 features changed across previews. Before you implement a feature, confirm the actual syntax and behavior against the installed SDK, using the compiler, a scratch project, or the official docs. Do not write code from memory and assume it compiles.
2. **Record what you find.** Keep `docs/feature-status.md` current. For each feature, list the SDK version tested, the exact syntax that compiles, whether a preview `LangVersion` or feature flag is needed, and any limitations found. The blog posts are written from this file.
3. **Never fake a feature.** If a feature is missing or behaves differently from what this plan assumes, stop and report it. Do not quietly swap in a workaround (custom converters, `OneOf`, hand-written base classes) and present it as the native feature. A clearly labeled "workaround until GA" is acceptable only after the author agrees to it.
4. **Code samples in this plan are illustrative.** Syntax marked `// VERIFY` is a best guess.
5. **One milestone per post.** Each milestone ends with a green build, passing tests, a working demo runner, and a git tag (`post-1` … `post-5`). Readers check out a tag to follow along.
6. **Keep the "before" code.** The anti-pattern code from Post 1 stays in the repo so the posts can show before/after diffs.
7. **Testing stack is fixed:** xUnit for tests, Shouldly for assertions, NSubstitute for any mocks or fakes. **Never use Moq.**
8. **Local resources run through Aspire.** Do not add docker compose files or a separate Testcontainers setup for Postgres.

---

## Game rules: "Tiles & Tokens"

Keep the rules small. The game exists only to give the type system something real to model.

- 2–4 players on an **8×8 grid**. Each cell is empty or holds one **tile**.
- A tile has a **terrain** (`Plains`, `Forest`, `Water`, `Mountain`) and an **owner**.
- Each player starts with a **hand of 3 cards**. A card either places a specific terrain tile or is a special card (e.g., `SwapTiles`, `BonusPoints`).
- On their turn, a player takes **exactly one action**:
  - **Roll dice:** roll 2d6. The total converts to points, and doubles grant a bonus card draw.
  - **Play a card:** play one card from the hand. Tile cards require a target `Position` and must pass placement validation.
  - **Pass:** do nothing. A player who passes 3 turns in a row is eliminated.
- **Placement rules** (all must pass):
  - The cell must be in bounds and empty.
  - After the first tile, the new tile must be orthogonally adjacent to one of the player's own tiles.
  - Terrain compatibility: `Water` cannot be placed next to `Mountain`.
- **Victory conditions** (chosen at game setup):
  - `MostPoints(int Rounds)`: the highest score after N rounds wins.
  - `FirstToEdge`: the first player whose connected tiles span from one edge of the board to the opposite edge wins.
  - `LastPlayerStanding`: the last non-eliminated player wins.
- **Randomness is injected** (`IDiceRoller`, `IDeckShuffler`) so tests and demos are deterministic. Use seeded implementations in the demo runner.

---

## Solution layout

```
TilesAndTokens.slnx
global.json                         # pin the .NET 11 SDK version used for verification
Directory.Build.props               # TargetFramework net11.0, LangVersion, Nullable, TreatWarningsAsErrors
Directory.Packages.props            # central package management
README.md                           # what this is, how to run, tag-per-post table
docs/
  feature-status.md                 # verified syntax/limitations per feature (see Ground rules)
  game-rules.md                     # the rules above
src/
  TilesAndTokens.Legacy/            # Post 1 "before" code: anti-patterns, deliberately C# 14 style
  TilesAndTokens.Domain/            # Posts 2–3: unions, closed hierarchies, game engine
  TilesAndTokens.Serialization/     # Post 4: System.Text.Json
  TilesAndTokens.Persistence/       # Post 5: EF Core 11
  TilesAndTokens.Demo/              # console runner, one command per post + `all`
  TilesAndTokens.AppHost/           # Aspire AppHost: Postgres + Demo runner (added in Milestone 5)
  TilesAndTokens.ServiceDefaults/   # Aspire service defaults (added in Milestone 5)
tests/
  TilesAndTokens.Legacy.Tests/
  TilesAndTokens.Domain.Tests/
  TilesAndTokens.Serialization.Tests/
  TilesAndTokens.Persistence.Tests/
```

- **Test stack:** xUnit (v3) + Shouldly. Use NSubstitute for any mocks (e.g., `IDiceRoller`, `IDeckShuffler` when a seeded fake isn't clearer). Never Moq.
- **Demo runner:** `dotnet run --project src/TilesAndTokens.Demo -- <command>`, where `<command>` is `legacy`, `unions`, `closed`, `json`, `efcore`, or `all`. Each milestone command prints a short narrated walkthrough that can be shown live in the talk.
- **`all` command:** runs every milestone demo that exists, in order (`legacy` → `unions` → `closed` → `json` → `efcore`). It prints a section header for each and pauses for a keypress between sections, so it can drive the talk. Add a `--no-pause` flag for CI and recordings. Each milestone registers its command with `all` when it lands. Under Aspire, `efcore` gets its connection string from the AppHost. Without the AppHost, `all` skips `efcore` with a clear message.
- **Warnings as errors** is enabled, so that missing switch cases fail the build during the talk demo. If exhaustiveness diagnostics are warnings, note their IDs in `feature-status.md`.

---

## Milestone 1 (Post 1): Scaffold + the "before" picture

**Goal:** A runnable solution and three anti-pattern implementations of a game turn, each with a test that exposes its weakness.

### Tasks
1. Install and verify the .NET 11 SDK. Pin it in `global.json` and record the version in `feature-status.md`.
2. Create the solution, `Directory.Build.props`, central package management, README, and `docs/game-rules.md`.
3. In `TilesAndTokens.Legacy`, model a turn three ways:
   - **String flags:** `class StringTurn { string ActionType; int? DiceTotal; string? CardId; }`
   - **Null-heavy wrapper:** `class TurnAction { DiceRoll? Roll; Card? Card; bool IsPass; }`
   - **Base class + casting:** `abstract class LegacyAction`, then `RollDiceAction`, `PlayCardAction`, `PassAction`, with an `if/else` cast chain ending in `default` / `throw`
4. Write a legacy `Result<T>` wrapper (`IsSuccess`, `Value`, `Error`) used by a legacy placement validator. Post 2 will replace it.
5. **Tests that demonstrate the problems.** These pass by proving the bug is possible:
   - A typo'd action string compiles and silently falls through.
   - `TurnAction` with both `Roll` and `Card` set (an invalid state) is constructible.
   - Adding a new `TradeAction` subclass compiles, and the cast chain throws at runtime.
   - `Result<T>.Value` can be read on failure without a compile-time error.
6. Add the Demo runner `legacy` command, which walks through the three failure modes. Also add the `all` command skeleton (it runs `legacy` only for now).

### Done when
- `dotnet build` and `dotnet test` are green, and `dotnet run -- legacy` and `dotnet run -- all` both run.
- Tag `post-1`.

---

## Milestone 2 (Post 2): Discriminated unions

**Goal:** Replace the legacy turn model and `Result<T>` with native unions.

### Types (`TilesAndTokens.Domain`)
```csharp
// Case types
public sealed record RollDice(PlayerId Player);
public sealed record PlayCard(PlayerId Player, CardId Card, Position? Target);
public sealed record Pass(PlayerId Player);

public union GameAction(RollDice, PlayCard, Pass);              // VERIFY syntax

// Placement outcome replaces legacy Result<T>
public sealed record Placed(Tile Tile, int PointsAwarded);
public sealed record Rejected(string Reason, PlacementRuleId Rule);
public sealed record Blocked(Position Position);              // cell already occupied

public union PlacementResult(Placed, Rejected, Blocked);       // VERIFY syntax
```
Supporting value types: `PlayerId`, `CardId`, `Position(int X, int Y)`, `Terrain` enum, `Tile(Position, Terrain, PlayerId Owner)`, `Card`, `Player`.

### Engine
- `TurnEngine.Apply(GameState state, GameAction action) → TurnOutcome`: a `switch` expression over `GameAction` with **no discard arm**.
- `TurnOutcome` is also a union (for example `ActionApplied | ActionInvalid | GameOver`) if that reads naturally. Do not force it.

### Tests
- Every `GameAction` case is handled. Assigning each case type to `GameAction` works through implicit conversion. VERIFY.
- `PlacementResult` cases round-trip through the engine.
- **Exhaustiveness proof:** keep a commented-out `TradeResources` case plus a test or script note showing the compiler diagnostic you get when it is added to the union and left unhandled. Capture the exact diagnostic ID and message in `feature-status.md` for the blog post.

### Investigate and record in `feature-status.md`
- How unions lower: boxing, struct vs. class, allocations. Add a BenchmarkDotNet micro-benchmark only if it adds clear value to the post.
- Nullability interactions (`GameAction?`, a `null` case value).
- Whether unions can be generic (e.g., `union Result<T>(T, Error)`), and pattern matching on cases vs. on the union.

### Demo runner
`unions`: plays 5 scripted turns with a seeded roller, printing each action and outcome.

### Done when
Build and tests are green, the Legacy project is untouched, `all` includes `unions`, and the tag `post-2` is created.

---

## Milestone 3 (Post 3): Closed hierarchies

**Goal:** Model `VictoryCondition` and `PlacementRule` as closed hierarchies whose cases share behavior. Validation returns the `PlacementResult` union from Milestone 2.

### Types
```csharp
public closed abstract record VictoryCondition                 // VERIFY: modifier, record vs class, implicit abstract?
{
    public abstract string Name { get; }
}
public sealed record MostPoints(int Rounds) : VictoryCondition { ... }
public sealed record FirstToEdge : VictoryCondition { ... }
public sealed record LastPlayerStanding : VictoryCondition { ... }

public closed abstract record PlacementRule                     // VERIFY
{
    public abstract PlacementResult Check(Board board, Tile candidate);
}
public sealed record InBoundsRule : PlacementRule { ... }
public sealed record CellEmptyRule : PlacementRule { ... }      // → Blocked
public sealed record AdjacencyRule : PlacementRule { ... }      // → Rejected
public sealed record TerrainCompatibilityRule : PlacementRule { ... } // → Rejected
```

### Engine
- `PlacementValidator` runs the rules in order and returns the first non-`Placed` result, or `Placed`.
- `VictoryChecker.Check(GameState, VictoryCondition) → PlayerId?`: a `switch` with **no discard arm**. Use pattern matching here deliberately. Instance methods would be fine in production, but the post is about exhaustiveness.
- `FirstToEdge` needs a flood-fill or BFS over a player's connected tiles. Keep it readable.

### Tests
- Each placement rule, both passing and failing.
- Each victory condition, using a hand-built board fixture.
- **Exhaustiveness proof:** the same approach as Milestone 2. Add `MostTerritory` and capture the diagnostic.
- **Boundary proof:** try deriving from `VictoryCondition` in a different assembly (e.g., a small `TilesAndTokens.Domain.Extensions` test project) and record the compiler error. Exclude it from the normal build, or keep it as a documented script, so the build stays green.

### Record in `feature-status.md`
- Where derived types can be declared (same file, assembly, or namespace?).
- `closed` vs. `sealed` vs. `abstract`, and whether intermediate types can themselves be closed.
- A decision checklist: union vs. closed hierarchy.

### Demo runner
`closed`: validates several placements (one per rule failure), then plays a short game to a `FirstToEdge` win.

### Done when
Build and tests are green, `all` includes `closed`, and the tag `post-3` is created.

---

## Milestone 4 (Post 4): System.Text.Json and union state

**Goal:** Save and load a mid-game `GameState`, including union and closed-hierarchy values, using built-in System.Text.Json support.

### Model
```csharp
public sealed record GameState(
    GameId Id,
    Board Board,                       // 8x8, contains Tiles
    IReadOnlyList<Player> Players,     // hands, scores, consecutive passes
    VictoryCondition Victory,          // closed hierarchy
    IReadOnlyList<GameAction> TurnLog, // union
    int Round,
    PlayerId CurrentPlayer);
```

### Tasks
1. In `TilesAndTokens.Serialization`, add `GameStateSerializer.Save(GameState, Stream)` and `.Load(Stream)`.
2. First, serialize **with no attributes or converters**. Record the exact JSON produced for a union value, a `List<GameAction>`, and a `VictoryCondition`, and whether deserialization round-trips. This is the core finding for the post.
3. Add **only** the configuration that turns out to be necessary. If attributes such as `[JsonPolymorphic]` are still required, note in `feature-status.md` exactly which ones and why.
4. Add a source-generated `JsonSerializerContext` (`TilesAndTokensJsonContext`) and confirm it works with unions and closed hierarchies. Run a quick trimming or AOT publish of the Demo app, and record the result.
5. Customize the output: discriminator property name (`"$type"` vs. `"kind"`), camelCase naming, and indented output for the demo.
6. **Versioning scenario:** commit `samples/save-v1.json`, then add a new action case. Show that the v1 file still loads, and record what happens on an unknown discriminator.
7. **Legacy comparison:** add a small `[JsonDerivedType]` / custom `JsonConverter` example in the Legacy project. Post 4 needs the "old way" to contrast with.

### Tests
- Round-trip equality for a full `GameState` built by the engine after 10 seeded turns.
- A test of the JSON shape: **compare against committed expected-JSON files** (decided with the author at the start of Milestone 4). No snapshot library.
- Unknown-case behavior.

### Demo runner
`json`: plays N turns, saves to `./saves/demo.json`, prints the JSON, reloads it, and resumes play.

### Done when
Build and tests are green, the AOT/trim result is recorded, `all` includes `json`, and the tag `post-4` is created.

---

## Milestone 5 (Post 5): EF Core 11 with deeply nested complex types

**Goal:** Persist saved games with EF Core 11. Map `Board → Tiles → Position/Terrain` as complex types, index nested properties, and query "games where a player has a tile on the board edge."

### Decided
- **Provider:** Postgres only, via the Npgsql EF Core provider. This ties into the existing NimblePros Postgres post.
- **Local resources:** Aspire. `TilesAndTokens.AppHost` adds a Postgres server and database (`builder.AddPostgres("postgres").AddDatabase("tilesandtokens")`, plus pgAdmin or pgweb if handy). It references the Demo project and waits for the database. The Demo project uses the Aspire Npgsql EF Core client integration (`AddNpgsqlDbContext<TilesAndTokensDbContext>("tilesandtokens")`) and `ServiceDefaults`. Verify package names and APIs against the Aspire version that ships alongside .NET 11, and record them in `feature-status.md`.
- **Running the full demo:** `dotnet run --project src/TilesAndTokens.AppHost` starts Postgres and the Demo runner. Set the Demo's launch args to `all` in the AppHost so the whole series can be played from the Aspire dashboard.

### Decision to make during the milestone
- **Mapping strategy** for the tile collection: a JSON column vs. table splitting. It depends on what EF Core 11 supports for indexing nested complex types on Npgsql. Verify first, then decide, and record the reasoning.

### Model
```csharp
public sealed class SavedGame                       // EF entity (aggregate root for persistence)
{
    public GameId Id { get; private set; }
    public DateTimeOffset SavedAt { get; private set; }
    public BoardSnapshot Board { get; private set; }        // complex type
    public VictoryCondition Victory { get; private set; }   // closed hierarchy → mapping TBD
    public string TurnLogJson { get; private set; }         // fallback if unions don't map natively; VERIFY
}
// BoardSnapshot → IReadOnlyList<TileSnapshot> → PositionSnapshot(X, Y), Terrain, OwnerId
```
- Decide whether persistence reuses the domain records directly or uses snapshot types. Prefer the domain types if EF Core 11 maps them cleanly, and record the decision.

### Tasks
1. Add the Aspire `AppHost` and `ServiceDefaults` projects and wire up Postgres. Apply migrations at Demo startup (dev only) or through a small migration step, and note which.
2. Add `TilesAndTokensDbContext` with complex type configuration for the nested board.
3. Add **indexes on nested complex-type properties**, e.g. `Board.Tiles[].Position.X` and `Board.Tiles[].OwnerId`. Verify the exact API and record it.
4. Generate the initial migration. Commit it, and copy the generated **index DDL** into `feature-status.md` for the blog post.
5. Map `VictoryCondition` (TPH with a discriminator, or a JSON column). Determine what EF Core 11 supports for union-typed properties (`GameAction`). If they are unsupported, use an explicit, labeled fallback (JSON column via the Milestone 4 serializer) and document it.
6. Add a query service with LINQ for:
   - games where a given player owns a tile on any edge (`X == 0 || X == 7 || Y == 0 || Y == 7`)
   - games using a given victory condition type
7. Capture the generated SQL with `ToQueryString()`. Capture the execution plan (`EXPLAIN ANALYZE`) with enough seeded rows that the index is used, and save it to `docs/` for the post.
8. **Gotchas:** change the nested shape (e.g., add `Tile.Rotation`), generate a second migration, and record what EF produces. Also cover nullability of nested complex types and Npgsql-specific behavior (e.g., `jsonb` vs. columns, GIN vs. B-tree indexes).

### Tests
- Persistence integration tests use **Aspire's testing support** (`Aspire.Hosting.Testing` / `DistributedApplicationTestingBuilder`) to start the AppHost's Postgres. Share one instance per test collection through an xUnit fixture. Cover the save/load round-trip and the edge query.
- A test that asserts the index exists, by querying `pg_indexes`.
- Unit tests for the query service's logic use NSubstitute where a seam is needed. Never Moq.

### Demo runner
`efcore`: runs under the Aspire AppHost, seeds about 1,000 generated games, runs the edge query, and prints the SQL and timing.

### Series finale
The `efcore` command ends with a full-pipeline walkthrough: union action → closed-rule validation → STJ snapshot → EF save → query. When run through `all` under the AppHost, this is the closing demo of the talk.

### Done when
- Build and tests are green (a container runtime is required for the Aspire integration tests).
- Migrations are committed, and the SQL and execution plan are captured in `docs/`.
- `dotnet run --project src/TilesAndTokens.AppHost` runs the complete `all` demo.
- The tag `post-5` is created.

---

## Repo-wide deliverables

- `README.md` with a table of tag → post → what to look at → command to run.
- `docs/feature-status.md`, which is the source of truth for the blog posts.
- A CI workflow (GitHub Actions) that runs build and tests on the pinned .NET 11 SDK, with Docker available for the Aspire integration tests, plus `dotnet run --project src/TilesAndTokens.Demo -- all --no-pause` as a smoke test.
- A `.claude/CLAUDE.md` in the demo repo that points to this plan and repeats the ground rules (including xUnit + Shouldly + NSubstitute, never Moq, and Aspire for local resources).

## Decisions made

- Database: **Postgres**, run locally through **Aspire**.
- Tests: **xUnit** + **Shouldly**. Mocks: **NSubstitute**, never Moq.
- An `all` demo command runs every milestone's demo in order.
- Repo name: **`tiles-and-tokens-net-11`** (matches the local folder name). Use it in the README and CI.
- Repo visibility: **private** during development.
- Milestone 4 JSON shape tests: **committed expected-JSON files**, no snapshot library.

## Open questions for the author

_None right now._

## Deferred decisions

_None right now._
