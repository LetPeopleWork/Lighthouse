# DEVOPS Wave Decisions — Track A / US-10 / #5374 (remote state + operator bootstrap)

> **Wave**: DEVOPS. **Mode**: DESIGN-ONLY (PROPOSE). **Date**: 2026-07-02. **Architect**: Apex.
> **Scope**: the substrate state-management plane (`infra/substrate/`) — migrate OpenTofu state
> local → Infomaniak S3 remote backend, and complete the operator-bootstrap runbook (A2, folded in).
> **NO IRREVERSIBLE ACTION** in this step: no `init -migrate-state`, no `apply`, no live-account touch.
> The `.tf` edits + migration execute in a follow-up, gated on explicit user confirmation.
> **Authoritative upstream**: ADR-088 (substrate boundary, names the remote-state gap), ADR-087
> (ESO+OpenBao / CC-3 no-secret-in-git), ADR-091 (slice-10 off-cluster backup bucket + EC2 cred).
> **New ADR proposed**: **ADR-098** (S3 remote state backend, provider-scoped via partial config) — draft §7 below.
> **User decision (2026-07-02)**: bake the **multi-provider** state structure in NOW — partial backend
> config + provider-scoped key. Do NOT ship the single-bucket/single-key form.

---

## 1. Decision summary

| # | Decision | Chosen option | Rejected | Ref |
|---|----------|---------------|----------|-----|
| A-D1 | State bucket topology | **SEPARATE bucket `lighthouse-tfstate`**, **provider-scoped key `substrate/infomaniak-dc4/terraform.tfstate`**, versioning ON | Co-mingle in `lighthouse-backups`; single flat key | ADR-098 |
| A-D2 | State-bucket creation (chicken-egg) | **Out-of-band `mc mb` once**, bucket UNMANAGED by tofu | Mini bootstrap-module (2nd local state) | ADR-098 |
| A-D3 | Backend credential | **Project-scoped EC2/S3 keypair** from the OpenStack app-cred → `AWS_*` env | Baked/committed creds; per-op unique root secret | ADR-087/098 |
| A-D4 | State locking | **`use_lockfile = true`** (S3 conditional-write); bump `required_version` to `>= 1.10.0` | `dynamodb_table` (Infomaniak has no DynamoDB); no-lock + social lock | ADR-098 |
| A-D5 | Backend wiring | **EMPTY `backend "s3" {}`** in `backend.tf` + **committed per-provider `backends/<provider>.s3.tfbackend`** partial-config files | Inline bucket/key/endpoint in `backend.tf` (single-provider) | ADR-098 |
| A-D6 | Migration sequencing | **backup-first → mc mb → init -migrate-state -backend-config=… → plan=no-op gate** | apply-then-verify | §4 |
| A-D7 | Rollback | Retain local `.tfstate` + explicit `.bak`; backend-removal re-init; bucket versioning | none | §5 |
| A-D8 | Multi-provider state scaling | **Provider-scoped key + per-provider partial-config file** — provider #2 = drop a `backends/hetzner.s3.tfbackend` + new key prefix; substrate module byte-identical | Redesign backend per provider; single flat key (collision) | ADR-098 |

---

## 2. State-bucket topology (A-D1) — SEPARATE from tenant backups

`object-storage.tf` (slice-10, ADR-091) already provisions `lighthouse-backups` + an
`openstack_identity_ec2_credential_v3.backup`, and its header calls that EC2 cred "the SAME credential
#5374 uses for the remote state backend." **Reconciled**: the *kind* of credential is shared
(a project-scoped EC2/S3 keypair), but the **bucket is separate** and the state does **not** live with
tenant DB dumps. Justification:

- **Blast radius**: `lighthouse-tfstate` holds the one object that can `tofu destroy` every tenant.
  Co-mingling it with tenant dumps means a single bucket lifecycle rule, `force_destroy`, or fat-finger
  risks both the backups AND the state that could rebuild them. Different failure classes → different buckets.
