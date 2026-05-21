# Architecture

## Project Layout

```
Flank/Assets/
├── Scripts/
│   ├── App/            # Cross-cutting concerns (audio, persistence, scene routing)
│   ├── Gameplay/       # Pure game logic — no Unity dependencies where possible
│   │   ├── AI/         # Decision-making brains and move generation
│   │   ├── Controllers/# Human and AI player controllers
│   │   ├── Core/       # Game loop, board builder, turn system
│   │   ├── LAN/        # LAN-specific bootstrap
│   │   ├── Model/      # Immutable game state (Board, Pieces, Flags)
│   │   ├── Rules/      # Movement, capture, flag, and win rules
│   │   ├── Shared/     # Enums and value types shared across layers
│   │   └── View/       # Hex and piece renderers
│   ├── Networking/     # Transport layer (LAN + Online)
│   ├── Settings/       # Global settings keys and config
│   └── UI/             # All UI panels and controllers
├── Prefabs/
│   ├── Gameplay/       # Attacker piece, Defender piece, Hex tile, Board container
│   └── LAN/            # LAN game controller, player entry UI
├── ScriptableObjects/
│   ├── AI/             # AI configuration assets
│   └── BoardConfigs/   # Board map definitions (DefaultBoardMap)
└── Scenes/
    ├── NavigationScene  # Main menu and all lobby panels
    ├── GameScene        # Active gameplay
    ├── PostGame         # Results and stats
    └── Persistence      # DontDestroyOnLoad data bridge
```

## Scene Flow

```
NavigationScene  ──► GameScene  ──► PostGame  ──► NavigationScene
      │
      └─ (Persistence scene loaded additively and never unloaded)
```

`SceneRouter` drives all transitions. `PersistentSceneLoader` ensures the `Persistence` scene stays loaded across transitions so save data and settings survive.

## Layered Architecture

```
UI Layer          (Scripts/UI)
    │  user input, display
    ▼
Controller Layer  (Gameplay/Controllers)
    │  IPlayerController — HumanPlayerController / AIPlayerController
    ▼
Game Loop         (Gameplay/Core / GameController)
    │  applies PlayerActions, advances turns
    ▼
Rules Layer       (Gameplay/Rules / RulesEngine)
    │  MovementRules, CaptureRules, FlagRules, WinRules
    ▼
Model Layer       (Gameplay/Model)
    │  GameState, BoardModel, PieceModel, FlagModel — pure data
    ▼
View Layer        (Gameplay/View)
       renders board and pieces from model state
```

`GameController` is the central orchestrator. It owns the `GameState`, calls `RulesEngine` to validate and apply moves, and notifies the view after each state change.

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `GameController` | Main game loop — turn sequencing, action application, win checking |
| `TurnSystem` | Tracks whose turn it is and advances to next turn |
| `MoveBudget` | Enforces how many piece-moves each role gets per turn |
| `RulesEngine` | Aggregates all rule checks; single entry point for legal-move queries |
| `LegalMoveGenerator` | Enumerates all valid `PlayerAction`s for a given `GameState` |
| `GameState` | Snapshot of pieces, flags, current turn, and result |
| `BoardModel` | Hex grid indexed by `HexCoord` (axial q/r coordinates) |
| `BoardMapBuilder` | Constructs `BoardModel` from a `BoardMapConfig` text definition |
| `MinimaxBrain` | Depth-limited minimax with alpha-beta pruning and move ordering |
| `SceneRouter` | Centralized scene navigation |
| `SaveSystem` | JSON-based save/load for game settings and state snapshots |

## AI Architecture

Five brain types implement `IAIBrain`:

| Brain | Strategy |
|-------|---------|
| `MinimaxBrain` | Depth-limited minimax, alpha-beta pruning, configurable node budget (default 10 000) |
| `HeuristicBrain` | Single-ply heuristic evaluation, fast |
| `RandomBrain` | Uniform random legal move selection |

The `AIPlayerController` wraps any `IAIBrain` and feeds its chosen `PlayerAction` into `GameController` on its turn.

## Networking Architecture

Two independent transport paths share the same game logic:

**LAN:**  `GameModeBootstrap` → Unity NGO → `LAN/` scripts  
**Online:** `Online/` UI controllers → matchmaking server (room discovery) → Unity NGO (direct peer connection)

The matchmaking server only brokers room advertisements; actual game traffic flows peer-to-peer via NGO after connection is established.
