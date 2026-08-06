# A feasibility question answered by building one — and the premise it dissolved on the way — Epic 5146

**Feature:** epic-5146-jira-forge-app | **ADO:** Epic #5146, Stories #5692 / #5693 / #5694 | **Shipped:** 2026-08-06 | **Commits:** `4c8ec9bd8..4b9ee15ad`

## What shipped

A Lighthouse instance now renders inside a `jira:globalPage` on a real Jira Cloud site **as the person looking at it**, not as a credential an administrator pasted at install time. Installation is zero-credential: an administrator supplies a URL and nothing else.

It is three hops, and each one exists because the hop before it could not do the job:

| Hop | Endpoint | Who calls it |
|---|---|---|
| 1 | `GET /embed/start?nonce=N` | the viewer's browser, **at top level**, where it can complete an OIDC sign-in |
| 2 | `GET /api/v1/embed/handshake/{nonce}` | the Forge resolver, polling, **holding no credential at all** |
| 3 | `GET /embed/enter?token=…` | the frame, redeeming a single-use token for a partitioned embed cookie |

Three slices carried it: #5692 built the viewer path in Lighthouse alongside the existing API-key one, #5693 cut the Forge app over (separate repository `LetPeopleWork/lighthouse-jira-app`, deployed v5.8.0), and #5694 deleted the API-key embed path entirely. ADR-137 is the design of record; ADR-129's identity model and both of its endpoints are superseded, ADR-130 survives whole, ADR-131 survives minus its `ApiKeyId` binding.

## The wall, and the two words that came down on the other side of it

The epic's first answer to *whose identity does a framed Lighthouse carry?* was **a shared API key**, and it was not a lazy answer. It was forced. A framed Lighthouse cannot complete an interactive login, because every identity provider refuses to be framed — verified across Auth0, Entra, Okta and Keycloak, and it is a category result rather than a misconfiguration. Auth0 deprecated embedded login precisely because typing credentials inside another site's iframe *is* the attack. No number of declared origins reaches past that. So the identity had to arrive from somewhere that was not the viewer, and everyone opening the Jira page saw exactly what one key could see. The security review kept circling the same consequence, and every mitigation it proposed was a mitigation of a design decision rather than of a defect.

Then a probe on 2026-08-06 established that Forge's `router.open` **is not `window.open`**. It is Atlassian's own navigation, performed outside the iframe, and it opens a top-level tab — where nothing is framed and therefore nothing refuses. The whole premise the API-key design rested on stopped existing.

D48 dropped the API-key mode the same day, on a one-word maintainer answer. Wallboards and kiosks were the only scenario that needed a session with nobody present, and they are not a case Lighthouse serves; with them gone, the shared credential had no justification left. The `setSecret` call went, the key scoping at install went, and the permanent "everyone sees what this key sees" disclaimer went — **the thing the security review kept circling stopped existing rather than being mitigated.**

The transferable part is not the Forge trivia. It is that the review was correctly identifying an irreducible problem, and the problem was only irreducible under an assumption nobody had thought to test. **A finding that keeps resisting mitigation is worth reading as a question about the premise, not only as a risk to accept.**

## The reversal that exposed a hole

D49 and D60 refused an embed session to a viewer holding no readable scope. It refused the maintainer on his own instance — signed in, System Admin, told to go away — and was reversed. The argument for reversing was sound on its face: RBAC is enforced on every request, so a viewer holding nothing sees nothing through the frame either way, and the conjunct only decided *who told them so*. What it actually did was turn ordinary onboarding into a dead end for an administrator who has not created a Team yet, a viewer waiting to be assigned one, or anyone whose identity provider was swapped.

**Adversarial review tested the argument instead of accepting it, and it was false.** `LogsController` carried no `[Authorize]` and no `[RbacGuard]` at all. It was admitted by the fallback policy, which asks only that the caller be *authenticated* — not authorized. Measured before fixing: an embed cookie for a viewer with no permissions answered **200** on `/api/latest/logs`, `/logs/download` and `/systeminfo`, and could `POST /logs/level` to raise the level and collect more — every Team name, Portfolio name, work-tracking URL and connector error the instance had captured. Before the conjunct was dropped, that set excluded a scope-less viewer. Afterwards it did not.

