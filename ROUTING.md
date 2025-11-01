# Routing Structure

This project uses a modular routing structure with `itty-router` to organize routes by feature/domain.

## Project Structure

```
src/
├── index.js          # Main entry point - initializes router and handles requests
├── router.js         # Router configuration - sets up all route handlers
└── routes/
    ├── health.js     # Health check endpoints (/health, /health/ready, /health/live)
    ├── users.js      # User management routes (/users, /users/:id, /users/:id/teams)
    └── teams.js      # Team management routes (/teams, /teams/:id, /teams/:id/players)
```

## Adding New Routes

### 1. Create a new route file

Create a new file in `src/routes/` (e.g., `src/routes/games.js`):

```javascript
/**
 * Game management routes
 */
export function setupGamesRoutes(router) {
  // GET /games - List all games
  router.get('/games', () => {
    return Response.json({ games: [] });
  });

  // GET /games/:id - Get a specific game
  router.get('/games/:id', ({ params }) => {
    return Response.json({ id: params.id });
  });

  // POST /games/:id/players - Add player to game (subroute example)
  router.post('/games/:id/players', async (request) => {
    const body = await request.json();
    return Response.json({ gameId: request.params.id, player: body });
  });
}
```

### 2. Register the route in router.js

Import and call your setup function in `src/router.js`:

```javascript
import { setupGamesRoutes } from './routes/games.js';

export function createRouter(env) {
  const router = Router();
  
  setupHealthRoutes(router);
  setupUsersRoutes(router);
  setupTeamsRoutes(router);
  setupGamesRoutes(router); // Add your new routes
  
  router.all('*', () => {
    return new Response('Not Found', { status: 404 });
  });
  
  return router;
}
```

## Route Patterns

### Basic Routes
- `router.get('/path', handler)` - GET request
- `router.post('/path', handler)` - POST request
- `router.put('/path', handler)` - PUT request
- `router.delete('/path', handler)` - DELETE request
- `router.all('/path', handler)` - All HTTP methods

### Parameter Routes
- `/users/:id` - Access via `request.params.id`
- `/teams/:teamId/players/:playerId` - Access via `request.params.teamId` and `request.params.playerId`

### Subroutes
Subroutes are handled by defining routes with nested paths:
- `/users/:id/teams` - Get teams for a user
- `/teams/:id/players` - Get players for a team

Routes are matched in the order they are registered, so more specific routes should be registered before more general ones.

## Handler Function Signature

Route handlers receive the request object with added properties:

```javascript
router.get('/users/:id', (request) => {
  // request.params - URL parameters (e.g., { id: '123' })
  // request.query - Query string parameters (e.g., { page: '1' })
  // request - Standard Request object (body, headers, method, etc.)
  
  return Response.json({ id: request.params.id });
});
```

For async handlers:

```javascript
router.post('/users', async (request) => {
  const body = await request.json();
  return Response.json(body, { status: 201 });
});
```

## Error Handling

Global error handling is done in `src/index.js`. Unhandled errors will return a 500 response with error details.

For route-specific error handling, you can wrap handlers in try-catch or use the router's built-in error handling features.

