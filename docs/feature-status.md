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

Verified 2026-10-02 on SDK `11.0.100-rc.1.26425.128`.

### Syntax that compiles

The plan guessed `closed abstract record`. **That doesn't compile:**

```
error CS9384: 'VictoryCondition': a closed type cannot be marked abstract because it is always implicitly abstract.
```

What works, with `LangVersion=15` and no preview flag:

```csharp
public closed record VictoryCondition
{
    public abstract string Name { get; }   // abstract members are fine; the type is implicitly abstract
}

public sealed record MostPoints(int Rounds) : VictoryCondition { ... }
```

- `closed record` and `closed class` both work. Both are emitted as **abstract** types with **protected** constructors, marked with `[System.Runtime.CompilerServices.IsClosedType]`.
- **`closed interface` is not allowed:** *CS0106: The modifier 'closed' is not valid for this item.*
- **Generic closed types work:** `public closed record Tree<T>;` with `public sealed record Leaf<T>(T Value) : Tree<T>;`. A switch over `Tree<int>` with a `Leaf<int>` arm is exhaustive.
- Closed types need C# 15: *CS9327: Feature 'closed classes' is not available in C# 14.0.* (The feature's own name in the compiler is "closed classes", even for records.)

### Where derived types can be declared

| Where the derived type is | Result |
|---|---|
| Same file | Allowed |
| Another file in the same assembly | Allowed |
| Another namespace in the same assembly | Allowed, and it joins the exhaustiveness check |
| **Another assembly** | **CS9382:** *'MostTerritory': cannot use a closed type 'VictoryCondition' from another assembly as a base type.* |
| Another assembly compiled with `LangVersion=14` | Still CS9382. The boundary is enforced by the compiler, not by the consumer's language version. |

The boundary is the **assembly**, not the namespace or file.

**Proof:** `dotnet build proofs/TilesAndTokens.Domain.Extensions`. That project isn't in the solution, so the normal build stays green. It fails with:

```
MostTerritory.cs(6,22): error CS9382: 'MostTerritory': cannot use a closed type 'VictoryCondition' from another assembly as a base type.
MostTerritory.cs(12,22): error CS9382: 'NoDesertRule': cannot use a closed type 'PlacementRule' from another assembly as a base type.
```

### `closed` vs. `sealed` vs. `abstract`, and intermediate types

- **`sealed`:** nobody can derive. **`abstract`:** anybody can derive, anywhere. **`closed`:** only this assembly can derive, and the type is implicitly abstract.
- **Direct subtypes don't have to be sealed.** An open `record OpenChild : VictoryCondition` is allowed. It is a **hole in the boundary**: another assembly *can* derive from `OpenChild`. Exhaustiveness still holds, because a switch must handle `OpenChild` itself, which covers its subclasses. All subtypes in this repo are `sealed`.
- **Intermediate types can be closed too:** `public closed record Timed : VictoryCondition` with `sealed record Sprint : Timed`. A switch is exhaustive when it handles `Sprint` (all of `Timed`'s subtypes) instead of `Timed`. Another assembly deriving from `Timed` gets CS9382 too.

### Exhaustiveness

Same diagnostic as unions. Missing a subtype gives **CS8509** (a warning, so an error under `TreatWarningsAsErrors`):

```
VictoryChecker.cs(10,89): error CS8509: The switch expression does not handle all possible values of its input type
(it is not exhaustive). For example, the pattern 'TilesAndTokens.Domain.MostTerritory' is not covered.
```

To reproduce: `dotnet build src/TilesAndTokens.Domain -p:DefineConstants=PROVE_CLOSED_EXHAUSTIVENESS`.

**Null:** a closed type is a reference type, so `null` is possible.
- With a **nullable** input (`VictoryCondition?`) the compiler asks for a null arm: *CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.*
- With a **non-nullable** input there's no warning, and a null that sneaks in (`null!`) throws `SwitchExpressionException`. That's the same trade-off as nullable reference types everywhere. It's a nicer story than unions, whose empty `default` value is never flagged.

### Only switch *expressions* are checked

Adding `GameOver` to `TurnOutcome` this milestone was a live test. It broke the switch **expression** in `TurnEngineTests.Applied` (CS8509). It did **not** flag the switch **statement** in `UnionsDemo.Play`, which compiled and would have hit its fallback `throw` at runtime. The rule is the same as for enums: only switch expressions get exhaustiveness checking. That's worth a callout in the post: prefer switch expressions over closed types and unions.

### Union vs. closed hierarchy: decision checklist

| Question | Union | Closed hierarchy |
|---|---|---|
| Do the cases already exist as unrelated types (including `int`, `string`, or types you don't own)? | ✅ | ❌ They must derive from the base. |
| Do the cases share behavior or data (an abstract `Check`, a `Name`)? | Only through a member that switches over `this` | ✅ Virtual and abstract members |
| Can a value be "none of the above"? | ⚠️ `default` is an empty union, never flagged | ⚠️ `null`, flagged only for nullable inputs |
| Allocation | Struct wrapper; value-type cases are boxed | One object per value, as usual |
| Who can add cases? | Only the union declaration | Any type in the same assembly |

In this repo, actions and outcomes (`GameAction`, `PlacementResult`, `TurnOutcome`, `Card`) are **unions**: plain data with no shared behavior. Rules and win conditions (`PlacementRule`, `VictoryCondition`) are **closed hierarchies**: each case carries its own behavior or description.

### Where closed hierarchies are used

| Closed type | Subtypes | File |
|---|---|---|
| `VictoryCondition` | `MostPoints`, `FirstToEdge`, `LastPlayerStanding` | `Domain/Victory/VictoryCondition.cs` |
| `PlacementRule` | `InBoundsRule`, `CellEmptyRule`, `AdjacencyRule`, `TerrainCompatibilityRule` | `Domain/Placement/PlacementRule.cs`, `Domain/Placement/Rules/` |

`VictoryChecker.Check` switches over `VictoryCondition` with no discard arm. `PlacementValidator` runs the rules in order and returns the first result that isn't `Placed`.

## Post 4: System.Text.Json + union state

_Not started._

## Post 5: EF Core 11 nested complex type indexes

_Not started._