- **Lifecycle divergence**: backups want per-tenant prefixes + expiry/retention (many objects, churn);
  state wants **exactly one key, never expire, versioning ON** (recover a clobbered state). Opposite policies.
- **Access pattern**: backups written by the in-cluster CronJob; state written by the operator's tofu on a
  workstation. Different writers, ideally different least-privilege creds (see A-D3 note).

**Layout**: bucket `lighthouse-tfstate`, **provider/cluster-scoped key
`substrate/infomaniak-dc4/terraform.tfstate`** (was a flat `substrate/terraform.tfstate`). The key
encodes provider + region/cluster, so a 2nd provider (RD-1 / S12) is a *new key*, never a collision —
see §6b (multi-provider state scaling).

## 3. Chicken-and-egg (A-D2) — the crux

The bucket that HOLDS tofu state cannot be created by the tofu run that stores its state there — the
resource would be tracked in the very state it hosts, so a rename/destroy self-corrupts. Two options:

**Option A — out-of-band `mc mb` (CHOSEN).** The operator creates the bucket once with the MinIO client
already in the toolbelt, then enables versioning:
```bash
mc alias set ik https://s3.pub1.infomaniak.cloud "$AWS_ACCESS_KEY_ID" "$AWS_SECRET_ACCESS_KEY"
mc mb ik/lighthouse-tfstate          # idempotent-ish: tolerate "already exists"
mc version enable ik/lighthouse-tfstate
```
The bucket is **foundational infra, unmanaged by tofu** — the standard Terraform/OpenTofu bootstrap
pattern (a state backend is never managed by the config it backs). Matches the project's established
"one irreducible out-of-band step" pattern (slice-10 already seeds OpenBao out-of-band because OpenBao
lives inside the cluster tofu builds).

**Option B — mini bootstrap module (`infra/tfstate-bootstrap/`) — REJECTED.** A tiny separate tofu root
with LOCAL state that creates only the bucket. Rejected because it merely **moves the chicken-egg down
one level** (the bootstrap's own state is local, so a fresh machine still can't reproduce it), adds a
whole root module + provider wiring for one bucket, and reintroduces the exact "local-only state"
problem US-10 exists to kill.

## 4. Migration procedure (A-D6) — run on `operator-workstation-primary` ONLY

> Single-actor, one-time. NOT executed in this design step — proposed sequence for the follow-up.

```
0. Pre-flight
   tofu version                 # confirm >= 1.10 (this machine: v1.12.3 ✓ — needed for use_lockfile)
   git status                   # clean working tree in infra/substrate/
   ls terraform.tfstate         # the authoritative local state exists

1. Backup-first (rollback safety net)
   cp terraform.tfstate terraform.tfstate.pre-s3-migration.bak    # gitignored; on top of tofu's own .backup

2. Obtain the S3 credential (reuse slice-10 EC2 cred, or mint one)
   tofu output -raw backups_s3_access_key   # → AWS_ACCESS_KEY_ID
   tofu output -raw backups_s3_secret_key   # → AWS_SECRET_ACCESS_KEY
   export AWS_ACCESS_KEY_ID=... AWS_SECRET_ACCESS_KEY=...

3. Create the state bucket out-of-band + versioning (§3 Option A)
   mc alias set ik https://s3.pub1.infomaniak.cloud "$AWS_ACCESS_KEY_ID" "$AWS_SECRET_ACCESS_KEY"
   mc mb ik/lighthouse-tfstate
   mc version enable ik/lighthouse-tfstate

4. Add the committed empty backend.tf + backends/infomaniak.s3.tfbackend (§6) to infra/substrate/
   + bump versions.tf floor to >= 1.10.0

5. Migrate  ── THE irreversible step, gated on user confirmation ──
   tofu init -migrate-state -backend-config=backends/infomaniak.s3.tfbackend
                                 # empty backend "s3" {} + partial config file → tofu detects
                                 # local→s3, prompts to copy state up → answer "yes"

6. Correctness gate (AC-1)
   tofu plan                     # MUST print "No changes" → migrated state matches reality
                                 # (a lock file is written/cleared during plan → proves use_lockfile works)

7. Retire the local copy (keep the .bak until the 2nd machine verifies)
   rm terraform.tfstate          # remote is now authoritative; DO NOT delete .pre-s3-migration.bak yet

8. Commit + push
   git add backend.tf versions.tf ; git commit -m "feat(substrate): S3 remote state backend (#5374)" ; git push
```

