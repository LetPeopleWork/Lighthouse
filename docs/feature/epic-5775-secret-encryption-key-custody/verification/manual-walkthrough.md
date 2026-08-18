# Manual verification walkthrough — epic-5775 secret encryption & key custody

**Run against**: this branch at slice-04 close (slices 01–04 built; **slice 05 chart and slice 06
"say what is true" are NOT built**).
**Upgrade baseline**: `v26.8.14.1`.
**Purpose**: two at once — confirm the custody behaviour is correct, and grade whether the banner,
the encryption panel and the installation docs actually guide an operator through it. Everything the
grading turns up is input to **slice 06**, not a defect list against slice 04.

Claude guides and sets up; the operator executes and drops observations into the ledger at the bottom.
Nothing is judged "pass" from a code reading — only from what the running instance says.

---

## What the code does, stated once

Two orderings decide every outcome below. Both are worth having open while running.

**Which key is in force** (`EncryptionKeyRingBootstrapper.Resolve`):

1. `Encryption:Keys` — a ring, first entry active
2. `Encryption:Key` — a single key
3. `EncryptionSettings:EncryptionKey` — the name every release before this one read. Still honoured,
   no longer documented, nudged against on every start.
4. `Encryption:KeysFile` — a mounted file
5. an existing generated key store
6. mint a new key — **only if the key store is durable**

Then the published default key is appended as a **retired, read-only** entry, always. That is what
makes an upgrade invisible: old secrets stay readable, new writes go to the active key.

**Where the key is kept** (`KeyStoreResolver.Resolve`):

1. `Encryption:KeyStorePath` → that directory, minting permitted
2. `Lighthouse:DataProtection:KeyStorePath` → that directory, minting permitted
3. the directory holding the SQLite file → `<db dir>/keys`, minting permitted
4. otherwise `ContentRoot/data-protection-keys` — **minting forbidden**

Case 4 is anything that is not a SQLite file on disk: Postgres, `:memory:`, or a bare relative
filename. With minting forbidden the instance either **refuses to start** (nothing stored yet) or
**runs on the published key and says so** (something already stored).

**Three consequences that shape the runs**

- An upgrade does **not** move existing secrets off the published key. It mints a key and makes it
  active; already-stored secrets stay where they are until a rotation or a re-encrypt runs. So every
  B-run has two halves, and the panel's "N stored credentials are still readable with the key
  published with Lighthouse" is the thing being read between them.
- "Custom key" is **two different starting states**, because the old release read one name and the old
  docs advertised another. They behave oppositely on upgrade. Group C splits them.
- Postgres with no key and no key-store path is a first-install **refusal**, not a warning.

---

## Assets to have ready before starting

| Asset | Notes |
|---|---|
| `v26.8.14.1` standalone installer, server binary, and `ghcr.io/letpeoplework/lighthouse:26.8.14.1` | The upgrade groups must start from the real old build, not a hand-made DB. |
| This branch built as: standalone app, server binary, container image | Container: local `docker build`, tag it `lighthouse:slice04` so no run accidentally pulls `latest`. |
| One real work-tracking credential | Used to prove decrypt → HTTP end-to-end, not just store-and-read. Same connection reused across runs. |
| A Postgres instance | For A4 and B4. Any container is fine. |
| A scratch directory per run | Every run starts from an empty one. Never reuse a directory between runs — a stale `keys/` folder invalidates the result silently. |

**Seeding the "old" state** (do this once per upgrade run, on `v26.8.14.1`):

1. Start the old build in an empty directory.
2. Add one work tracking system connection with the real credential, and one team using it.
3. Confirm the team refreshes — that proves the credential was stored and read back.
4. Stop it. Keep the directory. That directory is the fixture for the matching B or C run.

---

## What to look at, every run

Four surfaces. Grade each one every time, even when the run is expected to be boring.

1. **The startup banner line** — `🔑  Encryption    : <source> (<key id>) · <directory>`, plus any
   `⚠️  Warning` lines under it. Sources are: *generated for this instance*, *supplied by
   configuration*, *supplied by a mounted secret file*, *the key published with the product*.
2. **Settings → Encryption panel** — key source, active key id, keys held, kept-in path, the
   custody explanation sentence, the published-key warning banner and its count, and which of
   *Rotate key* / *Move stored secrets onto the active key* / *Check secrets* are offered.
3. **The docs an operator would have open** — `docs/Installation/server.md`,
   `docs/Installation/standalone.md`, `docs/Installation/configuration.md`. Question each time: if I
   had only this page, would I have got here?
4. **The failure text, when there is one.** Every refusal in this feature names remedies. A remedy
   that does not work verbatim is a slice-06 finding of the highest kind.

---

## Group A — first install of the latest release

**Goal**: start it, it works, nothing to be done.

### A1 · Standalone (Tauri), fresh

- Install this branch's standalone build on a machine with no prior Lighthouse data.
- Launch.

**Expect**: starts with no prompt. Banner says *generated for this instance*, a key id, and a
directory inside the app data location. Panel offers **Rotate key** (custody is app-owned). No
published-key warning — nothing is stored yet. `Check secrets` reports zero secrets without erroring.

**Also confirm**: the resolved key directory is inside the same durable app-data location as the
database. The standalone initializer sets an explicit data-protection key-store path, so this
resolves through case 2, not case 3 — read the banner path and confirm it is somewhere a reinstall
would not wipe.

### A2 · Server binary, fresh

- Extract this branch's binary into an empty directory, run it.

**Expect**: same as A1, except the key directory is `<db dir>/keys` (case 3) — i.e. beside
`LighthouseAppContext.db` in the run directory.

### A3 · Docker with a data volume, fresh

Documented command from `server.md`, pointed at the local image:

```bash
docker run -d -p 8081:443 -p 8080:80 \
  -v ".:/app/Data" -v "./logs:/app/logs" \
  -e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db" \
  lighthouse:slice04
```

**Expect**: banner names `/app/Data/keys`, i.e. on the mounted volume. Custody *generated for this
instance*. A `keys/` directory appears on the host beside the `.db` file.

**Docs check**: does `server.md` tell the operator that this directory now holds their key and must
be backed up? Today it does not mention it at all.

### A4 · Docker with Postgres, fresh, nothing configured — **expected refusal**

```bash
docker run --rm -p 8080:80 \
  -e "Database__Provider=Postgres" \
  -e "Database__ConnectionString=<postgres connection string>" \
  lighthouse:slice04
```

**Expect**: the container **does not start**. Log carries `NoDurableKeyStore.Refusal`:

> This instance has nowhere to keep an encryption key that would still be there after a restart, and
> it has nothing stored yet, so Lighthouse will not start on the key published with the product. […]
> Set `Encryption__Key` to a key of your own, or set `Encryption__KeyStorePath` to a directory on a
> volume that outlives this container, and start Lighthouse again.

**Grade hard here.** This is a working install today that stops working on upgrade-to-fresh, and the
only thing standing between the operator and a dead container is this paragraph. Read it as someone
who has never heard of a key ring: does it say what to do, and can it be done without opening a doc?

**Docs check**: `configuration.md` and `server.md` must carry this. An operator hitting this has no
running instance and no panel to look at — the log line and the docs page are the entire surface.

### A4b · A4's first remedy, verbatim

Add `-e "Encryption__Key=<base64 32 bytes>"` and start again.

**Expect**: starts. Custody *supplied by configuration*. Panel shows **no Rotate key button** — the
key is not Lighthouse's to replace — and the explanation sentence points at `Encryption__Keys`.

**Grade**: the refusal says "set `Encryption__Key` to a key of your own" and says nothing about how to
produce one. Is there a documented way to generate a valid value? If not, that is a slice-06 gap.

### A4c · A4's second remedy, verbatim

