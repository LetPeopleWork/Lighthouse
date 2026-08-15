# ADR-153: Kubernetes key custody is a mounted Secret the operator owns, hot-reloaded by polling; the chart never generates a key

**Status**: **Accepted** — the fork this ADR carried (retire the chart's generate-if-absent acceptance
criterion, and with it the upgrade-regeneration criterion that only existed to guard it) was confirmed
by the maintainer on 2026-08-14, together with the other six DESIGN forks. Nothing in this ADR is now
provisional; the retired criteria are marked as such in the feature delta.
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slice 05 / Story #5780)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D5, D6 · AC-5.1, AC-5.4, AC-5.5, AC-5.7, AC-5.9, AC-5.10, AC-5.11 · **Retires AC-5.2 and AC-5.3** (confirmed by the maintainer 2026-08-14)

---

## Context

`chart/templates/secret.yaml` renders the database connection string and the OIDC client secret and
stops. Every Kubernetes install, including every tenant on the platform, inherits the published default
key. Slice 05 fixes that.

DISCUSS asked for three things: an `existingSecret` hook, generate-if-absent, and upgrade idempotence —
with AC-5.3 calling the regeneration failure "the single most likely way to ship this slice broken".
That instinct is right and the mitigation it proposes is not sufficient.

**The standard Helm answer to generate-once is `lookup`, and `lookup` is unavailable in the deployment
model the platform actually uses.** `lookup` returns an empty map whenever there is no cluster
connection — which is every `helm template` render, every `--dry-run`, and **every ArgoCD sync**, because
ArgoCD renders manifests with `helm template` and never with `helm install`. The platform's tenants are
ArgoCD `Application`s. A `lookup`-guarded `randAlphaNum` in this chart would therefore regenerate the
encryption key on **every ArgoCD sync**, orphaning a tenant's entire credential set — the exact failure
AC-5.3 names, arriving through the door the mitigation leaves open.

There is a second, independent problem with the generate-in-chart shape. There is no reliable way for a
template to tell "no Secret exists yet" from "I cannot see the cluster". `.Release.IsUpgrade` is false
under `helm template`, so it cannot discriminate either. A chart that generates cannot be made safe by
adding conditions; it can only be made safe by not generating.

Finally, AC-5.5 contradicts itself in one sentence: the key must "never appear ... in a pod's
environment dump" *and* must "reach the container the same way the database password already does" — and
the database password reaches it as an environment variable from a `secretKeyRef`, which is readable from
`/proc/<pid>/environ` and from any process dump. The first clause is the security requirement; the second
is a convenience analogy. They cannot both be honoured.

## Decision

**1. The chart never generates an encryption key.** `helm install` with no `encryption.*` value **fails
at render** with a message naming the three ways forward:

```
encryption.key or encryption.existingSecret is required.
  Generate one:  --set encryption.key=$(openssl rand -base64 32)
  Or point at a Secret your own store owns:  --set encryption.existingSecret=my-lighthouse-encryption
```

**This is the chart's own established precedent, not a new rule.** ADR-082 already requires
`postgresql.auth.password` explicitly, with no default, for the same reason: a security-relevant value
that a chart invents is a value nobody owns. The encryption key is more consequential than the database
password, so it gets the same treatment or a stricter one — not a weaker one.

You cannot regenerate what you never generate. **AC-5.3 becomes vacuous and AC-5.2 is retired**, and the
class of failure the slice was most afraid of stops existing rather than being tested for. This was the
fork carried to the maintainer, confirmed on 2026-08-14; the alternative that keeps AC-5.2 is designed
below and was not taken.

**2. The key ring reaches the container as a mounted file, not an environment variable.** The Secret's
`Encryption__Keys` entry is projected as a volume at `/etc/lighthouse/encryption`, and the pod gets
`Encryption__KeysFile=/etc/lighthouse/encryption/keys`. This honours AC-5.5's binding first clause and
buys the reload capability in point 4, which the environment form structurally cannot have. It is a
deliberate divergence from how the database password travels, and the divergence is the point.