**2nd-machine verification (AC done=observable)** — on `operator-workstation-fresh-clone`, following the
runbook only: `git clone` → install toolbelt → obtain creds (§7 table) → `export AWS_*` →
`tofu init -backend-config=backends/infomaniak.s3.tfbackend` (downloads state, NO `-migrate-state`) →
**`tofu plan` = no-op** → `tofu output -raw kubeconfig` → `kubectl get nodes` = live cluster.

## 5. Rollback (A-D7) — designed first, per rollback-first principle

| Failure point | Rollback |
|---|---|
| `init -migrate-state` errors / prompts wrong | Abort; state never left local. `.tfstate` + `.pre-s3-migration.bak` intact. |
| Post-migrate `tofu plan` shows drift (state ≠ reality) | Do NOT apply. Remove `backend.tf`, `tofu init -migrate-state` back to local (or restore `.bak`), investigate. |
| Remote state object clobbered/corrupted later | Bucket versioning ON → `mc cp --version-id <v> ik/lighthouse-tfstate/substrate/infomaniak-dc4/terraform.tfstate <local>` then re-push. **This is why A-D1 mandates versioning.** |
| Need to abandon the S3 backend entirely | Delete `backend.tf`, `tofu init -migrate-state` → state returns local. Documented backend-removal path. |
| Stale lock blocks apply | `tofu force-unlock <lock-id>` (rare; documented). |

## 6. Proposed backend wiring (A-D5) — EMPTY block + per-provider partial config

**Partial backend config.** `backend.tf` commits an **empty** `backend "s3" {}` block; every value is
supplied at init time via `-backend-config=backends/<provider>.s3.tfbackend`. This is what makes the
backend provider-agnostic: the substrate module stays byte-identical across providers, and adding a
provider is "drop a new `.tfbackend` file" (§6b). Both files are committed and hold **no secrets** —
creds come from `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` env. `.gitignore` excludes
`*.tfvars`/`*.tfstate*`/`clouds.yaml`/`kubeconfig`/`*.key`/`*.pem` — **not** `*.tfbackend`, so the
config files commit cleanly.

`infra/substrate/backend.tf` (committed, empty block):
```hcl
# infra/substrate/backend.tf — OpenTofu remote state, PARTIAL config (#5374, ADR-098).
# The block is intentionally EMPTY: bucket/key/endpoint/flags come from a per-provider file at init:
#   tofu init -backend-config=backends/infomaniak.s3.tfbackend
# This keeps the substrate module provider-agnostic — a 2nd provider is a new backends/*.tfbackend
# file + its own S3 credential + a new key prefix, with ZERO change to this module. Holds no secrets.
terraform {
  backend "s3" {}
}
```