`LogsController` is now SystemAdmin. On `SystemInfoController` the guard went on `GetRefreshLog` alone: the first attempt guarded the whole controller and broke the app shell's banner, which reads `GetSystemInfo` for every signed-in user. The banner failing is what said so, which is the argument for that test existing at all. `AViewerWithNoReadableScope_CannotReachInstanceWideSurfaces` fails on the previous commit and passes on the fix.

The hole pre-dated this epic; the epic only widened who could walk through it. The durable rule is the one worth carrying forward: **the fallback policy makes "authenticated" the default for anything that forgets a guard, so an absent attribute is not an absent exposure.** A repository-wide sweep for controllers with no explicit policy is owed, and is not this epic's to run.

## Deletion residue keeps passing green

Slice 03 was a deletion, and deletions are the one change shape where a green suite means least. Four things stopped working or stopped being justified, and **not one test noticed**:

- `PruneSpentAsync` lost its only caller with the API-key mint. `EmbedSessionTokens` became append-only on the one path that remains — a row per sign-in attempt, grant or refusal, for the life of the instance. Nothing failed, because no test had ever asserted that anything prunes. Recording a handshake outcome inherits the job; two tests came with the fix and both fail without it.
- `EmbedSessionTokenMintResult` was `MintAsync`'s return type. Nothing produces that shape any more. The compiler stayed quiet because its last reference was a contract-shape test asserting the default of a type nobody constructs.
- `ApiKeyIdentityResolver` had exactly two callers and slice 03 deleted both. It stayed alive on a DI registration — **a registration is enough to keep a class compiled without keeping it useful.**
- The refusal page still told a viewer that no Team or Portfolio had been shared with them, describing the refusal that had been reversed hours earlier. The refusal that survives is "signed in, but the instance could not resolve an account" — an instance-side fault the reader cannot act on, so pointing them at an access request was actively wrong.

And the surviving branch is the least covered thing in the feature: `no_profile` appears exactly twice in the tree, once as the production constant and once as an **arbitrary argument** to a prune assertion. The tests that used to cover the old refusal were rewritten into grant assertions, and nothing replaced them on the branch that is still reachable. **When a refusal is narrowed rather than removed, the tests that covered the wide version do not narrow with it — they change sides.**

Every one of these was found by reading what the deletion orphaned, not by running anything.

## Two tests that had never been able to fail

Both surfaced while trying to make a *new* test able to fail, which is the only reliable way these get found.

**D72 — `ILoggerProvider` registrations are silently inert in this codebase.** `Program.cs` calls `builder.Host.UseSerilog(logger, true)`, where the positional argument is `dispose`; `writeToProviders` defaults to `false`, so `SerilogLoggerFactory` drops every provider added through `logging.AddProvider(...)`. A log-capturing test double therefore cannot work here at all. The embed harness captures through a Serilog sink instead, and the production pipeline was **deliberately not changed** — flipping a global observability setting to satisfy a test double is the wrong reason for it. The collateral was worse than the cause: six assertions across two delete-serialisation fixtures asserted `ContainsMessageFragment(...)` `Is.False` against a provider that can never capture anything. They had been vacuous since the day they were written, passing because the collection is always empty rather than because the exception never occurs.

**D73 — a `Barrier` that gates task *start* does not open a race** when the expensive setup sits inside the raced section. The repository's idiom calls `WebApplicationFactory.CreateClient` inside the barrier, and starting the test server costs orders of magnitude more than the request that follows it. The callers stagger, every loser reads the row after the winner has committed, an in-memory pre-check catches them, and **the atomic update under test is never exercised**. Hoisting the clients above the barrier was only half the fix. Hoisted, the redemption race failed under sabotage 3/3 *in isolation* and passed 3/3 in a full-fixture run: cold, the first request pays for JIT, the EF query compile and a physical Npgsql connection, and every racer pays it at once so they arrive together; warm, only the connection opens vary, the spread wins, and the race closes again. The fix is a **per-caller warm-up** — each client spends one refused redemption before the barrier, so the raced request is nobody's first. Measured after: sabotage fails 3/3 in a full-fixture run at **7 winners / 1 refusal**, unsabotaged green 3/3.