Instead of a key, mount a volume and add `-e "Encryption__KeyStorePath=/app/keys"` with
`-v "./keys:/app/keys"`.

**Expect**: starts, mints, custody *generated for this instance*, banner names `/app/keys`, Rotate is
offered. Recreating the container returns the **same** active key id.

---

## Group B — upgrade from `v26.8.14.1` on the published default key

**Goal**: move away from the published key. Two halves per run — the upgrade must cost nothing, and
the move must then be available and obvious.

Every B run starts from a directory seeded per the recipe above, on the old build, with a working
connection and team.

### B1 · Standalone · B2 · Server binary · B3 · Docker with a data volume

**Half 1 — the upgrade itself.** Replace the build, start it against the seeded data.

Expect:

- Starts with no prompt and no error.
- The seeded team still refreshes — the stored credential is still readable through the retired
  published key.
- Banner: *generated for this instance*, a **new** key id, the resolved directory.
- Panel: keys held shows **two** entries — the newly minted active key and the published default.
- Panel: the warning banner appears, counting the seeded secrets:
  *"N stored credentials are still readable with the key published with Lighthouse…"* with a **Move
  them now** action.

**Half 2 — the move.** This is scenario 13; it is not optional, it is the half that delivers the goal.

- Click **Check secrets** first. Expect every seeded secret reported *on an earlier key*, none
  unreadable.
- Then **Move them now** (or **Rotate key** — run one of each across B1/B2/B3 so both paths get
  exercised).
- Expect the report to name what moved and onto which key id; the warning banner disappears; a
  re-run of **Check secrets** shows everything on the active key.
- Refresh the team again. Still works, nothing re-entered.

**B3 additionally**: the old build kept its data-protection key store at
`/app/data-protection-keys` on the container's writable layer. `KeyStoreMigration` carries it across
to `/app/Data/keys`. Confirm the upgrade does not produce the "Two key rings were found and they are
not the same key" refusal, and that sessions/OAuth state survive the move.

### B4 · Docker with Postgres, upgraded — warning, not refusal

Seed on the old build against Postgres, then start this branch against the same database with
nothing else configured.

**Expect**: it **starts** (unlike A4 — something is stored, so refusing would take it away). Custody
reported as *the key published with the product*. Banner carries `NoDurableKeyStore.Warning`. Panel
shows **no Rotate** and no mint, the custody sentence names the two remedies, and the published-key
count equals the seeded secret count.

**The trap to check**: pressing **Move stored secrets onto the active key** here would move them onto
— the published key, which is the active key. Confirm the panel does not offer a move that
accomplishes nothing, or, if it does, note exactly what it says afterwards. This is the sharpest UX
question in the whole walkthrough.

Then apply a remedy (`Encryption__Key`, or a mounted `Encryption__KeyStorePath`) and confirm the
second start lets the operator complete the move.

---

## Group C — upgrade from `v26.8.14.1` with a custom key

**These are two different starting states.** The old release read
`EncryptionSettings__EncryptionKey`; the old docs advertised `Encryption__Key`, which nothing
consumed. Operators exist in both populations, and they upgrade into opposite outcomes.

### C1 · Seeded with `EncryptionSettings__EncryptionKey` — the name that worked

Seed on `v26.8.14.1` with `EncryptionSettings__EncryptionKey=<key>` set. Secrets are genuinely on that
key. Upgrade with the same variable still set.

**Expect**: works exactly as before. Custody *supplied by configuration*, active key id derived from
their key, no Rotate button. Team refreshes. **Plus** a second banner warning:

> The encryption key is being read from `EncryptionSettings__EncryptionKey`, which this release
> retired. It still works today and will stop being read in a future release. Set the same value as
> `Encryption__Key` and remove the old one.

**Grade**: this warning is shown on the banner only. An operator running as a service or a container
never reads a banner. Should the panel say it too? That is a slice-06 question and this run is where
it gets answered.

### C1b · Follow the nudge verbatim

Set `Encryption__Key` to the same value, remove `EncryptionSettings__EncryptionKey`, restart.

**Expect**: identical active key id to C1, every secret still readable, nudge gone. If the key id
changes or anything becomes unreadable, the nudge's instructions are wrong — a blocking finding.

### C2 · Seeded with `Encryption__Key` — the name the docs advertised

Seed on `v26.8.14.1` with `Encryption__Key=<key>` set. On the old build this was a **no-op**: the
secrets are really on the published default. Upgrade with the same variable still set.

**Expect**: the new build now **reads** it. Custody *supplied by configuration*, active key id is
theirs. Old secrets still readable through the retired published key, so the team still refreshes.
Panel shows the published-key warning with the seeded count — because the secrets are on the
published key, not on theirs.

**Grade**: from the operator's point of view nothing they did changed, yet their custody did. Nothing
in the release notes or the panel currently explains that their configuration *started working*. Does
the panel's warning banner plus the custody line add up to a story they can follow? Note the exact
wording gap.

### C3 · Standalone with a supplied key — feasibility, not correctness

Determine whether an environment variable can even be supplied to the Tauri build in a normal
install, and if so how. If it cannot be done without a terminal, note that: it decides whether the
custody explanation's advice is reachable for standalone users at all.

---

## Group D — durability

### D1 · Docker recreate, volume kept

From A3 or B3: `docker rm -f` the container, `docker run` again with the same volume.

**Expect**: **same** active key id, every secret still readable, nothing minted.

### D2 · Key store off the volume, then recreate — the legibility test

Run the container with the database on a mounted volume but the key store deliberately on the
writable layer:

```bash
-e "Encryption__KeyStorePath=/app/keys"   # and deliberately no -v for /app/keys
```

Store a secret, then `docker rm -f` and recreate.

**Expect**: the key is gone; the database still holds secrets written under it. The generated store
is empty, minting is permitted (explicit path), so a **new** key is minted — and nothing in the
database can be read with it. `RefuseWhenNothingStoredCanBeRead` fires and the instance **refuses to
start**.

**Grade**: this is the failure the whole "key store beside the database" decision exists to prevent,
deliberately provoked. The only acceptable outcome is a refusal that names the cause and the fix. A
silent blank, a crash loop with a stack trace, or a start-with-broken-secrets is a blocking finding.

---

## Observation ledger

Filled in live. Verdict is the operator's, not inferred from code.

