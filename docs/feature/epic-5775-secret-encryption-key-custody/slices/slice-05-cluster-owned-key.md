# Slice 05 — The cluster owns the key

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5780 · **Story**: US-05 · **Estimate**: ~5h
**Reference class**: `chart/templates/secret.yaml` — the database connection string and the OIDC client
secret already travel from values or an `existingSecret` into the pod without landing in a ConfigMap.
The encryption key follows the identical route; the only genuinely new behaviour is generate-if-absent.

## Goal

A Helm install gets a unique encryption key without anyone supplying one, an external secret store can
own that key instead, and an upgrade never takes it away.

## IN scope

- `encryption.existingSecret` — the deployment reads the key from a Secret that External Secrets
  Operator or OpenBao populates, and the chart renders no key of its own.
- Generate-if-absent — with nothing supplied, the chart creates a unique random key into its own Secret
  on first install.
- **Upgrade idempotence.** A `helm upgrade` with unchanged values must never regenerate a generated
  key. This is the whole risk of the slice and it gets its own chart unit test.
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

**Disproves** "Helm can own key generation" if a `helm upgrade` regenerates the key and orphans every
stored credential in the tenant's database. Helm templates re-render on every upgrade, and a random
function in a template is the standard way to produce exactly this failure — so the hypothesis is
aimed at a specific, likely, and catastrophic mistake rather than at a general worry.
**Confirms**, if it holds, that a self-hoster gets a unique key from `helm install` with no values
file, and that the platform can give each tenant a key its own secret store owns.

## Open question carried

**OQ-2 — narrowed.** "What happens when the external Secret is rotated underneath a running pod?" was
open while rotation was assumed to be one action. It is now the supported flow: both keys sit in the
Secret at once and re-encryption follows. What remains is the degenerate case — an operator who removes
the old key before re-encryption ran. The bar is that the affected secrets surface as unreadable rather
than the instance looping on work-tracking-system rejections.

## Acceptance criteria

AC-5.1 through AC-5.11 in `feature-delta.md`. The three that carry the slice:

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

Same day: install into Tenant Zero with no encryption values, save a Connection, run three
`helm upgrade`s, and confirm the Connection still syncs after each one. Then walk the documented
Kubernetes rotation end to end — add a second key to the Secret, roll the pod, re-encrypt, drop the
old key — and confirm nothing was re-entered.

## Pre-slice SPIKE

None. The risk is a known Helm behaviour and the unit test is the cheap answer to it.

## Verdict

_To be recorded at slice close: confirmed / disproved._
