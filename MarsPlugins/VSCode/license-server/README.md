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
- `GET /v1/license/client-state` (minimal client policy payload)
- `GET /v1/license/policy`
- `GET /v1/license/declaration?lang=en|zh`
- `POST /v1/license/test/claim` (admin only, test pool US/CN each 200)
- `GET /admin` (simple admin page)
- `GET /zelle` (user payment page for Zelle mode)
- `POST /v1/zelle/order/create` (server creates unique order ID)
- `POST /v1/zelle/order/proof` (user submits transfer proof + email)
- `GET /v1/zelle/order/status?orderId=...&email=...`
- `GET /v1/zelle/order/license?orderId=...&email=...` (returns license when approved)
- `GET /stripe` (user payment page for Stripe mode)
- `GET /stripe/success?session_id=...&orderId=...` (auto-finalize page)
- `POST /v1/stripe/checkout/create` (server creates unique order ID + checkout session)
- `GET /v1/stripe/session/finalize?sessionId=...|orderId=...` (immediate permission finalize)
- `GET /v1/stripe/order/license?orderId=...&email=...`
- `POST /v1/stripe/webhook` (Stripe webhook callback)
- `POST /v1/admin/login` (admin login check)
- `GET /v1/admin/stats` (admin only)
- `GET /v1/admin/licenses?type=ALL|PAID|TEST|TRIAL_LIMITED&q=...` (admin only)
- `POST /v1/admin/license/action` (admin only, `{ rowId, action: renew|revoke, days? }`)
- `GET /v1/admin/zelle/orders?status=...&q=...` (admin only)
- `POST /v1/admin/zelle/action` (admin only, `{ orderId, action: APPROVE|REJECT, reviewNote? }`)
- `GET /v1/admin/stripe/orders?status=...&q=...` (admin only)
- `POST /v1/admin/stripe/finalize` (admin only, `{ orderId, reviewNote? }`)
- `GET /v1/admin/revocations` (admin only)
- `GET /v1/admin/query?q=...` (admin only)
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
- Optional:
  - `ZELLE_RECEIVER_NAME`
  - `ZELLE_RECEIVER_ACCOUNT`
  - `PUBLIC_BASE_URL`
  - `STRIPE_SECRET_KEY`
  - `STRIPE_WEBHOOK_SECRET`
  - `STRIPE_PRICE_US_CENTS`
  - `STRIPE_PRICE_CN_CENTS`

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

## Claim a TEST license slot (example)

```bash
curl -X POST http://127.0.0.1:8787/v1/license/test/claim \
  -H "Content-Type: application/json" \
  -H "x-admin-key: <ADMIN_API_KEY>" \
  -d '{
    "customerEmail":"test-user@example.com",
    "region":"US"
  }'
```

## Security notes

- Never store `ADMIN_API_KEY`, `LICENSE_SIGNING_SECRET`, or `PRIVACY_HASH_SALT` in git.
- Run behind HTTPS (reverse proxy) in production.
- Rotate `LICENSE_SIGNING_SECRET` with key versioning when you move to production scale.
