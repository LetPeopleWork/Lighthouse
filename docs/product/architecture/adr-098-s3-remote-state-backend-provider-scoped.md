# ADR-098: OpenTofu Substrate State on an S3 Remote Backend, Provider-Scoped via Partial Config

**Status**: **PROPOSED** (2026-07-02, Apex — DEVOPS wave, awaiting Benjamin)
**Date**: 2026-07-02
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5374 / US-10, Track A) — closes the remote-state gap named in ADR-088 and unblocks multi-machine / multi-operator infra operation
**Decider**: Benjamin (product owner / operator) + Apex (Platform Architect)
**Relationship to prior work**: supersedes the "no backend block / known gap" note in **ADR-088** (substrate boundary); reuses the project-scoped EC2/S3 credential pattern from **ADR-091** (slice-10 off-cluster backups) and honours **ADR-087** / CC-3 (no plaintext secret in git)

---

## Context

The OpenTofu substrate (`infra/substrate/`) records provisioned cloud resources in a **state file** that today lives **only on the one machine that first ran `tofu apply`** (`*.tfstate` is gitignored, so it is not shared through the repo). A freshly-cloned second machine can edit git and see the cluster, but cannot safely `tofu plan/apply` (no shared state) — this is the single blocker for multi-machine infra operation (US-10 / #5374).

Constraints that shape the backend:

- **Infomaniak Object Storage is S3-compatible but is NOT AWS** — no DynamoDB (the classic Terraform lock table), no STS, no IMDS, and (observed at migration) **no S3 conditional PUTs** either. The backend config must skip all AWS-specific validation, and **no state-locking mechanism is available at all** (see Decision 5).
- **CC-3 (ADR-087): no plaintext secret in git.** The backend wiring must commit cleanly with credentials supplied only from the environment.
- **RD-1 (2026-07-02): multi-provider stays a document-path-only readiness bar** — Hetzner / Oracle Cloud may be added later. The backend must not require a redesign to add a second provider.
- **Key insight**: the state backend is **orthogonal to the compute provider**. S3 is a *protocol* (Infomaniak, Hetzner, Oracle object stores all speak it) and state is tiny metadata (~KB), so the scaling axis is **organization** (isolation, independence, per-provider credential), never storage capacity.

## Decision

1. **Separate bucket, provider-scoped key.** State lives in a **dedicated** bucket `lighthouse-tfstate` (in `dc4-a`, served by the `s3.pub2.infomaniak.cloud` endpoint — `pub1` fronts `dc3-a`), key **`substrate/infomaniak-dc4/terraform.tfstate`** — distinct from the slice-10 `lighthouse-backups` bucket. Blast radius (the state object can `tofu destroy` every tenant) and lifecycle differ, so they do not share a bucket. The key encodes provider + region/cluster, so a second provider is a *new key*, never a collision. (Object versioning is *intended* as a state-clobber safety net but is **not yet enabled** — Infomaniak Swift/S3 versioning needs separate tooling; deferred, tracked as follow-up.)

2. **Partial backend config.** `backend.tf` commits an **empty** `backend "s3" {}` block; every value is supplied at init via `-backend-config=backends/<provider>.s3.tfbackend`. Per-provider config files (`backends/infomaniak.s3.tfbackend`) are **committed** — they hold no credentials (`*.tfbackend` is not gitignored). The substrate module stays **byte-identical** across providers.

3. **State bucket created out-of-band, unmanaged by tofu.** The bucket that holds the state cannot be created by the tofu run that stores its state there (self-referential corruption). It is created once with `mc mb ik/lighthouse-tfstate` + `mc version enable` — the standard OpenTofu bootstrap pattern, matching the project's existing "one irreducible out-of-band step" (slice-10 seeds OpenBao out-of-band). A mini bootstrap-module was rejected: it merely moves the local-state problem down one level.

4. **Backend credential = a dedicated project-scoped EC2/S3 keypair** minted from the OpenStack application credential (`openstack ec2 credentials create`) → `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` env. It is **separate** from the slice-10 backup keypair so a leak of one does not expose the other. **Root-of-trust note**: this credential *cannot* come from OpenBao — OpenBao lives in the cluster whose kubeconfig lives in the state behind this very credential (circular). It is derived from the OpenStack app-cred (`clouds.yaml`), the root already documented in the operator-bootstrap runbook.

5. **No automatic state locking** — an accepted limitation, not a choice. Infomaniak Object Storage implements neither S3 conditional PUTs (OpenTofu's `use_lockfile` fails with `501 NotImplemented`, observed at migration) nor DynamoDB, so no backend lock is available. Operators **coordinate applies manually** (social locking). Acceptable at 1–2 operators / ~weekly change cadence; the `required_version` floor stays `>= 1.8.0` (no version-gated backend feature is in use). Revisit if a locking-capable object store, or a provider whose S3 supports conditional PUTs, is adopted.

## Consequences

- Any operator machine can `plan`/`apply` against shared state — the multi-machine unblocker (US-10 done=observable: 2nd machine reaches `tofu plan` = no-op + `kubectl get nodes` = live). The state is **not locked** (Infomaniak limitation), so operators must coordinate applies; safe at the platform's 1–2 operator scale.
- The S3 credential is a genuine bootstrap root-of-trust that must be minted from the OpenStack app-cred, not stored in the in-cluster secret store.
- The deferred multi-provider path (RD-1) becomes an **"add a file"**: provider #2 = drop `backends/hetzner.s3.tfbackend` + mint that provider's own S3 cred + a new key prefix + `tofu init -reconfigure` — no module change.
- **Independence trade-off (documented, deferred):** all provider state co-locates in the Infomaniak `lighthouse-tfstate` bucket initially (simplest; isolated by key prefix). For true blast-radius independence each provider's state can later move into *that provider's own* object store — its `backends/<provider>.s3.tfbackend` already isolates endpoint + bucket, so the move is config-only. Flagged as a future least-privilege / independence split, not required for a single-provider platform today.
- Optional CI drift-detection `tofu plan` (needs `AWS_*` GitHub secrets) is out of scope here.

## Alternatives rejected

- **Inline single-provider `backend.tf`** (bucket/key/endpoint hard-coded) — a mini-rewrite when provider #2 lands; partial config costs nothing extra now.
- **Co-mingled state in `lighthouse-backups`** — mixes the destroy-everything state object with churning tenant dumps under one lifecycle policy.
- **Single flat key** `substrate/terraform.tfstate` — collides the moment a second cluster/provider exists.
- **Mini bootstrap module** for the state bucket — reintroduces the local-only-state problem it is meant to kill.
- **DynamoDB / `use_lockfile` locking** — both unavailable on Infomaniak (no DynamoDB; conditional PUTs return `501`). Running unlocked is therefore forced, not chosen; mitigated by manual coordination at 1–2 operator scale (Decision 5).
- **Committed/baked credentials** — violates CC-3; env-supplied creds keep the config files secret-free.
