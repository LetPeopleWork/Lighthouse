# Slice 05 — The cluster owns the key

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5780 · **Story**: US-05 · **Estimate**: ~5h
**Reference class**: `chart/templates/secret.yaml` — the database connection string and the OIDC client
secret already travel from values or an `existingSecret` into the pod without landing in a ConfigMap.
The encryption key reuses that values-or-`existingSecret` shape but **not** the last step of it: it
reaches the container as a mounted file rather than an environment variable, because an environment
variable is readable in a process dump and cannot change under a running process. The two genuinely new
behaviours are the mounted projection and the refusal to render without a key.

## Goal

A Helm install cannot be made to run on a key nobody owns: with neither `encryption.key` nor
`encryption.existingSecret` it refuses to render, naming both. An external secret store can own the key,
the pod reads it from a mounted file, and nothing in the chart can ever regenerate it.

## IN scope

- `encryption.existingSecret` — the deployment reads the key from a Secret that External Secrets
  Operator or OpenBao populates, and the chart renders no key of its own.
- **Fail at render with nothing supplied** (DESIGN F-1, ADR-153). With neither `encryption.key` nor
  `encryption.existingSecret`, `helm install` fails immediately with a message naming both, reusing the
  `required` precedent ADR-082 already set for `postgresql.auth.password`. The chart does **not**
  generate a key: the only mechanism is Helm `lookup`, which returns empty on every `helm template`
  render — and that is how ArgoCD renders, so a generator would mint a fresh key on every tenant sync
  and orphan the tenant's whole credential set. Generate-if-absent was the original plan and is retired.
- **Upgrade safety is now structural.** Nothing is minted, so nothing can be regenerated. The property
  still gets its test: three consecutive `helm upgrade` runs with unchanged values leave every stored
  secret readable.
- **The ring reaches the container as a mounted file**, never an environment variable (DESIGN F-5). The
  database password's `secretKeyRef` env route is readable in `/proc/<pid>/environ`. Mounting is also
  what makes picking up an operator-added key without a restart possible.
- Explicit `encryption.key` used verbatim and reported as configuration-supplied.
- **The Secret carries a key ring, not a key** — one active entry and any number of retired ones. That
  is what makes the Kubernetes rotation work without Lighthouse ever writing to a Secret: the operator
  adds the new key alongside the old, rolls the pod, triggers re-encryption from slice 03's panel, then
  drops the old key. Every step is an operator action against their own secret store.
- The key never appears in a ConfigMap, in rendered values, or in a pod environment dump.
- Chart unit tests for all four cases: nothing supplied, `existingSecret`, explicit key,
  upgrade-idempotence.
- `values.schema.json`, chart README, and a chart version bump.
- **The standalone single-container product is byte-unchanged.**

## OUT of scope

- An OpenBao **Transit** driver where the key never enters the process. Named as the successor epic in
  the epic's locked decisions; this slice hands over key *material* via a Secret.
- Per-tenant rotation automation, which lives in the private platform repository.
- Migrating existing tenants onto per-tenant keys — that is a platform-side operation using slice 03's
  rotation, once this slice makes per-tenant keys possible.

## Learning hypothesis

**Resolved before the slice was written.** The original hypothesis — "Helm can own key generation,
disproved if `helm upgrade` regenerates the key" — was answered in DESIGN without building anything:
`lookup` returns empty under `helm template`, which is how ArgoCD renders, so generation is unsafe on
the deployment path the platform itself uses. The chart refuses instead.

**What this slice now stakes**: that refusing is affordable. Disproved if a self-hoster following the
chart README cannot get to a working install as easily as before — the cost of the decision is one
more required value, and if it turns out to be more than that, the README is the thing to fix.
**Confirms**, if it holds, that each tenant gets a key its own secret store owns and no two installs
share one.

## Open question carried

**OQ-2 — narrowed.** "What happens when the external Secret is rotated underneath a running pod?" was
open while rotation was assumed to be one action. It is now the supported flow: both keys sit in the
Secret at once and re-encryption follows. What remains is the degenerate case — an operator who removes
the old key before re-encryption ran. The bar is that the affected secrets surface as unreadable rather
than the instance looping on work-tracking-system rejections.

## Acceptance criteria

AC-5.1 through AC-5.11 in `feature-delta.md`. The three that carry the slice:

- **AC-5.2** — an install supplying neither key nor `existingSecret` fails at render, naming both.
- **AC-5.3** — three consecutive `helm upgrade` runs with unchanged values leave every stored secret
  readable.
- **AC-5.7** — the standalone product is unchanged by this slice.
- **AC-5.9** — the Secret carries a ring, so an operator can add a new key alongside the old one.

## Dependencies

- Slice 02 landed: the application already knows how to accept, report and prefer a supplied key.
- Slice 03 landed: the re-encryption action the Kubernetes rotation flow ends with already exists.
- Tenant Zero available as the proving ground.
- Chart publish path with the maintainer token (the release ruleset does not accept the default token).

## Dogfood moment

Same day: confirm first that an install with no encryption values **refuses to render**, naming both
keys. Then install into Tenant Zero with `encryption.existingSecret`, save a Connection, run three
`helm upgrade`s, and confirm the Connection still syncs after each one. Then walk the documented
Kubernetes rotation end to end — add a second key to the Secret, roll the pod, re-encrypt, drop the
old key — and confirm nothing was re-entered.

## Pre-slice SPIKE

None. The risk is a known Helm behaviour and the unit test is the cheap answer to it.

## Verdict

_To be recorded at slice close: confirmed / disproved._
