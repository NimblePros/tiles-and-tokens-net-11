# Tiles & Tokens (.NET 11 / C# 15)

Companion demo for the NimblePros blog series and conference talk on domain modeling with C# 15 and .NET 11:

> How do you model a game turn when a player can either roll dice, play a card, or pass, without relying on vague string flags, null-heavy wrapper classes, or unsafe casting?

The app is a small board game, [Tiles & Tokens](docs/game-rules.md). Its turns, tile placement validation, and victory conditions give the type system something real to model.

> **Prerelease:** this repo currently targets the .NET 11 **RC1** SDK. It will be updated when .NET 11 goes GA in November 2026. See [docs/feature-status.md](docs/feature-status.md) for exactly what was verified on which SDK.

## Prerequisites

- .NET SDK `11.0.100-rc.1.26425.128` or a later .NET 11 feature band (pinned in `global.json`)
- From Post 5 on: a container runtime (Docker or Podman) for Aspire

## Following along

Each post has a git tag. Check out the tag to see the code as it was at the end of that post.

| Tag | Post | What to look at | Run |
|---|---|---|---|
| `post-1` | Intro to .NET 11 and the "before" anti-patterns | `src/TilesAndTokens.Legacy`, `tests/TilesAndTokens.Legacy.Tests` | `dotnet run --project src/TilesAndTokens.Demo -- legacy` |
| `post-2` | Discriminated unions | `src/TilesAndTokens.Domain` (`Actions`, `Placement`, `Engine`), `tests/TilesAndTokens.Domain.Tests` | `dotnet run --project src/TilesAndTokens.Demo -- unions` |
| `post-3` | Closed hierarchies | `src/TilesAndTokens.Domain` (`Victory`, `Placement/Rules`), `proofs/TilesAndTokens.Domain.Extensions` | `dotnet run --project src/TilesAndTokens.Demo -- closed` |
| `post-4` | System.Text.Json and union state | _coming soon_ | `... -- json` |
| `post-5` | EF Core 11 indexes on nested complex types | _coming soon_ | `... -- efcore` |

## Running

```shell
dotnet build
dotnet test
dotnet run --project src/TilesAndTokens.Demo -- legacy
dotnet run --project src/TilesAndTokens.Demo -- all              # every demo, pausing between sections
dotnet run --project src/TilesAndTokens.Demo -- all --no-pause   # for CI and recordings
```

## Layout

```
src/
  TilesAndTokens.Legacy/   Post 1 "before" code (pinned to C# 14)
  TilesAndTokens.Domain/   Posts 2-3: unions, closed hierarchies, game engine
  TilesAndTokens.Demo/     Console runner, one command per post + `all`
tests/
  TilesAndTokens.Legacy.Tests/
  TilesAndTokens.Domain.Tests/
proofs/
  TilesAndTokens.Domain.Extensions/   Not in the solution: proves closed types can't be extended (CS9382)
docs/
  feature-status.md        Verified syntax and limitations per feature
  game-rules.md
```

Tests use xUnit v3, Shouldly, and NSubstitute.
