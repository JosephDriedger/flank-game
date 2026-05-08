'use strict';
// Zero-dependency Node.js matchmaking server.
// Deploy free on: Render.com, Railway.app, Fly.io, or run locally.
// Set process.env.PORT if needed; defaults to 3000.

const http = require('http');
const { URL } = require('url');

const ROOM_TTL_MS = 90_000;   // Room expires 90 s after last heartbeat
const MAX_ROOMS   = 100;

const rooms = new Map(); // id -> room

function generateId() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // no 0/O/1/I to avoid confusion
    let id = '';
    for (let i = 0; i < 6; i++) id += chars[Math.floor(Math.random() * chars.length)];
    return id;
}

function cleanExpired() {
    const now = Date.now();
    for (const [id, room] of rooms) {
        if (now - room.lastSeen > ROOM_TTL_MS) rooms.delete(id);
    }
}

setInterval(cleanExpired, 15_000);

function readBody(req) {
    return new Promise((resolve, reject) => {
        let data = '';
        req.on('data', chunk => data += chunk);
        req.on('end', () => resolve(data));
        req.on('error', reject);
    });
}

function send(res, status, body) {
    const json = JSON.stringify(body);
    res.writeHead(status, {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
    });
    res.end(json);
}

function publicRoom(room) {
    const { lastSeen, ...r } = room;
    return r;
}

const server = http.createServer(async (req, res) => {
    const url    = new URL(req.url, 'http://localhost');
    const path   = url.pathname.replace(/\/$/, '') || '/';
    const method = req.method.toUpperCase();

    if (method === 'OPTIONS') {
        res.writeHead(204, {
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type',
        });
        res.end();
        return;
    }

    cleanExpired();

    // POST /rooms — create a room
    if (method === 'POST' && path === '/rooms') {
        let body;
        try { body = JSON.parse(await readBody(req)); }
        catch { return send(res, 400, { error: 'invalid json' }); }

        const { name, hostIp, hostPort, hostName } = body;
        if (!name || !hostIp || !hostPort) {
            return send(res, 400, { error: 'name, hostIp, hostPort required' });
        }
        if (rooms.size >= MAX_ROOMS) {
            return send(res, 503, { error: 'server full' });
        }

        let id;
        let attempts = 0;
        do { id = generateId(); attempts++; } while (rooms.has(id) && attempts < 20);

        const room = {
            id,
            name:        String(name).slice(0, 32),
            hostIp:      String(hostIp).slice(0, 64),
            hostPort:    Number(hostPort),
            hostName:    String(hostName || 'Host').slice(0, 32),
            playerCount: 1,
            createdAt:   Date.now(),
            lastSeen:    Date.now(),
        };
        rooms.set(id, room);
        return send(res, 201, publicRoom(room));
    }

    // GET /rooms — list active rooms
    if (method === 'GET' && path === '/rooms') {
        return send(res, 200, [...rooms.values()].map(publicRoom));
    }

    // Room-scoped routes
    const idMatch = path.match(/^\/rooms\/([A-Z0-9]{4,16})(\/heartbeat)?$/);
    if (idMatch) {
        const id          = idMatch[1];
        const isHeartbeat = !!idMatch[2];

        // GET /rooms/:id
        if (method === 'GET' && !isHeartbeat) {
            const room = rooms.get(id);
            if (!room) return send(res, 404, { error: 'not found' });
            return send(res, 200, publicRoom(room));
        }

        // PUT /rooms/:id/heartbeat
        if (method === 'PUT' && isHeartbeat) {
            const room = rooms.get(id);
            if (!room) return send(res, 404, { error: 'not found' });
            room.lastSeen = Date.now();
            return send(res, 200, { ok: true });
        }

        // DELETE /rooms/:id
        if (method === 'DELETE' && !isHeartbeat) {
            rooms.delete(id);
            return send(res, 200, { ok: true });
        }
    }

    send(res, 404, { error: 'not found' });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log(`Matchmaking server on port ${PORT}`));
