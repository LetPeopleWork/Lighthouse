# Slice 02 — This install's key belongs to this install

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5024, carrying Bug #5776 · **Story**: US-02 · **Estimate**: ~6h
**Reference class**: `Program.cs:429-486` (`EnsureOAuthStateSecret`) — first-boot generation, Data
Protection wrapping, and stable resolution across restarts are already solved there and in production.
This slice applies the same shape to the encryption key. Story #5024 asks for exactly this.

## Goal

An instance that starts for the first time with no operator-supplied key generates one for itself, and
an instance upgrading from the published default keeps reading every secret it already had.

## IN scope

- First-boot generation: 32 cryptographically random bytes, wrapped with ASP.NET Data Protection,
  persisted to the existing key-store directory. Resolved identically on every later start.
- Precedence: an operator-supplied key wins over a generated one, and is reported as such. **This is
  where Bug #5776 is fixed** (D7 retired 2026-08-15). Today the documentation advertises one
  configuration name and `CryptoService` reads another, so the documented override silently does
  nothing and the operator stays on the published default believing otherwise. The ring parser gives
  the setting one name across all three transports, and a supplied key that cannot be used stops
  startup instead of being ignored — AC-2.3, AC-2.8 and AC-2.9 between them. No alias is introduced,
  so no second name has to be honoured forever.
- **The literal key is removed from `appsettings.json`** and enters the ring as a retired entry, so an
  upgrading instance reads its existing secrets while writing new ones under its own key.
- Key source and active key id on the startup log, and on Settings → System reading from the
  System-Admin-guarded encryption endpoint — **not** from `GET /systeminfo`, which is `[Authorize]`
  only and so reaches every embed viewer (DESIGN F-4). Never the key material.
- **The key store resolves beside the database by default**, and the resolved path appears in the
  startup line. Today it defaults to `ContentRootPath/data-protection-keys` — `/app/data-protection-keys`
  in the container — while the documented Docker setup mounts a volume at `/app/Data` and puts the
  database there. Generating a key into the container's writable layer would mean `docker rm` and
  recreate hands the operator their database with a new key and every secret unreadable: a worse
  failure than the one this epic fixes, landing on people who changed no setting. Anyone who already
  mounted their data volume keeps their key by doing nothing.
- Fail fast when a key store exists and cannot be read — no silent regeneration, because a fresh key on
  an existing database looks like a successful boot and orphans every secret in it.
- **Refuse to mint where durability cannot be argued** (DESIGN F-3, ADR-149). The standalone launcher
  already colocates key store and database, so the Docker case is the one this fixes; a hand-rolled
  Postgres install has no local database file to sit beside. An existing instance in that position
  keeps running on the legacy key and says so loudly; a fresh one refuses to start with two one-line
  remedies. Never mint a key you cannot promise to still have tomorrow.
- A supplied key that is not 32 bytes of base64 stops startup and says what is wrong with it.

## OUT of scope

- Rotation (slice 03). Nothing here re-encrypts anything; the retired default stays readable
  indefinitely until an operator rotates.
- Kubernetes custody (slice 05).
- Exporting or backing up the generated key (OQ-3, deferred).

## Learning hypothesis

**Disproves** "the upgrade is invisible" if any existing install loses access to a single secret when
the default key moves from configuration into the ring as a retired entry. The falsifying case is real
data: an instance whose secrets were written months ago under the published key.
**Confirms**, if it holds, that the epic's headline outcome — every install on its own key — costs the
operator nothing and can be released without an upgrade note demanding action.

## Acceptance criteria

AC-2.1 through AC-2.11 in `feature-delta.md`. The three that carry the slice:

- **AC-2.5** — an instance upgrading from the published default reads every existing secret, with no
  user action and no credential re-entry.
- **AC-2.7** — an unreadable key store stops startup rather than generating a replacement.
- **AC-2.11** — recreating the container against an existing mounted data directory reads the same key
  and every stored secret stays readable.

## Dependencies

- Slice 01 landed: the ring and the envelope exist, and a failed read is a real failure.
- The documentation change for the configuration name ships with this slice, since the name only
  becomes true here.
- `:5169` restored from a real backup, upgraded in place from a pre-epic build. Demo data cannot prove
  AC-2.5, because demo secrets were written by the same build that reads them.

## Dogfood moment

Same day: take a copy of the restored database written under the published key, start the new build
against it, confirm every Connection syncs without touching anything, then confirm a newly saved
secret lands on the instance's own key id. Then run the documented Docker command with its data
volume, `docker rm` the container, recreate it, and confirm every secret is still readable.

## Pre-slice SPIKE

None. `EnsureOAuthStateSecret` is the working precedent.

## Verdict

**Confirmed, on real data. The upgrade is invisible.** Nothing was disproved, and the hypothesis had a
falsifying case available: a backup written by a pre-epic build, restored and upgraded in place.

**The restored backup.** `DB_Backup/LighthouseAppContext.db` — 5 connections, 4 stored secrets, no OAuth
credentials — was copied outside the working tree and the new build was started against the copy with a
key store path of its own. It resolved a key it made for itself, `k-2026-08-15-01`, on a ring of
`[k-2026-08-15-01, k-legacy-default]`. All four secrets written months ago read back: not one of them
came out unreadable, so no operator upgrading from a pre-epic build is asked to retype anything. They
read as `LegacyCbc` rather than `Envelope`, which is the finding worth writing down — the retired entry
that keeps them readable is the published default doing exactly the job the slice put it on the ring
for, and nothing rewrote them in place. A credential saved after the upgrade landed under
`k-2026-08-15-01`, the instance's own key, not the published one. AC-2.5 holds against data the build
reading it did not write.

**The container recreate.** The image was built from this commit's tree and run with the documented
command and its data volume. The key store resolved to `/app/Data/keys` — on the volume, beside the
database, which is the change this slice exists for; before it, the key would have gone into the
container's writable layer. A credential was saved, the container was `docker rm`-ed, and a new one was
created against the same volume: same active key `k-2026-08-15-01`, and the stored secret still read.
That the id matched is weak evidence on its own, since the id is derived from the date it was minted;
the load-bearing part is the secret reading, because an envelope is authenticated and a wrong key
cannot produce a successful read. AC-2.11 holds.

**Owed.** The mutation run for Story #5024 has not been performed. Its two configs are committed beside
this brief (`mutation/stryker.5024.backend.json`, `mutation/stryker.5024.frontend.json`) and the kill
rates go into `mutation/results.md` after the push. One scoping gap to record with them: `Program.cs`
is deliberately absent from the backend mutate list, because Stryker.NET ignores line spans and the
whole file is 1500 lines of unrelated startup — so `EnsureEncryptionKeyRing`, `InitializeKeyStore` and
the custody banner lines carry integration tests but will carry no mutation score.

**Also closed here.** `Encryption:Keys`, the plural spelling the bootstrapper reads ahead of the
singular one, was supplied by no test in the repository. Two tests in `EncryptionBootstrapOrderTests`
now cover it through a real configuration provider: a multi-entry ring writes under its first entry and
only reads with the rest, and the ring wins when both spellings are set. `Encryption__KeysFile` is
still asserted only through the command-line provider and not the environment one — the test assembly
is `Parallelizable(ParallelScope.Fixtures)` and the environment provider reads process-global state, so
setting that variable for one test would hand it to every host booting concurrently in another fixture.
The double-underscore translation itself is a property of the provider and is already proved by the
`Encryption__Key` route, so what is left uncovered is that one setting name, not the binding rule.
