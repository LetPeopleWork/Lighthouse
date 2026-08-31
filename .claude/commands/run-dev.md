# /run-dev — start Lighthouse locally without breaking the key store

Start the backend for local development, and recover the two failures that come from key rings landing
in the wrong place. Read this before starting Lighthouse by hand.

## Start it

```bash
cd Lighthouse.Backend
pwsh ./Start-DevServer.ps1            # existing dev database
pwsh ./Start-DevServer.ps1 -Fresh     # throw the database away first, keep the key ring
```

Serves http://localhost:5169 with authentication disabled. It builds the two migration projects if
their assemblies are missing — they are referenced by path, so the app cannot load them otherwise.

The UI it serves is whatever `pnpm build` last wrote into `Lighthouse.Backend/Lighthouse.Backend/wwwroot`.
For live reload run `pnpm dev` in `Lighthouse.Frontend/` (port 5173) instead of rebuilding.

A bare `dotnet run` works and no longer collides with anything, but it keeps its key ring in `dev-keys/`
inside the project directory, where `git clean -xfd` or a fresh worktree throws it away — and a lost ring
is what makes stored credentials unreadable.

## Why any of this matters

Lighthouse refuses to start when it finds two key rings that are not the same key, because choosing the
wrong one leaves every stored secret unreadable. Until 2026-08-31 two rings appeared on their own during
ordinary work: `appsettings.Development.json` pointed `Encryption:KeyStorePath` at `./data-protection-keys`
inside the project directory, while a run without that setting — the backend test suite, on SQLite with a
database file — resolved `keys/` beside that file in the same directory. `Program.cs` reads
`<content root>/data-protection-keys` as a legacy store to carry over, and `KeyStoreMigration` refuses to
start when legacy and resolved hold different rings. One dev run plus one test run broke **both** at once:
the dev server would not start, and the backend suite failed in the hundreds.

The dev profile now names `./dev-keys` instead. The setting itself has to stay — a named path is what
permits an instance to create a key at all (`DefaultLocationNoDurableStore` forbids minting), which is
what a dev running the Postgres option depends on, since it has no database file to sit beside. Only the
*name* was the problem: `data-protection-keys` is the one directory the app treats as a legacy store to
carry over and compare against, and no other pair of directories is ever compared. The collision cannot
form from ordinary use any more.

The script still earns its place: it keeps the dev ring at `~/.config/Lighthouse/dev-keys`, outside the
repository, so it survives deleting the database, `git clean -xfd`, and worktrees — none of which a ring
in `keys/` survives, and losing a ring is what makes stored credentials unreadable. It also refuses to
start, printing the recovery command, if it finds a stale ring in the project directory.

## The two failure signatures

**The backend suite fails in the hundreds** (~1258 tests), every fixture that boots a
`WebApplicationFactory`, with `FATAL: Two key rings were found and they are not the same key`. A dev run
left a ring in the project directory. Move it aside — never delete it, it is what any secret stored under
it was encrypted with:

```bash
mv Lighthouse.Backend/Lighthouse.Backend/data-protection-keys ~/lighthouse-keystore-quarantine-$(date +%Y%m%d)
```

**The dev server refuses to start** with `This instance has stored credentials and not one of them can be
read with the key it started on, 'k-<date>-01'. They say they were written under 'k-<other date>-01'`.
The ring those credentials were encrypted with is gone or is not the one being read. In order:

1. Look for the named ring: `find ~ /storage -name encryption-keyring.protected 2>/dev/null`. If it
   turns up, point at it with `-KeyStorePath`.
2. If it is genuinely gone and the database is disposable, start fresh: `./Start-DevServer.ps1 -Fresh`.
3. If the data matters more than the credentials, start with
   `Encryption__StartEvenIfNothingStoredCanBeRead=true`. Everything renders; stored connection
   credentials read as unreadable and have to be re-entered before a sync will work. Nothing is deleted.

A ring is minted whenever a store is resolved and found empty, which is why a wiped directory silently
becomes a new key rather than an error.

## Related scripts

`Create-DbBackup.ps1`, `Restore-DbBackup.ps1`, `Remove-LighthouseDb.ps1`, `Create-Migration.ps1` — all in
`Lighthouse.Backend/`. Migrations go through `Create-Migration.ps1`, never `dotnet ef migrations add`.
