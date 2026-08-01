# ADR-115: The ServiceNow basic-auth restriction is a documented prerequisite and a failure-message hint — Lighthouse must never claim to detect it

- **Status**: **Accepted** (ratified 2026-08-01 by the maintainer, at Epic 5513's close).
- **Date**: 2026-07-29
- **Feature**: epic-5513-servicenow-integration (ADO Epic 5513, Story 5574)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

DISCUSS locked basic auth as the v1 method (**D3**) on the maintainer's first-hand evidence from the
target on-prem instance, and recorded the platform-wide restriction trend as a risk rather than a
blocker (**D3a**). The SPIKE then measured the restriction on PDI `dev191338` (Australia release) and
found it **armed, with a date**:

| Property | Value |
|---|---|
| `glide.authenticate.basic_auth.restriction.active` | `true` |
| `glide.authenticate.basic_auth.restriction.enforce` | `false` (tracking mode) |
| `glide.authenticate.basic_auth.restriction.enforcement_date` | `2026-07-29 20:31:17` UTC |
| `glide.authenticate.basic_auth.allowed_roles` | `snc_basic_auth_api_access` |
| `glide.authenticate.basic_auth.allowed_users` | *(empty)* |

After the enforcement date, basic-auth requests from accounts holding neither the role nor a place on
the allow-list are blocked. So a customer on a current release must have
`snc_basic_auth_api_access` granted to the integration account before Lighthouse works at all.

That is an **instance-side setup step**, which contradicts the DISCUSS scope line "Lighthouse requires
no instance-side configuration". The SPIKE already recorded that contradiction and it is not re-opened
here: D3 stands, the grant is one role on one user, and it is documentable.

The genuinely contested part is what Lighthouse should *do* about it. An earlier draft of the SPIKE
findings claimed connection validation could read the three properties and warn the customer before
their integration broke. **That claim was measured as `admin` and was disproven when re-measured as a
least-privilege account:**

| Signal | as `sn_*_read` | as `admin` |
|---|---|---|
| basic-auth restriction properties | **200, zero rows** | full values |
| `glide.war` (release/patch) | **200, empty** | `glide-australia-02-11-2026…` |
| `sys_plugins` | **403** | readable |

The properties are invisible through exactly the credential a customer would supply. A privileged human
can check; the product cannot. Worse, the enforcement date on this instance was written 24 hours before
it fired — so even a customer who *can* look gets about a day's notice.

The disproven claim is the reason this needs an ADR rather than a line in a docs page. It is a plausible,
attractive feature that will be re-proposed by anyone who reads only the property list.

## Decision

**1. The role grant is a documented prerequisite, owned by US-05.** The ServiceNow docs page states,
before the connection steps, that the integration account needs `snc_basic_auth_api_access` (or a place
in `glide.authenticate.basic_auth.allowed_users`) on any instance where the inbound basic-auth
restriction is enforced. A customer whose instance already blocks basic auth learns it before installing
rather than after.

**2. The `authentication_failed` rung of [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md)
carries a static hint**, phrased as a conditional and never as a claim of knowledge:

> ServiceNow returned 401. Check the username and password. If this instance enforces the inbound
> basic-auth restriction, the account also needs the `snc_basic_auth_api_access` role — Lighthouse
> cannot check this for you.

No new IO, no new state, no detection. It attaches one sentence to a failure Lighthouse already surfaces,
at the exact moment the administrator is looking at it.

**3. Lighthouse must not read the restriction properties, and must not present any UI that implies it
knows the enforcement state.** This is a standing prohibition, not a slice-01 scoping decision. Any
future proposal to add it must first re-run the Q8 measurement as a least-privilege account and show a
different result.

**4. The docs must also state that `snc_read_only` grants no read access whatsoever** — measured
identical to holding no roles at all. It is a UI-write-restriction role whose name invites precisely the
wrong guess, and an administrator who grants it in good faith will land on ADR-114 rung 6.

**5. This strengthens, and does not weaken, the OAuth successor case (D3a).** It is recorded here so the
successor epic inherits the reasoning. Note the separately-recorded open question: a ServiceNow OAuth
token resolves to a user and inherits that user's roles, with no scope mechanism, so OAuth is expected
to solve the restriction but **not** to lower the permission bar for history (`itil`). That expectation
is reasoning, not measurement, and should be measured before anyone plans OAuth work on it.

## Alternatives Considered

**A. Docs-only — no product surface at all.**
Rejected as insufficient rather than wrong. It is honest and free, but the customer meets the failure
months after reading the page, at which point a 401 with no context sends them to re-check the password
they know is correct. The marginal cost of decision 2 is one string.

**B. Detect and warn at validation time.**
Rejected on measurement. It works for the maintainer and silently never fires for any customer — the
worst class of feature, because its absence of output is indistinguishable from "all clear". This is the
alternative that was actually proposed and disproven, which is why it is written down.

**C. Detect opportunistically — try to read the properties, stay silent on empty.**
Rejected, and it is the subtlest of the three. Because the properties return `200` with zero rows rather
than `403`, "empty" is indistinguishable from "restriction not configured". The feature would therefore
report "no restriction detected" to every least-privilege account — actively misleading, and strictly
worse than saying nothing. This is the same denial-wearing-a-success-costume shape ADR-114 exists for,
and it is the reason that shape needs a standing prohibition rather than a case-by-case judgement.

**D. Block or warn on connection creation after a hardcoded enforcement date.**
Rejected. The date is per-instance and configurable; a hardcoded constant would produce false alarms on
instances that never enforce it, and would go stale. Hardcoding an instance-specific value is also
precisely the mistake the SPIKE hit with `close_code` and had to fix by resolving at runtime.

**E. Require OAuth for v1 to sidestep the restriction entirely.**
Rejected — it reverses D3, which rests on first-hand evidence from the deciding environment. OAuth needs
an instance-side application-registry entry made by someone the Lighthouse user usually is not, which is
a *larger* adoption barrier than a single role grant, not a smaller one.

## Consequences

**Positive.**
- The prerequisite reaches the customer twice: once in the docs before they install, once in the failure
  message when it bites. Neither channel makes a claim the product cannot back.
- The disproven detection idea has a durable home. The next person to read the property list finds the
  measurement that kills it instead of re-deriving it.
- No new code path, no new IO, no new configuration.

**Negative.**
- Lighthouse genuinely cannot warn a customer before their integration breaks. This is a real capability
  gap accepted on evidence, not a design preference. The mitigation is documentation and the OAuth
  successor.
- The hint fires on *every* 401, including plain wrong passwords, so some administrators will read a
  sentence about a role they already hold. Judged the correct trade: a slightly noisy true statement
  beats a silent one.
- Rung 2's message is longer than the equivalent Jira/Linear text, which is a small consistency cost
  across the connection settings UI.

**Operational note carried from the SPIKE.** The `updatedemoenv.yml` workflow runs at 01:00 UTC, after
the PDI's enforcement date, using an `admin` account that does **not** hold the role. The seeder step and
any integration test using those credentials fail on the first scheduled run after enforcement. The fix
is one role grant on the PDI — outside this ADR's scope, but it is the first place the decision becomes
visible.

## Related

- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — the ladder whose rung 2
  carries the hint.
- SPIKE evidence: `docs/feature/epic-5513-servicenow-integration/spike/findings.md` ("D3a has a date on
  it" and Q9).
- DISCUSS D3 / D3a: `docs/feature/epic-5513-servicenow-integration/feature-delta.md`.
