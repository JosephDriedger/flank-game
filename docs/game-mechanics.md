# Game Mechanics

## Roles

| Role | Goal |
|------|------|
| **Attacker** | Capture 2 flags by returning them to base |
| **Defender** | Eliminate all attacker pieces through flanking |

The Attacker always goes first.

## Board

The board is a hexagonal grid defined in `BoardMapConfig`. Each hex can hold:
- One piece (Attacker or Defender)
- One flag
- A terrain type

Hex positions use axial coordinates `(q, r)`. Adjacency is the standard 6-direction hex neighbourhood.

## Turn Structure

Turns alternate: Attacker → Defender → Attacker → …

### Attacker Turn
- Move **2 different pieces**, each up to their legal destinations.
- If only 1 attacker piece is alive, move that piece once.

### Defender Turn
- Move **1 piece** up to **2 steps**.
- The piece cannot return to the hex it started the turn on as its second step.

`MoveBudget` tracks remaining moves and enforces these limits.

## Movement

A piece may move to:

1. **Adjacent hex** — any of the 6 neighbouring hexes that is unoccupied.
2. **Jump** — exactly 2 hexes away in a straight line, passing over the occupied hex between them. The landing hex must be empty.

Pieces cannot move onto a hex already occupied by a friendly piece.

## Capture (Flanking)

An Attacker piece is **captured** after a Defender completes a move that places two Defenders on **opposite sides** of the Attacker on any straight hex axis.

- Only Attackers can be captured this way.
- Captured pieces are removed from the board immediately.
- A Defender moving into a flanking position triggers capture; the Defender is not at risk during this move.

The capture check runs in `CaptureRules` after every Defender move.

## Flags

There are **2 flags** on the board. Each flag has a fixed home location.

### Picking Up
- An **Attacker** stepping onto a flag's hex automatically picks it up.
- The flag is now carried — it moves with the Attacker.

### Carrying
- The carrying Attacker's position determines the flag's position.
- A carried flag cannot be picked up by another piece.

### Dropping
- If a carrying Attacker is captured, the flag drops to the hex where the Attacker was captured.
- A dropped flag can be picked up again.

### Capturing a Flag (Attacker wins one)
- An Attacker carrying a flag moves onto the **Attacker's base hex**.
- The flag is marked captured. Two captured flags = Attacker victory.

### Defenders and Flags
- Defenders cannot carry flags.
- Defenders can prevent Attackers from picking up or returning flags by controlling territory.

## Win Conditions

| Condition | Winner |
|-----------|--------|
| 2 flags captured and returned to base | Attacker |
| All Attacker pieces eliminated | Defender |

Win is evaluated by `WinRules` after every action.

## Board Configuration

Boards are defined as text maps in `BoardMapConfig` ScriptableObjects. The default board is `DefaultBoardMap`. Custom boards can be created by adding new assets with different hex layouts, start positions, and flag locations.
