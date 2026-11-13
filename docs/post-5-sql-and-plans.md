# Post 5: generated SQL and execution plans

Captured 2026-11-13 for tag `dotnet-11-release`: .NET SDK `11.0.100`, EF Core `11.0.0-rc.3.26522.148`, Npgsql EF Core provider `11.0.0-rc.3`, Aspire `13.6.5`. Both runs use the AppHost's Postgres: **PostgreSQL 18.3**. The RC1 captures (tag `post-5`) are summarized in [Compared with RC1](#compared-with-rc1-tag-post-5).

- **1,000 games:** the real `efcore` demo, pointed at the AppHost's Postgres (`ConnectionStrings__tilesandtokens`).
- **20,000 games:** the same Postgres, emptied and reseeded with `GameGenerator.Generate(20_000, seed: 2026)` in batches of 1,000, then `ANALYZE`. The harness lives outside the repo. It uses only repo code (`GameGenerator`, `SavedGameQueries`, `SqlDiagnostics`) plus `Aspire.Hosting.Testing`, the same way `PostgresFixture` does.

Plans use `EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)` on the exact SQL and parameters EF Core generated (`SqlDiagnostics.ExplainAnalyzeAsync`), after `ANALYZE "SavedGames"`. PostgreSQL 18 includes `Buffers` with `ANALYZE` by default. Timings in the demo sections are a single run, so treat them as rough.

## Schema (both migrations, `dotnet ef migrations script`)

The DDL is unchanged from RC1. rc.3 now creates the history table inside a `DO` block that tolerates a concurrent creator:

```sql
DO $EFHistory$
BEGIN
    IF pg_catalog.to_regclass('"__EFMigrationsHistory"') IS NULL THEN
        BEGIN
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
        EXCEPTION WHEN duplicate_schema OR duplicate_table OR duplicate_object OR unique_violation THEN
            NULL;
        END;
        PERFORM 1 FROM pg_catalog.pg_class LIMIT 1;
    END IF;
END $EFHistory$;

START TRANSACTION;
CREATE TABLE "SavedGames" (
    "Id" uuid NOT NULL,
    "SavedAt" timestamp with time zone NOT NULL,
    "StateJson" jsonb NOT NULL,
    "Summary_Round" integer NOT NULL,
    "Summary_TurnCount" integer NOT NULL,
    "Summary_Leader_PlayerId" text NOT NULL,
    "Summary_Leader_Score" integer NOT NULL,
    "Summary_Victory_Kind" text NOT NULL,
    "Summary_Victory_Rounds" integer,
    "Board" jsonb NOT NULL,
    CONSTRAINT "PK_SavedGames" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_SavedGames_Summary_Leader_PlayerId" ON "SavedGames" ("Summary_Leader_PlayerId");

CREATE INDEX "IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score" ON "SavedGames" ("Summary_Victory_Kind", "Summary_Leader_Score");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261002155612_InitialCreate', '11.0.0-rc.3.26522.148');

COMMIT;

START TRANSACTION;
CREATE INDEX "IX_SavedGames_Board_Gin" ON "SavedGames" USING GIN ("Board" jsonb_path_ops);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261002161723_AddBoardGinIndex', '11.0.0-rc.3.26522.148');

COMMIT;
```

