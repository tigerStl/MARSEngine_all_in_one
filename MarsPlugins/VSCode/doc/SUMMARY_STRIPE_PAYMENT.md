# Stripe Payment Integration Summary

## Goal

Switch paid license flow from manual Zelle review to Stripe, and ensure users get immediate usage permission after successful payment.

## What Changed

- Added Stripe order model and persistence in `license-server` (`data/stripe-orders.json`).
- Added Stripe checkout creation endpoint: `POST /v1/stripe/checkout/create`.
- Added success callback finalize path:
  - `GET /stripe/success?...` page auto-calls finalize endpoint.
  - `GET /v1/stripe/session/finalize?sessionId=...|orderId=...`.
- Added webhook fallback:
  - `POST /v1/stripe/webhook` with signature verification support.
- Added user license retrieval endpoint:
  - `GET /v1/stripe/order/license?orderId=...&email=...`.
- Added admin Stripe operations:
  - `GET /v1/admin/stripe/orders`.
  - `POST /v1/admin/stripe/finalize`.
- Updated extension panel actions:
  - `Pay` opens Stripe page.
  - `Fetch` retrieves license from Stripe order.
  - `Import Lic` remains available for local fallback.

## Immediate Permission Guarantee

- Primary path: after payment, success page finalizes order and issues license immediately.
- Fallback path: webhook finalizes order if user leaves success page early.
- Lazy path: license fetch endpoint attempts finalization again if payment is complete but order not finalized yet.

## Configuration

Required for Stripe mode:

- `PUBLIC_BASE_URL`
- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET` (recommended)
- `STRIPE_PRICE_US_CENTS`
- `STRIPE_PRICE_CN_CENTS`

## Validation

- `node --check license-server/server.js` passed.
- `npm run compile` passed.
- `/stripe` endpoint served successfully.
- When Stripe key is missing, checkout endpoint returns `stripe_not_configured`.
