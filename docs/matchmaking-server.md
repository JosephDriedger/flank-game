# Matchmaking Server

A zero-dependency Node.js HTTP server that brokers room advertisements for online multiplayer. It does **not** relay game traffic — it only lets hosts advertise and clients discover open rooms.

## Running Locally

```bash
cd matchmaking-server
node server.js
# Listening on http://localhost:3000
```

Set `PORT` to use a different port:

```bash
PORT=8080 node server.js
```

## Deployment

The server has no dependencies and a single file (`server.js`). It can be deployed to any platform that runs Node.js.

### Render

1. Connect the repo to [Render](https://render.com).
2. Create a new **Web Service**, set the root to `matchmaking-server/`.
3. Build command: *(leave empty)*
4. Start command: `node server.js`

### Railway

```bash
cd matchmaking-server
railway up
```

### Docker

```bash
cd matchmaking-server
docker compose up
```

A `Dockerfile` and `docker-compose.yml` are included.

---

## REST API

Base URL: `http://<host>:<port>`

All responses are JSON. All endpoints support CORS (`Access-Control-Allow-Origin: *`).

---

### Create a room

```
POST /rooms
```

**Request body**

```json
{
  "name":     "Joe's Game",
  "hostIp":   "192.168.1.42",
  "hostPort": 7777,
  "hostName": "Joe"
}
```

| Field | Required | Max length | Description |
|-------|----------|-----------|-------------|
| `name` | Yes | 32 chars | Display name shown in room list |
| `hostIp` | Yes | 64 chars | Host's reachable IP address |
| `hostPort` | Yes | — | NGO listen port |
| `hostName` | No | 32 chars | Host player name (default: `"Host"`) |

**201 Created**

```json
{
  "id":          "A3KZ7Q",
  "name":        "Joe's Game",
  "hostIp":      "192.168.1.42",
  "hostPort":    7777,
  "hostName":    "Joe",
  "playerCount": 1,
  "createdAt":   1716300000000
}
```

**Errors**

| Status | Meaning |
|--------|---------|
| 400 | Missing required field or invalid JSON |
| 503 | Server at capacity (100 rooms) |

---

### List rooms

```
GET /rooms
```

Returns all currently active (non-expired) rooms as a JSON array. Expired rooms (no heartbeat for 90 s) are automatically excluded.

**200 OK**

```json
[
  {
    "id":          "A3KZ7Q",
    "name":        "Joe's Game",
    "hostIp":      "192.168.1.42",
    "hostPort":    7777,
    "hostName":    "Joe",
    "playerCount": 1,
    "createdAt":   1716300000000
  }
]
```

---

### Get a room

```
GET /rooms/:id
```

**200 OK** — same shape as a single room object above.  
**404** — room not found or expired.

---

### Send a heartbeat

```
PUT /rooms/:id/heartbeat
```

Resets the room's TTL. Hosts should call this every **30–60 seconds** while the room is open.

**200 OK**

```json
{ "ok": true }
```

**404** — room not found or already expired.

---

### Delete a room

```
DELETE /rooms/:id
```

Removes the room immediately. Call this when the host closes or starts the game.

**200 OK**

```json
{ "ok": true }
```

---

## Room Lifecycle

```
POST /rooms          →  room created, TTL = 90 s
PUT  /rooms/:id/heartbeat  →  TTL reset to 90 s
DELETE /rooms/:id    →  room removed immediately
                         (auto-removed after 90 s of no heartbeat)
```

Room IDs are 6-character alphanumeric strings using an unambiguous character set (no `0`, `O`, `1`, `I`).

## Limits

| Parameter | Value |
|-----------|-------|
| Max concurrent rooms | 100 |
| Room TTL | 90 seconds after last heartbeat |
| Cleanup interval | Every 15 seconds |
| Name max length | 32 characters |
| Host name max length | 32 characters |
