# Tiles & Tokens: game rules

The rules are deliberately small. The game exists to give the type system something real to model.

## Setup

- 2–4 players on an **8×8 grid**. Each cell is empty or holds one **tile**.
- A tile has a **terrain** (`Plains`, `Forest`, `Water`, `Mountain`) and an **owner**.
- Each player starts with a **hand of 3 cards**. A card either places a specific terrain tile or is a special card (e.g. `SwapTiles`, `BonusPoints`).
- A **victory condition** is chosen at setup (see below).

## Turns

On their turn, a player takes **exactly one action**:

| Action | Effect |
|---|---|
| **Roll dice** | Roll 2d6. The total is added to the player's score. Doubles also draw a bonus card. |
| **Play a card** | Play one card from the hand. Tile cards need a target position and must pass placement validation. A placed tile scores 1 point. |
| **Pass** | Do nothing. A player who passes 3 turns in a row is eliminated. |

## Placement rules

All of these must pass:

1. The cell is on the board and empty.
2. After a player's first tile, each new tile must be orthogonally adjacent to one of that player's own tiles.
3. `Water` can't be placed next to `Mountain` (in either direction).

## Victory conditions

| Condition | Winner |
|---|---|
| `MostPoints(Rounds)` | Highest score among players still in the game after N rounds. Ties go to the player earliest in turn order. |
| `FirstToEdge` | First player whose connected tiles span from one edge of the board to the opposite edge. |
| `LastPlayerStanding` | Last player who hasn't been eliminated. |

## Randomness

Dice rolls and deck shuffles are injected (`IDiceRoller`, `IDeckShuffler`), so tests and demos are deterministic.