The general form of both: **a test that has never been observed failing is a claim, not a result.** Neither of these was found by a reviewer reading them; both were found by someone deliberately breaking the production code and noticing the test did not care.

## Four decisions amended by running them

The design was written to be executed, and execution corrected it four times.

**D68 — four columns, not three.** D55 (clear the nonce hash on consumption) and D62 (a replayed nonce must be *observable*) contradict each other: a cleared hash makes the row unfindable by nonce, so a replay becomes indistinguishable from a nonce that never existed and the scenario asserting it can never pass. A crafter following D55 literally arrives at an unfixable test with no explanation. A fourth nullable column, `HandshakeConsumedAt`, keeps the hash and stamps consumption instead — and D45's indistinguishability survives, because that is a property of the *response*, not of the row. Unknown and consumed both answer `204`; only the log tells them apart.

**D69 — falsified by running it.** The prediction was that EF emits SQLite's table-rebuild (`CreateTable(ef_temp_…)` → `INSERT…SELECT` → `DropTable` → `RenameTable`) into the migration *source*, where `ExpandOnlyMigrationGuard`'s textual scan of `Up()` bodies would see the `DropTable` and fail. It does not. EF's SQLite migrations **SQL generator** performs the rebuild at SQL-generation time; the C# `Up()` body contains a plain `AddCheckConstraint`. No change was needed and none was made. It is recorded rather than deleted because "we predicted a collision and there wasn't one" is worth knowing the next time someone reasons about EF's SQLite behaviour from the migration file alone — and because the same run settled the peer review's only HIGH with evidence: the explicit `OnDelete(DeleteBehavior.Cascade)` re-declaration survives the rebuild.

**D70 — the refusal vocabulary closes at one code.** Three were proposed; two are unreachable. `Disabled` and `Misconfigured` both answer 404 and `Blocked` answers 403, all of them *before* a row exists, so every path that reaches a row write reaches it signed in on an Enabled instance. **A closed set whose other members are unreachable is a claim the next reader has to disprove; one `public const` is a claim they can check.**

**D71 — a grant row carries a digest whose plaintext nobody holds.** It falls out of the storage constraint: a grant must carry a `SecretHash` at write time, but the poll happens on a later request and cannot recover a plaintext from a digest, so consumption mints the real secret and rewrites the hash. What looks like a workaround is the better property — **a grant row that leaks before the legitimate poll is unredeemable, because the secret it would need does not exist anywhere yet.**

## The session the frame could not see

Navigating away from the Jira page and back tore down the iframe, built a new one, and sent the viewer through all three hops again — for an embed session they were still holding. The cookie survives that: it is a session cookie in the frame's partitioned storage, and the configured session lifetime is still running.

The app had no way to know. The cookie is `HttpOnly`, and D13 gives a cross-origin frame no signal the parent can read, so there was nothing to observe and nothing to ask. The fix is that **the grant advertises `sessionLifetimeSeconds` rather than the app guessing** — seconds rather than an instant, because the session starts at hop 3 and the grant is hop 2. It is grant-only and omitted elsewhere, because a field appearing on any other outcome would be an existence oracle for live sessions.

The alternative — hardcoding the window in the Forge app — would have duplicated a Lighthouse setting into a second place that cannot see it change. That is the exact shape of Bug #5696, filed by this epic: guidance text naming a configuration prefix (`LIGHTHOUSE_AUTHENTICATION__TRUSTEDPROXIES`) that does not exist, so following it verbatim changes nothing, silently.

## An ADR number collided invisibly

A rebase onto `main` put **two different ADR-132s in the same directory** — Epic 5375's Feature-ordering decision, already on main and already cited by its four siblings 133–136, and this feature's viewer-identity embed session. The filenames differed, so git saw no conflict and nothing failed. The embed one yielded, because the other was the one already referenced from elsewhere, and 137 was the next free number: 47 references rewritten across code comments, the feature workspace and the architecture brief. Nothing in the toolchain would have caught this; it was found by reading the directory.

## Gates

