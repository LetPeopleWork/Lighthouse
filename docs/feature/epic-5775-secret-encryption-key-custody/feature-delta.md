# Feature Delta — epic-5775-secret-encryption-key-custody

**ADO**: Epic #5775 "Secret Encryption: Unique Keys and Safe Rotation" (New, created 2026-08-14) ·
child Stories #5777 (slice 01), #5024 (slice 02, re-parented from Epic #5511 during this wave), #5778
(slice 03), #5779 (slice 04), #5780 (slice 05), #5781 (slice 06) — five created at wave close ·
related Bug #5776 "Encryption key override does not apply the configured key" (ships independently,
before the epic) · **Feature type**: cross-cutting (crypto + persistence + bootstrap +
chart + docs) · **Density**: lean · **DISCUSS run**: 2026-08-14

The epic starts from a lost evaluation. A prospect's team looked at Lighthouse, concluded "credentials
are stored in plain text", and picked something else. That conclusion is wrong about the mechanism and
close enough to right about the outcome to be worth taking seriously. Reading the code during this wave
turned up five things, and the first two are the reason this is an epic and not a hardening ticket.

1. **There is exactly one key, and it is published.** `appsettings.json:43-44` carries a literal
   32-byte AES key, committed to a public repository. Every install that has not overridden it — the
   downloaded exe, `docker run`, `helm install`, and every tenant on the platform — encrypts every
   token with the same key any reader of the repository already has. Encryption at rest with a public
   key is a filing cabinet with the key taped to it.
2. **The documented way to override it does not reach the code.** The docs say `--Encryption:Key` /
   `Encryption__Key`; `CryptoService.cs:12` reads `EncryptionSettings:EncryptionKey`. There is no
   alias anywhere in the repository. Operators who read the configuration page and did the right thing
   set a value nothing consumes, and nothing tells them. That is the population most deserving of the
   fix and the one least likely to know it needs one.
3. **A wrong key does not fail.** `CryptoService.Decrypt` catches `CryptographicException` and
   `FormatException` and returns the ciphertext as though it were the secret. So finding 2 is invisible
   by construction, and — more dangerous for what this epic wants to build — a re-encryption pass over
   the whole database would cheerfully write garbage over good secrets and report success.
4. **The ciphertext carries no identity.** AES-CBC, random IV prepended, no MAC and no key tag.
   Nothing in a stored blob says which key wrote it, and nothing proves it decrypted correctly rather
   than into plausible-looking bytes.
5. **The chart provisions no key at all.** `chart/templates/secret.yaml` writes the database connection
   string and the OIDC client secret and stops. Kubernetes installs, including the platform's tenants,
   inherit the published default.

