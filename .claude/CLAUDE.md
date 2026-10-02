# Tiles & Tokens

Companion demo for a 5-part NimblePros blog series and a conference talk on C# 15 / .NET 11 domain modeling. The full plan is in [plans/initial-plan.md](plans/initial-plan.md). Read it before starting a milestone.

## Ground rules

1. **Verify before you write.** C# 15 / .NET 11 features changed across previews. Confirm syntax and behavior against the installed SDK (compiler, scratch project, or official docs) before implementing. Never write code from memory and assume it compiles.
2. **Record what you find** in `docs/feature-status.md`: SDK version, exact syntax that compiles, whether preview `LangVersion` or a feature flag is needed, and limitations. The blog posts are written from this file.
3. **Never fake a feature.** If a feature is missing or behaves differently from the plan, stop and report it. A clearly labeled "workaround until GA" is only acceptable after the author agrees.
4. **One milestone per post.** Each ends with a green build, passing tests, a working demo command, and a git tag (`post-1` … `post-5`).
5. **Keep the "before" code.** `TilesAndTokens.Legacy` stays in the repo, pinned to C# 14, so the posts can show before/after diffs.
6. **Testing:** xUnit v3 + Shouldly. NSubstitute for mocks/fakes. **Never use Moq.**
7. **Local resources run through Aspire.** No docker compose files, no separate Testcontainers setup.

## Working in this repo

- SDK is pinned in `global.json` (currently .NET 11 RC1). When .NET 11 goes GA (Nov 2026), bump it and re-verify everything in `feature-status.md`.
- `dotnet test` runs on Microsoft.Testing.Platform (opted in via `global.json`).
- `TreatWarningsAsErrors` is on everywhere, on purpose: missing switch cases should fail the build during the talk.
- Code is teaching material: favor clarity over cleverness, keep files small, make each feature easy to find.
- Each milestone registers its demo in `src/TilesAndTokens.Demo/DemoRunner.cs` so `all` picks it up.
- `dotnet test` needs Docker running: the Persistence tests start Postgres through the Aspire AppHost (`Aspire.Hosting.Testing`).
- EF Core migrations: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/TilesAndTokens.Persistence --startup-project src/TilesAndTokens.Persistence`. Build before `dotnet ef migrations script`.
- Never `git clean` uncommitted work: it deletes untracked source files, not just build output.