| Gate | Result |
|---|---|
| Backend suite, finalized tree | **4487 passed / 0 failed / 0 skipped** (orchestrator-run; the per-pass logs record 4522 after the slice-01 mutation additions and 4498 at every slice-03 refactor module gate) |
| Mutation, slice 03 (#5694) | **89.00 %** — 200 tested, 178 killed, 14 survived, 13 m 10 s. Gate 80 |
| Mutation, slice 01 (#5692) | **88.46 %** — 208 tested, 184 killed, 19 survived, 12 m 03 s. Gate 80. First run scored 76.44 %; the pass added 29 test cases and **no production change** |
| E2E auth suite vs. real Keycloak | **14/14** (orchestrator-run; the flow's own skeleton is `ViewerEmbedSession.spec.ts`) |
| `dotnet build` / `dotnet format analyzers` | 0 warnings, 0 errors; zero findings in any file this epic touched |
| `des-verify-integrity` | exit 0 — all 7 steps carry complete traces |
| Live verification | 2026-08-06, real Jira Cloud site: the framed Lighthouse names the **viewer**, not a shared key |

Frontend mutation is *N/A rather than skipped* on both slices — `git diff --stat -- Lighthouse.Frontend/` is empty for each. Nothing for StrykerJS to mutate.

Slice 03's mutation run was deliberately taken **after** the adversarial review rather than before it. The review had rejected the slice on the `LogsController` blocker; scoring code a reviewer had rejected would have measured the wrong thing.

## Still open

- **`verdict.md` is superseded on its central point and still says so.** It was written 2026-08-05 and states that per-viewer identity is not reachable without changing Lighthouse's login. The `router.open` probe landed the next day and it is now reachable and shipped. Everything else in that document stands — most importantly **K1 is still 0: no prospect has seen this.** The learning hypothesis is tested by prospect reaction, and the failure mode the epic was built to detect is a shrug, which is only observable in front of a person. **The next step is a pitch, not a build.**
- **The refusal branch that survives has no behavioural test.** `no_profile` is reachable and unasserted.
- **"Point it at my own Lighthouse" is not delivered.** Forge frames and fetches only manifest-declared origins; customer-managed egress (#5663) is the fix and is open. Until then every demo shows our data, not the prospect's.
- **Bug #5696 and the TLS-proxy failure.** Any TLS-terminating reverse proxy breaks the embed flow — the scheme reads `http`, the frame blocks the mixed-content entry URL, and the `Secure` cookie never comes back. Nothing documents it, and every prospect behind nginx, Traefik, Caddy or a cloud load balancer meets it first.
- **Naming debt, deliberate (D63).** `EmbedSessionToken` names a row that may hold no token; `ApiKeyPrincipalFactory` builds principals for people. Renaming a table is a destructive migration and the project is expand-only, so both renames — and the `ApiKeyId` column drop — ride the same contract-phase drop.
- **The Forge app has no tests, no linter, no CI and no dependency scanning**, by design: D7 put it in a separate repository so a throwaway showcase would not face this repository's gates. That exemption expires the moment anyone decides to keep it, and the gap belongs in the estimate rather than being discovered afterwards.
- **Two repository-wide sweeps are owed and unstarted**: controllers admitted only by the fallback policy, and `Barrier`-inside-`Task.Run` fixtures whose races have never been observed opening.

## Why this one is worth re-reading

Three of this epic's most valuable findings are the same finding wearing different clothes.

The API-key design was irreducible under an assumption nobody tested. The readable-scope reversal was safe under an argument nobody tested. Two concurrency fixtures were green under a barrier nobody had watched fail. In each case the artifact — a design, a review comment, a passing test — looked like evidence and was actually a **claim someone had accepted on its plausibility**.

What separated the three that got caught from the ones that did not is not care or seniority. It is that somebody executed the claim: opened a top-level tab and watched what happened, sent an embed cookie at `/logs/download` and read the status code, deleted a production guard and checked whether the test went red. All three were cheap. All three overturned something.

**The cheapest thing you can do to a written-down assumption is run it**, and this epic's yield is a strong argument for doing that earlier than the review stage — because the two claims that were never run are still in the tree, and the only reason we know that is that a third one wasn't.