| Run | Verdict | Banner said | Panel said | Docs gap | Notes |
|---|---|---|---|---|---|
| A1 standalone fresh | **PASS**, behaviour | `generated for this instance (k-2026-08-16-01) · /home/benjamin/.config/Lighthouse/data-protection-keys` — log only, no terminal | source/active/held/kept-in all correct; `k-legacy-default` also listed under Keys held; Rotate + Move + Check all offered; no published-key warning | panel has no header saying what it is for, and no link to docs | Nothing interrupted the launch. `encryption-keyring.protected` created beside the existing DP key and `oauth-state-secret.protected`. Rotate on an empty instance produced `k-2026-08-16-02` and reported "Moved 0 stored secrets". |
| A1b standalone, real secret on a minted key | **PASS** | — | check: `1 on the active key k-…02`; rotate: `Moved 1 stored secrets onto key k-…03`; check: `1 on the active key k-…03` | — | Added a real connection + team on the dev build, synced. Rotated. Re-synced after the move — works, nothing re-entered. Keys held now **4 chips**. Plural agreement wrong throughout (F-7). |
| A2 binary fresh (SQLite, v26.8.16.7) | **PASS** | `generated for this instance (k-2026-08-16-01) · …/Lighthouse-linux-x64-v26.8.16.7/keys` — **visible in the terminal**, unlike standalone | *generated for this instance*, `k-2026-08-16-01`, 2 chips, Kept in = `…/keys`, no warning | — | Key-store case 3 confirmed: `<app dir>/keys`, not `data-protection-keys`. **Only one key directory exists** — `keys/` holds the ring, the Data Protection key and `oauth-state-secret.protected`; no second folder to confuse an operator or miss in a backup. On the banner: "you don't need to see it, but for debugging/logs it's nice to have" — so F-1 is about the panel carrying the load, not about making the banner louder. |
| A2c binary, key supplied by configuration | **PASS**, behaviour — **Bug #5776 confirmed dead** | `supplied by configuration (k-cfg-afa1daf1) · …/a2c/keys` | *supplied by configuration*, `k-cfg-afa1daf1`, 2 chips, **Rotate absent**, Move + Check only | the rotation instruction is unactionable (F-15) | `Encryption__Key` reaches the code and the id is derived from the value, not random. `keys/` exists and holds the Data Protection key + `oauth-state-secret.protected` but **no `encryption-keyring.protected`** — nothing minted, exactly right. Findings F-14, F-15. |
| A2d binary, key supplied *after* secrets exist | **PASS**, behaviour — refuses correctly | `FATAL: This instance has stored credentials and not one of them can be read with the key it started on…` | — | — | Refused to start, wrote nothing. The reassurance sentence ("Nothing has been changed and nothing is lost") is the strongest copy in the feature — it is what stops a stuck operator deleting their key store. Neither remedy offered is the one that fits: F-16, F-17. **Recovery verified**: removing the variable and restarting brings back `generated for this instance (k-2026-08-16-01)` and the team refreshes — so F-16/F-17 are wording, not blocking. |
| A3 docker, documented one-command install | **FAIL — blocking**, see F-22 | — | — | `server.md:81` + the website's copy button + `configuration.md:165` all carry a command that does not run on Linux | `Access to the path '/app/Data/keys' is denied.` — exit 1, nothing created. **Not a regression**: v26.8.14.1 fails the same command with `SQLite Error 14: 'unable to open database file'`. |
| A3-fix docker, named volume at `/app/data` | **PASS**, on **both** releases | `generated for this instance (k-2026-08-16-01) · /app/data/keys` | — | — | The one-command replacement: no flags, no ownership setup, works on 26.8.14.1 and 26.8.16.7. |
| A4 docker+postgres, shipped `examples/postgres/docker-compose.yml` | **FAIL — blocking**, see F-24 | first start: `the key published with the product (k-legacy-default) · /app/data-protection-keys` + no-durable-store warning. After restart: `FATAL … nothing stored yet, so Lighthouse will not start` | — | the example sets no encryption key and no key-store path | Starts on `docker compose up`, then **crash-loops on the first restart** under `restart: always`. |
| A4b remedy: `Encryption__Key` (Postgres compose) | **PASS** | `supplied by configuration (k-cfg-b5a1749a) · /app/data-protection-keys` | — | — | Starts, survives restart, no crash loop. Custody is operator-owned, so no minting and no Rotate — the Kubernetes experience in Docker. Reinforces F-14 at its worst: *Kept in* names a directory that does not hold the key **and** dies with the container. Probe file `candidate-postgres-suppliedkey.yml`. |
| A4c remedy: `Encryption__KeyStorePath` on a named volume (Postgres compose) | **PASS** — recommended fix | `generated for this instance (k-2026-08-16-01) · /app/data/keys` | — | — | **Same key id across start, `restart`, and a full `down`+`up` recreate.** Custody stays app-owned, so Rotate is offered and Docker-Postgres behaves like Docker-SQLite. Also proves D1 for Postgres. Candidate file `candidate-postgres-compose.yml`. |
| B1 standalone upgrade — half 1 | **PASS** | `generated for this instance (k-2026-08-16-01) · …/data-protection-keys` | 2 chips (`k-2026-08-16-01`, `k-legacy-default`); warning fired: `1 stored credentials are still readable with the key published with Lighthouse…` + *Move them now*; check: `0 on the active key, 1 on an earlier key` | — | Launch normal, no prompt. Team refreshed **before** touching any encryption UI — the credential written by v26.8.14.1 still authenticates. `encryption-keyring.protected` created; the old DP key and `oauth-state-secret.protected` untouched. Layout + duplication findings F-8, F-9. |
| B1 standalone upgrade — half 2 | **PASS** | — | `Moved 1 stored secrets onto key k-2026-08-16-01. 0 could not be read.`; warning replaced by a green success in place; check: `1 on the active key, 0 on an earlier key`; `k-legacy-default` still held | — | Team re-synced after the move — credential travelled from the published key onto the instance's own, nothing re-entered. Maintainer reached for the button at the **bottom**, not the one in the alert. Findings F-10, F-11, F-12. |
| B2 binary upgrade — half 1 (sanctioned: appsettings.json overwritten) | **PASS** | `generated for this instance (k-2026-08-16-01) · …/b2/keys` | 2 chips, warning fired with count 1 + *Move them now*; check: `0 on the active key, 1 on an earlier key` | — | New `appsettings.json` has no `EncryptionSettings` block, so the published literal is gone and the instance mints. Team refreshed before touching the panel. **`KeyStoreMigration` verified on real data**: `key-c75b….xml` and `oauth-state-secret.protected` appear inside the new `keys/` still stamped 15:37, the old build's time — carried across from `data-protection-keys/`, not regenerated. Banner length finding F-18. |
| B2 binary upgrade — half 2 | **PASS** | — | via *Move them now* this time: `Moved 1 stored secrets onto key k-2026-08-16-01. 0 could not be read.`; check: `1 on the active key` | — | Team re-synced after the move. Both entry points to the same action verified across B1 (bottom button) and B2 (alert button). Duplicate key-store finding F-19. |
| B2b binary upgrade, operator kept their own `appsettings.json` | **FAIL — blocking**, see F-20 | `supplied by configuration (k-cfg-27d69a05) · …/b2b/keys` + the retired-name nudge | warning fired with count 1; after *Move*: `Moved 1 stored secrets onto key k-cfg-27d69a05`, **warning disappeared**, check reports `1 on the active key` | `server.md` says to override all files; an operator who keeps their edited config lands here | `k-cfg-27d69a05` is derived from the **published** key's bytes. The instance is pinned to the public key by configuration, cannot mint, has no Rotate button — and the panel now reports it as healthy. |
| B3 docker upgrade (SQLite, named volume, container replaced) | **PASS**, both halves | `generated for this instance (k-2026-08-16-01) · /app/data/keys` | 2 chips, warning with count 1; check: `0 on the active key, 1 on an earlier key`; move: `Moved 1 stored secrets onto key k-2026-08-16-01`; team re-synced | — | First run to survive an actual **container replacement** rather than a file swap. `/app/data-protection-keys` was **gone** — it lived in the old container's writable layer — so `KeyStoreMigration` never fires on Docker and a fresh DP key + `oauth-state-secret.protected` are generated in `/app/data/keys`. Nothing lost: the old encryption key was compiled in, and the state secret only guards in-flight handshakes. See F-25. |
| B4 postgres upgrade (secrets already stored, no custody configured) | **PASS** behaviour, **FAIL** wording (F-27) | `the key published with the product (k-legacy-default) · /app/data-protection-keys` + no-durable-store warning | 1 chip only; no Rotate; custody sentence names both remedies correctly; published-key warning with count 1; after *Move*: `Moved 1 stored secrets onto key k-legacy-default. 0 could not be read.` and **the warning does not clear** | — | Starts rather than refuses, which is right — secrets exist, so refusing would take a working system away. The contrast with B2b is the point: here the published key wears its own name, so the count still sees it and the panel stays honest. |
| C1 binary upgrade, genuine custom key under the retired name | **PASS** behaviour, **FAIL** wording (F-21) | `supplied by configuration (k-cfg-d893325b) · …/c1/keys` + retirement nudge | check before move: `1 on the active key k-cfg-d893325b`; warning simultaneously claimed `1 stored credentials are still readable with the key published with Lighthouse`; move: `Moved 1 stored secrets onto key k-cfg-d893325b`; check after: unchanged | — | **The custom-key population survives the upgrade** — team refreshed before anything was touched. New `appsettings.json` confirmed free of `EncryptionSettings`, so the key really came from the env var. |
| C1b nudge followed verbatim | **PASS** | `supplied by configuration (k-cfg-d893325b) · …/c1/keys`, **nudge gone** | — | — | Set `Encryption__Key` to the same value, dropped `EncryptionSettings__EncryptionKey`. Active key id **unchanged** — the id is a fingerprint of the material, so moving the value between setting names cannot strand a secret. The nudge's instructions are safe to follow. |
| C2 docs name (`Encryption__Key` set on the old build, where it was a no-op) | **SKIPPED**, with reason | — | — | — | Its population is a subset of the custom-key population, which the maintainer assesses as effectively empty ("I don't think many people (if anyone at all) is using a custom encryption key"). Both halves of the mechanism are already covered from other directions: A2c proved `Encryption__Key` now reaches the code, B2b proved the retired name is still honoured. Only the combination is unexercised. An attempt was made and mis-seeded — the recalled command used the retired name, so it ran as a second C1 — and was not rebuilt. |
| C3 standalone supplied key | **N/A**, with reason | — | — | — | A standalone install has no writable configuration: `StandaloneInitializer` reads `appsettings.json` out of the packaged resources directory, which is read-only inside the AppImage / Program Files / .app bundle. The only route is an environment variable set before launch, i.e. starting the app from a terminal — outside the edition's whole premise. C1/C2 are therefore not run for standalone either: neither population can exist there. See F-13. |
| D1 recreate, volume kept | **PASS** | `generated for this instance (k-2026-08-16-01) · /app/data/keys` | — | — | `docker rm -f` + `docker run` against the same volume returns the **same key id**, with `encryption-keyring.protected` untouched (timestamp predates the recreate). Self-asserting: the database holds a secret by this point, so had the key not survived, `RefuseWhenNothingStoredCanBeRead` would have stopped the start. A clean boot is the proof. |
| D2 key store off the volume, then recreate | **PASS** behaviour, **FAIL** recoverability (F-26) | `FATAL: This instance has stored credentials and not one of them can be read with the key it started on…` | — | — | Provoked on purpose: database on a named volume, `Encryption__KeyStorePath=/app/keys` on the writable layer. `docker rm` + recreate destroys the key while the secrets survive. Refuses, names the cause, suggests deleting nothing — all correct. But the key here is **destroyed, not misplaced**, and the message cannot tell the difference. |

