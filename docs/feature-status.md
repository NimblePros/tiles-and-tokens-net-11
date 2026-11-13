# Feature status

The source of truth for the blog posts. Each entry records what was **verified** against the installed SDK, not what the docs or previews promised.

> **Status:** the posts were written on .NET 11 RC1 (tags `post-1` … `post-5`). Everything was re-verified on the **.NET 11 GA** SDK on 2026-11-13 (tag `dotnet-11-release`). See [.NET 11 GA re-verification](#net-11-ga-re-verification) for what changed. Sections below keep their RC1 findings, with GA changes marked **GA:**.

## Toolchain

| Item | Value | Verified |
|---|---|---|
| .NET SDK | `11.0.100` (GA). Was `11.0.100-rc.1.26425.128` (RC1). | 2026-11-13 |
| Roslyn | `5.12.0-1.26529.118`. Was `5.11.0-1.26425.128` (RC1). | 2026-11-13 |
| Default `LangVersion` for `net11.0` | `15.0` (no `preview` needed to *select* C# 15) | 2026-11-13 |
| `LangVersion=14` | Accepted. Used by `TilesAndTokens.Legacy` to keep the "before" code honest. | 2026-11-13 |

## .NET 11 GA re-verification

Done 2026-11-13 on SDK `11.0.100` / runtime `11.0.0`. All 105 tests pass (Domain 52, Legacy 22, Serialization 17, Persistence 14), and the `all` demo runs.

### Packages

| Package | RC1 (`post-5`) | `dotnet-11-release` | Why |
|---|---|---|---|
| .NET SDK (`global.json`) | `11.0.100-rc.1.26425.128` | `11.0.100`, `allowPrerelease: false` | GA |
| Microsoft.Extensions.Hosting | `11.0.0-rc.1.26425.128` | `11.0.0` | GA |
| Npgsql.EntityFrameworkCore.PostgreSQL | `11.0.0-rc.1.1` | **`11.0.0-rc.3`** | **No Npgsql 11.0.0 GA on nuget.org yet.** rc.3 (2026-10-28) is the latest. |
| Microsoft.EntityFrameworkCore.Design, `dotnet-ef` | `11.0.0-rc.1.26425.128` | **`11.0.0-rc.3.26522.148`** | Held back to match Npgsql (see below). |
| Aspire (SDK + 3 packages) | `13.6.0` | `13.6.5` | **Security:** 13.6.0 now fails restore under `TreatWarningsAsErrors` with NU1904/NU1903/NU1902 (GHSA-4g86-54fm-h9h8 critical, GHSA-f7vh-h5g3-7jrh high, MessagePack GHSA-qhrr-8q5h-9q3h moderate). This affects `post-5` as tagged, independent of .NET 11 GA. |

**EF Core 11.0.0 GA can't be used yet.** Npgsql `11.0.0-rc.3` depends on *exactly* `[11.0.0-rc.3.26522.148]` of EF Core / EF Core Relational. Referencing EF Core Design `11.0.0` fails restore:

```
error NU1107: Version conflict detected for Microsoft.EntityFrameworkCore.Relational.
  TilesAndTokens.Persistence -> Microsoft.EntityFrameworkCore.Design 11.0.0 -> Microsoft.EntityFrameworkCore.Relational (>= 11.0.0 && < 12.0.0-a)
  TilesAndTokens.Persistence -> Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0-rc.3 -> Microsoft.EntityFrameworkCore.Relational (= 11.0.0-rc.3.26522.148)
```

Decision (with the author): keep the whole EF stack on rc.3, a consistent supported set, instead of forcing GA Relational under an RC provider. **TODO: when Npgsql 11.0.0 ships, bump EF Core Design, `dotnet-ef` and Npgsql to GA and re-run the Post 5 checks.**

### Unchanged on GA (re-run, same result)

- C# 15 unions and closed types: syntax, CS8509, CS9384 (`closed abstract`), CS0106 (`closed interface`), CS9382 (proofs project), union lowering (struct, `[Union]`, `IUnion`, one `object Value`), `default` is an empty union, 24 bytes per boxed `int` case, generic unions, closed types emitted as abstract with protected constructors and `[IsClosedType]`.
- Both `PROVE_*` builds fail with the same diagnostics at the same lines.
- STJ default behavior: unions write the bare case value and fail to read (*ambiguous for union type*); closed types write only base properties and fail to read (*Deserialization of interface or abstract types is not supported*). The structural classifier works for `Card`.
- **No union discriminator in STJ GA.** `JsonUnionAttribute` still has only `TypeClassifier`. The `GameAction` workaround stays.
- EF Core (rc.3): `FromSql` on `SavedGame` still throws `NullReferenceException`. The domain `Board`, `VictoryCondition` and `GameAction` still can't be mapped (same messages). No pending model changes against the RC1 migrations.

### Changed on GA / rc.3

1. **STJ: `[JsonPolymorphic(InferClosedTypePolymorphism = true)]`** (new in GA). STJ discovers a closed type's subtypes, so the `[JsonDerivedType]` list is no longer needed. **Adopted for `VictoryCondition`** (author's decision). See Post 4 below.
2. **STJ API additions** since RC1: `JsonTypeClassifierFactory<T>`, `JsonTypeClassifierKind`, `JsonTypeClassifierContext.DeclaringType`, `JsonUnionCaseInfo` (`CaseType`, `IsNullable`), `JsonUnionInfoValues<T>`, and on `[JsonPolymorphic]`: `InferClosedTypePolymorphism`. Not used beyond item 1.
3. **Npgsql rc.3 generates JSON path indexes.** On RC1 every index into JSON became a B-tree over the whole column. See Post 5 below.
4. **`EF.Functions.JsonContains` translates for a complex JSON property** (Npgsql rc.3). `WithTileOnEdgeUsingGin` is now plain LINQ.
5. **`IMutableEntityType.AddIndex(properties, collectionIndices)` changed shape:** `collectionIndices` is now `IReadOnlyList<IReadOnlyList<int?>>` (one list per property, one entry per complex collection on its path).
6. Test-run noise: at AppHost shutdown, an `ObjectDisposedException` from the EventLog logger is printed (*An error occurred while writing to logger(s)*). Tests still pass.

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

Verified 2026-10-02 on SDK `11.0.100-rc.1.26425.128` (System.Text.Json `11.0.0-rc.1.26425.128`).

### Step 1: no attributes, no converters

The plain `JsonSerializer.Serialize(value)` / `Deserialize<T>(json)` results:

| Value | JSON written | Reads back? |
|---|---|---|
| `GameAction` holding `RollDice` | `{"Player":{"Value":"alice"}}`: the bare case value, no discriminator | ❌ *JsonException: JSON value type 'Object' is ambiguous for union type 'GameAction' because multiple case types can use this value type. Specify a custom type classifier to support deserialization.* |
| `List<GameAction>` | `[{"Player":...},{"Player":...}]` | ❌ Same error |
| `Card` holding `TileCard` | `{"Id":{"Value":"forest-1"},"Terrain":1}` | ❌ Same error |
| `VictoryCondition` holding `MostPoints(10)` | `{"Name":"Most points after 10 rounds"}`. **`Rounds` is silently lost.** Only the base type's properties are written. | ❌ *NotSupportedException: Deserialization of interface or abstract types is not supported.* |

**Headline:** STJ 11 *knows about unions*. It writes the case value instead of the wrapper struct. But by default it can't read them back, and it doesn't know about closed hierarchies at all.

### The union API in System.Text.Json 11

New public types found by reflecting over the RC1 assembly:

- `[JsonUnion(TypeClassifier = typeof(...))]`: picks how a union's case is chosen when reading.
- `JsonUnionTypeStructuralClassifier`: built in. It chooses the case by which properties appear in the JSON.
- `JsonTypeClassifierFactory`, `JsonTypeClassifierContext` (`Kind`, `UnionCases`, `DerivedTypes`, `TypeDiscriminatorPropertyName`), and `JsonTypeClassifier`, a delegate of the form `Type (ref Utf8JsonReader)`.
- `JsonSerializerOptions.TypeClassifiers` and `[JsonSourceGenerationOptions(TypeClassifiers = ...)]`.
- `JsonTypeInfoKind.Union`, `JsonTypeInfo.UnionCases`, `UnionConstructor`, `UnionDeconstructor`. `[JsonPolymorphic]` also gained a `TypeClassifier` property.

### Step 2: what each type actually needs

**`Card`: one attribute, fully native.** Its cases have different properties (`terrain` vs. `points`), so the built-in structural classifier works:

```csharp
[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union Card(TileCard, BonusPointsCard) { ... }
```

```json
[ { "id": { "value": "water-2" }, "terrain": "Water" }, { "id": { "value": "bonus-1" }, "points": 3 } ]
```

> **GA:** this changed. See [VictoryCondition on GA](#victorycondition-on-ga-inferclosedtypepolymorphism) below. The RC1 finding follows.

**`VictoryCondition`: the same attributes as before C# 15 (RC1).** `[JsonPolymorphic]` on its own fails with *InvalidOperationException: Polymorphic configuration for type 'VictoryA' should specify at least one derived type.* (`VictoryA` was the scratch-project type.) STJ doesn't discover a closed type's subtypes, so each one is listed:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MostPoints), "mostPoints")]
[JsonDerivedType(typeof(FirstToEdge), "firstToEdge")]
[JsonDerivedType(typeof(LastPlayerStanding), "lastPlayerStanding")]
public closed record VictoryCondition { ... }
```

An unknown discriminator fails with *JsonException: Read unrecognized type discriminator id 'mostTerritory'. Path: $.victory*.

#### VictoryCondition on GA: `InferClosedTypePolymorphism`

Verified 2026-11-13 on SDK `11.0.100`. One attribute, no subtype list:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", InferClosedTypePolymorphism = true)]
public closed record VictoryCondition { ... }
```

- Works with reflection **and** the source generator (`TilesAndTokensJsonContext`, metadata mode), for writing and reading.
- **The discriminator is the bare type name**, `"kind": "MostPoints"`. The naming policy is **not** applied: with `JsonSerializerOptions.Web` the properties are camelCase, but the discriminator is still `"MostPoints"`, and `"mostPoints"` fails to read.
- An explicit `[JsonDerivedType(typeof(X), "x")]` next to it overrides the inferred name for that subtype.
- On a type that isn't closed (`abstract record`), it fails: *InvalidOperationException: The type 'OpenBase' enables JsonPolymorphicAttribute.InferClosedTypePolymorphism but is not a closed type, so no derived types can be inferred. Declare the type 'closed' or register its derived types using JsonDerivedTypeAttribute.*
- Unknown discriminators fail as before: *Read unrecognized type discriminator id 'MostTerritory'.*

**Cost: a breaking format change.** `samples/save-v1.json` (RC1, `"kind": "mostPoints"`) no longer loads: *Read unrecognized type discriminator id 'mostPoints'.* It stays frozen and unmodified, and `VersioningTests.SaveV1_NoLongerLoads_BecauseInferredVictoryKindsAreTypeNames` proves the break. `samples/save-v2.json` is the GA format and is the file that must keep loading. Only the victory discriminator differs between the two (`SaveV1_TurnLog_IsUnchangedInV2`). The union `"kind"` values come from our own `UnionCaseKinds` and didn't change.

**This updates the post's comparison:** on GA, closed hierarchies *do* gain something in serialization. The compiler-known subtype list replaces the hand-maintained `[JsonDerivedType]` list, so a new subtype can't be forgotten. The price is type-name discriminators, unless you list names explicitly again.

The Persistence `VictorySnapshot.Kind` column values (`"mostPoints"`, …) are separate constants and didn't change. `SavedGame.StateJson` now holds `"MostPoints"`.

**`GameAction`: not possible natively. A labeled workaround was added with the author's agreement.**

- `RollDice(alice)` and `Pass(alice)` both serialize to `{"player":{"value":"alice"}}`. The case is lost on write, so no classifier can recover it.
- The structural classifier rejects the type when it's first used: *NotSupportedException: The JsonUnionTypeStructuralClassifier cannot classify union type 'GameAction' because the case type 'RollDice' can never be selected uniquely. The case type 'PlayCard' recognizes every property name recognized by 'RollDice' and has no stricter required-property or unmapped-member constraints.*
- `[JsonPolymorphic]` / `[JsonDerivedType]` **can't be applied to a union**: *CS0592: Attribute 'JsonPolymorphic' is not valid on this declaration type. It is only valid on 'class, interface' declarations.* (Unions are structs.)
- Making each case "polymorphic over itself" fails: *Specified type 'RollDice' does not support polymorphism. Polymorphic types cannot be structs, sealed types or System.Object.*
- The classifier context has `TypeDiscriminatorPropertyName = null` for unions. **There is no union discriminator setting in RC1.**

The workaround, in `src/TilesAndTokens.Serialization/GameActionKinds/` and marked `WORKAROUND` in the code, uses only STJ extension points. **No `JsonConverter`.**

1. `UnionCaseKinds`: maps each case type to a name (`RollDice` to `"rollDice"`, and so on).
2. `UnionCaseKindModifier`: a contract modifier that adds a `"kind"` property to each case type's JSON contract.
3. `UnionCaseKindClassifier`: a `JsonTypeClassifierFactory` that looks ahead for `"kind"` on a copy of the reader and returns the case type.

```json
{ "kind": "playCard", "player": { "value": "bob" }, "card": { "value": "forest-1" }, "target": { "x": 2, "y": 3 } }
```

### Gotchas found while building the workaround

- **`JsonTypeInfo.CreateJsonPropertyInfo` isn't trimming/AOT safe** (IL2026, IL3050). `JsonMetadataServices.CreatePropertyInfo<T>`, the API the source generator emits, is safe and is what the modifier uses.
- **`IgnoreReadOnlyProperties = true` drops a getter-only synthetic property.** The added `"kind"` property needs a (no-op) setter or it's never written.
- **The source generator validates unions at compile time:** *SYSLIB1227: Union type 'GameAction': case types 'RollDice', 'PlayCard', 'Pass' all serialize as JSON value type 'Object'. Set JsonUnionAttribute.TypeClassifier to provide a custom classifier that can disambiguate.* A classifier added only at runtime doesn't satisfy it. Registering it with `[JsonSourceGenerationOptions(TypeClassifiers = [typeof(UnionCaseKindClassifier)])]` does, which requires a parameterless constructor.
- **The default fast-path source generation skips modifiers for collections of unions.** Serializing `IReadOnlyList<GameAction>` directly wrote no `"kind"`, while the same list *inside* `GameState` did. `GenerationMode = JsonSourceGenerationMode.Metadata` fixes it.

### Source generation, trimming, AOT

- `TilesAndTokensJsonContext` (`[JsonSerializable(typeof(GameState))]`, metadata mode) works with unions and closed hierarchies. Round-trip tests run through it.
- `TilesAndTokens.Serialization` builds with `IsAotCompatible=true` and **no warnings**.
- **Trimmed publish:** `dotnet publish src/TilesAndTokens.Demo -c Release -r win-x64 --self-contained -p:PublishTrimmed=true` gives **0 trim warnings**. The trimmed `json` demo saves, loads, and resumes correctly.
- **Native AOT:** *not verified end to end* (still true for `dotnet-11-release`; not re-run on GA). `-p:PublishAot=true` stops at linking on the dev machine: *vswhere.exe failed to locate Visual Studio with Microsoft.VisualStudio.Component.VC.Tools.x86.x64* (no C++ build tools). This is a machine prerequisite, not a code problem. TODO: run it on a machine (or a CI job) that has the native toolchain.

### Output customization

Set on `TilesAndTokensJsonContext`: `JsonSerializerDefaults.Web` (camelCase), `WriteIndented`, `UseStringEnumConverter` (`"terrain": "Water"`), and `IgnoreReadOnlyProperties`. The last one keeps computed members such as `Player.IsEliminated` and `VictoryCondition.Name` out of the file. Discriminators use `"kind"` everywhere, not `"$type"`.

`PlayerId` and `CardId` are written as `{"value":"alice"}`. A converter would flatten them to `"alice"`, but that's unrelated to unions, so it was left out.

### Versioning

- `samples/save-v1.json` is frozen and must keep loading (`VersioningTests.SaveV1_StillLoads`). **GA:** after adopting `InferClosedTypePolymorphism`, it intentionally no longer loads. `samples/save-v2.json` took over this role (`SaveV2_StillLoads`).
- **Adding a union case is backward compatible.** A test-only `union GameActionV2(RollDice, PlayCard, Pass, TradeResources)` loads the v1 turn log unchanged and round-trips its new case. A test-only union is used so the real game rules don't change.
- **v1 code reading a v2 file:** *JsonException: Unknown GameAction kind 'tradeResources'. Known kinds: rollDice, playCard, pass.* That message is ours, from the classifier. A missing `"kind"` gives *GameAction JSON has no "kind" property.*

### Tests

The JSON shape is checked against committed files in `tests/TilesAndTokens.Serialization.Tests/Expected/`: `game-state.json`, `turn-log.json`, `cards.json`, and `victory-conditions.json`. On a mismatch the test writes `*.received.json` next to the expected file (gitignored) for review.

### The "old way" (Legacy project)

- `Legacy/Json/TurnActionJsonConverter.cs`: a hand-written converter for the null-heavy `TurnAction`, 63 non-blank lines. Tests show that it silently drops the second action of an invalid turn, and that a typo in `"kind"` loads as an empty action.
- `[JsonPolymorphic]` / `[JsonDerivedType]` on the open `LegacyAction`. `TradeAction` isn't listed, so serializing it compiles and then throws `NotSupportedException` at runtime.

The comparison for the post (RC1; **GA:** closed hierarchies now need only `InferClosedTypePolymorphism`, see above): closed hierarchies need **the same attributes** as before (no gain), but the compiler now guarantees the subtype list is complete within the assembly. Unions get real built-in support when their cases have different shapes. When they don't, the extension-point workaround is **not shorter** than a converter (79 vs. 63 non-blank lines). Its advantages are elsewhere: it's written once for any union (each new case is one line in `UnionCaseKinds`), STJ still reads and writes every property, and an unknown kind fails loudly instead of loading as an empty action.

## Post 5: EF Core 11 nested complex type indexes

Verified 2026-10-02 against a real Postgres. The packages:

| Package | Version |
|---|---|
| EF Core / EF Core Design / `dotnet-ef` (local tool) | `11.0.0-rc.1.26425.128` |
| Npgsql.EntityFrameworkCore.PostgreSQL | `11.0.0-rc.1.1` |
| Aspire (`Aspire.AppHost.Sdk`, `Aspire.Hosting.PostgreSQL`, `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Aspire.Hosting.Testing`) | `13.6.0` (stable) |
| PostgreSQL started by the AppHost | 18.3 |

SQL and execution plans for the post: [post-5-sql-and-plans.md](post-5-sql-and-plans.md).

### The plan's assumption didn't hold

The plan wanted indexes on `Board.Tiles[].Position.X` and `Board.Tiles[].OwnerId`. Verified on RC1:

1. **A complex collection must be JSON-mapped.** RC1's `RelationalStrings.ComplexCollectionNotMappedToJson` reads *The complex collection property '{entityType}.{property}' must be mapped to a JSON column. Use 'ToJson()'...* (from the API docs; every configuration tested used `ToJson()` on `Tiles`). Its **root must be JSON too**, which was triggered: *Complex property 'SavedGame.Board#BoardSnapshot.Tiles' is mapped to JSON but its containing type 'SavedGame.Board#BoardSnapshot' is not. Map the root complex type to JSON.* So `Board` is one `jsonb` column, and table splitting isn't an option for the tiles.
2. **EF Core 11 accepts indexes into JSON, including through collections:**
   - `HasIndex(g => g.Board.Tiles[0].OwnerId)`;
   - the new metadata API `IMutableEntityType.AddIndex(properties, collectionIndices)`, where a `null` entry means "all elements" and a number means one element (backed by `RelationalJsonIndex`).

   **Npgsql 11 RC1 doesn't generate a path index for either.** It emits a B-tree over the **whole column**:

   ```sql
   CREATE INDEX "IX_Games_Board_Tiles_OwnerId" ON "Games" ("Board");
   ```

   The same happens for a nested property inside a JSON-mapped (non-collection) complex type. The edge query can't use these indexes, so none is configured.

   **GA (Npgsql `11.0.0-rc.3`, EF Core `11.0.0-rc.3.26522.148`, verified 2026-11-13 from `GenerateCreateScript`):** this changed.

   | Index | Npgsql rc.3 DDL |
   |---|---|
   | `HasIndex(g => g.Board.Tiles[0].OwnerId)` (fixed position) | `CREATE INDEX ... ON "SavedGames" (("Board" #>> '{Tiles,0,OwnerId}'));` |
   | Nested property in a JSON (non-collection) complex type, `Summary.Leader.PlayerId` with `Summary.ToJson()` | `CREATE INDEX ... ON "SavedGames" (("Summary" #>> '{Leader,PlayerId}'));` |
   | All elements: `AddIndex([ownerId], [[null]])` | Rejected at model validation: *InvalidOperationException: PostgreSQL expression indexes cannot traverse all elements of a JSON collection. Index the collection itself or select a fixed collection position instead.* |

   Real path expression indexes now exist. The edge query, though, needs "*any* tile on an edge", which is the all-elements case Postgres can't express as an expression index. So option A still stands: column-mapped `Summary` indexes, plus the GIN index for containment. (Core messages that exist in RC1: *Indexes cannot reference properties whose path goes through a complex collection* is `CoreStrings.IndexOnComplexCollection`. `UniqueIndexOnComplexProperty` says unique indexes aren't supported on complex properties. Neither fired for the configurations above.)

**Decision (option A, agreed with the author):** split the post along what works.

### What works: indexes on nested complex properties mapped to columns

`GameSummary` → `LeaderSnapshot` / `VictorySnapshot` are complex types mapped to ordinary columns (table splitting). Indexing their nested properties is plain fluent API:

```csharp
game.HasIndex(g => g.Summary.Leader.PlayerId);
game.HasIndex(g => new { g.Summary.Victory.Kind, g.Summary.Leader.Score });
```

```sql
CREATE INDEX "IX_SavedGames_Summary_Leader_PlayerId" ON "SavedGames" ("Summary_Leader_PlayerId");
CREATE INDEX "IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score" ON "SavedGames" ("Summary_Victory_Kind", "Summary_Leader_Score");
```

- Three levels deep works too: `HasIndex(x => x.Standings.Leader.Player.Id)` produced `("Standings_Leader_Player_Id")` in a scratch model.
- At 1,000 rows Postgres uses both: `Index Scan Backward using "IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score"` and `Bitmap Index Scan on "IX_SavedGames_Summary_Leader_PlayerId"`.
- Tests check `pg_indexes` (`NestedComplexPropertyIndexes_ExistInPostgres`).

### The edge query over JSON

`Board.Tiles.Any(t => t.OwnerId == p && (t.Position.X == 0 || ...))` translates. EF Core 11 / Npgsql produce `EXISTS (SELECT 1 FROM ROWS FROM (jsonb_to_recordset(s."Board" -> 'Tiles') ...))`. It always means a sequential scan with a per-row function scan.

**Optional Postgres-native GIN index** (`AddBoardGinIndex`, hand-written `migrationBuilder.Sql`, labeled in code):

- `CREATE INDEX "IX_SavedGames_Board_Gin" ON "SavedGames" USING GIN ("Board" jsonb_path_ops);`
- It's used only by `@>` containment, so the query is raw SQL (`WithTileOnEdgeUsingGin`) that selects ids and is composed into LINQ (`WHERE "Id" IN (...)`). It returns the same rows as the LINQ query (tested).
- **GA (Npgsql rc.3):** `WithTileOnEdgeUsingGin` is plain LINQ with `EF.Functions.JsonContains(g.Board, json)`, which generates `WHERE s."Board" @> @left OR s."Board" @> @right OR ...`. Still the same rows (tested). The plans in `post-5-sql-and-plans.md` were captured on RC1 with the raw-SQL form and **haven't been re-captured**.
- **Result at 20,000 games:** when the index is used it's about 15× faster (3.5 ms vs. 53.4 ms for LINQ). **But Postgres's planner doesn't choose it.** It picks a parallel seq scan (75.1 ms), because Postgres keeps no statistics for `jsonb @>` selectivity. At 1,000 games it scans the table too. Shown honestly in the demo and in the plans doc.
- Getting there hit two more RC1 limits:
  - **`FromSql` on `SavedGame` throws `NullReferenceException`** (`RelationalQueryableMethodTranslatingExpressionVisitor.GenerateComplexJsonShaper`). The workaround is to select ids with `Database.SqlQuery<Guid>` and compose.
  - **`EF.Functions.JsonContains(g.Board, ...)` doesn't translate** for a complex JSON property (*The LINQ expression ... could not be translated*). **GA (Npgsql rc.3): fixed, it translates to `@>`.** The `FromSql` NRE is still there.

### Domain types vs. snapshot types: snapshots

| Tried to map | Result on RC1 |
|---|---|
| Domain `Board(IReadOnlyList<Tile> Tiles)` as a JSON complex type | ❌ *No suitable constructor was found for the type 'Board'* |
| `VictoryCondition` (closed hierarchy) as a complex type | ❌ Mapped as the base type: *Complex type 'Row.Victory#VictoryCondition' has no properties defined.* Complex types have no inheritance. (`Row` was the scratch entity.) |
| `GameAction` (union) as a property | ❌ *The 'GameAction' property ... could not be mapped because the database provider does not support this type.* |
| `GameAction` as a complex property or complex collection | ❌ **The type 'Row.LastAction#GameAction' cannot be mapped because union types are not supported. See https://github.com/dotnet/efcore/issues/36375** (EF Core recognizes unions and rejects them explicitly.) |

So persistence uses mutable snapshot classes (`BoardSnapshot` → `TileSnapshot` → `PositionSnapshot`, `GameSummary` → `LeaderSnapshot` / `VictorySnapshot`):

- **`VictoryCondition`** is flattened to `VictorySnapshot { Kind, Rounds? }` by an exhaustive switch with no discard arm. A new victory condition breaks the build in persistence too.
- **Unions, labeled FALLBACK:** `SavedGame.StateJson` (`jsonb`) holds the full `GameState`, turn log included, written by the Post 4 serializer. `SavedGame.Restore()` reads it back.
- `jsonb` reorders object keys. The Post 4 classifier finds `"kind"` anywhere in an object and closed-type discriminators sort first (shorter key), so it round-trips. That's tested by `SaveAndLoad_RestoresTheWholeGameState`.

### Migrations

- Generated with the local tool (`dotnet tool restore`, then `dotnet ef migrations add ... --project src/TilesAndTokens.Persistence --startup-project src/TilesAndTokens.Persistence`). An `IDesignTimeDbContextFactory` means no database is needed.
- **Migration class names include the timestamp** in RC1: `public partial class _20261002155612_InitialCreate : Migration`.
- **Applied at Demo startup** (`Database.MigrateAsync()` in the `efcore` command, a dev-only shortcut). The test fixture calls the same method.
- `dotnet ef migrations script --no-build` straight after `migrations add` uses the old assembly and silently leaves the new migration out. Build first.

### Gotchas: changing the nested shape (tested on a scratch copy, not committed)

| Change | Migration EF generates | Reading existing rows |
|---|---|---|
| Add `LeaderSnapshot.TileCount` (column-mapped) | `AddColumn<int>("Summary_Leader_TileCount", nullable: false, defaultValue: 0)` | Normal |
| Add `TileSnapshot.Rotation` (inside the `Board` JSON) | **Nothing.** The JSON column has no schema. | Old rows load: `int Rotation` reads **0**, `int? Rotation` reads **null**. No error. |

**Nullable nested complex types:** an optional complex property mapped to columns (`Leader? Leader`) is accepted. Every one of its columns becomes nullable (`"Leader_Score" integer`, no `NOT NULL`). Only the DDL was checked. A round-trip of a null `Leader` wasn't tested.

### Npgsql specifics

- `ToJson()` complex types map to **`jsonb`** (not `json`). `StateJson` is set to `jsonb` explicitly.
- `Terrain` inside the JSON is stored as a string (`HasConversion<string>()`).
- B-tree for the column indexes. GIN `jsonb_path_ops` only for containment, see above.

### Aspire

- **Installed templates are stale:** `dotnet new aspire-apphost` on this machine generates Aspire **9.5.1** and `net9.0`, so the AppHost and ServiceDefaults were written by hand for 13.6.0. The AppHost uses `<Project Sdk="Aspire.AppHost.Sdk/13.6.0">` with no separate `Aspire.Hosting.AppHost` package.
- **Mixing versions works:** `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.0 targets net10.0 / Npgsql EF 10, but it works with the RC1 provider. Npgsql EF `11.0.0-rc.1.1` is referenced explicitly. All Persistence tests and the AppHost run pass.
- Warning **ASPIRE010**: *AppHost is configured with AspireUseCliBundle=false. Some Aspire features require the Aspire CLI bundle.* No feature used here needed it. Left as is.
- `ServiceDefaults` is trimmed for a console app: OpenTelemetry only. The template's health checks, HTTP resilience and service discovery need a web host. OpenTelemetry 1.19 uses `builder.AddOpenTelemetry()` on the host builder.
- The `efcore` demo clears console logging providers so its narration stays readable. EF's command logs still reach the dashboard through OpenTelemetry.
- **Aspire testing:** `DistributedApplicationTestingBuilder.CreateAsync<Projects.TilesAndTokens_AppHost>()`. The fixture removes the `demo` and pgweb resources and shares one Postgres per xUnit collection.
  - `ResourceLoggerService.GetAllAsync("demo")` / `WatchAsync(...)` returned **no lines** for the finished demo resource.
  - The AppHost test captures the forwarded logger category `TilesAndTokens.AppHost.Resources.demo` instead.
- Startup log noise: Aspire's database health check runs once before Aspire creates the database (*3D000: database "tilesandtokens" does not exist*), then passes.