`infra/substrate/backends/infomaniak.s3.tfbackend` (committed — NO secrets):
```hcl
# Infomaniak Object Storage (S3-compatible) backend config for the substrate state. #5374 / ADR-098.
# Committed (holds no credentials — AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY come from the env, mapped
# from a project-scoped EC2/S3 credential; see operator-bootstrap.md). The bucket is created out-of-band
# (`mc mb ik/lighthouse-tfstate`) — a backend cannot create the bucket that holds its own state.
bucket = "lighthouse-tfstate"
key    = "substrate/infomaniak-dc4/terraform.tfstate"   # provider/region-scoped → provider #2 = new key
region = "us-east-1"                                     # dummy; Infomaniak ignores it, AWS SDK requires a value

endpoints = {
  s3 = "https://s3.pub1.infomaniak.cloud"
}

use_path_style              = true   # path-style (bucket in path), not vhost-style
skip_credentials_validation = true   # no AWS STS
skip_requesting_account_id  = true   # no AWS account lookup
skip_metadata_api_check     = true   # no EC2 IMDS
skip_region_validation      = true   # region is a dummy

use_lockfile = true                  # S3-native conditional-write lock (OpenTofu >= 1.10); no DynamoDB
encrypt      = true                  # SSE at rest
```
Companion change: `versions.tf` `required_version = ">= 1.8.0"` → **`">= 1.10.0"`** (gates `use_lockfile`).

## 6b. Multi-provider state scaling (A-D8) — baked in NOW, not a future redesign

**Rationale.** The state backend is **orthogonal to the compute provider**. S3 is a *protocol*, not a
vendor — Infomaniak, Hetzner, and Oracle Cloud object stores all speak it, and tofu state is tiny
metadata (~KB), so the scaling axis is **organization** (isolation, independence, per-provider
credential), NOT storage capacity. Organization is solved by **key-prefix + partial config**, so the
multi-provider structure costs nothing to bake in now and turns RD-1 (DOCUMENT-PATH-ONLY) into a real
"add a file" path.

**Adding provider #2 (e.g. Hetzner) later** =
1. drop `infra/substrate/backends/hetzner.s3.tfbackend` (its own bucket/endpoint, key
   `substrate/hetzner-fsn1/terraform.tfstate`, same `use_lockfile`/`skip_*` shape);
2. mint that provider's own S3 credential → its `AWS_*`;
3. `tofu init -reconfigure -backend-config=backends/hetzner.s3.tfbackend`.

The `backend "s3" {}` block and the whole substrate module stay **byte-identical**. No redesign.

**Independence trade-off (documented decision).** Initially ALL provider state co-locates in the
Infomaniak `lighthouse-tfstate` bucket (simplest — one bucket to bootstrap, isolated by key prefix).
For true blast-radius independence, each provider's state can later move into *that provider's own*
object store — its `backends/<provider>.s3.tfbackend` already isolates the endpoint+bucket, so the move
is config-only, no module change. **Flagged as a future least-privilege / independence split, not
required now** (a single-provider platform gains nothing from cross-provider state buckets today).

## 7. Operator-bootstrap runbook — EXTEND `docs/operator-bootstrap.md` (do NOT replace)

The existing runbook is well-structured; it already carries the artifacts table, the toolbelt, the
7-step new-machine flow, and the ⚠️ "state is still LOCAL / #5374" caveat. #5374 completes it with:

**(a) One new artifacts-table row (AC-2)** — the S3 backend credential:

| Artifact | Why not in git | Documented source |
|---|---|---|
| **S3 state-backend creds** (`AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`) | Live credential (CC-3). | **Derived, not a new root secret.** Mint a project-scoped EC2/S3 keypair from your OpenStack application credential: `openstack ec2 credentials create` (or reuse the slice-10 pair from your out-of-band store), then `export AWS_ACCESS_KEY_ID=<access> AWS_SECRET_ACCESS_KEY=<secret>`. Keystone EC2 creds are **project-scoped**, so any operator's keypair reads the same shared `lighthouse-tfstate` bucket. **Root-of-trust note**: this creds CANNOT come from OpenBao — OpenBao lives in the cluster whose kubeconfig lives in the state behind these very creds. It must come from the OpenStack app-cred (`clouds.yaml`), the same root already in this table. |

