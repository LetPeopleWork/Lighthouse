# ADR-259: Licence restrictions are one shared query, and refuse creation while they cannot be told

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode; revised after the architecture review). The create-only
refusal is listed for the maintainer (Q-M1).
**Feature**: `epic-6400-fast-with-many-features` (Story #6406, slice 02)
**Relies on**: the server-side `LicenseGuard` (creation and update limits are enforced by the API whatever the UI
shows).

## Context

`useLicenseRestrictions()` is mounted at 23 call sites. Every mount keeps its own state and fires `/license`,
`/teams` and `/portfolios`, only to count Teams and Portfolios. The first Overview load fires three of each. Eighteen
call sites read only the licence status; five read the count-based answers: two create gates (`canCreateTeam`,
`canCreatePortfolio`, on the Overview) and three edit or update gates (`canUpdateTeamData`, `canUpdatePortfolioData`,
in `EditTeam`, `ModifyProjectSettings` and the detail pages).

Today the hook is **permissive** while it loads and when any of the three requests fails: the count-based answers are
`true`. Premium gating is already closed in both states (`licenseStatus?.canUsePremiumFeatures` is falsy). D30 and
AC-2.5 require that "a count that failed to load never reads as under the limit"; D30 also requires "exactly as today".

## Decision

1. **One query per answer, shared through TanStack Query.** The licence status uses the key `["licenseStatus"]`, the
   same key `LicenseStatusIcon` already uses (and invalidates on a licence import), so the header and every gate share
   one request. The counts come from the existing list endpoints under shared keys (`["teams"]`, `["portfolios"]`),
   **enabled only once the licence is known and not Premium**. A Premium instance never fetches lists for licensing.
2. **The counts keep today's meaning**: the length of the list the caller is served (RBAC-filtered, as today). The
   count queries use a stale time of zero, so a page mounted after a create or delete counts again; concurrent mounts
   share one request.
3. **The hook reports a status** (`loading`, `ready`, `failed`) beside the answers. `ready` answers exactly as today.
   While `loading` or `failed`, only the two **create** answers are refused (no reason while loading; "Couldn't check
   your licence" when failed, copy settled at DISTILL's sketch). The edit and update answers stay allowed in every
   state, as today: an existing Team or Portfolio is never locked out of its own settings because a count could not be
   read, and `LicenseGuard` still refuses an update over the limit on the server.
4. **UI gating stays derived from hooks**: no component fetches `/license`, `/teams` or `/portfolios` for licensing on
   its own; `useRbac()` is untouched.

## Alternatives considered

- **Refuse every count-based answer while loading or failed** (the first draft of this ADR): satisfies AC-2.5 as
  written, but a transient failure would block saving settings someone is mid-way through, on an instance that is
  within its licence, and it changes three edit screens D30 says stay "exactly as today". Rejected; the maintainer may
  choose it (Q-M1).
- **Keep fail-open everywhere**: matches today and the RBAC precedent (`PERMISSIVE_SUMMARY` in `useRbac`), but offers a
  create that `LicenseGuard` then refuses with an error, against AC-2.5. Rejected.
- **A new count endpoint** (`GET /license/usage`): a tiny payload, but a new contract, and the RBAC-filtered count
  would be re-implemented on the server. On the instances where counts matter (not Premium) the lists are small.
  Rejected.
- **A React context provider for licence state**: a second cache beside TanStack Query, which App already provides.
  Rejected.

## Consequences

**Positive**
- One `/license` request per page instead of up to 23; no licence-driven `/teams` or `/portfolios` on Premium.
- A new Team or Portfolio is never offered when the instance cannot tell whether one more is allowed.
- Edits behave exactly as today in every state.

**Negative / accepted**
- For the sub-second wait on a Community instance, the two create buttons render disabled before they enable.
- If the API is down, Community users see the two create actions refused with a reason, where today they were offered
  and then failed.
- AC-2.5 is narrowed from "a gated action" to "a gated create action" (upstream-changes UC-1).

## Enforcement

- Hook tests: one request per key for N simultaneous mounts; Premium fires no list request; `loading` and `failed`
  refuse both create answers and allow both update answers; `ready` answers equal today's for counts at, under and over
  each limit, RBAC on and off.
- Frontend architecture rule (a test over the source tree): `getLicenseStatus` is called only from the one shared query
  function that the hook and `LicenseStatusIcon` both use, and no component calls `getTeams` / `getPortfolios` to count
  for licensing.