---

## Decisions taken on the findings (2026-08-16, maintainer)

**[V1] F-26 gets an escape hatch, shaped like the emergency admin.** A configuration switch — env var
or command line, the same delivery as `Authorization:EmergencySystemAdminSubjects` — that skips
`RefuseWhenNothingStoredCanBeRead` and lets a stuck instance start. The operator then re-enters the
credentials by hand. Following the emergency-admin precedent, it must be *visible* once used: that
setting is surfaced through `SystemInfo` and `RbacStatus` precisely so it cannot sit switched on
unnoticed, and this one needs the same treatment on the encryption panel plus a startup line.

Two things to settle while building it:

- **Saving must actually work in that state.** A guard already stops a save from overwriting a
  credential it cannot read. That guard is right, but it must not also block an operator supplying a
  *new* value — otherwise the hatch opens the door and leaves the room locked.
- The check report already names Connection and field for every unreadable secret, so the panel can
  hand the operator their exact to-do list rather than making them hunt.

**[V5] The custody line is mirrored into the UI system information, System Administrator only**
(maintainer, 2026-08-16). The startup banner is the design's primary custody surface and the entire
standalone population is structurally blind to it — that is F-1, and the encryption panel only half
answers it, because an operator diagnosing an instance goes to system information first.

The reason key state was kept off that response stands: `GET /api/systeminfo` is deliberately
unguarded, because the application shell needs the version and the authentication posture before
anyone is authorised, and a viewer who opens Lighthouse inside an embedded frame satisfies "signed in".
So the field is **served as null to everyone who is not a System Administrator**, and the row is drawn
only when it is present. An embedded-frame viewer learns nothing; an administrator gets the mirror.

The line carries the same shortened custody wording as the banner (see the slice 06a decisions):
custody word, then the path.

**[V6] The same response already leaks `emergencyAdminSubjects`, and that is folded into this epic**
(maintainer, 2026-08-16). It is the identical question — what does a caller who is merely signed in
learn about the security posture of the installation — and it is worse in one respect: the emergency
administrator subjects are real user identities, not a category. It predates this epic and would
otherwise have been raised as its own bug; the maintainer chose to fix it alongside the system
information work rather than leave two halves of one decision in two places.

**[V7] No security advisory. Release notes only** (maintainer, 2026-08-16). This reverses AC-6.7 in
slice 06, which said an advisory publishes when the fixed version is installable. The reasoning: the
key was never withheld. It shipped in `appsettings.json` in every build and sat in the public
repository, which is documented behaviour rather than a defect, and a CVE against the product would
describe the fix rather than a breach. What operators need is the upgrade instruction and the move,
and the release notes carry both. Slice 06 drops the advisory and keeps everything else.

**[V2] F-6: hide unreferenced keys now, offer explicit removal later.** Keeping every key costs
nothing, and the complaint was that four meaningless chips were confusing, not that the keys existed.
So: hide keys nothing references, keep them in the ring. A later explicit *Remove unused keys* action,
gated on a zero-reference check and warning before it executes, is the follow-up — and V1 is what makes
it safe, since an operator who removes a key they still needed for an old backup would otherwise have
no way back in.

**[V3] F-27: suppress the move where it cannot achieve anything.** Where the active key *is* the
published key, do not offer *Move stored secrets onto the active key*, and let the published-key
warning point at the custody sentence above it — which already names both real remedies — instead of at
a button that re-encrypts the published key onto itself.

**[V4] F-22, F-23, F-24 are fixed inside this epic** rather than split into their own bug. They land
naturally with the docs work: the Dockerfile path casing, `server.md`, `configuration.md`, the
`examples/postgres` compose file, and the website's `lighthouseDownloads.ts` copy button.

---

## Findings → slice 06

Anything the walkthrough turns up lands here, sorted by what it changes. Slice 06 is
"say what is true"; this list is its input.

### Wording — banner

