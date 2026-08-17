# Epic 5775 — brief for an independent security review

Written 2026-08-17, after slice 05 shipped and before slice 06 (documentation) is written. Paste the
block at the bottom into a fresh session. Everything above it is context for whoever reads the result.

## Why a review, and what it is for

The epic changed how Lighthouse protects every credential it stores. Before it, every install
encrypted with a key published in `appsettings.json` in every build and readable in the public
repository. The epic replaced that with per-instance keys, three custody modes, a rotation that costs
no credential re-entry, and a chart that refuses to install on a key nobody owns.

Nothing in the epic has been run against a real instance yet. Nine walkthrough runs are owed across
slices 04b, 05b, 06a, 06b and 05, all deferred until after the release. A review of the code is
therefore the only independent check that exists right now.

## The claims worth attacking

Each of these is asserted somewhere in the code and defended by a test. The review's job is to find
where the assertion and the defence disagree, or where the property does not hold on a path nobody
listed.

1. **No key material escapes.** Not in a log line, not in a structured log property, not in
   `IConfigurationRoot.GetDebugView()`, not in an API response, not in a ConfigMap, not in a container
   environment variable, not in rendered Helm values.
2. **The published key can never be the active key.** It is compiled in deliberately so an upgraded
   instance can still read what it stored, and it is only ever appended behind an active key.
3. **Resolution precedence is one ordered list in one place** — configuration, mounted file, generated,
   mint — and a supplied key stops an instance minting one of its own.
4. **An instance refuses rather than starting on a key it cannot use.** The one switch that lets an
   operator past that refusal is narrow and loud.
5. **Re-encryption never overwrites what it could not read**, and is safe against a key changing
   underneath it.
6. **Only a System Administrator learns custody.** A merely signed-in caller, including a viewer in an
   embedded frame, learns nothing; an unauthenticated caller learns nothing at all.
7. **The chart cannot generate a key**, the key reaches the container as a mounted file rather than an
   environment variable, and the encryption Secret is not on the reloader watch.
8. **Lighthouse never writes to a Kubernetes Secret** and holds no permission to; it references no
   Kubernetes client at all.
9. **A reload is fail-safe-old.** Content that will not parse leaves the running keys in force; a key
   an operator removed is applied but surfaces the affected credentials as unreadable rather than
   looping against the work tracking system.

## The one thing to attack hardest

`SecretCustodySeamArchUnitTest.NothingThatResolvesOrKeepsAKey_CanWriteToALogAtAll` bans every type in
`Services.Implementation.Encryption` from depending on `Microsoft.Extensions.Logging`, so that no line
can carry key material by accident. `KeyRingFileWatcher` handles key material **and** must log, because
it runs on a timer and has no caller to hand a sentence to. It was therefore placed in
`Services.Implementation.BackgroundServices` instead — outside the namespace the rule guards, which is
the same position `CryptoService` already occupies for the same reason.

That is a deliberate decision and it is also, structurally, a type that handles keys and can log. It
deserves to be challenged rather than accepted because a comment says it is fine. Specifically: does
anything it writes carry material on any path, including exception messages it did not compose?

## Decisions already taken — do not re-litigate these

Raise them only if the reasoning is actually wrong, not because they look surprising.

- **The published legacy key is compiled into the product.** Deliberate, documented, read-only. It was
  never a secret: it shipped in every build. See ADR-148.
- **The mounted key file is mode 0444, not 0400.** The runtime image ends on a non-root user, the chart
  sets no pod security context, and a projected Secret is owned by root — at 0400 the application
  cannot open its own keys and the pod crash-loops. Verified against a real kubelet on 2026-08-17. If a
  pod security context with an fsGroup is ever added, 0440 becomes available.
- **`helm lint` does not enforce the required encryption value.** Lint neutralises `required`/`fail`;
  it never enforced the Postgres password either. Left visible rather than papered over.
- **The chart refuses to render rather than generating a key.** See ADR-153.
- **No GitHub Security Advisory is being published.** Maintainer decision V7: the key was never
  withheld, so an advisory would describe a fix rather than a breach.
- The **embed nonce** accepted risk belongs to Epic 5146, not this one. Different attack, already
  recorded.

## Known open item, not a security finding

