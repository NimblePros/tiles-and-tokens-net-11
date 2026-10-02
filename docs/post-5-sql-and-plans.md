# Post 5: generated SQL and execution plans

Captured 2026-10-02. EF Core `11.0.0-rc.1.26425.128`, Npgsql EF Core provider `11.0.0-rc.1.1`.

- **1,000 games:** from `dotnet run --project src/TilesAndTokens.AppHost` (the `efcore` demo). The AppHost's Postgres is **PostgreSQL 18.3**, the image Aspire 13.6 uses.
- **20,000 games:** a separate run of the same queries against a scratch **PostgreSQL 17** container. That run isn't part of the repo, and it uses a newer Postgres version than the 1,000-game run.

Plans use `EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)` on the exact SQL and parameters EF Core generated (`SqlDiagnostics.ExplainAnalyzeAsync`), after `ANALYZE "SavedGames"`.

## Schema (both migrations, `dotnet ef migrations script`)

```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

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
VALUES ('20261002155612_InitialCreate', '11.0.0-rc.1.26425.128');

COMMIT;

START TRANSACTION;
CREATE INDEX "IX_SavedGames_Board_Gin" ON "SavedGames" USING GIN ("Board" jsonb_path_ops);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261002161723_AddBoardGinIndex', '11.0.0-rc.1.26425.128');

COMMIT;
```

## 1,000 games (the demo)

### Query 1: edge query in LINQ over the `Board` JSON column

`SavedGameQueries.WithTileOnEdge`. EF Core 11 translates `Board.Tiles.Any(...)` into `jsonb_to_recordset`. No index can serve it.

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

Result: 9 games in 57 ms.

```
Seq Scan on "SavedGames" s (actual rows=9.00 loops=1)
  Filter: EXISTS(SubPlan 1)
  Rows Removed by Filter: 991
  Buffers: shared hit=213
  SubPlan 1
    ->  Function Scan on jsonb_to_recordset t (actual rows=0.01 loops=1000)
          Filter: (("OwnerId" = 'player-042'::text) AND (((("Position" ->> 'X'::text))::integer = 0) OR ((("Position" ->> 'X'::text))::integer = 7) OR ((("Position" ->> 'Y'::text))::integer = 0) OR ((("Position" ->> 'Y'::text))::integer = 7)))
          Rows Removed by Filter: 8
```

### Query 1b: the same query as jsonb containment (Postgres-native GIN)

`SavedGameQueries.WithTileOnEdgeUsingGin`. Raw SQL selecting ids, composed into LINQ. **At 1,000 games the planner scans the table instead of using the GIN index** (213 pages).

```sql
-- p0='{"Tiles":[{"OwnerId":"player-042","Position":{"X":0}}]}'
-- p1='{"Tiles":[{"OwnerId":"player-042","Position":{"X":7}}]}'
-- p2='{"Tiles":[{"OwnerId":"player-042","Position":{"Y":0}}]}'
-- p3='{"Tiles":[{"OwnerId":"player-042","Position":{"Y":7}}]}'
SELECT s."Id", s."SavedAt", s."StateJson", s."Board", s."Summary_Round", s."Summary_TurnCount", s."Summary_Leader_PlayerId", s."Summary_Leader_Score", s."Summary_Victory_Kind", s."Summary_Victory_Rounds"
FROM "SavedGames" AS s
WHERE s."Id" IN (
    SELECT s0."Value"
    FROM (
        SELECT "Id" AS "Value" FROM "SavedGames"
        WHERE "Board" @> @p0::jsonb OR "Board" @> @p1::jsonb OR "Board" @> @p2::jsonb OR "Board" @> @p3::jsonb
    ) AS s0
)
```

Result: 9 games in 35 ms.

```
Seq Scan on "SavedGames" (actual rows=9.00 loops=1)
  Filter: (("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb))
  Rows Removed by Filter: 991
  Buffers: shared hit=213
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

Result: 5 games in 25 ms.

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

Result: 5 games in 27 ms.

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

`rows: 20000, LINQ matches: 175, GIN matches: 175`

### LINQ edge query

```
Seq Scan on "SavedGames" s (actual rows=175 loops=1)
  Filter: EXISTS(SubPlan 1)
  Rows Removed by Filter: 19825
  SubPlan 1
    ->  Function Scan on jsonb_to_recordset t (actual rows=0 loops=20000)
          Filter: (("OwnerId" = 'player-042'::text) AND (((("Position" ->> 'X'::text))::integer = 0) OR ((("Position" ->> 'X'::text))::integer = 7) OR ((("Position" ->> 'Y'::text))::integer = 0) OR ((("Position" ->> 'Y'::text))::integer = 7)))
          Rows Removed by Filter: 8
```

### Containment query, planner's choice

Postgres has no statistics for `jsonb @>` selectivity, so it guesses, and here it guesses wrong. It picks a parallel sequential scan:

```
Gather (actual rows=175 loops=1)
  Workers Planned: 2
  Workers Launched: 2
  ->  Nested Loop (actual rows=58 loops=3)
        ->  Parallel Seq Scan on "SavedGames" (actual rows=58 loops=3)
              Filter: (("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb) OR ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb))
              Rows Removed by Filter: 6608
        ->  Index Scan using "PK_SavedGames" on "SavedGames" s (actual rows=1 loops=175)
              Index Cond: ("Id" = "SavedGames"."Id")
```

### Containment query, seq scans disabled (`SET enable_seqscan = off`, for comparison only)

```
Nested Loop (actual rows=175 loops=1)
  ->  Bitmap Heap Scan on "SavedGames" (actual rows=175 loops=1)
        Rows Removed by Index Recheck: 115
        Heap Blocks: exact=283
        ->  BitmapOr (actual rows=0 loops=1)
              ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=130 loops=1)
                    Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 0}}]}'::jsonb)
              ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=156 loops=1)
                    Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"X": 7}}]}'::jsonb)
              ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=139 loops=1)
                    Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 0}}]}'::jsonb)
              ->  Bitmap Index Scan on "IX_SavedGames_Board_Gin" (actual rows=154 loops=1)
                    Index Cond: ("Board" @> '{"Tiles": [{"OwnerId": "player-042", "Position": {"Y": 7}}]}'::jsonb)
  ->  Index Scan using "PK_SavedGames" on "SavedGames" s (actual rows=1 loops=175)
        Index Cond: ("Id" = "SavedGames"."Id")
```

(The `Recheck Cond` line is omitted above for width. It repeats the four `@>` conditions.)

### Timings (median of 9 runs, ids only)

| Query | Plan | Median |
|---|---|---|
| LINQ over JSON | Seq Scan + `jsonb_to_recordset` | 53.4 ms |
| Containment, planner's choice | Parallel Seq Scan | 75.1 ms |
| Containment, GIN used | BitmapOr of GIN scans | 3.5 ms |

**Takeaway:** the GIN index is about 15× faster when it's used. Postgres won't reliably choose it for `@>`, because it can't estimate how selective the condition is. The B-tree indexes EF Core 11 creates on nested complex properties are chosen even at 1,000 rows.
