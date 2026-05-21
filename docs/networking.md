# Networking

Flank supports two multiplayer paths: LAN (local network) and Online (via a matchmaking server). Both use Unity NGO (Netcode for GameObjects) for game transport once a connection is established.

## LAN Multiplayer

LAN discovery uses UDP broadcast — no internet connection or server required. Both devices must be on the same local network.

### How it works

1. **Host** opens the LAN lobby panel and creates a game. The host begins broadcasting its presence via UDP.
2. **Client** opens the LAN join panel. The client listens for UDP broadcasts and lists discovered hosts.
3. Client selects a host from the list and connects via Unity NGO.
4. `GameModeBootstrap` handles LAN-specific initialization once the NGO session is established.

### Key scripts

| Script | Role |
|--------|------|
| `LanHostPanelController` | Host creation UI and broadcast initiation |
| `LanJoinPanelController` | Client discovery UI and host list |
| `LanLobbyPanelController` | Lobby room list once connected |
| `GameModeBootstrap` | LAN-specific game session setup |
| `LanGameOverStatsAndTransition` | Post-game flow for LAN sessions |

### Troubleshooting

- Ensure both devices are on the same subnet.
- Check firewall rules — the game uses UDP broadcast on its discovery port.
- Only one host session per device at a time is supported.

---

## Online Multiplayer

Online multiplayer uses a centralized matchmaking server for room discovery, then a direct peer-to-peer NGO connection for gameplay.

### How it works

1. **Host** creates a room via `POST /rooms` on the matchmaking server, advertising their IP, port, and display name.
2. **Client** fetches the room list via `GET /rooms` and picks a room to join.
3. The client connects directly to the host's IP/port via Unity NGO — the matchmaking server is no longer involved.
4. The host sends periodic `PUT /rooms/:id/heartbeat` requests to keep the room visible. The room expires after 90 seconds without a heartbeat.
5. When the game ends, the host deletes the room via `DELETE /rooms/:id`.

### Key scripts

| Script | Role |
|--------|------|
| `OnlineHostPanelController` | Host creation UI, room registration, heartbeat |
| `OnlineBrowsePanelController` | Room browser, polls matchmaking server |
| `OnlineMenuPanelController` | Online lobby entry point |
| `OnlineRoomEntryUI` | Individual room entry in the browse list |

### Configuration

Set the matchmaking server URL in the game's settings before building. For local development, point it at `http://localhost:3000`. For production, use the deployed server URL.

See [matchmaking-server.md](matchmaking-server.md) for server setup and deployment.

---

## Shared: Post-Game

After a networked game ends, `GameOverStatsAndTransition` (or its LAN/Online variant) collects stats via `GameStateSnapshot` and transitions to the `PostGame` scene where results are displayed.
