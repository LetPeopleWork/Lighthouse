# Slice 01: The history fill is on by default

**Goal:** every new and upgraded instance runs *Fill in past days on over-time charts* without an admin
switching it on, and an admin's later off sticks.

## IN scope
- `OptionalFeatureSeeder` seeds `OverTimeHistoryFill` with `Enabled = true` (D2).
- A one-time flip of an existing row to on, guarded by a marker in `AppSettings` (D1, D3).
- The marker is written on fresh installs too, so a fresh instance's later off sticks (AC-1.4).
- Docs: `docs/settings/configuration.md`, `docs/metrics/predictability.md` and `ARCHITECTURE.md` if it applies.
- Lighthouse-Clients copy (client index.ts, mcp-core index.ts, SKILL.md) and a patch version.
- A release-notes line on ADO #6083.

## OUT of scope
- Removing the switch or the Preview badge (#6084).
- Recording toggles locally or telling seeded offs from deliberate ones.
- Any change to the filler.

## Learning hypothesis
- **Disproves** "on by default is a safe default" if, within 30 days, at least 10 % of reporting
  instances switch it off (`OptionalFeatureToggled`) or report surprising past days.
- **Confirms** it if the off-rate stays low and #6084 (remove the switch) can follow.

## Acceptance criteria
AC-1.1 … AC-1.6 in `../feature-delta.md` (US-01).

## Dependencies
#6053 released. No other dependencies.

## Effort
About 2–3 hours of production and test change, plus the docs and client copy.
Reference class: story 5913 (Faster Updates switch removal, done in the seeder).

## Dogfood
Upgrade the dev instance (`:5169`, real history) and check that the row is on. Switch it off, restart
twice, and check that it is still off.
