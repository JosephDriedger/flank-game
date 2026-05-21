# Flank

A turn-based tactical hex grid game for two players — Attacker vs Defender — built in Unity with LAN and online multiplayer.

## Overview

Flank is played on a hexagonal board where an Attacker tries to capture flags and a Defender tries to eliminate all attacking pieces. Each role uses distinct movement rules, and captures happen through flanking mechanics — positioning pieces on opposite sides of an enemy.

**Game modes:**
- Single Player — Human vs AI (configurable difficulty)
- Pass & Play — Two humans on the same device
- LAN Multiplayer — Local network via UDP broadcast discovery
- Online Multiplayer — Via a hosted matchmaking server

## Winning

| Role | Win Condition |
|------|--------------|
| Attacker | Return 2 flags to base |
| Defender | Eliminate all attacker pieces |

## Quick Start

### Unity Project

1. Open `Flank/` in Unity (2022.3 LTS or later)
2. Open `NavigationScene` as the startup scene
3. Press Play

### Matchmaking Server (Online Mode)

```bash
cd matchmaking-server
node server.js
```

Defaults to port `3000`. Set `PORT` env var to override. See [docs/matchmaking-server.md](docs/matchmaking-server.md) for deployment options.

## Documentation

| Doc | Contents |
|-----|----------|
| [Architecture](docs/architecture.md) | Project layout, scene flow, script organization |
| [Game Mechanics](docs/game-mechanics.md) | Movement, capture, flags, turn rules, win conditions |
| [Networking](docs/networking.md) | LAN and online multiplayer setup |
| [Matchmaking Server](docs/matchmaking-server.md) | REST API reference and deployment guide |

## Building an Installer

Installer scripts for both platforms live in [`installer/`](installer/). See [installer/README.md](installer/README.md) for full details.

**Windows** — [Inno Setup 6](https://jrsoftware.org/isdl.php) required:
```cmd
iscc installer\flank-setup.iss
```
Output: `installer\Output\FlankSetup-1.0.0.exe`

**macOS** — `create-dmg` recommended (`brew install create-dmg`), falls back to `hdiutil`:
```bash
cd installer && ./build-dmg.sh
```
Output: `installer/Output/FlankSetup-1.0.0.dmg`

## Project Structure

```
flank-game/
├── Flank/                      # Unity project
│   └── Assets/Scripts/
│       ├── App/                # Audio, persistence, scene routing
│       ├── Gameplay/           # Game logic, rules, AI, model
│       ├── Networking/         # LAN and online transport
│       ├── Settings/           # Global config
│       └── UI/                 # All UI panels and controllers
├── installer/                  # Inno Setup installer script
├── matchmaking-server/         # Node.js room broker (zero dependencies)
└── docs/                       # This documentation
```

## Tech Stack

- **Engine:** Unity (C#)
- **Networking:** Unity NGO (Netcode for GameObjects) + custom LAN discovery
- **AI:** Minimax with alpha-beta pruning
- **Backend:** Node.js (zero dependencies)