**F-1 · A standalone user never sees the banner** (A1, 2026-08-16). **FIXED in slice 06b (Story #5793),
2026-08-16.** The custody line is mirrored onto the system information page, System Administrator only
— which is where somebody diagnosing an instance looks first, and the one surface a standalone operator
actually has. One sentence, rendered in two places, so the console and the page cannot drift apart.
The same change closes the unguarded `emergencyAdminSubjects` on that response.

The custody line is correct and
complete, and it goes to Serilog — into `~/.config/Lighthouse/logs/log-<date>.txt`, a directory the
operator has no reason to know exists. Standalone has no terminal. The banner is the design's primary
custody surface and the entire standalone population is structurally blind to it. Whatever the banner
is load-bearing for has to be reachable from the panel too, or it is not reachable at all for this
edition.

**F-18 · The custody line is long enough to be skimmed past** (B2, 2026-08-16). Observed:
`🔑  Encryption    : generated for this instance (k-2026-08-16-01) · /storage/…/b2/keys`. Maintainer:
"it's a bit long now, could we do something like a single word here and then the path, like
'instance - /storage/…'?" Every other banner line is a label and a short value; this one is a clause.

Worth reconciling with F-17 before acting: the key id is the single most useful thing in that line for
diagnosing a refusal later, so shortening should come out of the custody phrase — `instance`,
`configured`, `mounted secret`, `published key` — rather than out of the id.

### Wording — encryption panel

**FIXED in slice 06a (Story #5791), 2026-08-16 — F-2, F-3, F-4, F-5, F-7, F-8, F-9, F-10, F-11, F-12,
F-14, F-15 and F-18 all close together**, because they were one decision made twice. The panel now
lists only the keys something is stored under, offers the move only where it would achieve something
and never where the key in force is the published key, carries one action with the alert naming it,
drops every zero, agrees in number, reports a rotation as the key it made, names the setting rather
than a directory the key is not in, gives a rotation instruction that can be followed, and shortens the
startup custody line. `docs/settings/encryption.md` is the page it links to. Owed: the A1, A1b, A2c and
B1 runs repeated, and the screenshot regenerated.

**F-2 · The panel does not say what it is for** (A1, 2026-08-16). Maintainer, reading it as a
first-time user: "I would genuinely not understand what I'm seeing." The table opens on *Key source*
with no sentence establishing that this is about the credentials stored in Connections, that they are
encrypted at rest, and that this key is what encrypts them. Wanted: a short header explaining the
subject, and a link to the docs page. Everything below the header is fine once the subject is known.

**F-3 · `k-legacy-default` is listed under Keys held on an install that never had a legacy secret**
(A1, 2026-08-16). Technically true — the published default is appended to every ring as a read-only
retired entry — but on a first install it is at best noise and at worst alarming: a brand-new instance
appears to be holding a key called "legacy-default" for no reason the operator can see. Consider
hiding it where nothing is stored under it, or labelling the chips with their role (active / kept for
reading) rather than listing bare ids.

**F-4 · "Move stored secrets onto the active key" is offered when there is nothing to move**
(A1, 2026-08-16). Maintainer: "should this even have been active? I would assume this button only
appears if we have secrets on an old key and we wanna move it." The button renders unconditionally in
`EncryptionPanel.tsx`, and on a clean instance it reports `Moved 0 stored secrets onto key k-…`.
The code already holds the principle this violates — the comment above `summaryOf` says a summary
counting what moved "would greet an operator with 'Moved 0' on a perfectly healthy instance", which is
why *Check* was given its own vocabulary. The same reasoning applies to offering the action at all.

Worse in configuration custody (A2c): because `canMint` is false the Move button is promoted to the
filled, primary style, so on a fresh instance holding **zero** secrets the single most prominent
control on the page is an action with nothing to act on.

**F-7 · The report sentences do not agree in number** (A1b, 2026-08-16). Observed verbatim:
`Checked 1 stored secrets.` and `Moved 1 stored secrets onto key k-2026-08-16-03.` Both summaries in
`summaryOf` interpolate a count straight into a hardcoded plural. Singular is not a rare case here —
one connection with one secret field is the smallest real instance there is, and it is what a first-time
operator has.

Confirmed again in B1 on the warning banner, which is a different string with the same defect:
`1 stored credentials are still readable with the key published with Lighthouse`. This is the worst
place for it — that sentence is the one piece of copy an upgrading operator is most likely to read, and
quite possibly the only one.

**F-8 · The warning banner's action button wraps to three lines** (B1, 2026-08-16). *Move them now*
renders as `Move` / `them` / `now` stacked vertically at the right edge. The MUI `Alert action` slot
gets whatever width the two-line message leaves it, and this message is long. Visible in the B1
screenshot; it makes the call to action look like a rendering accident.

**F-9 · The same action is offered twice on one screen, and the wrong one looks primary** (B1,
2026-08-16). In the upgraded state the panel shows *Move them now* inside the warning and *Move stored
secrets onto the active key* a hundred pixels below it. Both call `reEncryptSecrets`; they are the same
thing under two names. Meanwhile the only filled, primary-styled button on the screen is **Rotate
key** — which is not what this instance needs, mints yet another key, and would leave the operator with
a three-key ring for no reason. The visual hierarchy points away from the action the warning just
asked for.

**F-10 · The published-key warning is too long to act on** (B1, 2026-08-16). Two sentences of
explanation before the reader learns what to do. Maintainer's own shape: *"You have credentials
encrypted with an old key, please move them to an active key"* — the state, then the action, and
nothing else. Why the published key is bad belongs behind the docs link (F-2), not in the alert. Note
the alert is also the second place the wrong plural shows up (F-7).

**F-11 · The alert should point at the action, not carry its own copy of it** (B1, 2026-08-16).
Maintainer, unprompted and before being asked which button he would use: "I would not add a button to
the alarm, but point to the button on the bottom" — and that is the one he reached for. This is the
resolution for F-9: keep one action, in the button row, and let the alert name it rather than
duplicate it.

**F-12 · Drop every zero from the report sentences** (B1, 2026-08-16). Observed:
`Checked 1 stored secrets. 1 on the active key k-2026-08-16-01, 0 on an earlier key, 0 never
encrypted, 0 could not be read.` Maintainer, twice in one run: "the 0's I would skip in all the
messages. I don't need to know that 0 could not be read or 0 are on old keys, just skip it. It's
interesting if it's > 0 as then there is potentially something I need to do."

This is the strongest signal of the session because it is a rule, not a rewrite: a count of zero is
not information, it is four categories of nothing competing for attention with the one number that
matters. A healthy instance should read `Checked 1 stored secret. It is on the active key
k-2026-08-16-01.` and a sick one should read the same plus only the categories that are non-zero.
Applies to both summaries in `summaryOf`.

**F-14 · "Kept in" names a directory the key is not kept in** (A2c, 2026-08-16). With custody
*supplied by configuration* the panel still shows `Kept in …/a2c/keys`. The key is not there and never
will be — it lives in the environment. What makes this actively dangerous rather than merely untidy is
that the directory **exists and is full of key-shaped files**: the Data Protection key and
`oauth-state-secret.protected` are both in it. An operator who backs that folder up alongside the
database has every reason to believe they have taken their encryption key with them, and they have
not. Restoring that pair onto a host without the environment variable yields a database whose
credentials cannot be read.

The row should say where the key came from in this mode — the setting name — or be replaced by a
backup-oriented sentence that is true for the custody in force.

**F-15 · The rotation instruction cannot be followed** (A2c, 2026-08-16). Verbatim: *"To replace it,
put the new key ahead of the old one in `Encryption__Keys`, start Lighthouse again, and then move the
stored secrets onto it."* Everything an operator needs to actually type is missing:

- It names `Encryption__Keys`, which is **not** the variable they set (`Encryption__Key`). Nothing says
  both exist, that the plural takes a ring, or that the plural wins when both are present. Two names
  differing by one character, with different grammars, is exactly the confusion this epic was created
  to end.
- The ring grammar is never given. Per `KeyRingSerializer` it is comma-separated entries, each either
  bare base64 or `name:base64`, first entry active. "Ahead of" implies the ordering but not the
  separator, and an operator guessing a newline or a semicolon gets a startup refusal.
- It does not say what to do with the old `Encryption__Key` afterwards, or that leaving it set is
  harmless because the plural is read first.

This sentence is the only rotation procedure this population is ever given — and per the same wording
in `SuppliedByExternalSecret`, it is what every Kubernetes operator will read too once slice 05 lands.
It needs the concrete grammar, an example ring, and a docs link.

**F-16 · The refusal never offers the remedy that actually fits: undo what you just set** (A2d,
2026-08-16). **FIXED in slice 05b (Story #5790), 2026-08-16** — together with F-17: the refusal now
leads with removing the key that was just set where there is something to remove, names both key ids,
stops asserting that nothing is lost, and names the way past itself. Owed: the A2d run repeated.
Full text observed:

> FATAL: This instance has stored credentials and not one of them can be read with the key it started
> on, so this is not the key they were written under. Nothing has been changed and nothing is lost -
> the credentials are still there, encrypted under the key they were written with. Set
> `Encryption__Key` to the key this instance was using before, or set `Encryption__KeyStorePath` to the
> key store that belongs to this database, and start Lighthouse again.

Both remedies assume the operator **lost** a key. The far likelier cause — and the one actually
reproduced here — is that they **added** one: an instance that had been happily minting for months
gets `Encryption__Key` set for the first time, and the configured key displaces the minted key out of
the ring. In that state the key store is already correct and already present; `Encryption__KeyStorePath`
is a no-op, and "set `Encryption__Key` to the key this instance was using before" asks for a value that
was never written down anywhere, because Lighthouse generated it and kept it in a file.

The one instruction that works — *remove the `Encryption__Key` you just set and start again* — is the
only one missing. Worth stating first, because it is both the most likely cause and the only remedy an
operator can carry out unaided.

**F-17 · The refusal knows which keys are involved and does not say** (A2d, 2026-08-16). It says "this
is not the key they were written under" without naming either side. Both are known: the ring reports
its active key id, and every stored envelope carries the id of the key that wrote it — that is exactly
what the check pass reads. "Your stored credentials were written under `k-2026-08-16-01`; this
instance started on `k-cfg-9f2ab1c4`" turns a puzzle into a diagnosis, and costs nothing in secrecy:
key **ids** are already displayed on the encryption panel, and no key material is involved.

**F-5 · Rotate on an empty instance reports in move-vocabulary** (A1, 2026-08-16). Rotating with
nothing stored is legitimate and should stay available, but its report reads `Moved 0 stored secrets
onto key k-2026-08-16-02. 0 could not be read.` — which says nothing about the thing that actually
happened, namely that a new key was minted and made active. Rotation's success sentence should lead
with the new key, and mention moved secrets only when there were any.

### Docs — `configuration.md`

**F-28 · F-24 confirmed in the wild, on a maintainer's own deployment** (2026-08-17). A Compose stack on
Postgres — same shape as the shipped example, a named volume at `/app/data`, no `Encryption__Key` and no
`Encryption__KeyStorePath` — upgraded to `26.8.17.16`, migrated cleanly, started, served, and reported
`🔑 Encryption : published key · /app/data-protection-keys` with the no-durable-store warning. It did not
refuse, because it holds two credentials, so the probe answers *HoldsSome* rather than *HoldsNone*. That
is the F-24 crash-loop's quieter sibling and the more likely one for an existing user: **it keeps
working, on the published key, indefinitely, for old and new credentials alike**.

The remedy verified on that instance, and the one the documentation should lead with:
`Encryption__KeyStorePath: /app/data/keys` on the existing named volume. One line, one restart. The
banner then reads `instance · /app/data/keys`, the key store contains
`encryption-keyring.protected` + the key XML at mode `0600` owned by `app`, and a `--force-recreate`
leaves the same key id in place. **Move stored secrets** appears once there is a destination key.

**F-29 · The published-key warning tells the operator to press a button that is not offered.** On that
same instance the amber banner read *"2 stored credentials are still encrypted with the key published
with Lighthouse. Move them onto this instance's own key — nothing has to be re-entered."* while the
panel offered only *Check secrets*. Suppressing the move there is correct and deliberate (decision V3 —
there is nowhere to move to), but the sentence beside it was not updated to match, so it reads as an
instruction that cannot be followed. The maintainer's first question on seeing the screen was "I can
check but I cannot move the secrets?", which is the defect stated exactly. The wording must point at the
custody sentence above it — the one that names the two settings — instead of at a move.

### Docs — `server.md` / `standalone.md`

**F-13 · A standalone install cannot be given a key, and the panel offers advice that assumes it can**
(C3, 2026-08-16). `StandaloneInitializer` loads `appsettings.json` from the packaged resources
directory — read-only inside an AppImage, Program Files or an `.app` bundle — so the only way to supply
`Encryption__Key` is an environment variable set before launch, from a terminal. For this edition that
is not a supported gesture.

This is not a defect: standalone mints its own key, which is the right answer and needs no operator
input. It matters for two other reasons. First, `standalone.md` should say where the key lives
(`~/.config/Lighthouse/data-protection-keys` on Linux, the equivalent app-data directory elsewhere) and
that backing up the database without that directory produces a backup whose credentials cannot be
read — today the page says nothing about either. Second, `WHO_OWNS_THE_KEY.SuppliedByConfiguration`
and the two `NoDurableKeyStore` remedies all instruct the reader to set `Encryption__Key`; none of
those states is reachable in standalone, so no standalone user should ever be shown that text — worth
confirming when the server and Docker runs exercise those states for real.

### Release notes / upgrade guidance

_(to be filled)_

### Behaviour defects (not wording)

**F-22 · BLOCKING · The documented one-command Docker install does not run on Linux — on either
release** (A3, 2026-08-16). The command in `docs/Installation/server.md:81`, duplicated byte-for-byte
in the website's copy-to-clipboard button (`website/src/lib/lighthouseDownloads.ts:10`) and again at
`configuration.md:165`, bind-mounts a host directory at `/app/Data`. The container runs as
`uid=1654(app)`; a host directory created by an ordinary user is `uid=1000`, mode 755. The container
cannot write into it.

- **v26.8.16.7**: `FATAL: Access to the path '/app/Data/keys' is denied.`
- **v26.8.14.1**: `FATAL: SQLite Error 14: 'unable to open database file'.`

So this is **pre-existing, not caused by this epic** — the epic only made it fail earlier and with a
clearer message. It is invisible on Docker Desktop for macOS/Windows, whose file-sharing layer
translates ownership, which is presumably why it has gone unreported. Linux hosts hit it every time.

*Verified fix, one command, no flags, both releases:* mount a **named volume** at the lowercase
`/app/data` — the path the image already prepares and chowns:

```bash
docker run -d -p 8081:443 -p 8080:80 \
  -v lighthouse-data:/app/data \
  -e "Database__ConnectionString=Data Source=/app/data/LighthouseAppContext.db" \
  ghcr.io/letpeoplework/lighthouse:latest
```

Docker initialises a named volume from the image path's contents *and ownership*, so `app` owns it and
there is nothing for the operator to do. Verified green on 26.8.14.1 and 26.8.16.7.

*Rejected alternative:* bind mount plus `--user $(id -u):$(id -g)` works on 26.8.16.7 but **fails on
26.8.14.1** with `Access to the path '/app/data-protection-keys' is denied` — the old build kept its
key store in a root-prepared image path. Worth noting that this epic moving the key store beside the
database is exactly what makes running as an arbitrary uid viable at all. It is also not portable:
`$(id -u)` has no PowerShell equivalent for a copy-paste command.

*Consequence for the backup story.* A named volume takes the data off the host filesystem, which
changes the advice in F-13/F-19 — but arguably for the better here, since the database and the key end
up in one volume and cannot be backed up separately by accident.

**F-23 · The image prepares `/app/data` and every document mounts `/app/Data`** (A3, 2026-08-16).
`Dockerfile:61` runs `mkdir -p /app/logs /app/data && chown -R app:app /app/logs /app/data`. Every doc,
the website command and the compose examples mount **`/app/Data`**, capital D. On Linux those are two
different directories, so the one directory the image goes out of its way to prepare is never the one
used. Fixing this is what makes F-22's named-volume command work without any further ceremony.

**F-24 · BLOCKING · The shipped Postgres compose example starts once, then crash-loops forever**
(A4, 2026-08-16). `examples/postgres/docker-compose.yml` — linked from `configuration.md:173` and
pinned to `dev-latest`, so its users are already on the new build — sets `Database__Provider: postgres`,
mounts only `./logs`, and sets **no `Encryption__Key` and no `Encryption__KeyStorePath`**. That is
key-store case 4: no durable store, minting forbidden.

Observed sequence, verbatim:

1. `docker compose up -d` → **starts**, reporting `the key published with the product
   (k-legacy-default)` plus the no-durable-store warning. It starts because the Postgres schema does
   not exist yet at bootstrap, so the stored-secret presence probe cannot answer, and *cannot tell*
   deliberately warns rather than refuses.
2. Any restart — `docker compose restart`, a host reboot, a `down`/`up` — now finds the schema present
   and empty, which is `HoldsNone`, and **refuses**: `FATAL: … it has nothing stored yet, so Lighthouse
   will not start on the key published with the product.`
3. `restart: always` turns that into an endless crash loop repeating the same FATAL.

The three-valued probe is right and the refusal is right. The defect is that the two of them together
make a shipped example that appears to work and then bricks — and the window in which it bricks is
exactly the first-run window, before the operator has configured a connection. Configure a connection
first and stored secrets exist, so every later start warns and runs; restart before configuring
anything and the stack will not come back.

Worse, step 1 is the only moment the operator is told anything, and it is a log line in a container
they have no reason to be tailing. By the time they see a message they can act on, the application will
not start.

*Fix, verified both ways (A4b, A4c).* The example must supply custody, and the choice is not cosmetic
— it decides what product a Docker-Postgres operator gets:

- **`Encryption__KeyStorePath: /app/data/keys` on a named volume** — custody stays app-owned. Lighthouse
  mints, the panel offers **Rotate key**, and Docker-Postgres behaves exactly like Docker-SQLite.
  Verified: identical key id across start, restart and a full `down`+`up` recreate. **Recommended**,
  and written up as `candidate-postgres-compose.yml` beside this document.
- **`Encryption__Key`** — custody becomes operator-owned. No minting, no Rotate, rotation is a manual
  ring edit. That is the Kubernetes experience, and it is the right answer *there* because a pod may
  not mint; importing it into Docker splits the Docker product in two for no forced reason.

The key store must sit **under `/app/data`**, not at a path of its own: a bind mount would be
host-owned and unwritable by uid 1654 (F-22), and a named volume at a path the image does not prepare
is created root-owned. `/app/data` is the one directory `Dockerfile:61` creates and chowns — which is
also why F-23's casing mismatch has to be fixed for any of this to be a one-liner.

The same treatment is owed to `docs/Installation/configuration.md`, where the Postgres setup is
described, because an operator writing their own compose file from that page lands in the identical
trap.

**F-20 · BLOCKING · An operator can be pinned to the published key and then told they are safe**
(B2b, 2026-08-16). Observed end to end, not inferred.

**FIXED in slice 04b (Story #5789), 2026-08-16.** Supplied key material equal to the published key is
refused as the active key at the one moment it can still be said, in the one parser every transport
funnels through, and the refusal names the setting that carried it. Owed before the slice closes: the
B2b run repeated on the same substrate.

*How it is reached.* `server.md` tells operators to upgrade by copying the new files over the old
folder, overriding everything. Anyone who has edited `appsettings.json` — a port, a certificate path, a
Postgres connection string — keeps or merges their own copy instead. That file still carries the
pre-epic `EncryptionSettings.EncryptionKey`, whose value is the literal published key from the public
repository. On the upgraded build the retired-name branch honours it, so the published key becomes the
**active** key, under a fresh id derived from its bytes (`k-cfg-27d69a05`). Custody reads *supplied by
configuration*: no minting, no Rotate button.

*Why it is silent.* `PublishedKeySecretCount` decides the question by envelope prefix — either
`LH1.k-legacy-default.` or no envelope at all. Nothing in the codebase compares key **material**;
every reference to `LegacyDefaultEncryptionKey` matches on the id. So the published key wearing a
`k-cfg-` id is invisible to the one check that exists to catch exactly this.

*The sequence, verbatim.* The panel correctly warned `1 stored credentials are still readable with the
key published with Lighthouse… Moving them onto this instance's own key is the fix`. Moving reported
`Moved 1 stored secrets onto key k-cfg-27d69a05`. **The warning then disappeared** and the check
reported `1 on the active key`. The credential is now encrypted with bytes that ship inside every copy
of Lighthouse, and every surface says the instance is healthy.

This is worse than not warning at all. The operator was told they were exposed, took the offered
remedy, and was told they were fixed — so the one prompt that would ever have made them look again is
spent, and nothing will raise it a second time.

*The retirement nudge makes it worse.* It says "Set the same value as `Encryption__Key` and remove the
old one." Followed faithfully, that pins the published key under the current setting name — and the
nudge stops firing, because the name is now correct. The last remaining signal is removed by obeying
it.

*Reachable other ways.* Any operator who copies the old `EncryptionSettings` value into
`Encryption__Key` or `Encryption__Keys` — a reasonable reading of "keep using my existing key" — lands
in the identical state.

*Fix.* Compare supplied key material against the published key at bootstrap and refuse to accept it as
an active key, naming what happened and what to do. The material is compiled in and the comparison is
constant-time over 32 bytes, so this costs nothing. Everything else about the state is already correct;
only the identity check is missing. Whether the same guard should also reject it as a *retired* entry
is a separate question — it must stay readable, so it should not.

**F-21 · LOW PRIORITY · The published-key notice fires for operators who were never on the published
key — and the correct answer is already on the same screen** (C1, 2026-08-16). Downgraded on the
maintainer's assessment that the custom-key population is effectively empty, and because nothing
breaks: the instance works, and taking the wrongly-motivated remedy is still an improvement (it moves
the value from unauthenticated AES-CBC onto an authenticated envelope under the operator's own key).
Recorded in full because the underlying gap is shared with F-20, which is not low priority.

**FIXED in slice 04b (Story #5789), 2026-08-16.** The count stops deciding by envelope shape and asks
the published key whether it can read the value. The narrowing predicate stays in SQL, so an instance
that has moved everything still decrypts nothing. The sentence the panel draws from that count belongs
to slice 06a; this closes the number behind it. Owed before the slice closes: the C1 run repeated.

An install that had a genuine custom key set under `EncryptionSettings__EncryptionKey` upgrades cleanly and keeps working. It is then told:
`1 stored credentials are still readable with the key published with Lighthouse, which anyone who has
a copy of Lighthouse can obtain.` That is false. The credential was written under the operator's own
key and the published key has never been able to read it.

Cause: `PublishedKeySecretCount` counts any stored value that lacks the `LH1.` envelope prefix, on the
reasoning that a pre-epic value can only have been written under the published key. True for the
default install, false for exactly the population that did the right thing.

What makes this cheap to fix is that **the check pass already knows**. Run before the move, it
reported `1 on the active key k-cfg-d893325b` — because it decrypts, so it names the key that actually
read the value. So the panel simultaneously displayed a warning saying the secret is on the published
key and a report saying it is on the active key, about the same single credential, six inches apart.
The notice should be derived from the same knowledge the check uses, or at minimum must not claim the
published key for a legacy value that a non-published key reads.

Relationship to F-20: same missing distinction, opposite direction. F-20 says *safe* when the operator
is not; F-21 says *exposed* when they are safe. Both follow from reasoning about envelope shape rather
than about keys. Note the check pass does **not** rescue F-20 — it labels by which ring key read the
value, and in F-20 that key carries a `k-cfg-` id while holding published material. Only a material
comparison closes that one.

*Second-order cost.* An operator who knows their own setup reads this sentence, knows it is wrong, and
concludes the encryption panel cannot be trusted — which is expensive for a feature whose entire job is
to be believed about exactly this question.

**F-27 · In no-durable-store custody the panel offers a move that cannot achieve anything** (B4,
2026-08-16). An upgraded Postgres instance with no custody configured runs on the published key. The
panel then shows the published-key warning — *"Moving them onto this instance's own key is the fix"* —
on an instance that **has no own key**: the active key is `k-legacy-default`, and *Move stored secrets
onto the active key* is the filled primary button, because `canMint` is false.

Pressing it reports `Moved 1 stored secrets onto key k-legacy-default. 0 could not be read.` and the
orange warning **stays exactly as it was**, since a value re-encrypted under `k-legacy-default` still
carries the prefix the counter matches. The operator is left looking at a green success and an
unchanged warning at the same time, and can repeat this forever.

The behaviour is right and the honesty is right — this is precisely where B2b lied, and the only
difference is that here the published key wears its own name instead of a `k-cfg-` disguise. What is
wrong is offering the action at all. In this custody the fix is not a button: it is the custody
sentence directly above, which already names both remedies correctly. Wanted: suppress the move where
the active key *is* the published key, and let the warning point at that sentence instead of at an
action that cannot help. Same root as F-4 (offering a move with nothing to move) and F-11 (the alert
carrying its own copy of an action).

**F-26 · A lost key leaves an instance that can never start again, and no supported way out**
(D2, 2026-08-16).

**FIXED in slice 05b (Story #5790), 2026-08-16.** `Encryption__StartEvenIfNothingStoredCanBeRead` lets
such an instance start, says so on every start and on the encryption panel for as long as it is set,
and the check pass hands over the list of Connections and fields to re-enter. Owed before the slice
closes: the D2 run repeated end to end.

With the database on a volume and the key store on the container's writable layer, a
recreate destroys the key and keeps the secrets. The refusal is correct and its restraint is right — it
never suggests deleting anything. Two things about it are not right for this case:

1. **The reassurance is false here.** "Nothing has been changed and nothing is lost - the credentials
   are still there, encrypted under the key they were written with." True of A2d, where the operator
   merely pointed at the wrong key and recovery was one variable away. In D2 the key that wrote them no
   longer exists anywhere, so the credentials are lost in every sense that matters to their owner. The
   message is identical in both cases because the application cannot tell them apart — but it can stop
   asserting the reassuring one as fact.
2. **Neither remedy is reachable, and no third one is offered.** "Set `Encryption__Key` to the key this
   instance was using before" asks for a value Lighthouse generated and never showed anyone. "Set
   `Encryption__KeyStorePath` to the key store that belongs to this database" names a directory that
   was destroyed. There is no instruction for the case where the key is genuinely gone.

*And there is no escape hatch.* Nothing in the codebase can start an instance whose stored secrets are
all unreadable — `RefuseWhenNothingStoredCanBeRead` has no flag, no override, no confirmation path
(only two references exist, the throw and the message). Pointing the instance at a fresh key store does
not help either: the database still holds unreadable secrets, so it refuses again. The operator's only
route back is manual surgery on the secret columns of their own database, which is undocumented and
requires knowing which three columns they are.

So the outcome of a lost key is a permanently unstartable instance, including all the data that has
nothing to do with encryption — teams, forecasts, history. Wanted: an explicit, deliberately
uncomfortable path that discards the unreadable secrets and starts, telling the operator exactly which
Connections must have their credentials re-entered. The check pass already produces that list.

**F-25 · A Docker upgrade silently rotates the Data Protection ring, and the key-store migration can
never run there** (B3, 2026-08-16). v26.8.14.1 kept its Data Protection keys in
`/app/data-protection-keys` — the container's writable layer, not a volume. Upgrading Docker means
replacing the container, which destroys that layer. By the time the new image starts, the legacy
directory does not exist, so `KeyStoreMigration` has nothing to find: it is effectively binary- and
standalone-only.

Observed: the old `key-f574a5ec….xml` and its `oauth-state-secret.protected` vanished, and a fresh
`key-88247a2d….xml` plus a fresh state secret appeared in `/app/data/keys`.

**Nothing is lost today**, and that is worth stating plainly so nobody 'fixes' it in a hurry: the old
encryption key was compiled into the product rather than kept in that folder, and the OAuth state
secret only protects in-flight handshakes. The reason to record it is forward-looking — the moment
anything else is persisted under Data Protection outside `/app/data`, a Docker upgrade will discard it
with no message. This epic's own ring would have been exactly such a thing had it not been placed
beside the database.

**F-19 · An upgraded binary install ends up with the same key material in two directories** (B2,
2026-08-16). `KeyStoreMigration` copies rather than moves, deliberately — removing the source would be
irreversible if the migration picked wrongly. The consequence on a real upgrade is that
`data-protection-keys/` and `keys/` both survive, holding byte-identical copies of the Data Protection
key and `oauth-state-secret.protected`, with `data-protection-keys/` now dead.

Nothing on disk or on screen distinguishes them. An operator writing a backup script, or tidying up,
has a 50 % chance of keeping the wrong one — and the wrong one looks exactly as plausible, since it is
the directory that has been there for years and is the one every older doc and forum post names.

Not a defect, and copying is the right default. But the leftover should either be named as safe to
delete somewhere the operator will look, or marked on disk — a short `README` or a rename to something
visibly retired — so the live one is identifiable without reading source. Note this does not arise for
standalone, where both names resolve to the same configured directory (A1).

**F-6 · Nothing ever leaves the ring, and there is no surface that would remove it** (A1,
2026-08-16). After one rotation on an empty instance, Keys held reads
`k-2026-08-16-02  k-2026-08-16-01  k-legacy-default`. `k-2026-08-16-01` never encrypted anything — it
was minted and superseded seconds later — and it will be listed forever. Maintainer: "why is the old
stuff there? Should it not be cleaned if no secret is using it anymore?"

Additive-only rotation is deliberate and right: it is what makes a rotation unable to strand a secret.
But the other half of that decision was never built. D3 says the published default "can be dropped
from the ring only once a rotation has left no row referencing it" — and no code drops anything, from
any position, ever. The check pass already computes exactly the fact the drop would need (which key
each stored secret is on), so the missing piece is the action and its guard, not the knowledge.

Consequences, in order of how much they matter: an operator who rotates monthly accumulates an
unbounded list of key ids they cannot interpret or clean up; a key that leaked stays in the ring and
stays able to decrypt anything still on it; and the panel's Keys held becomes less readable with every
rotation. Wanted: a way to drop keys that no stored secret references, offered only when the check
proves the count is zero.

---

## The 2026-08-18 re-runs

All nine runs the slice verdicts owed have been run against this branch and are written up in
`run-log-2026-08-18.md`, with their substrates, verdicts and evidence: **A2c, A2d, C1, B2b, D2**, the
**slice 05 cluster** checks on a throwaway `kind` cluster, and the standalone **A1, A1b, B1** on the
AppImage built by run 32104826180.

Everything the earlier runs raised as blocking is fixed and was re-proved end to end. Nothing found on
this pass is a behaviour defect. The new finding ids live in that file so nothing here is renumbered:

- **F-30** — `Kept in` still names a directory the key is not in, in the very custody F-14 was about.
- **F-31** — the panel offers a move that Check secrets says is unnecessary; the real difference is the
  storage format and it is nowhere on screen.
- **F-32** — under the retired setting name the rotation instruction tells the operator to keep using
  it, and the retirement nudge is still banner-only.
- **F-33** — the published-key refusal names `EncryptionSettings:EncryptionKey`, the internal
  configuration path, whichever way the value was supplied. Slice 04b's own criterion asked for the
  `__` spelling an operator actually sets.
- **F-34** — the re-entry script does not terminate on the substrate that produces the failure: the key
  store has to move onto the volume before the credentials are typed back in.
- **F-35** — the Kubernetes reload is documented as thirty seconds and measured at seventy, because
  kubelet's own projection of the changed Secret comes first.

Two runs stayed unexercised for stated reasons: **Tenant Zero** (the cluster assertions were run on
`kind` instead; the tenant run needs the maintainer) and the **rendered standalone panel** (the
standalone sidecar serves no SPA, so only the API behind the panel could be graded from outside the
Tauri window).