**3. The Secret carries a ring (AC-5.9).** One line, ADR-148's canonical form, first entry active:

```yaml
stringData:
  keys: |
    k-2026-08-14-01:BASE64…,k-legacy:BASE64…
```

Authoring this as one line is why ADR-148 rejected indexed binding. An External Secrets `template` or an
OpenBao `ExternalSecret` emits it with one `{{ .active }}`/`{{ .retired }}` interpolation.

**4. A key added to the Secret is noticed without a restart, by polling the file's content.** A
`KeyRingFileWatcher` hosted service re-reads the mounted path every 30 seconds and compares a hash of its
content. On change it parses, validates, and calls `IEncryptionKeyRingHolder.Replace` (ADR-150), logging
`encryption.keyring.reloaded` with the key ids and never any material.

Polling rather than `FileSystemWatcher` on purpose: the kubelet updates a projected Secret by writing a
new `..data` directory and **swapping a symlink**, so an inotify watch registered on the file path
follows the old inode and never fires. Watching the directory can be made to work and is fragile across
CRI implementations; a 30-second content poll is four lines and cannot be defeated by a substrate detail.

Two guards on the reload, both fail-safe-old:

- A reload that does not parse, or that yields an empty ring, is **rejected**: the previous ring stays in
  force, an Error is logged, and the panel says the file could not be read. The previous ring is
  known-good, so keeping it is strictly better than any alternative.
- A reload that *removes* a key the ring currently holds is **applied** — the operator owns custody and
  AC-5.11 requires the degenerate case to surface — but at Warning, naming the ids that disappeared, and
  the readability check will report the affected secrets as unreadable rather than the instance looping
  on tracker rejections.

**5. `encryption.existingSecret` points at a Secret an external store owns**, and the chart renders no
key of its own (AC-5.1). An explicit `encryption.key` is rendered into the release's Secret and reported
as configuration-supplied (AC-5.4).

**6. The standalone single-container product is byte-unchanged** (AC-5.7). Nothing in this ADR reaches
it: `Encryption:KeysFile` is unset there, so the resolver never looks for a mounted file.

**7. The documented Kubernetes rotation** (AC-5.10) is four operator actions against their own secret
store: add the new key as the *first* entry alongside the old → wait for the pod to log the reload, or
roll it → trigger **Re-encrypt onto the active key** from the panel → drop the old key. Lighthouse never
writes to the Secret and needs no permission to.

## Alternatives Considered

**`lookup`-guarded generation with a `fail` when `lookup` comes back empty on a non-fresh release.** The
answer that keeps AC-5.2 alive, and the fallback if the maintainer wants generate-if-absent kept.
**Rejected as the default** because the guard cannot distinguish "fresh install" from "no cluster
visible", so the only safe formulation of it fails *every* ArgoCD and `helm template` render — which is
a chart that does not work under GitOps, dressed up as a chart that generates. If the maintainer chooses
this path, the chart README must state that generation is unsupported under ArgoCD and
`helm template`, and Tenant Zero must use `existingSecret` regardless.

**A `pre-install` hook Job that creates the Secret when absent.** Works under ArgoCD, generates exactly
once, and hooks are skipped on upgrade. **Rejected**: it needs a ServiceAccount with `create` on Secrets
in the namespace, which is the permission D6 spent its whole argument avoiding, and a hook that fails
leaves an install half-applied with no key at all.

**Let the application generate into a PVC.** Would give Kubernetes the same first-boot experience as
Docker. **Rejected**: an RWO PVC on the API workload contradicts the multi-replica design, RWX is not
generally available, and it puts the key on the least durable storage in the deployment while the
cluster already has a purpose-built store for exactly this.

**Environment variable from `secretKeyRef`, matching the database password.** Simplest, consistent, and
what AC-5.5's second clause asks for. **Rejected**: it is readable in a process dump, so it fails the
same AC's first clause, and it makes point 4 impossible — an environment variable cannot change under a
running process, so "the operator added a key and the panel can see it" would require a pod roll every
time.

