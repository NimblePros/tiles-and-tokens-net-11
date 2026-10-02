# Feature status

The source of truth for the blog posts. Each entry records what was **verified** against the installed SDK, not what the docs or previews promised.

> **Prerelease notice:** everything here was verified on a .NET 11 release candidate. It will be re-verified when .NET 11 goes GA (November 2026).

## Toolchain

| Item | Value | Verified |
|---|---|---|
| .NET SDK | `11.0.100-rc.1.26425.128` (RC1, 2026-09-08) | 2026-10-02 |
| Roslyn | `5.11.0-1.26425.128` | 2026-10-02 |
| Default `LangVersion` for `net11.0` | `15.0` (no `preview` needed to *select* C# 15) | 2026-10-02 |
| `LangVersion=14` | Accepted. Used by `TilesAndTokens.Legacy` to keep the "before" code honest. | 2026-10-02 |

How the language version was checked: put `#error version` in a source file and build. CS8304 reports the compiler version and the effective language version.

### Test stack

| Package | Version |
|---|---|
| xunit.v3 | 4.0.1 |
| Shouldly | 4.3.0 |
| NSubstitute | 6.2.0 |
| NSubstitute.Analyzers.CSharp | 1.0.17 |

Findings:

- `dotnet new xunit` on RC1 still generates an **xUnit v2** project. We use `xunit.v3` instead.
- `xunit.v3` 4.x runs on **Microsoft.Testing.Platform (MTP)**. On the .NET 10+ SDK, `dotnet test` fails with *"Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later"* unless you opt in through `global.json`:

  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```

  With MTP, `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` are not needed.
- Test projects must be `OutputType=Exe` (set in `Directory.Build.props` for every `*.Tests` project).

### Restore

With central package management, NuGet raises NU1507 (an error under `TreatWarningsAsErrors`) when more than one package source is configured on the machine. The repo's `nuget.config` clears all sources and uses only nuget.org.

## Post 1: the "before" picture

No C# 15 features. `TilesAndTokens.Legacy` is pinned to C# 14.

| Anti-pattern | Where | Test that proves the problem |
|---|---|---|
| String flags | `Legacy/StringFlags` | `StringTurnTests.TypoInActionType_CompilesAndSilentlyDoesNothing` |
| Null-heavy wrapper | `Legacy/NullableWrapper` | `TurnActionTests.RollAndCardBothSet_IsConstructible_AndTheCardIsSilentlyIgnored` |
| Base class + casting | `Legacy/BaseClass` | `LegacyActionTests.NewSubclass_CompilesButTheCastChainThrowsAtRuntime` |
| Hand-rolled `Result<T>` | `Legacy/Result.cs` | `ResultTests.ValueOnFailure_CompilesButThrowsAtRuntime` |

Extra weaknesses that came out of writing it (useful for the post):

- `StringTurn` stores only `DiceTotal`, so it can't tell whether a roll was doubles. The bonus card draw is lost.
- `TurnAction` with nothing set compiles and throws at runtime.
- `LegacyPlacementValidator` returns failures as strings, so "cell occupied" and "rule broken" can only be told apart by comparing messages.

## Post 2: discriminated unions

Verified 2026-10-02 on SDK `11.0.100-rc.1.26425.128`.

### Syntax that compiles

The plan's guessed syntax works as written. `LangVersion=15` is enough; neither `preview` nor a feature flag is needed.

```csharp
public sealed record RollDice(PlayerId Player);
public sealed record PlayCard(PlayerId Player, CardId Card, Position? Target = null);
public sealed record Pass(PlayerId Player);

public union GameAction(RollDice, PlayCard, Pass);
```

- **Implicit conversion** from each case type works for assignment, arguments and `return`: `GameAction a = new Pass(bob);`, `return new Rejected(...);` from a method returning `PlacementResult`.
- **Switch arms with different case types** (`ActionApplied` and `ActionInvalid`) in a switch expression returning `TurnOutcome` compile without casts.
- **Pattern matching** works directly on the union: `action switch { RollDice r => ..., PlayCard p => ..., Pass => ... }` and `if (action is Pass p)`.
- **A union can have a body.** `GameAction.Player` and `Card.Id` are members that switch over `this`.
- **Generic unions work:** `public union Result<T>(T, Error);` compiles, and `Result<int> ok = 42;` converts implicitly.
- **Matching a type that isn't a case** is a compile error: *CS8121: An expression of type 'GameAction' cannot be handled by a pattern of type 'string'.*

### Exhaustiveness

A switch expression that misses a case gives **CS8509**. It's a *warning*, so `TreatWarningsAsErrors` turns it into a build error:

```
error CS8509: The switch expression does not handle all possible values of its input type
(it is not exhaustive). For example, the pattern 'TilesAndTokens.Domain.TradeResources' is not covered.
```

To reproduce: `dotnet build src/TilesAndTokens.Domain -p:DefineConstants=PROVE_EXHAUSTIVENESS`. This adds `TradeResources` to `GameAction` (see the `#if` in `Actions/GameAction.cs`) and fails in two places: `GameAction.Player` and `TurnEngine.Apply`.

### How unions are lowered

Inspected with reflection:

- A union is a **struct** (`System.ValueType`) marked with `[System.Runtime.CompilerServices.Union]`, implementing `System.Runtime.CompilerServices.IUnion`.
- It has **one constructor per case** and a single `object Value { get; }` property. That's the whole layout: one object reference.
- **Reference-type cases** (all the records here): wrapping costs no extra allocation. The union is a struct holding a reference to the existing case object.
- **Value-type cases are boxed.** `union Num(int, string)` allocates 24 bytes per `int` assignment (measured with `GC.GetAllocatedBytesForCurrentThread`). A reference case measured ~0 bytes/op.

No BenchmarkDotNet run: the layout makes the cost obvious, and the post doesn't need more.

### Gotcha: `default` is an empty union

Because unions are structs, `default(GameAction)` exists. Its `Value` is `null` and it matches **no** case.

- A switch that handles every declared case compiles **without** a warning and throws `SwitchExpressionException` at runtime on `default`.
- A `null` arm matches the empty union: `{ RollDice => ..., PlayCard => ..., Pass => ..., null => "empty" }`. The compiler never asks for that arm.
- Watch out for LINQ: `hand.FirstOrDefault(...)` on an `IReadOnlyList<Card>` returns an **empty union**, not `null`. `Player.TryGetCard` exists for this reason.

Test: `GameActionTests.Default_IsAnEmptyUnion_ThatNoCaseMatches`.

### Nullability

- Converting a possibly-null case value warns: *CS8604: Possible null reference argument for parameter 'value' in 'GameAction.GameAction(RollDice value)'*. The conversion goes through the generated constructor, so nullable analysis applies. With `TreatWarningsAsErrors` this is a build error.
- If you suppress it (`null!`), the result is the same empty union as `default`.
- `GameAction?` is `Nullable<GameAction>`, an ordinary nullable struct.

### Where unions are used

| Union | Cases | File |
|---|---|---|
| `GameAction` | `RollDice`, `PlayCard`, `Pass` | `Domain/Actions/GameAction.cs` |
| `PlacementResult` | `Placed`, `Rejected`, `Blocked` | `Domain/Placement/PlacementResult.cs` (replaces legacy `Result<T>`) |
| `TurnOutcome` | `ActionApplied`, `ActionInvalid` | `Domain/Engine/TurnOutcome.cs` |
| `Card` | `TileCard`, `BonusPointsCard` | `Domain/Cards/Card.cs` (replaces the legacy card's two nullable fields) |

`TurnEngine.Apply` switches over `GameAction` with no discard arm. `PlaceTile` switches over `PlacementResult` the same way.

## Post 3: closed hierarchies

_Not started._

## Post 4: System.Text.Json + union state

_Not started._

## Post 5: EF Core 11 nested complex type indexes

_Not started._