**(b) Replace the ⚠️ CRITICAL caveat** — flip "state is still LOCAL / do not apply from a 2nd machine"
to the resolved flow: step (4) becomes
`tofu init -backend-config=backends/infomaniak.s3.tfbackend`, which initialises against the **committed
S3 backend (empty block + partial-config file)** and downloads shared state; any operator machine can
plan/apply safely (serialized by `use_lockfile`). Update the "what works from any machine" table: the
last row `tofu plan/apply/destroy` moves from ❌ "needs the state-holding machine" to ✅ "any machine
(shared S3 state + lock)".

**(c) Add a short "How the state moved" section** pointing to ADR-098 + `substrate-opentofu.md`
"Where the state lives" (which must also flip from "no backend block / known gap" to "S3 backend,
see ADR-098"). Add `openstack` CLI to the toolbelt table (needed to mint the EC2 cred on a fresh machine).

**(d) Toolbelt**: `mc` already listed (used for `mc mb` + versioning); add **`openstack` CLI** (EC2-cred mint).

## 8. AC-3 audit — local config that could safely live in git

| Local/gitignored artifact | Secret? | Verdict |
|---|---|---|
| `backend.tf` (empty block) + `backends/infomaniak.s3.tfbackend` (was implicitly "not in git" — didn't exist) | No | **MOVED INTO GIT** — the AC-3 win. Backend wiring + per-provider partial config is config, holds no creds. `*.tfbackend` is not gitignored. |
| `terraform.tfvars` (2 numeric IDs + region/flavor) | No, but account-specific | Reproducible from committed `terraform.tfvars.example` + Infomaniak console. Kept local (blanket `*.tfvars`); IDs are non-secret and MAY be committed if the operator prefers zero-fill bootstrap — operator choice, flagged not forced. |
| `terraform.tfstate` | **Yes** (embeds kubeconfig, EC2 secret) | Correctly gitignored → now in S3. |
| `clouds.yaml`, `kubeconfig`, OpenBao unseal keys, ArgoCD deploy key, `*.key`/`*.pem` | **Yes** | Correctly gitignored; each has a documented regeneration source (AC-2 table). |

**Audit conclusion**: exactly one piece of config was local-only-by-omission and safely git-able — the
backend configuration — and #5374 commits it. Everything else gitignored is a genuine secret with a
documented source. No secret is withheld from reproducibility; none is committed.

---

## ADR-098 (proposed) — "S3 remote state backend, provider-scoped via partial config"

> Draft for `docs/product/architecture/adr-098-s3-remote-state-backend-provider-scoped.md`. Canonical
> ADR file is a PUBLIC-repo architecture artifact; created at execution, not in this design-only step.

- **Status**: Proposed (2026-07-02) — supersedes the "no backend / known gap" note in ADR-088.
- **Context**: substrate tofu state is local-only on one machine; blocks multi-machine operation (US-10).
  Infomaniak Object Storage is S3-compatible but is NOT AWS — no DynamoDB, no STS, no IMDS. The platform
  is single-provider today but RD-1 keeps a multi-provider path open (Hetzner/Oracle) — the backend must
  not have to be redesigned to add one. State backend is **orthogonal** to compute provider (S3 is a
  protocol; state is tiny metadata) — the real axis is organization, not capacity.
- **Decision**:
  1. State on a **separate** bucket `lighthouse-tfstate`, **provider/region-scoped key**
     `substrate/infomaniak-dc4/terraform.tfstate`, versioning ON — distinct from the slice-10
     `lighthouse-backups` bucket (blast-radius + lifecycle).
  2. **Partial backend config**: empty `backend "s3" {}` committed in `backend.tf`; all values in
     committed per-provider `backends/<provider>.s3.tfbackend` files supplied at
     `tofu init -backend-config=…`. Substrate module stays byte-identical across providers; provider #2
     = drop a new `.tfbackend` + own S3 cred + new key prefix (no module change).
  3. State bucket created **out-of-band** (`mc mb`) and left **unmanaged by tofu** (bootstrap pattern);
     no self-referential bootstrap module.
  4. Backend auth = **project-scoped EC2/S3 keypair** minted from the OpenStack application credential →
     `AWS_*` env; `backend.tf` + `.tfbackend` committed, credential-free (`*.tfbackend` not gitignored).
  5. Locking = **`use_lockfile`** (S3 conditional-write); raise `required_version` to `>= 1.10.0`;
     DynamoDB rejected (unavailable on Infomaniak).
- **Consequences**: any operator machine can plan/apply against shared, locked state; the S3 credential
  is a true root-of-trust bootstrap secret (cannot live in OpenBao — circular); the deferred
  multi-provider path (RD-1) is now an "add a file", not a redesign. Independence trade-off documented:
  all state co-locates in the Infomaniak bucket initially; each provider's state can later move into its
  own object store (config-only, no module change) — a flagged future least-privilege/independence split.
  Optional CI drift-detection `tofu plan` (needs AWS_* GH secrets) is out of scope.
- **Alternatives rejected**: inline single-provider `backend.tf`; co-mingled bucket; single flat key
  (provider collision); mini bootstrap module; DynamoDB/no-lock locking; committed/baked creds.

---

## 9. LIVE EXECUTION + PROOF (2026-07-02) — deltas from the design-only proposal

Migration executed on the machine holding the live local state. `tofu init -migrate-state` →
**`tofu plan` = "No changes"** — AC-1 correctness gate PASSED, state now in S3. Four deltas from the
design-only §1–§8, all forced by live Infomaniak behaviour:

| Design assumption (§1–§8) | Live reality | Resolution |
|---|---|---|
| **A-D4 `use_lockfile = true`**, floor `>= 1.10.0` | Infomaniak S3 → `501 NotImplemented: Conditional object PUTs are not supported`; no DynamoDB either | **No backend locking available.** `use_lockfile = false`; floor reverted to `>= 1.8.0`. Operators coordinate applies manually (acceptable at 1–2 operators). ADR-098 Decision 5 rewritten. |
| Endpoint `s3.pub1.infomaniak.cloud` (§6 HCL) | `openstack catalog show object-store`: **`dc4-a` (cluster + state bucket) is served by `pub2`**; `pub1` fronts `dc3-a` → pub1 gave `404 NoSuchBucket` | `endpoints.s3 = https://s3.pub2.infomaniak.cloud`. |
| Bucket via **`mc mb`** + `mc version enable` (§3) | `mc` not installed on the operator machine | Bucket created with **`openstack container create lighthouse-tfstate`** (Swift container, served over the S3 endpoint). Versioning **deferred** (needs `mc`/S3 API; migration rollback net was the local `.pre-s3-migration.bak`). |
| Credential reuse-or-mint (A-D3) | user chose **dedicated** | Minted a **separate** project-scoped EC2/S3 keypair (`openstack ec2 credentials create`) — isolated from the backup keypair. Keystone EC2 creds are project-scoped, so "dedicated" = separate keypair, not narrower privilege; true least-privilege needs a separate OpenStack project (future). |

**Latent slice-10 observation (NOT changed here):** `lighthouse-backups` is also in `dc4-a` yet
`variables.tf` defaults `backups_s3_endpoint` to `pub1` — same mismatch. CronJob may override it, or it
is a latent bug; flagged for a slice-10 follow-up, untouched.

**Deferred follow-ups:** (1) enable object versioning on `lighthouse-tfstate` once `mc`/S3-API tooling is
available; (2) least-privilege state credential via a dedicated project; (3) verify/repair the slice-10
`backups_s3_endpoint` pub1/pub2 default.

**PROOF:** `tofu plan` → `No changes. Your infrastructure matches the configuration.` (state read from
the S3 backend, not local; local `terraform.tfstate` removed, `.pre-s3-migration.bak` retained).
