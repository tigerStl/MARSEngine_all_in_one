# MARS License Server (Minimal + Privacy-First)

This is a minimal license server for paid features in the MARS VS Code extension.

## Privacy-first design

- Data minimization: no raw customer email is persisted by default.
- Pseudonymization: `customerEmail` is converted to salted SHA-256 hash (`customerRef`).
- No IP storage in business logs.
- Security headers enabled (`X-Frame-Options`, `Referrer-Policy`, `Cache-Control: no-store`).
- Signature verification uses HMAC-SHA256 with timing-safe comparison.
- Revocation list stores only hashed license IDs.

## Endpoints

- `GET /health`
- `POST /v1/license/issue` (admin only)
- `POST /v1/license/verify`
- `POST /v1/license/revoke` (admin only)
- `POST /v1/privacy/delete-audit-by-customer` (admin only)

## Quick start

1. Copy env file:

```bash
cp .env.example .env
```

2. Set secure values:

- `ADMIN_API_KEY`
- `LICENSE_SIGNING_SECRET`
- `PRIVACY_HASH_SALT`

3. Start service:

```bash
npm start
```

Server listens on `127.0.0.1:${PORT}`.

## Issue a license (example)

```bash
curl -X POST http://127.0.0.1:8787/v1/license/issue \
  -H "Content-Type: application/json" \
  -H "x-admin-key: <ADMIN_API_KEY>" \
  -d '{
    "customerEmail":"user@example.com",
    "plan":"pro",
    "currency":"USD",
    "amount":19,
    "region":"NA",
    "durationDays":365,
    "features":["mcp_tools","replay","diagnostics"]
  }'
```

## Verify a license (example)

```bash
curl -X POST http://127.0.0.1:8787/v1/license/verify \
  -H "Content-Type: application/json" \
  -d '{"license": {"...":"..."}}'
```

## Security notes

- Never store `ADMIN_API_KEY`, `LICENSE_SIGNING_SECRET`, or `PRIVACY_HASH_SALT` in git.
- Run behind HTTPS (reverse proxy) in production.
- Rotate `LICENSE_SIGNING_SECRET` with key versioning when you move to production scale.
