# Slice 02 — Note a Delivery moment

**Epic** #5698 · **Story** US-02 · **ADO** #5639 · **Estimate** ≤1 day

## Goal
A dated, attributed line can be written against a live Delivery and read back later.

## IN scope
- A `DeliveryNote` entity (text, created-at, optional author reference) with a cascade from Delivery.
- One EF migration per supported provider via the `CreateMigration` script, additive-only.
- `GET` and `POST /api/latest/deliveries/{deliveryId}/notes`, gated `PortfolioRead` / `PortfolioWrite`
  with `ScopeIdRouteKey`, matching the existing Delivery endpoints.
- A third tab, Notes, in the Delivery section beside Work Items and Metrics — always enabled.
- Attribution through the existing current-user profile service; unattributed when it resolves to
  nobody (auth-off instance).
- Empty / whitespace-only rejection, plain-text rendering.

## OUT of scope
- Editing and deleting notes — Slice 03.
- Read-only behaviour on an archived Delivery — Slice 05 (nothing archives yet).
- System-written notes on threshold crossings.
- Rich text, attachments, mentions.

## Learning hypothesis
**Disproves if it fails**: that Lighthouse's identity plumbing is usable for per-row authorship. The
current-user profile service returns `null` without a stable subject claim, so an auth-off instance
has no author at all. If "unattributed" turns out to be unacceptable rather than merely honest, the
whole authorship rule of Slice 03 has to be rethought — and it costs one slice to find out, not three.
**Confirms if it succeeds**: attribution is a display concern, not an access-control concern, and
Slice 03 can build the author restriction on top of it.

## Acceptance criteria
AC-02.1 … AC-02.9 in `../feature-delta.md`.

## Dependencies
None.

## Reference class
`DeliveriesController` RBAC guards; `CurrentUserProfileService`; the existing two-tab shell in
`DeliverySection.tsx`.

## Dogfood moment
Write the first real note on the team's own Delivery the day it ships.