(`ProductVersion` is the EF Core version that writes the history row, not the one that generated the migration. The migrations themselves weren't regenerated.)

## 1,000 games (the demo)

### Query 1: edge query in LINQ over the `Board` JSON column

`SavedGameQueries.WithTileOnEdge`. EF Core 11 translates `Board.Tiles.Any(...)` into `jsonb_to_recordset`. No index can serve it. The SQL is unchanged from RC1.

```sql
-- @playerId='player-042'
SELECT s."Id", s."SavedAt", s."StateJson", s."Board", s."Summary_Round", s."Summary_TurnCount", s."Summary_Leader_PlayerId", s."Summary_Leader_Score", s."Summary_Victory_Kind", s."Summary_Victory_Rounds"
FROM "SavedGames" AS s
WHERE EXISTS (
    SELECT 1
    FROM ROWS FROM (jsonb_to_recordset(s."Board" -> 'Tiles') AS (
        "OwnerId" text,
        "Position" jsonb
    )) WITH ORDINALITY AS t
    WHERE t."OwnerId" = @playerId AND ((CAST(t."Position" ->> 'X' AS integer)) = 0 OR (CAST(t."Position" ->> 'X' AS integer)) = 7 OR (CAST(t."Position" ->> 'Y' AS integer)) = 0 OR (CAST(t."Position" ->> 'Y' AS integer)) = 7))
```

Result: 9 games in 75 ms.

```
Seq Scan on "SavedGames" s (actual rows=9.00 loops=1)
  Filter: EXISTS(SubPlan 1)
  Rows Removed by Filter: 991
  Buffers: shared hit=214
  SubPlan 1
    ->  Function Scan on jsonb_to_recordset t (actual rows=0.01 loops=1000)
          Filter: (("OwnerId" = 'player-042'::text) AND (((("Position" ->> 'X'::text))::integer = 0) OR ((("Position" ->> 'X'::text))::integer = 7) OR ((("Position" ->> 'Y'::text))::integer = 0) OR ((("Position" ->> 'Y'::text))::integer = 7)))
          Rows Removed by Filter: 8
```

### Query 1b: the same query as jsonb containment (Postgres-native GIN)

`SavedGameQueries.WithTileOnEdgeUsingGin`. Plain LINQ with `EF.Functions.JsonContains(g.Board, ...)`, which Npgsql rc.3 translates to `@>` on the complex JSON column. (On RC1 it didn't translate, and this was raw SQL composed as `WHERE "Id" IN (...)`.) **At 1,000 games the planner scans the table instead of using the GIN index** (214 pages).

```sql
-- @left='{"Tiles":[{"OwnerId":"player-042","Position":{"X":0}}]}' (DbType = Object)
-- @right='{"Tiles":[{"OwnerId":"player-042","Position":{"X":7}}]}' (DbType = Object)
-- @top='{"Tiles":[{"OwnerId":"player-042","Position":{"Y":0}}]}' (DbType = Object)
-- @bottom='{"Tiles":[{"OwnerId":"player-042","Position":{"Y":7}}]}' (DbType = Object)
SELECT s."Id", s."SavedAt", s."StateJson", s."Board", s."Summary_Round", s."Summary_TurnCount", s."Summary_Leader_PlayerId", s."Summary_Leader_Score", s."Summary_Victory_Kind", s."Summary_Victory_Rounds"
FROM "SavedGames" AS s
WHERE s."Board" @> @left OR s."Board" @> @right OR s."Board" @> @top OR s."Board" @> @bottom
```

Result: 9 games in 48 ms.

```
Seq Scan on "SavedGames" s (actual rows=9.00 loops=1)
  Filter: (("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb))
  Rows Removed by Filter: 991
  Buffers: shared hit=214
Planning:
  Buffers: shared hit=4
```

### Query 2: victory kind, ordered by leader score (EF Core 11 nested complex property index)

`SavedGameQueries.UsingVictory(...).Take(5)`, served by `IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score` on `Summary.Victory.Kind`, `Summary.Leader.Score`.

```sql
-- @victoryKind='firstToEdge'
-- @p='5'
SELECT s."Id", s."SavedAt", s."StateJson", s."Board", s."Summary_Round", s."Summary_TurnCount", s."Summary_Leader_PlayerId", s."Summary_Leader_Score", s."Summary_Victory_Kind", s."Summary_Victory_Rounds"
FROM "SavedGames" AS s
WHERE s."Summary_Victory_Kind" = @victoryKind
ORDER BY s."Summary_Leader_Score" DESC
LIMIT @p
```

Result: 5 games in 46 ms.

```
Limit (actual rows=5.00 loops=1)
  Buffers: shared hit=7
  ->  Index Scan Backward using "IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score" on "SavedGames" s (actual rows=5.00 loops=1)
        Index Cond: ("Summary_Victory_Kind" = 'firstToEdge'::text)
        Index Searches: 1
        Buffers: shared hit=7
```

### Query 3: games led by a player (EF Core 11 nested complex property index)

`SavedGameQueries.LedBy`, served by `IX_SavedGames_Summary_Leader_PlayerId` on `Summary.Leader.PlayerId`.

```sql
-- @playerId='player-042'
SELECT s."Id", s."SavedAt", s."StateJson", s."Board", s."Summary_Round", s."Summary_TurnCount", s."Summary_Leader_PlayerId", s."Summary_Leader_Score", s."Summary_Victory_Kind", s."Summary_Victory_Rounds"
FROM "SavedGames" AS s
WHERE s."Summary_Leader_PlayerId" = @playerId
```

Result: 5 games in 59 ms.

```
Bitmap Heap Scan on "SavedGames" s (actual rows=5.00 loops=1)
  Recheck Cond: ("Summary_Leader_PlayerId" = 'player-042'::text)
  Heap Blocks: exact=5
  Buffers: shared hit=7
  ->  Bitmap Index Scan on "IX_SavedGames_Summary_Leader_PlayerId" (actual rows=5.00 loops=1)
        Index Cond: ("Summary_Leader_PlayerId" = 'player-042'::text)
        Index Searches: 1
        Buffers: shared hit=2
```

## 20,000 games: does the GIN index help?

`rows: 20000, LINQ matches: 175, GIN matches: 175`. Both queries return the same 175 ids. Seeding took 28 s.

### LINQ edge query

```
Seq Scan on "SavedGames" s (actual rows=175.00 loops=1)
  Filter: EXISTS(SubPlan 1)
  Rows Removed by Filter: 19825
  Buffers: shared hit=4215
  SubPlan 1
    ->  Function Scan on jsonb_to_recordset t (actual rows=0.01 loops=20000)
          Filter: (("OwnerId" = 'player-042'::text) AND (((("Position" ->> 'X'::text))::integer = 0) OR ((("Position" ->> 'X'::text))::integer = 7) OR ((("Position" ->> 'Y'::text))::integer = 0) OR ((("Position" ->> 'Y'::text))::integer = 7)))
          Rows Removed by Filter: 8
Planning:
  Buffers: shared hit=22
```

### Containment query, planner's choice

Postgres keeps no statistics for `jsonb @>` selectivity, so it guesses, and here it guesses wrong. It still reads the whole table (4,215 pages):

```
Seq Scan on "SavedGames" s (actual rows=175.00 loops=1)
  Filter: (("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb))
  Rows Removed by Filter: 19825
  Buffers: shared hit=4215
Planning:
  Buffers: shared hit=4
```

### Containment query, seq scans disabled (`SET enable_seqscan = off`, for comparison only)

```
Bitmap Heap Scan on "SavedGames" s (actual rows=175.00 loops=1)
  Rows Removed by Index Recheck: 115
  Heap Blocks: exact=283
  Buffers: shared hit=1259
  ->  BitmapOr (actual rows=0.00 loops=1)
        Buffers: shared hit=976
        ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=130.00 loops=1)
              Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb)
              Index Searches: 1
              Buffers: shared hit=244
        ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=156.00 loops=1)
              Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb)
              Index Searches: 1
              Buffers: shared hit=244
        ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=139.00 loops=1)
              Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb)
              Index Searches: 1
              Buffers: shared hit=244
        ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=154.00 loops=1)
              Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb)
              Index Searches: 1
              Buffers: shared hit=244
Planning:
  Buffers: shared hit=4
```

(The `Recheck Cond` line is omitted above for width. It repeats the four `@>` conditions.)

### Timings (median of 9 runs after 1 warm-up, ids only)

| Query | Plan | Median |
|---|---|---|
| LINQ over JSON | Seq Scan + `jsonb_to_recordset` | 52.2 ms |
| Containment, planner's choice | Seq Scan | 71.1 ms |
| Containment, GIN used (`enable_seqscan = off`) | BitmapOr of GIN scans | 6.6 ms |

**Takeaway:** when it's used, the GIN index is about 8× faster than the LINQ query. Postgres won't choose it for `@>` by itself, because it can't estimate how selective the condition is, so without the index the containment query is actually *slower* than the LINQ one. The B-tree indexes EF Core 11 creates on nested complex properties are chosen even at 1,000 rows.

## Compared with RC1 (tag `post-5`)

The RC1 captures (2026-10-02) used EF Core `11.0.0-rc.1.26425.128` and Npgsql `11.0.0-rc.1.1`. The 1,000-game run used the AppHost's PostgreSQL 18.3. **The 20,000-game run used a scratch PostgreSQL 17 container outside Aspire**, so the 20,000-game numbers below differ in Postgres version, query shape, *and* EF version. Don't read the ratio change as an EF Core improvement or regression.

| | RC1 | `dotnet-11-release` |
|---|---|---|
| Query 1b SQL | Raw SQL via `Database.SqlQuery<Guid>`, composed as `WHERE s."Id" IN (SELECT ...)` | `EF.Functions.JsonContains` → `WHERE s."Board" @> @left OR ...` |
| 20k, containment, planner's choice | Parallel Seq Scan + PK nested loop, 75.1 ms | Seq Scan, 71.1 ms |
| 20k, containment, GIN forced | BitmapOr + PK nested loop, 3.5 ms | BitmapOr, 6.6 ms |
| 20k, LINQ over JSON | 53.4 ms | 52.2 ms |
| 20k, GIN speed-up when used | ~15× | ~8× |
| Matches at 20k | 175 / 175 | 175 / 175 (same ids) |
| Planner picks GIN by itself? | No | No |

The 1,000-game plans for queries 1, 2 and 3 are the same as on RC1, apart from one buffer page (213 → 214) and single-run timings.
