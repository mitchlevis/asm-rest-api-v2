## asportsmanager-api-cf

Short Cloudflare Workers API migrated from an AWS Lambda REST service.

### Quick start
- **Install**: `npm install`
- **Secrets**: set DB password once (stored by Cloudflare)

```bash
npx wrangler secret put DATABASE_PASSWORD
```

- **Dev server**: `npm run dev` (served at http://localhost:8787)
- **Tests**: `npm test`

### Deploy
- Default: `npm run deploy`
- Staging/Preview: `npm run deploy -- --env preview`
- Production: `npm run deploy -- --env production`

Routes are configured in `wrangler.jsonc` with custom domains for preview and production.

### Endpoints (examples)
- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /actions/wall-posts-for-user`
- `GET /actions/wall-posts-for-user/:regionId`

### Project layout
- Entry: `src/index.js` (Worker `fetch` handler)
- Router: `src/router.js`
- Routes: `src/routes/*`
- Controllers: `src/controllers/*`
- Tests: `test/*` (Vitest + Workers pool)

### Environment & config
- Config: `wrangler.jsonc` (`main`, `compatibility_date`, `nodejs_compat`, env vars, routes)
- DB connection settings come from `wrangler.jsonc` env vars; `DATABASE_PASSWORD` is a secret.

### Requirements
- Node.js 18+ and npm
- Cloudflare account (Wrangler runs from devDependencies)

### MCP Cloudflare Server

- Install Cloudflare MCP Server

```bash
npx @cloudflare/mcp-server-cloudflare init
```
*Follow instructions to sign in and authenticate your computer on CF*

- Open Cursor Settings and go to 'Tools & MCP' and add the following entry:
```json
"cloudflare-asm": {
	"command": "npx",
	"args": ["@cloudflare/mcp-server-cloudflare", "run", "CHANGE_THIS_TO_THE_ACCOUNT_ID"]
}
```
- Restart Cursor
