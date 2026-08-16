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
| A1 standalone fresh | | | | | |
| A2 binary fresh | | | | | |
| A3 docker+volume fresh | | | | | |
| A4 docker+postgres fresh | | | | | |
| A4b remedy: key | | | | | |
| A4c remedy: key store path | | | | | |
| B1 standalone upgrade | | | | | |
| B2 binary upgrade | | | | | |
| B3 docker upgrade | | | | | |
| B4 postgres upgrade | | | | | |
| C1 old name | | | | | |
| C1b nudge followed | | | | | |
| C2 docs name | | | | | |
| C3 standalone supplied key | | | | | |
| D1 recreate, volume kept | | | | | |
| D2 key store off volume | | | | | |

---

## Findings → slice 06

Anything the walkthrough turns up lands here, sorted by what it changes. Slice 06 is
"say what is true"; this list is its input.

### Wording — banner

_(to be filled)_

### Wording — encryption panel

_(to be filled)_

### Docs — `configuration.md`

_(to be filled)_

### Docs — `server.md` / `standalone.md`

_(to be filled)_

### Release notes / upgrade guidance

_(to be filled)_

### Behaviour defects (not wording)

_(to be filled)_
