# Networking API Documentation

## Overview
The networking API provides endpoints for game session management, state synchronization, and command processing in multiplayer games.

## Authentication

### Headers
```
Authorization: Bearer <token>
Content-Type: application/json
```

### Authentication Endpoints

#### Login
```http
POST /api/auth/login
```

Request:
```json
{
    "username": "string",
    "password": "string"
}
```

Response:
```json
{
    "token": "string",
    "userId": "string",
    "username": "string"
}
```

## Game Session Management

### Create Game Session
```http
POST /api/game/create
```

Request:
```json
{
    "mapId": "string",
    "gameMode": "string",
    "maxPlayers": number,
    "factionType": "string"
}
```

Response:
```json
{
    "sessionId": "string",
    "hostId": "string",
    "mapDetails": {
        "id": "string",
        "name": "string",
        "size": {
            "width": number,
            "height": number
        }
    }
}
```

### Join Game Session
```http
POST /api/game/join
```

Request:
```json
{
    "sessionId": "string",
    "factionType": "string"
}
```

Response:
```json
{
    "success": boolean,
    "position": number,
    "players": [
        {
            "id": "string",
            "username": "string",
            "faction": "string",
            "ready": boolean
        }
    ]
}
```

## Game State Management

### Get Game State
```http
GET /api/game/state/{sessionId}
```

Response:
```json
{
    "gameTime": number,
    "players": [
        {
            "id": "string",
            "resources": {
                "gold": number,
                "power": number
            },
            "units": [
                {
                    "id": "string",
                    "type": "string",
                    "position": {
                        "x": number,
                        "y": number,
                        "z": number
                    },
                    "health": number,
                    "state": "string"
                }
            ],
            "buildings": [
                {
                    "id": "string",
                    "type": "string",
                    "position": {
                        "x": number,
                        "y": number
                    },
                    "health": number,
                    "state": "string"
                }
            ]
        }
    ],
    "resources": [
        {
            "id": "string",
            "type": "string",
            "position": {
                "x": number,
                "y": number
            },
            "amount": number
        }
    ]
}
```

## Command System

### Send Command
```http
POST /api/game/command
```

Request:
```json
{
    "sessionId": "string",
    "commandType": "string",
    "targetIds": ["string"],
    "parameters": {
        "position": {
            "x": number,
            "y": number,
            "z": number
        },
        "formation": "string",
        "attackType": "string"
    }
}
```

Response:
```json
{
    "success": boolean,
    "commandId": "string",
    "timestamp": number
}
```

## WebSocket Events

### Connection
```javascript
const socket = new WebSocket('ws://server/game/{sessionId}');
```

### Event Types

#### State Update
```json
{
    "type": "stateUpdate",
    "data": {
        "timestamp": number,
        "changes": [
            {
                "entityId": "string",
                "property": "string",
                "value": any
            }
        ]
    }
}
```

#### Command Broadcast
```json
{
    "type": "command",
    "data": {
        "commandId": "string",
        "playerId": "string",
        "command": {
            "type": "string",
            "parameters": object
        }
    }
}
```

#### Error Events
```json
{
    "type": "error",
    "data": {
        "code": number,
        "message": "string"
    }
}
```

## Error Codes

| Code | Description |
|------|-------------|
| 1001 | Invalid session |
| 1002 | Player not in session |
| 1003 | Invalid command |
| 1004 | Command validation failed |
| 1005 | Synchronization error |

## Rate Limits

- Authentication: 5 requests per minute
- Game Creation: 2 requests per minute
- Commands: 60 requests per minute
- State Updates: 10 requests per minute

## Best Practices

1. Command Batching
   - Group similar commands
   - Use command queues
   - Implement client-side prediction

2. State Synchronization
   - Use delta compression
   - Implement interpolation
   - Handle network latency

3. Error Handling
   - Implement exponential backoff
   - Handle reconnection gracefully
   - Validate client state
