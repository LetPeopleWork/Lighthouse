# Epic #5874 — Lighthouse moves off MIT to a source-available licence

**Shipped** 2026-09-11 · ADO Epic #5874 · workspace `docs/feature/epic-5874-relicense-source-available/`

## What changed

Lighthouse is no longer MIT licensed. Everything released up to and including **v26.9.9.9** (9
September 2026) stays under the MIT License in perpetuity; source from the relicensing commit onward
is under the **Lighthouse Source Available License 1.0** (`LicenseRef-Lighthouse-SAL-1.0`).

The source stays public and inspectable on GitHub. This is not a move to closed source, and
OSI-approved status was never a goal. Customers may still read, audit, run and modify the code —
including with an AI assistant — and serve a whole company from one instance. What is now prohibited
is providing Lighthouse to others as a hosted or managed service, circumventing the licence key,
removing notices, and building a separate product that substitutes for Lighthouse. That last one
holds even when the substitute is free, used only inside one company, or written in another
language.

Consultants and coaches are expressly carved out: installing, configuring, operating, training on or
consulting about Lighthouse **on a client's own deployment** is permitted, and stays permitted even
though LetPeopleWork sells those services itself.

## The licence is one file in two parts

`LICENSE` opens with a notice, then Part 1 — the Lighthouse Additional Terms — then Part 2, the
Elastic License 2.0 reproduced **byte-for-byte and unaltered**.

That shape was arrived at, not designed up front, and the reasoning matters more than the outcome:

- **ELv2 alone does not do the job.** It prohibits hosted resale and licence-key circumvention, and
  restricts nothing about modification or derivation. Under plain ELv2 an organisation may legally
  build a rival forecasting product from this source. That is the first thing the epic set out to
  prevent.
- **ELv2's text may not be edited to fix that.** It carries no modified-versions clause the way
  MPL 2.0 does, its FAQ never raises adaptation, and "Elastic License" is a trademark. Verbatim
  adoption is clearly fine and widespread; editing is unanswered.
- So our terms sit **above** Elastic's rather than inside them, and Part 2 is always the tail of the
  file. The integrity check is length-anchored and survives any future edit to Part 1.
- The no-competing-use wording derives from **PolyForm Shield 1.0.0**, the one licence family whose
  steward expressly permits adapting its text, and which reaches competitors *"even when provided
  free of charge"* — which FSL's equivalent does not. FSL itself was rejected: it converts every
  version to Apache-2.0 or MIT on its **second** anniversary, half the BUSL clock the original
  decision record had already rejected.

## What this cannot do, stated plainly

Everything up to v26.9.9.9 is MIT, has been public since 2025, and is already in every code-model
training corpus that crawls GitHub. **The substitute-derivation risk this epic exists to close is,
for the codebase as it stood, already open and cannot be closed.** The relicence protects the next
years of work, which is most of the value — but it is a forward-looking gate, not a fix, and
describing it as protecting Lighthouse from AI-assisted cloning overstates it.

Two further ceilings are recorded rather than papered over. A team that replaces the internals
feature by feature inside a fork never visibly crosses the line between a modified deployment and a
separate product. And `LicenseGuardAttribute.cs` remains one attribute on a controller: removing the
free-tier cap is now *prohibited*, but it is not *hard*. Raising that cost is
[Epic #5972](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5972), deliberately split
out.

## Accepted risks

- **Reproducing ELv2 verbatim.** Two counsel opinions split on this. One called it safe given the
  disclaimer; the other wanted written confirmation from Elastic first. The founders shipped on the
  permissive opinion, reasoning that ELv2 is published for adoption, that the disclaimer is good, and
  that asking and being refused would leave a weaker position than publishing on advice. Recorded in
  the workspace with both opinions and the revisit condition.
- **ELv2's patent provisions come with it.** Part 2 grants every licensee a patent licence with
  defensive termination. Accepted knowingly; it interacts with acquiring a patent later.
- **Losing the "open source alternative" search query.** The `/compare` page's schema.org FAQ asked
  exactly that question and answered "Yes." The question was changed rather than the answer, because
  answering a premise we no longer satisfy — machine-readable, on our own comparison page — was the
  worse option. That is the single largest measurable cost of the change.

## Contributor copyright

Eight commits from three outside contributors exist in the history, all accepted under MIT with no
CLA. About 450 of their lines survive, 59 of them in shipped product code. The original decision
record said "seven commits from two people"; measuring the repository corrected that, and the third
contributor appeared nowhere in it.

MIT's sublicensing right permits relicensing the combined work, and the contributors were asked and
agreed. `NOTICE` names all three and carries the MIT text as the accompaniment MIT requires — the
first draft named only LetPeopleWork, which would have breached the licence their code is held under.

## Guardrail

`Scripts/check_license_claims.sh` fails if any public surface calls Lighthouse open source or MIT
licensed. It went from 17 hits to zero across this epic and is now the standing check. Thirty
known-good matches are allowed individually — the licence documents, true statements about Postgres
and Keycloak, third-party inventories — each keyed on path *and* text with its reason, so the
licensing page can explain the MIT history without the gate passing someone writing "Lighthouse is
open source" on it.

## Surfaces touched

| Where | What |
|---|---|
| Repository | `LICENSE`, `NOTICE`, both package manifests, `.dockerignore`, CI path filters |
| Artefacts | Docker image and all three standalone packagers now ship the licence with the app |
| In-app | System Info footer, feedback dialog, both with their assertions updated |
| Docs site | index, licensing (new source-licence section with worked allowed/not-allowed tables), security, contributions, compliance policy, README, SECURITY |
| TDM | `/.well-known/tdmrep.json` on the docs site; Jekyll needed `include:` or it would never have shipped |
| Website repo | 25 sites including the `/compare` schema.org FAQ and `llms.txt` — the file AI assistants read, which had answered "Is Lighthouse open source?" with "Yes, 100% open source" |

`AI.tsx`, `AIIntegrationSection.tsx` and the plugin lines in `llms.txt` are deliberately untouched:
they describe LetPeopleWorkShop and LetPeopleGrow, which are genuinely MIT.

## What did not ship

**The contribution enforcement mechanism.** A PR template and a `pull_request_target` workflow that
auto-closes fork PRs were scoped and then dropped as not worth the effort (ADO #5971, Removed). Only
the prose fix moved into the docs sweep. The consequence, stated rather than hidden: a code pull
request can still be opened, and somebody has to notice and reply by hand.

## One thing that went wrong

`COPY LICENSE NOTICE /app/` was added to the `Dockerfile` so the licence would travel with the image.
It could never have worked — `.dockerignore` had excluded `LICENSE` since January — and the Docker
job failed on both architectures. Worse, the CI `paths` filter watched neither `Dockerfile` nor
`.dockerignore`, so the fix would have triggered no run at all and a Dockerfile-only change had never
been built. Both are fixed: the exclusion is gone, and all four files the image is built from are now
watched.