Slice 06a merged `IPublishedKeySecretCount` and `IReferencedKeyIds` behind `IStoredSecretSummary`,
departing from the S107 entry in `docs/ci-learnings.md`. The maintainer has not ruled. It is a design
disagreement, not a vulnerability.

---

## The prompt

```
Do a thorough, adversarial security review of Epic 5775 — secret encryption and key custody — in
/storage/repos/Lighthouse. It is shipped to main and unreleased. Read
docs/feature/epic-5775-secret-encryption-key-custody/verification/security-review-brief.md first: it
lists the claims to attack, the one design decision I most want challenged, and the decisions already
taken deliberately that you should not spend effort re-litigating.

Scope is the whole product, with the epic first. Do the epic surface properly, then sweep the rest —
finding something outside the epic is a good outcome, not scope creep.

Priority one, the epic (by path rather than by commit range, because its commits are interleaved with
~150 unrelated ones and a range diff sweeps in noise):

  Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Encryption/
  Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/KeyRingFileWatcher.cs
  Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/CryptoService.cs
  Lighthouse.Backend/Lighthouse.Backend/Models/Encryption/
  Lighthouse.Backend/Lighthouse.Backend/API/EncryptionController.cs
  Lighthouse.Backend/Lighthouse.Backend/API/SystemInfoController.cs
  Lighthouse.Backend/Lighthouse.Backend/Models/SystemInfo.cs
  Lighthouse.Backend/Lighthouse.Backend/Startup/StartupBanner.cs
  Lighthouse.Backend/Lighthouse.Backend/Program.cs   (encryption bootstrap + key store resolution)
  Lighthouse.Frontend/src/  (the encryption panel and the system information page)
  chart/templates/, chart/values.yaml, chart/values.schema.json
  .github/workflows/ci_chart.yml

Then the wider surface, which has never had a review in one pass:

- **Authentication and authorisation.** Every controller and endpoint: what the fallback policy is
  when an attribute is missing, which routes answer before authorisation, and whether any of them
  says more than it should. An unguarded LogsController was found this way once already (Epic 5146),
  so the question "which endpoints are reachable unauthenticated, and what do they return" is worth
  asking from scratch rather than from the attribute list.
- **RBAC.** All of it flows through IRbacAdministrationService by convention. Find where it does not.
- **The other credentials.** API keys, OAuth access and refresh tokens, the OAuth state secret, work
  tracking connector options marked secret — stored, logged, returned to the client, or written into
  an error message anywhere they should not be.
- **The embed surface and the MCP OAuth path**, including the RFC 9728 discovery route.
- **Transport and browser-facing configuration**: CORS allowed origins, forwarded headers, trusted
  proxies and networks, rate limiting, cookie and token handling.
- **Data protection keys** and where they live relative to the encryption key store.
- **Licensing**, since it gates premium behaviour.
- The **frontend**: anything that renders untrusted text, builds a URL from user input, or holds a
  credential in state.
- **Dependencies** with known advisories — there is a NU1903 warning on SSH.NET in the test project.

Design intent for the epic lives in docs/product/architecture/adr-146 through adr-153; the wider
architecture is in ARCHITECTURE.md and docs/product/architecture/. Where code and an ADR disagree,
that is a finding — say which one you think is wrong.

What I want:
- Concrete findings with file:line and a failure scenario an attacker or an unlucky operator could
  actually reach. Not a checklist restatement.
- For each finding, try to refute it before reporting it. Say what you tried.
- Explicitly check the paths nothing has exercised: an instance that upgrades from the published key,
  a key removed while re-encryption is running, a mounted file replaced mid-write, two replicas
  resolving a ring at once, and anything that reads a stored secret outside CryptoService.
- Tell me plainly if a claim in the brief does not hold, including "the ArchUnit rule is now decorative".
- If you find nothing on a claim, say so — silence reads as unchecked.
- Rank by what an attacker could actually do, not by category. A theoretical issue on an
  unauthenticated route outranks a sharp-sounding one behind System Administrator.
- Separate the epic's findings from the wider sweep's, so I can act on them on different timescales.

Do not change code. Report only.
```

Prefix the prompt with the word `ultracode` if you want it fanned out across many agents rather than
reviewed by one.