What the audit did **not** find is worth stating with the same weight: API keys are PBKDF2-SHA256 with
a per-key salt and a constant-time comparison (`ApiKeyService.HashKey`), embed session secrets and
handshake nonces are hashed rather than stored, and the encrypted set is exactly the two things it
should be — connection options flagged `IsSecret` and OAuth access/refresh tokens. The storage design
is sound. The key custody is not.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ `docs/feature/epic-5775-secret-encryption-key-custody/discover/` (not found — no DISCOVER wave ran)
- ⊘ `docs/feature/epic-5775-secret-encryption-key-custody/diverge/` (not found — no DIVERGE wave ran)
- ✓ `docs/product/jobs.yaml` (schema_version 1, 92 jobs) — none covers secret custody, key rotation or
  encryption. Nearest neighbour is `job-saas-operator-isolate-tenant-secrets` (epic-5306 slice-04,
  about which store holds a tenant's secrets), which assumes the application-side encryption question
  is already answered. It is not; this epic answers it.
- ✓ `docs/product/journeys/` (41 journeys) — none touches encryption or key handling.
- ✓ `docs/product/personas/` (9 personas) — `platform-operator`, `config-admin` and
  `lighthouse-maintainer` are reused verbatim. No new persona needed.
- ✓ `docs/product/kpi-contracts.yaml` — the `measurement_scope` convention (per_instance /
  vendor_demo_only / opt_in_telemetry_required) is inherited by every KPI below. Lighthouse does not
  phone home and this epic does not change that.
- ⊘ `docs/product/vision.md`, `docs/project-brief.md`, `docs/stakeholders.yaml` (not found — product
  SSOT lives under `docs/product/` in this repo)
- ✓ `CLAUDE.md`, `docs/ci-learnings.md` — standing rules applied (expand-only migrations, quality
  gates, per-feature docs, no internal references in comments).
- ✓ **Code read during this wave**: `Services/Implementation/CryptoService.cs`,
  `Data/LighthouseAppContext.cs:565-609`, `Program.cs:389-486` and `:884-911`,
  `API/WorkTrackingSystemConnectionsController.cs:144-160`, `Services/Implementation/OAuth/OAuthService.cs`,
  the four `WorkTrackingConnectors/Auth/*Strategy.cs`, `Models/Auth/ApiKey.cs`,
  `Services/Implementation/Auth/ApiKeyService.cs`, `appsettings.json`, `chart/templates/secret.yaml`.
- ✓ **Docs read**: `docs/Installation/configuration.md:172-189`,
  `docs/compliance/cra-self-assessment.md:31-33`.
- ✓ **ADO** #5024, #5019, #2438, #5511.

No DISCOVER evidence exists to contradict, so no contradiction check was possible and none is claimed.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `platform-operator` | Primary, in both its flavours. The self-hoster who runs the exe or the container and should never have to read a page to get a unique key; and the platform operator who runs many tenants and needs custody to sit in the cluster's secret store. |
| `config-admin` | The person who would otherwise pay for a key change by re-entering every token in every Connection. Rotation exists so this cost goes away. |
| `lighthouse-maintainer` | Has to answer "is it secure?" to a prospect's security team, in writing, without hedging. Currently cannot, because the compliance self-assessment's secure-by-default claim does not survive finding 2. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-operator-unique-key-without-effort` | When I install Lighthouse, protect my stored credentials with a key that is mine alone, without my having to read a configuration page first. |
| `job-operator-rotate-key-without-recredentialing` | When my key may be exposed, move every stored secret onto a new key from inside Lighthouse, without asking a single team to re-enter a token. |
| `job-operator-know-the-key-is-actually-in-effect` | Tell me plainly which key this instance is using and whether every stored secret can be read with it, so I stop finding out from a failed sync. |
| `job-saas-operator-tenant-owned-encryption-key` | When I run many tenants, give each one a key its own cluster secret store owns, so one leaked backup is one tenant's problem. |
| `job-maintainer-answer-the-secret-storage-question` | When a prospect's security team asks how credentials are protected, let me give a specific, true answer about the shipped defaults. |

Full job stories, dimensions, four forces and opportunity scores are written to
`docs/product/jobs.yaml`.

### Opportunity scores

| Job | Importance | Satisfaction | Gap | Note |
|---|---|---|---|---|
| `job-operator-unique-key-without-effort` | 5 | 1 | **4** | The published default key. Highest leverage: fixing it retires the whole class of criticism. |
| `job-operator-rotate-key-without-recredentialing` | 5 | 1 | **4** | Today's documented answer is "reconfigure your work tracking systems" — an outage of every sync. |
| `job-saas-operator-tenant-owned-encryption-key` | 5 | 1 | **4** | The chart provisions nothing; every tenant shares the published key. Blocks enterprise procurement. |
| `job-operator-know-the-key-is-actually-in-effect` | 4 | 1 | **3** | Silent-by-design today: the decrypt fallback guarantees a wrong key is indistinguishable from a right one. |
| `job-maintainer-answer-the-secret-storage-question` | 4 | 2 | **2** | Partly satisfied — the docs describe the intent correctly, they just describe a mechanism that does not run. |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

| Surface | Location | State today |
|---|---|---|
| Key resolution | `CryptoService` ctor (`CryptoService.cs:10-20`) | Single key, read from `EncryptionSettings:EncryptionKey`, base64, must be 32 bytes; throws only if absent or wrong length. |
| Default key | `appsettings.json:43-44` | Literal key committed to the public repository. |
| Documented override | `docs/Installation/configuration.md:178-179` | `--Encryption:Key` / `Encryption__Key` — consumed by nothing. |
| Encrypt path | `LighthouseAppContext.EncryptSecrets` (`:581-609`) | On `SaveChanges`, encrypts `WorkTrackingSystemConnectionOption.Value` where `IsSecret`, and `OAuthCredential.AccessToken` / `RefreshToken` when added or modified. Also `WorkTrackingSystemConnectionsController:154`. |
| Decrypt path | `CryptoService.Decrypt` (`:47-84`) | Catches `CryptographicException` / `FormatException` and **returns the ciphertext** as the plaintext. |
| Consumers | `PatAuthStrategy`, `JiraCloudBasicAuthStrategy`, `LinearApiKeyAuthStrategy`, `ServiceNowBasicAuthStrategy`, `LinearWorkTrackingConnector:633`, `OAuthService` (`:197`, `:238-240`, `:328-329`) | Each calls `Decrypt` and hands the result straight to an HTTP client. A failed decrypt reaches the tracker as a credential. |
| Cipher | `CryptoService.Encrypt` (`:22-45`) | AES-CBC, `GenerateIV()` per write, IV prepended, PKCS7 default padding, no MAC, no key identifier, no format version. |
| Encrypted set | — | Connection options flagged `IsSecret` (includes PAT, Jira API token, Linear API key, ServiceNow password, OAuth clientId/clientSecret) and OAuth access/refresh tokens. Nothing else. |
| Not encrypted, correctly | `ApiKeyService.HashKey` (`:290-300`), `EmbedSessionTokenService.HashSecret` | PBKDF2-SHA256 with per-key salt and constant-time compare; embed secrets and nonces hashed. Verifiers, not recoverable secrets. Out of scope and sound. |
| Model to copy | `Program.cs:429-486` (`EnsureOAuthStateSecret`) | Generates 32 random bytes on first boot, wraps them with ASP.NET Data Protection, persists to the key-store directory, resolves the same value every restart. Exactly the shape #5024 asks for. |
| Multi-replica precedent | `Program.cs:884-911` (`ConfigureDataProtection`) | Data-protection key ring already persists to filesystem or Redis, so the multi-replica story has a precedent to follow. |
| Chart | `chart/templates/secret.yaml` | Renders the database connection string and OIDC client secret only. No encryption key, no `existingSecret` hook for one. |
| Compliance claim | `docs/compliance/cra-self-assessment.md:31` | "Unique encryption keys can be specified per installation" — cited as evidence for secure-by-default. Does not survive finding 2. |

---

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] The ciphertext becomes an authenticated, self-describing envelope.** A stored secret gains a
  format version, the id of the key that wrote it, a nonce, and an authentication tag — AES-GCM
  replaces bare AES-CBC. This is not hardening bolted onto the epic; it is the precondition for
  everything else in it. Without a tag there is no way to distinguish "decrypted with the right key"
  from "decrypted into plausible bytes", so a re-encryption pass cannot be trusted, and without a key
  id a rotation cannot tell which rows it still has to visit. *Rejected*: keep AES-CBC and rotate
  anyway (rotation becomes unverifiable, and the pass can overwrite good secrets with garbage);
  defer to a follow-up epic (ships the rotation before its own safety net). User decision, 2026-08-14.
- **[D2] The decrypt-returns-ciphertext fallback is deleted.** A secret that cannot be read raises,
  and the caller surfaces it as an unreadable secret on the owning Connection. Legacy plaintext values
  written before encryption existed are handled by an explicit, recognisable legacy branch in the
  envelope reader, not by a catch-all around every failure.
- **[D3] One active key, several retired ones — a key ring, not a key.** Writes always use the active
  key. Reads try the key named by the envelope. The published default key enters the ring as a retired
  entry on upgrade, which is what makes the upgrade invisible: existing secrets stay readable while
  every new write moves to the instance's own key. Once a rotation has run and no row references it,
  the default can be dropped from the ring.
- **[D4] Standalone custody copies `EnsureOAuthStateSecret` verbatim in shape.** First boot with no
  operator-supplied key generates 32 random bytes, wraps them with ASP.NET Data Protection, and writes
  them beside the data. No flag, no page to read, no ceremony — which is the whole of
  `job-operator-unique-key-without-effort`. The literal key leaves `appsettings.json`.
- **[D5] Kubernetes custody is a Secret the cluster owns.** The chart accepts
  `encryption.existingSecret` so External Secrets Operator or OpenBao can populate it, and generates a
  random key into its own Secret when neither is supplied. A `helm upgrade` must never regenerate it —
  that would orphan every secret in the tenant's database, and it is the single most likely way to
  ship this slice broken. *Rejected for this epic*: an OpenBao **Transit** driver where the key never
  leaves the vault. It is the stronger design and it is explicitly out of scope here, because it turns
  `ICryptoService` into a per-secret network round trip with its own failure and latency modes, and
  because the standalone product has to keep working without any vault at all. User decision,
  2026-08-14. Recorded as the successor epic, not as a gap.
- **[D6] Rotation is two jobs, and only one of them is always the application's.** *Minting* a new key
  means creating it and persisting it; *re-encrypting* means walking every stored secret onto the
  active key. Re-encryption is always the application's job and is always available in the app.
  Minting depends on who owns custody:
  - **App-owned custody** (standalone exe, Docker) — the application mints, activates, re-encrypts and
    retires in one action. One button.
  - **Operator-owned custody** (a Kubernetes Secret, whether the chart made it or External Secrets /
    OpenBao owns it) — the operator adds the new key to the Secret *alongside* the existing one, the
    pod picks up the ring with the new key active and the old one retired, and then the operator
    triggers re-encryption. The application never mints a key it cannot persist.

  Conflating the two is what would make the Kubernetes story impossible: an application that mints must
  also persist, and persisting into a Kubernetes Secret means granting Lighthouse write access to its
  own Secret — a permission that should not be granted and that an external secret store would
  overwrite on its next sync anyway. Splitting them means the same re-encryption code path serves all
  three environments and no new cluster permission is needed. Scheduled or automatic rotation remains
  out of scope. User decision, 2026-08-14.
- **[D12] The key store lives beside the database.** It resolves, by default, to the directory the
  database file is in, and the resolved path is stated in the startup line. Today the data-protection
  key store defaults to `ContentRootPath/data-protection-keys` — `/app/data-protection-keys` in the
  container — while the documented Docker setup mounts a volume at `/app/Data` and puts the database
  there. That costs nothing today, because the key ships inside the image and a recreated container
  reads the same one. After D4 it inverts: `docker rm` and recreate would hand the operator their
  database with a brand-new key and every stored secret unreadable. That is a worse failure than the
  one this epic fixes, and it would land on people who changed no setting. Anyone who already mounted
  their data volume must keep their key by doing nothing.
- **[D7] Bug #5776 ships first and separately.** Accepting the documented configuration path is a small
  change with a live audience — the operators who followed the documentation and believe they are not
  on the default key. It does not wait for the epic. User decision, 2026-08-14.
- **[D8] Neutral commits, advisory at release.** Commit messages describe the change without narrating
  the exposure. A GitHub Security Advisory and a release-notes entry land when the fixed version is
  available to install, so the people affected learn about it at the same moment they can act on it.
  User decision, 2026-08-14.
- **[D9] The key is never logged, and the key source always is.** Startup and the System Info surface
  state *where* the active key came from — generated for this instance, supplied by configuration,
  supplied by an external secret — and never any part of its material. "Which key am I on?" has to be
  answerable without a debugger, because today it is not answerable at all.
- **[D10] Fail fast on a key store that exists but cannot be read.** If a key ring is present and
  unreadable, the instance refuses to start rather than generating a fresh key. Generating one would
  look like a successful boot and would silently orphan every stored secret.
- **[D11] No vendor telemetry.** Every KPI below is `per_instance` or `vendor_demo_only`, per the
  standing convention in `kpi-contracts.yaml`. Nothing about key state leaves a customer's instance.

### Open questions

- **OQ-1** Does a re-encryption pass need to hold off the sync pipeline, or is per-row optimistic
  concurrency enough? `OAuthCredential` rows are rewritten by token refresh while a rotation could be
  walking them. Slice 03 carries a timeboxed probe; if concurrency turns out to need a lock, that is a
  design change, not a slice tweak.
- **OQ-2 — resolved by D6, retained for the record.** "How does an instance behave when the external
  Secret is rotated underneath a running pod?" was an open question when rotation was assumed to be one
  action. Under D6 it is not an accident to survive but the supported Kubernetes flow: the operator
  adds the new key alongside the old, the ring carries both, and re-encryption follows. What slice 05
  still owes is the degenerate case — a Secret whose *old* key was removed before re-encryption ran —
  which must surface unreadable secrets clearly rather than looping on work-tracking-system rejections.
- **OQ-3** Does the standalone key file need an operator-facing export/backup path? Losing it loses
  every stored secret, which is the correct security property and a support burden. Deferred; named
  here so it is not discovered by a user first.

---

## Wave: DISCUSS / [REF] Scope Assessment

**Verdict: OVERSIZED as stated, split accepted.**

Oversized signals present (2 or more triggers the gate; four are present):

- Touches five areas — cryptography, EF persistence, application bootstrap, Helm chart, docs and
  compliance surface.
- Estimated effort well beyond two weeks if taken as one change.
- Contains at least three independently shippable user outcomes: a unique key by default, a rotation
  that costs no credentials, and cluster-owned custody. Each is valuable without the others.
- The walking-skeleton path crosses more than five integration points (config resolution, data
  protection key store, DbContext save path, every auth strategy, the OAuth refresh path, the chart).

**Split**: six thin slices, each shipping end to end, ordered so the abstraction ships before anything
depends on it. Bug #5776 lands before slice 01 as an independent precursor. The OpenBao Transit driver
becomes a successor epic rather than a slice.

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy B — extend an existing skeleton.** This is brownfield with a working encryption path and a
working first-boot secret-provisioning path (`EnsureOAuthStateSecret`) already in production. No new
walking skeleton is built. Slice 01 is the thin end-to-end proof: one secret written through the new
envelope, read back, and shown failing legibly under the wrong key — the whole vertical, one row of
data.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Introduced by |
|---|---|---|
| Application bootstrap | Key-ring resolution at builder time — configuration, generated file, or external secret | slice 02 |
| HTTP (admin) | `POST` rotate, `GET`/`POST` verify, under the existing authorization surface — System Admin only | slice 03, slice 04 |
| UI | Settings → an encryption panel showing key source, active key id, the key ids the ring holds, and secret readability. **Custody-aware**: it offers **Rotate key** where the application owns the key, and **Re-encrypt onto the active key** where an operator does. It never offers to mint a key it cannot persist. | slice 03, slice 04 |
| UI (existing) | Connection detail — an unreadable secret is named on the Connection that owns it | slice 01 |
| Startup log | One line naming the key source; never the key | slice 02 |
| Helm values | `encryption.existingSecret`, `encryption.key`, plus the generate-if-absent behaviour | slice 05 |
| Docs | Installation configuration page, compliance self-assessment, `SECURITY.md`, security advisory | slice 06 |

---

## Wave: DISCUSS / [REF] Pre-requisites

- Bug #5776 landed and released, so the documented configuration path works before slice 02 changes
  what happens when no key is supplied.
- EF migrations generated with the `CreateMigration` script, additive only — column widths grow for
  the envelope, nothing is dropped or renamed.
- The dev instance on `:5169` restored from a real backup, so slices 02 and 03 run against genuine
  history rather than seeded demo rows.
- A premium licence on the verification instance where the epic touches licensed surfaces.
- Chart floor: the current published chart line; slice 05 bumps it.
- Tenant Zero available as the platform-side proving ground for slice 05.

---

## Wave: DISCUSS / [REF] Out of Scope

- **OpenBao Transit / KMS-backed encryption** where key material never enters the process. Recorded as
  the successor epic (D5).
- Scheduled or automatic key rotation (D6).
- Hardware security modules, cloud KMS drivers, per-tenant automation inside the private platform
  repository.
- Changing how API keys or embed session secrets are stored — they are hashed, which is correct.
- Encrypting anything not currently encrypted: work item data, settings, forecasts. Full-database
  encryption is the database's job, not the application's.
- The OIDC client secret in configuration — it is supplied by the operator's own secret management and
  never persisted by Lighthouse.
- Marketing website security copy. Flagged for the DELIVER checklist, not built here.

---

## Wave: DISCUSS / [REF] User Stories

Every story traces to a `job_id` in `docs/product/jobs.yaml`. One story is labelled `@infrastructure`
and is a precursor commit inside slice 01, never a slice of its own.

---

### US-01 — A secret that cannot be read says so

`job_id: job-operator-know-the-key-is-actually-in-effect` · persona `platform-operator` · **slice 01**

As a platform operator, I want a stored secret that cannot be decrypted to be reported as an unreadable
secret, so that a key problem looks like a key problem instead of arriving days later as a work tracking
system rejecting my token.

#### Elevator Pitch
Before: a wrong or changed encryption key produces no error anywhere — `Decrypt` returns the ciphertext
and the connector sends it to Jira as the API token, so the first symptom is a 401 that reads as an
expired credential.
After: open Settings → Connections → *(the connection)* → sees `Secret cannot be read with the current
encryption key` on the offending field, and the update log carries one matching line.
Decision enabled: whether to go fix the key or go re-issue the token — two very different afternoons,
currently indistinguishable.

**Acceptance criteria**
- AC-1.1 A secret written after this slice is stored as an envelope carrying a format version, the
  active key's id, a nonce and an authentication tag.
- AC-1.2 A secret written before this slice — bare AES-CBC — is still read correctly, unchanged, with
  no migration and no user action.
- AC-1.3 A value that was never encrypted at all is recognised as legacy plaintext by an explicit
  branch, not by a caught exception.
- AC-1.4 Decrypting an envelope whose authentication tag does not verify raises. It does not return the
  ciphertext, the plaintext, or an empty string.
- AC-1.5 A tampered ciphertext — any byte flipped — fails to decrypt rather than producing altered
  plaintext.
- AC-1.6 A Connection holding a secret that cannot be read shows that state on the field, naming the
  encryption key as the cause.
- AC-1.7 No auth strategy and no OAuth path sends an unreadable secret to a remote system.
- AC-1.8 The failure surfaces once per affected secret, not once per sync attempt.
- AC-1.9 No key material, ciphertext or plaintext appears in any log line at any level.

---

### US-02 — This install's key belongs to this install

`job_id: job-operator-unique-key-without-effort` · persona `platform-operator` · **slice 02**

As a self-hoster who downloaded the exe or ran one `docker run`, I want Lighthouse to protect my
credentials with a key generated for my instance alone the first time it starts, so that a copy of my
database is worth nothing to whoever takes it — without my having read anything first.

#### Elevator Pitch
Before: every install that has not overridden the key encrypts every token with the key published in
`appsettings.json` on GitHub, so obtaining the database file is obtaining the tokens.
After: run `docker run ghcr.io/letpeoplework/lighthouse:latest` → sees
`Encryption key: generated for this instance` in the startup log, and Settings → System shows the same
thing with the active key's id.
Decision enabled: whether the operator has anything left to do — and for the first time, an honest
answer to "is this instance's data safe if the file leaks?".

**Acceptance criteria**
- AC-2.1 A first start with no operator-supplied key generates 32 cryptographically random bytes,
  wraps them with ASP.NET Data Protection, and persists them alongside the existing key store.
- AC-2.2 A restart resolves the same key. Restarting is not a rotation.
- AC-2.3 An operator-supplied key — configuration, environment, or command line — takes precedence and
  is reported as such.
- AC-2.4 The literal default key is removed from `appsettings.json`.
- AC-2.5 An instance upgrading from a version that used the published default key keeps reading every
  existing secret, with no user action and no credential re-entry. The old key is present in the ring
  as a retired entry.
- AC-2.6 After the upgrade, newly written secrets use the instance's own key, not the retired one.
- AC-2.7 A key store that exists but cannot be read stops startup with a message naming the key store.
  It does not generate a replacement.
- AC-2.8 The startup log and System Info state the key source and the active key id, never the key.
- AC-2.9 A supplied key that is not 32 bytes of base64 stops startup with a message that says what is
  wrong with it.
- AC-2.10 The key store resolves by default to the directory the database file lives in, and the
  resolved path appears in the startup line. An instance whose data directory is a mounted volume
  keeps its key across container replacement without the operator configuring anything.
- AC-2.11 Recreating the container against an existing mounted data directory reads the same key and
  every stored secret stays readable.

---

### US-03 — Rotate the key without asking anyone for a token

`job_id: job-operator-rotate-key-without-recredentialing` · persona `config-admin` · **slice 03**

As the administrator of an instance whose key may have been exposed, I want to move every stored secret
onto a new key from inside Lighthouse, so that I can contain the exposure the same afternoon instead of
asking every team to re-enter every credential.

#### Elevator Pitch
Before: the documented procedure for changing the key is to change it and then reconfigure every work
tracking system — every PAT, every API token, every OAuth connection, re-entered by hand, with every
sync down until they are.
After: open Settings → Encryption → **Rotate key** (where the application owns the key) or
**Re-encrypt onto the active key** (where an operator does) → sees `Rotated 47 secrets onto key
`k-2026-08-14-01`. 0 unreadable. Previous key retired.`
Decision enabled: whether the exposure is contained — answerable in a minute, with a number, instead of
by scheduling a week of credential re-entry.

**Acceptance criteria**
- AC-3.1 Rotation generates a new key, makes it active, and retires the previous one without removing
  it from the ring.
- AC-3.2 Every stored secret readable under any ring key is re-encrypted under the new active key.
- AC-3.3 No credential is requested, re-entered, or invalidated. Every Connection works immediately
  afterwards with no user action.
- AC-3.4 The result reports how many secrets were re-encrypted and how many could not be read, per
  Connection.
- AC-3.5 A secret that cannot be read is left byte-for-byte untouched and named in the report. Rotation
  never overwrites what it could not verify.
- AC-3.6 Rotation is idempotent and resumable: interrupted halfway, the instance still functions —
  both keys are in the ring — and running it again completes the remainder.
- AC-3.7 A token refresh writing an `OAuthCredential` while rotation is walking it neither loses the
  refreshed token nor leaves the row unreadable.
- AC-3.8 Rotation is restricted to a System Admin and is recorded — who, when, how many, with no key
  material.
- AC-3.9 On an instance still on the published default key, rotation removes the last row referencing
  it and the default leaves the ring.
- AC-3.10 Where the application owns the key, one action mints, activates, re-encrypts and retires.
- AC-3.11 Where an operator owns the key, the panel offers re-encryption onto the already-active key
  and does **not** offer to mint one. Lighthouse never writes to a Kubernetes Secret and needs no
  permission to.
- AC-3.12 The panel names the custody mode and lists the key ids the ring currently holds, so the
  operator can see the new key has arrived before triggering re-encryption.
- AC-3.13 Re-encryption is the same code path in both custody modes. Only the minting step differs.

---

### US-04 — Check before you rotate, and prove it after

`job_id: job-operator-know-the-key-is-actually-in-effect` · persona `config-admin` · **slice 04**

As an administrator about to rotate — or having just rotated — I want a read-only check that tells me
whether every stored secret is readable and which key each one is on, so that I find out before the
write pass rather than from a failed sync at three in the morning.

#### Elevator Pitch
Before: the only way to learn whether the instance's secrets are intact is to trigger a sync per
Connection and watch what the tracker says.
After: open Settings → Encryption → **Check secrets** → sees `47 secrets · 45 on the active key · 2 on
a retired key · 0 unreadable`, with the two named.
Decision enabled: rotate now, or fix the two unreadable secrets first — the difference between a clean
rotation and a report full of skipped rows.

**Acceptance criteria**
- AC-4.1 The check reads every stored secret and writes nothing.
- AC-4.2 It reports, per secret, the owning Connection, the field, and the key id it is encrypted under.
- AC-4.3 Legacy AES-CBC values and legacy plaintext values are each reported as their own state, not
  merged into "unreadable".
- AC-4.4 An unreadable secret is reported with the Connection and field that own it, so it can be
  fixed by hand.
- AC-4.5 The check is available before any rotation has ever run, on an instance still on the default
  key.
- AC-4.6 Running it immediately after a rotation shows every readable secret on the active key.
- AC-4.7 The check is restricted to a System Admin.
- AC-4.8 It completes within the request timeout on a Tenant-Zero-sized instance, or streams progress.

---

### US-05 — The cluster owns the key

`job_id: job-saas-operator-tenant-owned-encryption-key` · persona `platform-operator` · **slice 05**

As an operator installing Lighthouse with Helm — for my own organisation or as one tenant among many —
I want the encryption key to come from a Kubernetes Secret that my own secret store can own, and to be
generated uniquely for me if I supply nothing, so that no two installs share a key and no key sits in a
values file.

#### Elevator Pitch
Before: the chart provisions a database connection string and an OIDC secret and no encryption key at
all, so every Kubernetes install — including every tenant on the platform — runs on the published
default.
After: run `helm install lighthouse letpeoplework/lighthouse` with no encryption values → sees the pod
log `Encryption key: supplied by external secret`, and a `helm upgrade` afterwards leaves every stored
credential readable.
Decision enabled: whether a security review of a Kubernetes install can be passed — and for the
platform, whether one tenant's leaked backup is one tenant's problem.

**Acceptance criteria**
- AC-5.1 `encryption.existingSecret` points the deployment at a Secret an external store owns —
  External Secrets Operator or OpenBao — and the chart renders no key of its own.
- AC-5.2 With no encryption values supplied, the chart generates a unique random key into its own
  Secret on first install.
- AC-5.3 **A `helm upgrade` never regenerates a generated key.** An upgrade with unchanged values
  leaves every stored secret readable. This is the failure that would orphan a tenant's entire
  credential set, and it has a chart unit test of its own.
- AC-5.4 An explicitly supplied `encryption.key` is used verbatim and reported as configuration-supplied.
- AC-5.5 The generated key never appears in a ConfigMap, in rendered values, or in a pod's environment
  dump; it reaches the container the same way the database password already does.
- AC-5.6 Chart unit tests cover: nothing supplied, `existingSecret` supplied, explicit key supplied,
  and upgrade-idempotence.
- AC-5.7 The standalone single-container product is byte-unchanged by this slice.
- AC-5.8 Chart README and values schema document all three custody modes.
- AC-5.9 The Secret carries a key **ring**, not a single key: one active entry and any number of
  retired ones. An operator adds the new key alongside the old, and the pod reads both.
- AC-5.10 The documented Kubernetes rotation is: add the new key to the Secret alongside the old, roll
  the pod, trigger re-encryption, then drop the old key. Every step is an operator action against
  their own secret store.
- AC-5.11 A Secret whose old key was removed before re-encryption ran surfaces the affected secrets as
  unreadable rather than looping on work-tracking-system rejections.

---

### US-06 — Say what is actually true

`job_id: job-maintainer-answer-the-secret-storage-question` · persona `lighthouse-maintainer` · **slice 06**

As the maintainer answering a prospect's security questionnaire, I want the documentation, the
compliance self-assessment and the security policy to describe what the product actually does by
default, so that I can answer a security team with a link instead of a paragraph of caveats.

#### Elevator Pitch
Before: the configuration page documents an override that nothing consumes, and the compliance
self-assessment cites per-installation unique keys as evidence of secure-by-default, which finding 2
makes untrue for anyone who followed the page.
After: open the Installation → Configuration page → sees the three custody modes, what happens on
first boot, how to rotate, and what an operator must back up; and a published advisory says plainly
what was wrong and which version fixes it.
Decision enabled: a prospect's security reviewer can complete their assessment from the documentation,
without asking — the exact conversation that lost the evaluation this epic came from.

**Acceptance criteria**
- AC-6.1 The Encryption Key section documents the configuration path the code actually reads, and the
  first-boot generation behaviour.
- AC-6.2 All three custody modes — generated, configuration-supplied, external secret — are documented
  with the observable signal that tells an operator which one is in effect.
- AC-6.3 Rotation is documented, including that it requires no credential re-entry and what the report
  means.
- AC-6.4 What an operator must back up, and what is lost if they do not, is stated explicitly.
- AC-6.5 `docs/compliance/cra-self-assessment.md` rows 1.3 and 1.5 cite evidence that matches shipped
  behaviour.
- AC-6.6 `SECURITY.md` states the supported reporting path and what this epic changed.
- AC-6.7 A GitHub Security Advisory is published when the fixed version is installable, not before.
- AC-6.8 Release notes lead with what the operator should do, in the terminology the product uses.
- AC-6.9 Documentation uses the seeded terminology defaults, never a single tracker's vocabulary.
- AC-6.10 The Kubernetes rotation procedure is documented as its own sequence: add the new key to the
  Secret alongside the old, roll the pod, re-encrypt, drop the old key — and states that Lighthouse
  never writes to the Secret itself.
- AC-6.11 The Docker page states where the key store lives, that it must be on the mounted data
  volume, and what a container recreation without that volume costs.

---

### US-07 — The key ring `@infrastructure`

`job_id: infrastructure-only` · **precursor commit inside slice 01**

`infrastructure_rationale`: introduces the key-ring type and the envelope reader/writer with no
user-visible behaviour of its own. It ships as the first commit of slice 01 and is observable only
through US-01. It is **not** a slice: a slice containing only this story would be a structural failure
under the slice-composition gate.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone** (operator's timeline with an instance):
`Install` → `Discover which key I am on` → `Use it` → `Suspect exposure` → `Rotate` → `Prove it worked`
→ `Answer for it`

| Slice | Story | Backbone step | Ships | Learning hypothesis |
|---|---|---|---|---|
| — (precursor) | Bug #5776 | Discover / Use | The documented configuration path is accepted; startup states which key source is in effect | Disproves "the mismatch is the only thing hiding key state" if operators still cannot tell which key is active |
| 01 · #5777 | US-01 (+US-07) | Use | Authenticated self-describing envelope; unreadable secrets named on their Connection; plaintext fallback deleted | Disproves "we can migrate the format in place" if real stored blobs turn out to be ambiguous between legacy and envelope |
| 02 · #5024 | US-02 | Install | Per-instance key generated on first boot; default key leaves `appsettings.json` and enters the ring retired | Disproves "the upgrade is invisible" if any existing install loses access to a secret |
| 03 · #5778 | US-03 | Rotate | Operator-triggered rotation, no credential re-entry, resumable, reported | Disproves "in-place re-encryption is safe unlocked" if a concurrent token refresh corrupts a row (OQ-1) |
| 04 · #5779 | US-04 | Prove | Read-only readability check, per-secret key attribution | Disproves "the rotation report is enough" if an operator cannot map an unreadable secret back to something they can fix |
| 05 · #5780 | US-05 | Install (k8s) | Chart custody: `existingSecret`, generate-if-absent, upgrade-safe | Disproves "Helm can own key generation" if `helm upgrade` regenerates the key and orphans the tenant |
| 06 · #5781 | US-06 | Answer for it | Docs, compliance evidence, `SECURITY.md`, advisory, release notes | Disproves "the doc gap was the only false claim" if writing the threat model surfaces further mismatches |

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | **Pass.** Slice 01 introduces one concept (the envelope) and one type (the ring). Slices 03 and 04 each add one service and one surface. |
| Every slice depends on a new abstraction? | **Pass by construction.** The abstraction *is* slice 01, shipped first and on its own, exactly as the rule prescribes. Slices 02-05 consume it; none introduces another. |
| Does any slice disprove a pre-commitment? | **Pass.** Slice 02 stakes "the upgrade is invisible" — falsifiable the moment one real secret stops reading. Slice 05 stakes "Helm can generate a key safely", against a well-known way for that to be false. |
| Synthetic data only? | **Pass.** Slices 02, 03 and 04 are accepted against the `:5169` instance restored from a real backup, and slice 05 against Tenant Zero. Demo rows are not sufficient evidence for any of them. |
| Two slices identical except for scale? | **Flagged, not merged.** Slices 03 and 04 both walk every stored secret. They stay separate because one writes and one does not, and because the read-only one is what makes the writing one safe to run. If slice 03 lands and slice 04 turns out to be its report with a flag flipped, merge them and say so in the brief. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **Bug #5776 first, outside the epic.** Smallest change, live audience, and it makes the key source
   observable — which every later slice is verified against.
2. **Slice 01 before everything.** Rotation without an authentication tag is unverifiable, and a
   re-encryption pass over a decrypt that cannot fail is a way to lose every secret in one command. The
   highest-consequence uncertainty in the epic is retired first, when it is still cheap.
3. **Slice 02 next** because it is the epic's headline outcome and the one that answers the criticism
   the epic came from. It also produces the first real upgrade evidence, on genuine data.
4. **Slice 03, with a timeboxed probe on OQ-1 before the brief is committed to.** Highest
   implementation risk left; the probe is cheap and its answer changes the design rather than the code.
5. **Slice 04** immediately after, so the "prove it" surface exists in the same release as rotation.
6. **Slice 05** once the application side is settled — the chart can only hand over a key the
   application knows how to accept.
7. **Slice 06 last, and it gates the release.** The advisory is only honest once the fix is installable
   (D8).

Dogfood moment per slice: `:5169` restored from a real backup for 01-04, Tenant Zero for 05, the
published docs site for 06.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement | Scope |
|---|---|---|---|
| **KPI-1** Instances not on a shared key | 0 startups report a shipped default key after slice 02; 100% report `generated`, `configured` or `external` | Startup key-source log line (AC-2.8) | `per_instance` |
| **KPI-2** Rotation costs no credentials | 100% of rotations complete with 0 Connections requiring re-entry | Rotation report (AC-3.4), on `:5169` and Tenant Zero | `per_instance` + `vendor_demo_only` |
| **KPI-3** Rotation completes promptly | < 60 s on a Tenant-Zero-sized secret set | Timed rotation, recorded in the slice 03 brief | `vendor_demo_only` |
| **KPI-4** Silent decrypt failures | 0 — the code path returning ciphertext as plaintext no longer exists | Absence test asserting `Decrypt` raises on a bad tag (AC-1.4) | `per_instance` |
| **KPI-5** Upgrade transparency | 0 secrets lost across an upgrade from the pre-epic version on a real restored database | Readability check before and after (AC-2.5, AC-4.6) | `vendor_demo_only` |
| **KPI-6** Helm upgrade safety | 0 key regenerations across 3 consecutive `helm upgrade` runs with unchanged values | Chart unit test (AC-5.3) + Tenant Zero | `vendor_demo_only` |
| **KPI-7** Question answerable from docs | The documentation answers key source, rotation, backup and threat model without a maintainer in the loop | Reviewed at release against the questionnaire that started this epic | `vendor_demo_only` |

KPI-1 is unmeasurable before Bug #5776 makes the key source observable — which is the argument for
shipping it first.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All acceptance criteria for the slice pass as automated tests.
2. `dotnet build` zero warnings; `dotnet test` green.
3. `pnpm test`, `pnpm build` (zero warnings), Biome clean — stated explicitly as N/A per slice where
   there is no frontend change.
4. Mutation testing ≥ 80% kill rate on the changed backend surface. Non-negotiable on the crypto and
   rotation surfaces, where a surviving mutant is a real hole rather than a metric.
5. SonarQube Cloud: no new issues of any severity, including security hotspots.
6. EF migration generated with the `CreateMigration` script, additive only.
7. Docs updated per-feature, in the seeded terminology.
8. ADO story transitioned; slice pushed only after CI is green.
9. The slice's learning hypothesis has an explicit verdict recorded in its brief — confirmed or
   disproved, never blank.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Business value articulated | ✅ | A named lost evaluation, plus five findings read from code during this wave; KPI-1 and KPI-2 carry the outcome |
| 2 | Job traceability | ✅ | 5 jobs written to `docs/product/jobs.yaml`; every story carries a real `job_id` except US-07, which is `@infrastructure` and is a precursor commit inside slice 01 |
| 3 | Acceptance criteria testable | ✅ | 63 ACs, each observable from a stored value, a report, a log line, a rendered chart template, or a documentation page |
| 4 | Dependencies identified | ✅ | Bug #5776 released first; `:5169` restored from a real backup; Tenant Zero for slice 05; `CreateMigration`; premium licence on the verification instance |
| 5 | Sliced ≤ 1 day each | ⚠️ | 6 briefs. Five are 4-6h. **Slice 03 is the exception** and is written with a timeboxed probe on OQ-1 first; if the probe says a lock is needed, the brief is re-cut before it is dispatched |
| 6 | No known blockers | ✅ | None. OQ-1 is scheduled as a probe, OQ-2 is inside slice 05's acceptance, OQ-3 is explicitly deferred |
| 7 | Observable surface defined | ✅ | Driving Ports table — every slice names the surface an operator reads |
| 8 | Test data / environment available | ✅ | `:5169` with real recorded history is the only environment that can prove KPI-5; Tenant Zero for KPI-6 |
| 9 | Outcome KPI with numeric target | ✅ | 7 KPIs, each with a number or a binary and a named measurement source |

**Requirements completeness: 0.96.** The missing 0.04 is item 5: slice 03's size depends on OQ-1's
answer, and the brief says so rather than guessing.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions

See Locked Decisions above (D1-D12). The five that shape everything downstream:

- **[D1]** The authenticated, self-describing envelope. Rotation is not safe to build without it, so it
  ships first and alone.
- **[D2]** Deleting the decrypt-returns-ciphertext fallback. It is what made every other problem
  invisible, and it is what would make a re-encryption pass destructive.
- **[D3/D4]** A key ring with the published default retired into it — the mechanism that makes "every
  install gets its own key" arrive without anyone re-entering a credential.
- **[D5]** Kubernetes custody is a Secret the cluster owns. OpenBao Transit is named, scoped out, and
  recorded as the successor epic rather than left as an implied gap.
- **[D6/D12]** Minting and re-encrypting are separate jobs, so one re-encryption path serves standalone,
  Docker and Kubernetes without Lighthouse ever needing write access to a Secret — and the key store
  moves beside the database, so the Docker failure mode D4 would otherwise introduce never arrives.

### Requirements summary

- **Primary needs**: every instance on a key that is its own, without the operator doing anything; a
  rotation that costs no credentials and can be proven; cluster-owned custody for Kubernetes and the
  platform; and public documentation that matches all of it.
- **Walking skeleton scope**: none built (strategy B). Slice 01 is the thin end-to-end proof through
  the existing path.
- **Feature type**: cross-cutting.

### Constraints established

- The standalone single-container product must keep working with no vault, no cluster and no
  configuration. Every custody mode beyond the generated one is additive.
- Expand-only migrations; no stored secret is dropped, renamed, or rewritten except by an explicit
  rotation the operator asked for.
- No key material in logs, reports, config maps, rendered values, or telemetry. Ever.
- No vendor telemetry. Key state never leaves the instance.
- Neutral commit messages until the advisory publishes with the installable fix.

### Upstream changes

None. No DISCOVER or DIVERGE wave ran for this feature, so no prior assumption was altered.

---

## Wave: DISCUSS / [REF] SSOT Updates

- `docs/product/jobs.yaml` — 5 jobs appended, `epic-5775-secret-encryption-key-custody` added to
  `feature_context`.
- `docs/product/journeys/epic-5775-secret-encryption-key-custody.yaml` — created; 4 journeys.
- `docs/product/personas/platform-operator.yaml` — 3 jobs appended to `primary_jobs`.
- `docs/product/personas/config-admin.yaml` — 1 job appended.
- `docs/product/personas/lighthouse-maintainer.yaml` — 1 job appended.

---

## Wave: DISCUSS / [REF] Peer Review

Per-wave review was invoked (not default) because DoR item 5 carries a ⚠️ rather than a ✅.
`nw-product-owner-reviewer`, 2026-08-14.

**Passed**: journey coherence with arcs earned rather than asserted; all five shared artifacts
single-sourced with consumers listed; every elevator pitch naming a real user-invocable surface rather
than an internal API; job traceability for all six value stories; the slice-composition gate; all five
carpaccio taste tests; every AC testable; no LeanUX antipatterns; no confirmation bias.

**Verdict recorded here: approved with notes**, overriding the reviewer's
`rejected_pending_revisions`. The two findings it blocked on — a per-story "Domain Examples" section
and a per-story "UAT Scenarios" section in Given/When/Then — come from a generic eight-item story DoR,
not from this wave's contract. DISCUSS specifies a nine-item DoR, which this artifact carries and
passes; Gherkin is a Tier-2 expansion here, lazy by design; and authoring acceptance tests belongs to
DISTILL, where `nw-acceptance-designer` owns it against a settled design. Writing them now would
duplicate that work from a wave holding less information. The repository's own shipped precedent
(`epic-5687-faster-updates`) carries neither section. User decision, 2026-08-14.

The reviewer's two medium findings — slice 03's conditional estimate and the slices 03/04 merge
question — were already written as open in the story map, the DoR and the slice briefs. Confirming
flags, not new findings.

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set. `nw-platform-architect` (DEVOPS) — the
Outcome KPIs section only.

DESIGN owns four questions this wave deliberately left open: the envelope's exact wire format and how
it stays distinguishable from a legacy AES-CBC blob; how a key ring is expressed in each custody mode —
an on-disk store beside the database, and a Kubernetes Secret carrying one active key and any number of
retired ones; whether the re-encryption pass needs to hold off the sync pipeline (OQ-1); and how the
application detects that an operator has added a key to the Secret without a restart, so the panel can
show it has arrived before re-encryption is triggered.