**`FileSystemWatcher` on the mounted path.** Immediate rather than up-to-30-seconds. **Rejected**: the
kubelet's symlink swap defeats a watch on the file, and the directory-watch workaround varies by
container runtime. Thirty seconds is well inside the operator's own round trip of editing a Secret and
looking at a panel.

## Consequences

**Positive**

- The catastrophic failure — an upgrade or a sync regenerating a tenant's key — is removed by
  construction rather than guarded against, and it is removed under GitOps too, which a `lookup` guard
  never could have been.
- The key never enters an environment variable, a ConfigMap, or a rendered values file, so AC-5.5's real
  requirement holds on every path.
- A Kubernetes rotation is four operator actions with a visible confirmation between each, and Lighthouse
  holds no write permission on any Secret at any point.
- Every tenant's key is owned by the tenant's own secret store, which is what makes one leaked backup one
  tenant's problem.

**Negative / accepted**

- **`helm install` gains a required value.** A self-hoster's first command grows one flag. Mitigated by
  the failure message carrying a copy-pasteable `openssl rand`, and justified by the ADR-082 precedent
  the chart already sets for a less consequential secret.
- **AC-5.2 is retired** and AC-5.3 with it. That is an upstream change, so it was carried to the
  maintainer rather than applied silently, and confirmed on 2026-08-14.
- Up to 30 seconds between an operator's Secret edit and the panel showing the new key id. Documented,
  and shorter than the time to notice the panel.
- Encryption travels differently from the database password, so the chart has two Secret-consumption
  idioms. The chart README must say why in one sentence, or someone will "align" them back.

## Earned Trust — the substrate lies, and the probe exercises the lie

| Substrate lie | Probe |
|---|---|
| "`helm upgrade` with unchanged values is idempotent for a generated value" — it is not, and ArgoCD never even reaches `upgrade` | Chart unit test: render the templates with **no cluster** (`helm template`, which is what ArgoCD does) → the render **fails** rather than emitting a key. The test that would have caught the ArgoCD path |
| "A projected Secret update fires inotify" — the kubelet swaps a symlink, so it does not | Integration test: replace the file behind a symlink swap → the poller still observes the change within one interval |
| "Env vars are not visible" — they are, in `/proc/<pid>/environ` | Test on the rendered Deployment: no container env var name matches `Encryption__Key*` except `Encryption__KeysFile`, whose value is a path |
| "An external store's next sync preserves what we wrote" — it overwrites | Nothing to probe: Lighthouse never writes. Enforced structurally — no Kubernetes client is referenced anywhere in the backend |
| A malformed Secret takes the instance down | Test: replace the mounted file with garbage → the previous ring stays in force, an Error is logged, every Connection still syncs |
| An operator drops the old key before re-encrypting | Test (AC-5.11): remove the retired entry with rows still on it → those secrets report unreadable on the panel, and no tracker call is attempted with an unreadable credential |

## Cross-reference

- [ADR-148](./adr-148-key-ring-canonical-form-and-retired-default.md) — the one-line ring form the
  Secret carries, and why it is not indexed configuration.
- [ADR-150](./adr-150-key-ring-resolved-at-builder-time-into-a-singleton.md) — the holder the watcher
  swaps.
- [ADR-152](./adr-152-custody-mode-and-the-encryption-admin-surface.md) — why this mode gets the
  re-encrypt-only panel.
- [ADR-082](./adr-082-chart-required-values-fail-fast.md) — the chart's existing precedent for
  refusing to invent a security-relevant value, and the failure shape this reuses verbatim.
- [ADR-087](./adr-087-secrets-eso-openbao.md) — the External Secrets / OpenBao path the
  `existingSecret` hook is built for.
- `chart/templates/secret.yaml`, `chart/values.yaml`, `chart/values.schema.json` — the surfaces slice 05
  extends.
