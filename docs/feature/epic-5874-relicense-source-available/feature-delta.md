# Feature Delta — epic-5874-relicense-source-available

**ADO**: Epic [#5874](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5874) — "Relicense
Lighthouse off MIT: ELv2 or BUSL 1.1" (state `Active`, tag `Community`)
**ADO children**: [#5967](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5967) SPIKE ·
[#5968](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5968) slice 01 ·
[#5969](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5969) slice 02 ·
[#5970](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5970) slice 03 ·
[#5971](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5971) slice 04 —
sibling Epic [#5972](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5972) carries D11.
**Upstream decision**: `organization/decisions/2026-08-01-do-we-stay-open-source-or-not.md` — DECIDED
2026-08-31, two 3-lens panels plus a 2026-08-28 ELv2-vs-BUSL comparison.
**Waves**: DISCUSS (this document) → DESIGN
**Density**: lean (Tier-1 `[REF]` only) per `~/.nwave/global-config.json`

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `lighthouse-maintainer` | Executes the change once, and has to keep every public surface saying the same accurate thing afterwards without a drift hunt at release time. |
| `forecasting-prospect` | Evaluates Lighthouse before installing it. Their security or legal colleague asks what the company may do with the source, and they need an answer they can paste. |

Two personas, both existing. No new persona is created.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

- `job-maintainer-protect-future-increments-without-going-closed` — When AI assistance makes it cheap
  to derive a substitute from source I publish, I want the licence to prohibit that and the
  key-stripping it enables, so I can keep the code inspectable without funding my own competitor.
- `job-prospect-know-what-i-may-do-with-the-source` — When my security or legal colleague asks what we
  are allowed to do with a self-hosted tool's source, I want one authoritative statement of what is
  and is not permitted, so I can answer without guessing or opening a vendor conversation.
- `job-maintainer-say-the-same-thing-everywhere` — When the licence changes, I want every public
  surface to say the same accurate thing on the day the diff lands, so nobody reads "open source" on
  the website and "source available" in the repo.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Verified 2026-09-11 against `main` + the `website` checkout. This is the blast radius.

**Licence instrument**

| Surface | Current state |
|---|---|
| `LICENSE` | MIT License, 21 lines, © 2025 LetPeopleWork GmbH |
| GitHub repo metadata | `license.spdx_id = MIT`, `visibility = public`, 17 stars, 4 forks |
| `Lighthouse.EndToEndTests/package.json:21` | `"license": "MIT"` |
| `Lighthouse.Frontend/package.json` | no `license` field (`private: true`) — nothing to change |
| SBOM root components (`backend.cdx.json`, `frontend.cdx.json`) | `metadata.component.licenses = null` — our own licence is not declared, so the generated SBOM needs no change |

**Copy claiming "open source" about Lighthouse**

| Surface | Lines |
|---|---|
| `README.md` | 5 ("free, open-source forecasting…"), 17 ("Free & open-source (MIT)"), 103 ("MIT licensed") |
| `SECURITY.md` | 70 ("provided \"as-is\" under the MIT license") |
| `docs/index.md` | 7, 15 |
| `docs/contributions/contributions.md` | 7, 20 |
| `docs/licensing/licensing.md` | 9 |
| `docs/security.md` | 32 ("Lighthouse is open source, so the paragraphs below name what to look for") |
| `docs/compliance/security-update-policy.md` | 23 |
| `Lighthouse.Frontend/src/components/App/LetPeopleWork/LighthouseVersion.tsx` | 392 ("Licensed under MIT License") |
| `Lighthouse.Frontend/src/components/App/Header/FeedbackDialog.tsx` | 141, 169 ("completely Free and Open Source") |
| `…/FeedbackDialog.test.tsx` | 122 (asserts the string) |

**Website repo (`LetPeopleWork/website`, separate checkout)**

| Surface | Lines |
|---|---|
| `src/components/Hero.tsx` | 27 — "Open-source flow metrics and forecasting" |
| `src/components/LighthouseSection.tsx` | 131 — "100% open source" |
| `src/components/SEO.tsx` | 26 (keyword list), 60 (org description) |
| `src/pages/Index.tsx` | 24, 59, 68, 118 — including **schema.org JSON-LD** product descriptions |
| `src/pages/Lighthouse.tsx` | **1307 — pricing comparison row `"100% Open Source (MIT License)"`, shown as a feature of all three tiers** |
| `public/compare/index.html` | 7, 13, **24/27 (schema.org FAQ: "Is there an open-source alternative to ActionableAgile and Nave?" → "Yes.")**, 35, 43, 85, **97 (comparison table row "Open source: Yes, 100% (GitHub)")**, 113 |
| `public/manifest.json` | 4, and `README.md:82` mirrors it |

**Not in scope, deliberately**: `website/src/pages/AI.tsx` and `AIIntegrationSection.tsx` say
"open-source" about the **LetPeopleWorkShop and LetPeopleGrow Claude Code plugins**. Those are
genuinely MIT and unaffected. Changing them would make the copy wrong.

**GitHub contribution channels (verified via `gh api`)**

| Channel | State | Can it be turned off? |
|---|---|---|
| Issues | already `false` | — |
| Discussions | already `false` | — |
| Wiki | already `false` | — |
| Forks | `allow_forking: true`, 4 forks exist | **No.** GitHub's forking policy covers private/internal repos only; a public repo cannot disable forks. |
| Pull requests | open | **No native setting.** A `pull_request_target` workflow that auto-closes PRs from forks is the only mechanism. |

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — Two documents: ELv2 verbatim, plus binding Lighthouse Additional Terms

*Revised 2026-09-11 after the base-text research below. The earlier form of D1 adapted ELv2's text
directly; that is no longer the shape.*

**`LICENSE` reproduces the Elastic License 2.0 word for word, unaltered.** Its three prohibitions —
(1) no providing the software to others as a hosted or managed service, (2) no circumventing the
licence-key functionality or obscuring the features it protects, (3) no removing or altering
licensing and copyright notices — stand exactly as Elastic published them. Nothing inside Elastic's
text is edited, reworded or deleted.

**Part 1 of the same file carries our additions.** `LICENSE` is one file in two parts — an opening
notice, then the Lighthouse Additional Terms (Part 1), then ELv2 verbatim (Part 2) at the tail. Part
1 holds six sections: no competing use (1), prohibited results however produced (2), rights that
cannot be restricted (3), relationship to commercial agreements (4), termination linkage (5), and
governing law (6). The two that carry the epic's purpose are:

- **a no-competing-use clause**, taking its wording from **PolyForm Shield 1.0.0** rather than FSL —
  see D2. Shield's Noncompete makes any purpose permitted *except* providing a product that competes
  with Lighthouse or with any product we provide using it, and its Competition paragraph states that
  products compete across different interfaces, platforms and programming languages, **and even when
  provided free of charge**. That last clause is the one that reaches the case this epic actually
  fears: an organisation rebuilding Lighthouse internally with an AI assistant and never selling it.
  PolyForm's own README grants permission to adapt its text provided every mention of "PolyForm" and
  polyformproject.org is removed — so this wording may be taken and reshaped, which ELv2's may not.
- **an explicit sentence naming AI**: that using the source — in whole or in part, directly or as
  input to an automated or AI-assisted process, including code assistants and agents, and including
  use as training data — to produce a work falling under any of the prohibitions above is itself
  prohibited.

Permanent. No change date. Source stays public and inspectable on GitHub.

**Why ELv2 could not be taken as the whole instrument**: it restricts nothing about modification or
derivation. Under plain ELv2 an organisation may legally build a rival forecasting product out of
this source, as long as they do not host it for others and do not touch the key. That is the first
thing the epic set out to prevent.

**Why ELv2's text is not edited to fix that**: no grant to do so exists. ELv2 carries no
modified-versions clause — unlike MPL 2.0 §10.3, which expressly permits a renamed modified version —
its seven-question FAQ never addresses adaptation, and the text bears no copyright notice or reuse
statement of its own. "Elastic License" is Elastic's trademark, and licence texts are copyrightable
works. Verbatim adoption is clearly fine and widely practised; editing is an unanswered question.
Two documents answer it by not asking it.

**Why one file and not two.** The first draft put Part 1 in a separate
`LICENSE-ADDITIONAL-TERMS.md` incorporated by reference. Two independent review rounds both said
that was the weakest point: a package manager, or a reader who opens only `LICENSE`, may never see
the second file and can argue lack of notice. Embedding solves it **and costs nothing**, because our
terms go *above* Elastic's rather than into them. DESIGN must treat "nothing inside Part 2 is edited,
reworded, reordered or deleted" as a hard constraint on slice 01, not a preference — the check is
length-anchored on the tail of the file (AC-01.1) and survives any future edit to Part 1.

**What this costs**: the composite is not ELv2 and must never be called ELv2 or carry the
`Elastic-2.0` SPDX tag. It needs its own name and a `LicenseRef-` identifier, and both added clauses
need a lawyer's read (D13).

### D2 — FSL 1.1 examined and rejected, and its clause superseded by PolyForm Shield's

FSL grants every version an automatic Apache-2.0 or MIT licence **on its second anniversary** and
carries no licence-key clause at all. The 2026-08-28 comparison rejected BUSL's four-year clock as
"committing today to giving away the thing whose protection is the entire point"; FSL's clock is half
as long. Rejected as an instrument.

Its Competing Use definition was, briefly, the clause D1 borrowed. **PolyForm Shield 1.0.0's
Noncompete replaces it**, for two reasons. Substance: FSL bars *"making the Software available to
others in a commercial product or service"*, which leaves the free internal clone untouched — the
exact case AI assistance makes cheap. Shield states that products compete across interfaces,
platforms and languages and **"even when provided free of charge"**. Permission: the
`getsentry/fsl.software` repository carries no `LICENSE` or `NOTICE` file at all and the template
grants nothing about reusing its own wording, whereas PolyForm's README explicitly permits changes so
long as every mention of "PolyForm" and polyformproject.org is removed. Shield's wording can be taken
and reshaped with permission; FSL's cannot, on anything better than custom.

### D3 — BUSL 1.1 stays rejected, unchanged from the BDR

Bespoke Additional Use Grant to draft, a Change Date, and the heavier Terraform→OpenTofu community
memory with this audience. Nothing in this wave reopens it.

### D4 — Scope is the `Lighthouse` repo only

`lighthouse-clients` (CLI + MCP) and `lighthouse-jira-app` stay MIT. They are thin clients over the
API; they carry neither the forecasting engine nor the licence gate, and an MIT npm package is
materially easier to pull into an AI toolchain — that is a funnel, not a moat. This is a licence
decision only: both repos still get the wording pass wherever they claim Lighthouse is open source.

### D5 — Every version shipped to date stays MIT in perpetuity

Carried verbatim from the BDR. The change binds future increments only. Stated in the repo so nobody
has to infer it, and so the four existing forks are not implicitly accused of anything.

### D6 — The public term is "source available", everywhere

One term, used identically in the repo, the docs site, the in-app strings and the website. Not "fair
source", not a bespoke phrase per surface. Decided against "source code public and inspectable" —
accurate but unusable in a pricing-table cell or a page title.

### D7 — The genuinely-open plugins keep the words "open source"

`LetPeopleWorkShop` and `LetPeopleGrow` are MIT and stay MIT. The wording sweep must not touch them,
and any grep gate built in slice 02/03 must exclude them by name rather than by hand-waving.

### D8 — Outside code contributions end

`contributions.md` is rewritten: bug reports, feedback, docs corrections and word-of-mouth stay
welcome; code pull requests are not accepted. Evidence base: seven commits from two people in the
project's history, Issues and Discussions already disabled. Mechanism, given GitHub offers no switch
(see inventory): a `pull_request_target` workflow closes PRs opened from forks with a comment naming
where to send the thing instead. Forks themselves cannot be prevented and are not treated as a
problem.

### D9 — The LICENSE diff lands in the next release, regardless of the Enterprise launch

The BDR's sequencing risk — "the free thing got narrowed the month the paid thing shipped" — is read
and accepted. It was sized against a community of 17 stars, two outside contributors and zero of
sixteen connection records citing GitHub.

### D10 — The scanner question is now answered by observation, and the answer is "nobody cared"

The BDR left this as its single most decision-relevant open item and asked for evidence rather than
feeling. It has been asked directly in the community Slack: **effectively nobody cared whether
Lighthouse carries an OSI-approved licence; those who had a requirement at all needed "source
available".** That closes BDR open item 1 in favour of proceeding, on the same evidentiary standard
both panels applied to the fork threat.

### D11 — Hardening the Premium gate is out of scope and needs its own ADO item

BDR Option E survived both panels untouched and is the only lever that raises the *technical* cost of
stripping the key; `LicenseGuardAttribute.cs` is a one-file strip point whatever the LICENSE says. It
is a different design space (server-side verification, attestation, obfuscation) with its own slices,
and folding it in here would make this epic oversized against its own scope gate. **Not a silent
N/A** — it is real, it is wanted, and it belongs on the board as a sibling of #5874.

### D12 — The AI clause is a signal layered on an outcome clause, not the load-bearing mechanism

Two threats wear one name, and only one is a licence problem.

- *Agent-assisted derivation* — someone points a code assistant at this repo and ships a modified
  build. This is an ordinary derivative work; copyright does not care who typed it. The clause that
  catches it is the one that bans the **outcome** (competing use, key circumvention, notice removal).
- *Training-corpus ingestion* — the source lands in model weights, and the model later emits similar
  code to someone who never opened the repo. A licence clause here is untested, contested, and
  undetectable in practice.

No mainstream software licence — ELv2, BUSL, FSL, PolyForm, SSPL — carries an AI clause; AI
restrictions live in model licences and content contracts. So D1's AI sentence is drafted from
scratch, and its job is to state the position publicly and remove the "we didn't know" defence. The
protection comes from the outcome clause underneath it. DESIGN must not let the AI sentence be the
only thing standing between the source and a substitute.

### D13 — Legal review is a hard pre-requisite, not a review step

No LICENSE diff lands before a lawyer has read the drafted text. Same shape as epic 5733's legal DoR
item.

The question that used to lead this list — *may ELv2's text be adapted and renamed at all?* — is
**answered and designed around** rather than carried: no grant exists, so the text is not adapted
(D1). What remains for the lawyer is narrower and concrete:

1. Does the one-file structure bind reliably? Two review rounds called incorporation by reference the
   draft's weakest point, so the Additional Terms moved inside `LICENSE` as Part 1 rather than being
   pointed at from it. Counsel confirms the shape rather than assessing the risk we removed.
2. Does the Shield-derived no-competing-use wording sweep in a customer's own internal modifications?
   It must not. Shield's Changes and New Works License expressly permits changes for any permitted
   purpose, and that property has to survive the reshaping.
3. Is PolyForm's README permission sufficient authority to reuse Shield's wording inside a licence of
   a different name, once every "PolyForm" and polyformproject.org reference is stripped as it
   requires?
4. Does the AI sentence, which has no precedent in any software licence, do anything a court would
   read — and does it risk being read so broadly that it voids something we did intend to permit?

### D14 — The managed-service clause versus partner hosting: CLOSED, no carve-out needed

BDR open item 2, carried unanswered since 2026-08-01 and answered by the founders on 2026-09-11: a
partner hosting Lighthouse for their client is **not a motion we want**, and would not be permitted
regardless of what the licence says. ELv2 clause 1 therefore restricts nothing we intended to do, and
no carve-out is drafted.

Two consequences worth carrying. The drafting is unblocked — this was the last open question in front
of the SPIKE. And clause 1 turns out never to have been load-bearing for us, which is part of why D1
can afford to leave Elastic's text untouched and put the clauses that *do* carry weight into a
separate document.

### D15 — RBAC: N/A, explicitly

No endpoint, role, scope or gate changes. Nothing in this epic touches `IRbacAdministrationService`
or `useRbac()`.

### D16 — Terminology: N/A, explicitly

"Licence", "source available" and "contribution" are not terms a user can rename under
Settings → Terminology. Nothing here renders a configurable term.

### D17 — Lighthouse-Clients CLI/MCP version bump: N/A, explicitly

Per D4 both client repos stay MIT and no client-facing contract changes. They receive wording
corrections only, which do not warrant a version bump.

### D18 — Generated SBOMs need no change

Verified: `metadata.component.licenses` is `null` in both `backend.cdx.json` and
`frontend.cdx.json`. The SBOMs declare our dependencies' licences, never our own. The docs page about
OSS attribution (`releasenotes.md:935`) describes third-party components and stays accurate.

---

## Wave: DISCUSS / [REF] Scope Assessment

**Right-sized after slicing — two oversized signals fired and are resolved by the slice split, not by
narrowing the epic.**

| Heuristic | Reading |
|---|---|
| >10 user stories | No — four. |
| >3 bounded contexts or modules | **Fires.** Repo-root legal text, in-app TS strings, Jekyll docs site, a second React repo, and GitHub repo config / CI. |
| WS needs >5 integration points | No. |
| effort >2 weeks | No — four slices, roughly 3.5h–5h each. |
| multiple independent user outcomes | **Fires.** "The terms are in force", "the public copy is honest", and "contributions are closed" each ship and deliver value alone. |

Resolution: four independent slices, each releasable on its own, ordered so the licence text lands
before the copy that describes it. D11 (gate hardening) moves out to its own ADO item rather than
being carried as a fifth slice — that is the scope reduction the assessment asks for.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield, and no mechanism here is one nobody has run. Editing
markdown, editing a TS string with its Vitest assertion, and adding a GitHub Actions workflow are all
established in this repo. The single genuinely new artefact is the licence text itself, and that is
gated by a legal review (D13) rather than by a technical spike.

---

## Wave: DISCUSS / [REF] Driving Ports

No HTTP endpoint, no CLI subcommand, no new UI control. The user-invocable entry points this feature
changes are all *read* surfaces:

| Port | What the reader does |
|---|---|
| `github.com/LetPeopleWork/Lighthouse` → `LICENSE` | Reads the terms; GitHub's sidebar shows the licence name. |
| `docs.lighthouse.letpeople.work/licensing/licensing.html` | Reads what may and may not be done with the source. |
| Lighthouse → Settings → System Info (footer) | Reads the licence line under the version. |
| Lighthouse → header → Feedback dialog | Reads how the product describes itself. |
| `letpeople.work/lighthouse` | Reads the hero, the pricing comparison row and the FAQ. |
| Opening a pull request from a fork | Receives an automated close with a pointer to the feedback channel. |

---

## Wave: DISCUSS / [REF] Pre-requisites

1. **Legal review of the drafted licence text (D13)** — blocking for slice 01 only. Slices 02–04 do
   not depend on it, but slice 02's copy must not claim terms the final text does not carry, so
   slice 02 ships after slice 01 in practice.
2. ~~**The partner-hosting question (D14)**~~ — answered 2026-09-11: partner hosting is not a motion
   we want and would not be permitted anyway, so no carve-out is needed and the SPIKE is unblocked.
3. ~~**A sibling ADO item for the Premium-gate hardening (D11)**~~ — done: Epic
   [#5972](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5972), linked `related` to
   #5874, so the deferral is visible on the board rather than only in this document.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Hardening the Premium gate** (D11) — own ADO item, own design.
- **Relicensing `lighthouse-clients` or `lighthouse-jira-app`** (D4) — they stay MIT; wording only.
- **Any change to the four existing forks** — they are inert snapshots under a perpetual MIT grant
  (D5) and nothing here reaches them.
- **Removing "open source" from the plugin copy** (D7) — those claims are true.
- **Terms and Conditions on the website** (`letpeople.work/lighthouse#lighthouse-license`) — the paid
  licence agreement is a separate commercial document from the source-code licence. Named here so it
  is an explicit exclusion rather than an oversight; DESIGN should confirm the two do not contradict.
- **A long-form essay or announcement campaign** — the BDR settled the artefact as a short dated
  factual note plus one docs FAQ line, after Social withdrew its own essay proposal.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — Read what I am actually allowed to do with this source

`job_id: job-prospect-know-what-i-may-do-with-the-source`

As a **Forecasting Prospect** whose security colleague asked "can we self-host this and change it?",
I want the repository's licence to state plainly what is permitted and what is not, so that I can
answer them from the repo instead of opening a vendor conversation.

#### Elevator Pitch
Before: the repo says MIT, which means "do anything", so the honest answer to "may we fork and strip
the paid gate?" is yes — and the maintainers would rather it were no.
After: open `github.com/LetPeopleWork/Lighthouse` → `LICENSE` names the `Lighthouse Source Available
License 1.0`, states that it has two binding parts and that both are in that file, then sets out the
Lighthouse Additional Terms and reproduces the Elastic License 2.0 unaltered beneath them — five
numbered prohibitions in all, one of which covers results produced by an AI assistant, each with an
explicit statement of what it does **not** restrict.
Decision enabled: the reader decides whether their intended use is permitted, without asking anyone.

**Acceptance criteria**

- AC-01.1 `LICENSE` ends with the Elastic License 2.0 reproduced **byte-for-byte unaltered** as
  Part 2. Verified by `diff <(tail -n "$(wc -l < elv2-verbatim.txt)" LICENSE) elv2-verbatim.txt` —
  a non-empty diff fails this criterion. Part 2 is always the tail of the file, so the check survives
  any future edit to Part 1.
- AC-01.2 `LICENSE` carries the Lighthouse Additional Terms as **Part 1, inside the same file and
  above Part 2** — sections 1 (no competing use), 2 (prohibited results however produced),
  3 (termination linkage) and 4 (governing law). There is no separate terms file. No mention of
  "PolyForm" or polyformproject.org appears anywhere in `LICENSE` (PolyForm's own condition for
  reusing Shield's wording).
- AC-01.3 `LICENSE` opens by stating that the licence has two parts, that both bind, that both are in
  that file, and that no rights are granted except subject to both. It states that the composite is
  not the Elastic License 2.0 and must not be identified as `Elastic-2.0`.
- AC-01.4 A statement that versions up to and including v26.9.9.9 remain under the MIT licence in
  perpetuity — in `NOTICE` and in `LICENSE`'s opening notice, never inside Part 2. `NOTICE`
  reproduces the MIT text in full. No placeholder anywhere: the forward boundary reads "every version
  released after v26.9.9.9" and needs nothing filled in at release.
- AC-01.5 The licence name and its `LicenseRef-Lighthouse-SAL-1.0` identifier are used identically in
  `LICENSE`, `NOTICE`, `README.md` and `docs/licensing/licensing.md` — one string, not four
  near-misses. Nothing claims the `Elastic-2.0` identifier.
- AC-01.6 `gh api repos/LetPeopleWork/Lighthouse --jq .license` no longer reports `mit`.
- AC-01.7 `Lighthouse.EndToEndTests/package.json` no longer declares `"license": "MIT"`.
- AC-01.8 `LICENSE` and `NOTICE` ship in every published artefact — the Docker image, the standalone
  archives and the chart — not only in the git repository.

### US-02 — See the same claim in the product that I saw in the repo

`job_id: job-maintainer-say-the-same-thing-everywhere`

As a **Forecasting Prospect** who read the LICENSE and then opened the running product, I want the
in-app and documentation copy to describe Lighthouse the same way, so that I do not have to work out
which surface is stale.

#### Elevator Pitch
Before: the LICENSE says source available while the app footer says "Licensed under MIT License" and
the feedback dialog says "completely Free and Open Source" — three answers to one question.
After: open Settings → System Info → the footer line reads `© 2026 LetPeopleWork GmbH. Source
available — see LICENSE.`, and every docs page saying "open source" about Lighthouse says "source
available" instead.
Decision enabled: the reader trusts the product's self-description enough to quote it to their
security team.

**Acceptance criteria**

- AC-02.1 `LighthouseVersion.tsx` renders the source-available line; its Vitest assertion is updated,
  not deleted.
- AC-02.2 `FeedbackDialog.tsx` lines 141 and 169 no longer claim open source; the existing
  `FeedbackDialog.test.tsx:122` assertion is updated to the new string.
- AC-02.3 `README.md`, `SECURITY.md`, `docs/index.md`, `docs/licensing/licensing.md`,
  `docs/security.md`, `docs/compliance/security-update-policy.md` make no "open source" or "MIT"
  claim about Lighthouse itself.
- AC-02.4 `docs/licensing/licensing.md` gains a short FAQ entry: what changed, when, what it means
  for an existing self-hosted instance, and that prior versions stay MIT. It also carries the
  **worked "allowed / not allowed" examples** — running a modified instance, building a plugin,
  integrating with another tool, building a substitute — stated as guidance, not as terms. Both
  review rounds asked for examples; they live here rather than in `LICENSE`, following ELv2's own
  split, because examples inside a binding document become interpretive anchors on cases nobody
  anticipated. The FAQ must not contradict Part 1 or purport to grant anything Part 1 does not.
- AC-02.5 A repeatable check (grep gate) reports zero "open source"/"MIT" claims about Lighthouse
  across the repo, while still passing on the plugin references it must not touch (D7) and on
  third-party dependency licence listings.
- AC-02.6 `pnpm test` green, `pnpm build` zero errors and zero warnings, Biome clean.

### US-03 — Find Lighthouse without being told something untrue

`job_id: job-maintainer-say-the-same-thing-everywhere`

As a **Forecasting Prospect** arriving from a search for a self-hosted alternative, I want the
website to describe the licence accurately while still telling me the thing I actually came for — the
data stays on my infrastructure — so that the page survives the check I will run on it.

#### Elevator Pitch
Before: the hero, the pricing table and a schema.org FAQ answer all assert open source, so a reader
who opens the LICENSE finds the site contradicting the repo on the first click.
After: open `letpeople.work/lighthouse` → the hero reads "Source-available flow metrics and
forecasting", the comparison row reads "Source available", and the FAQ answers the
open-source-alternative question honestly instead of with "Yes."
Decision enabled: the reader judges whether Lighthouse fits their constraints on facts that hold up
when they check.

**Acceptance criteria**

- AC-03.1 `Hero.tsx`, `LighthouseSection.tsx`, `SEO.tsx` and `Index.tsx` make no open-source claim
  about Lighthouse, including inside JSON-LD blocks.
- AC-03.2 The pricing comparison row at `Lighthouse.tsx:1307` reads "Source available" and stays a
  true-for-all-tiers row.
- AC-03.3 `public/compare/index.html`: the comparison table row and every schema.org FAQ answer are
  rewritten. The "Is there an open-source alternative…" question is either answered accurately or
  replaced — it is not left asserting "Yes."
- AC-03.4 `public/manifest.json` and `README.md:82` updated in step, so the PWA manifest and the repo
  description do not reintroduce the claim.
- AC-03.5 `AI.tsx` and `AIIntegrationSection.tsx` are unchanged — verified, not assumed (D7).
- AC-03.6 The website builds and its existing tests pass.

### US-04 — Send feedback instead of a pull request

`job_id: job-maintainer-protect-future-increments-without-going-closed`

As a **Lighthouse Maintainer**, I want someone who opens a code pull request to be told immediately
that code contributions are not accepted and where to send the thing instead, so that the new licence
position is enforced by the repo rather than by me answering each one by hand.

#### Elevator Pitch
Before: `contributions.md` opens with "We develop Lighthouse as an Open Source project, so that
people can actively contribute" — an invitation the new licence contradicts, and no mechanism behind
it either way.
After: open a pull request from a fork → within a minute it is closed with a comment naming the
feedback channels, and `docs/contributions/contributions.md` says the same thing in prose.
Decision enabled: the would-be contributor decides where to put their report or idea, instead of
waiting on a PR nobody will merge.

**Acceptance criteria**

- AC-04.1 `docs/contributions/contributions.md` states that code pull requests are not accepted and
  names what is still welcome (bug reports, feedback, docs corrections, word of mouth).
- AC-04.2 It thanks existing contributors by name without implying the door is still open, and the
  `CONTRIBUTORS.md` pointer still resolves.
- AC-04.3 A `pull_request_target` workflow closes a PR whose head repo differs from the base repo,
  posting a comment naming the feedback channel.
- AC-04.4 A PR opened from a branch inside the repo is **not** closed by the workflow.
- AC-04.5 The docs record that forks cannot be disabled on a public repository and that this is
  accepted, so the question is not re-asked later.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone**: Decide the terms → Put the terms in force → Say the same thing everywhere we control →
Say it where prospects find us → Close the channel the old terms invited.

| Slice | ADO | Stories | Ships | Est. |
|---|---|---|---|---|
| pre-slice SPIKE — draft and review the licence text | #5967 | — | Nothing user-visible; gates slice 01 | ~half a day + external turnaround |
| 01 — Licence terms in force | #5968 | US-01 | `LICENSE`, `NOTICE`, SPDX id, GitHub tag, E2E `package.json` | ~3.5h |
| 02 — The product and its docs say source available | #5969 | US-02 | in-app strings + tests, README, SECURITY, six docs pages, grep gate | ~5h |
| 03 — The website says source available | #5970 | US-03 | hero, pricing row, SEO, JSON-LD, compare page, manifest | ~5h |
| 04 — Contributions closed, and enforced | #5971 | US-04 | contributions.md, PR template, auto-close workflow | ~4h |

The SPIKE is not a slice: it has no user-visible output and would fail the slice-composition gate.
It is a precursor to slice 01, per the carpaccio rule that infrastructure lands *before* a slice
rather than *as* one.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Result |
|---|---|
| Any slice shipping 4+ new components? | No. The only new artefacts are one licence text and one workflow file. |
| Every slice depends on a new abstraction? | No. The one shared dependency is the licence text, and it ships first as slice 01. |
| Does any slice disprove a pre-commitment? | Yes — slice 01 disproves "ELv2 can be taken off the shelf", slice 03 disproves "the wording change is cosmetic", slice 04 disproves "GitHub lets you turn contributions off". |
| Synthetic data only? | **Fails by nature, and the failure is documented.** This feature has no data path. The substitute acceptance bar is the reader's view of the real public surface: the rendered GitHub sidebar, the deployed docs page, the live website, a real PR opened from a real fork. Every slice's acceptance is stated against that rendered surface, not against a source diff. |
| Two slices identical except for scale? | Slices 02 and 03 are both wording passes and were checked for merge. Kept separate: different repositories, different review and deploy paths, and slice 03 carries an SEO risk slice 02 does not. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **SPIKE first** — it is the only item with an external dependency (legal turnaround) and the
   longest lead time. Everything else is same-day work that must not queue behind it unnecessarily.
2. **Slice 01 next** — highest learning leverage. If the drafted text does not survive review, the
   wording in slices 02 and 03 would have described terms that do not exist. Failing here is cheap;
   failing after the copy has shipped means saying it twice.
3. **Slice 02 then 03** — inside-out. The repo and docs are the surface a reader reaches *from* the
   LICENSE; the website is the surface they arrive from. An inconsistency in that order reads as the
   marketing site lagging, which is ordinary. The reverse order reads as the licence being unsettled.
4. **Slice 04 last** — it is the only slice whose value does not depend on the others landing, so it
   absorbs schedule pressure best. It is also the one most likely to attract a reply, which is worth
   having after the terms are already public rather than before.

Dogfood cadence: slices 01, 02 and 04 are verifiable on the public repo the same day they merge.
Slice 03 is verifiable on the website preview deploy the same day.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement |
|---|---|---|---|
| K1 | Surfaces still claiming Lighthouse is open source, 7 days after slice 03 | **0** | The grep gate from AC-02.5, run across all three repos; excludes the plugin references (D7) and third-party dependency listings. |
| K2 | Founder time spent answering licence questions, 30 days post-change | **≤5 working days cumulative** | Self-recorded. The BDR's own re-priced estimate was 3–5 days; exceeding it means the rollout note (AC-02.4) under-explained. |
| K3 | Inbound licence questions needing an individual reply, 30 days post-change | **≤10** | Counted across Slack, email and the feedback dialog. A question already answered by the docs FAQ and re-asked anyway counts, and counts as a docs defect. |
| K4 | Organic sessions to `/compare`, 60 days post-change vs the 60 days prior | **no worse than −20%** | Website analytics. This page ranks on "open-source alternative to ActionableAgile"; rewriting the FAQ answer is the single largest known SEO risk in the feature. |
| K5 | Trial-licence requests (30-day Self-Service), 90 days post-change vs the prior 90 | **no worse than −10%** | Existing licensing funnel. This is the BDR's "must not hinder selling into orgs" constraint, made numeric. |
| K6 | Code pull requests reaching a human after slice 04 | **0** | GitHub. Any PR that is not auto-closed is a defect in AC-04.3. |
| K7 | New forks carrying divergent commits, at the 2026-11-30 revisit | **observation, no target** | The BDR's revisit trigger — reopen if a named, funded competitor ships a commercial fork *and* an identifiable deal is lost to it. Recorded so the revisit has a number to look at. |

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every slice's acceptance criteria pass — as automated tests where a test is possible (Vitest for
   the in-app strings, the grep gate for the copy sweep, a workflow run for AC-04.3/AC-04.4), and as
   a recorded check of the rendered public surface where it is not.
2. `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean on `./src`.
3. `dotnet build` zero warnings and `dotnet test` green on the non-connector filter — regression check
   only; **no backend file is edited**.
4. SonarQube Cloud introduces no new issue of any severity.
5. Mutation testing: **N/A, because** the only code changes are two literal display strings and a
   GitHub Actions workflow. Neither stack has mutable logic to score here.
6. Docs prose updated: `docs/licensing/licensing.md` carries the FAQ entry, `docs/security.md` no
   longer leans on "Lighthouse is open source" to justify its verifiability claim, and
   `docs/contributions/contributions.md` matches D8.
7. Screenshots: **N/A, because** no UI layout changes. Two text strings change and neither appears in
   a committed screenshot — to be verified against `docs/assets/` before this item is signed off,
   not assumed.
8. `ARCHITECTURE.md`: **N/A, because** no concept, port, adapter or store is added or changed.
9. Lighthouse-Clients CLI/MCP version bump: **N/A, because** both client repos stay MIT (D4) and no
   client-facing contract changes — wording corrections only.
10. Website marketing surface: **done in slice 03**, not N/A. It is the largest single surface in the
    feature and carries K4.
11. ADO: the sibling item for Premium-gate hardening (D11) exists (Epic #5972, linked `related`) —
    **done at DISCUSS close**, and each slice's Story reaches `Resolved` as it ships.
12. The BDR is updated — `outcome`, `revisited` and `learned` frontmatter, plus D10's Slack evidence
    recorded against its open item 1.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Four elevator pitches. Upstream: a founder decision taken 2026-08-31 after two 3-lens panels, carried to the board as Epic #5874. |
| 2 | Job traceability | US-01 → `job-prospect-know-what-i-may-do-with-the-source`; US-02 and US-03 → `job-maintainer-say-the-same-thing-everywhere`; US-04 → `job-maintainer-protect-future-increments-without-going-closed`. No `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 25 ACs. Each asserts a rendered string, a file's content, an API response field, or an observed workflow outcome. AC-01.1 is a literal diff against Elastic's published text. |
| 4 | Dependencies known | Three, all named in Pre-requisites. One is genuinely blocking (legal review, D13) and blocks slice 01 only. |
| 5 | Sized | Four slices, ~3.5h–5h each, plus one pre-slice SPIKE. |
| 6 | Technical feasibility | Every mechanism is established in this repo except the auto-close workflow, whose only unknown (`pull_request_target` from a fork) is named in slice 04's learning hypothesis. |
| 7 | Non-functional constraints | No runtime, performance or data-path change. The real non-functional risk is commercial, and it is carried as K4 and K5 rather than left as a worry. |
| 8 | UX defined | The exact replacement strings are fixed by D6 and enumerated per surface in the inventory. Two open wording calls — the footer line and the `/compare` FAQ answer — are explicit in AC-02.1 and AC-03.3 rather than left implicit. |
| 9 | Testable in isolation | Vitest against the two components; the grep gate runs standalone; the workflow is exercisable on a throwaway PR; the licence text is checkable by reading one file. |

**Requirements completeness: 0.96**, revised up from 0.95 on 2026-09-11 when D14 closed and the
base-text question was answered. The remaining gap is D13: the licence text cannot be finalised
inside DISCUSS because it needs a lawyer, and the AI sentence has no precedent in any software
licence to copy. The requirement is specified, its shape is fixed — one `LICENSE` in two parts,
Elastic's text untouched — and as of 2026-09-11 the words **are** drafted and have been through three
rounds of practitioner review; what they have not had is counsel. That is the SPIKE, and it is why slice 01 is second
rather than first.

**Nothing else is left open.** D14 is closed (partner hosting is not a motion we want), and the
base-text question is closed by designing around it rather than answering it.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions

- [D1] One `LICENSE` in two parts: Part 1 the Lighthouse Additional Terms, Part 2 ELv2 verbatim and
  unaltered at the tail. Permanent, no change date. ELv2's text is not edited, because no grant to
  edit it exists — our terms sit above it instead.
- [D2] FSL rejected for its 2-year conversion clock and missing key clause; its competing-use wording
  superseded by PolyForm Shield's, which reaches free clones and is explicitly adaptable.
- [D4] Lighthouse repo only; clients and Jira app stay MIT.
- [D6] "Source available" is the one public term.
- [D8] Code contributions end; enforced by an auto-close workflow, since GitHub offers no setting.
- [D10] The BDR's open scanner question is answered by observation: nobody cared about OSI status.
- [D11] Premium-gate hardening is out of scope; it lives on Epic #5972.
- [D12] The AI clause signals; the outcome clause protects.
- [D13] Legal review gates the LICENSE diff.

### Requirements Summary

- Primary jobs: let an evaluating organisation read what it may do with the source; let the
  maintainers protect future increments without closing the source; make every public surface say
  the same accurate thing on the day the change lands.
- Walking skeleton scope: none (strategy C).
- Feature type: cross-cutting — legal text, in-app strings, two documentation surfaces, a second
  repository, and repository configuration.

### Constraints Established

- Source stays public and inspectable on GitHub. This is not a move to closed source.
- OSI-approved status is explicitly not a goal.
- Every version shipped before the change date stays MIT in perpetuity.
- The change ships in the next release regardless of the Enterprise-tier launch timing (D9).
- The rollout artefact is a short dated factual note plus one docs FAQ line — not an essay campaign.

### Upstream Changes

The BDR (`2026-08-01-do-we-stay-open-source-or-not.md`) recorded ELv2 as the standing leaning and
listed three unresolved items. Two are resolved by this wave and one is deferred, none silently:

1. **"The scanner question has still never been evidenced."** → Resolved by observation (D10). The
   community was asked directly; nobody cared about OSI status, and those with a requirement needed
   "source available". The BDR's own symmetric-evidence standard is now met.
2. **"ELv2's managed-service clause vs partner hosting."** → **Resolved 2026-09-11** (D14). Partner
   hosting is not a motion LetPeopleWork wants and would not be permitted regardless, so clause 1
   restricts nothing intended and no carve-out is drafted. Record the answer in the BDR.
3. **"Option E — harden the Premium gate technically."** → Deferred out of this epic with a reason,
   and requires its own ADO item (D11).

**One BDR assumption is changed by this wave.** The BDR concluded, in its 2026-08-28 comparison:
*"If we relicense, ELv2 is the instrument — it says what we mean, off the shelf, with no expiry and
lighter baggage."* That is no longer accurate. ELv2 says **part** of what we mean: it prohibits
hosted resale and key circumvention, and prohibits nothing about deriving a substitute from the
source. The BDR was written before AI-assisted derivation was stated as a first-class threat, and
against a framing in which key-stripping was the whole of the concern. The revised assumption: ELv2
is the **base text**, not the instrument, and the instrument is a named Lighthouse licence that adds
a competing-use clause and an AI sentence to it (D1). The BDR document is not edited; this section is
the record of the change.

---

## Wave: DISCUSS / [WHY] Alternatives Considered

Rendered on request. Triggered by cross-context complexity — five surfaces across three repositories
and four technologies. Written so that a re-litigation, or a lawyer picking this up cold, can see
what was weighed and rejected without re-deriving it.

### D1 — The instrument

Eight candidates were live at some point across the two BDR panels, the 2026-08-28 comparison and
this wave. Ordered by how close each came.

**Stay MIT.** The commercial lens' position, and it was never refuted — only outweighed. Its argument
stands on its own terms: CHF 6,401 against 40,000 with 58% of the year gone is a demand problem, not
a leakage problem, and no licence text closes a CHF 33,600 gap. It also had the strongest evidentiary
footing: four forks exist, all inert, newest commit the maintainer's own; no lost deal has ever been
attributed to the licence. Rejected because the decision is about future exposure rather than current
revenue, and because "nothing has happened yet" is the argument that is only ever wrong once. The
honest framing is that this was a founder call against the loudest single argument in the record, and
it should not be expected to behave like a revenue move.

**AGPL.** Examined and dropped before the second panel. It blocks closed-source rebranding but
explicitly permits an organisation self-hosting and never paying — which is the outcome the epic
cares least about preventing and the one AGPL is worst at. It also costs enterprise OSS-governance
friction without buying anything the goal names. Dropped on fit, not on ideology.

**BUSL 1.1.** Carried into the second panel as a serious option and rejected on three counts. It
requires drafting an Additional Use Grant — bespoke prose encoding the current tiers — and all three
lenses independently concluded that grant buys nothing against the failure mode it was proposed to
fix, because scanners classify on the SPDX tag at ingestion and never open the text. It carries a
Change Date after which the whole thing converts to an open licence, which for a two-person company
means committing today to giving away the thing whose protection is the entire point. And two lenses
named the Terraform→OpenTofu memory as heavier baggage with this audience than ELv2's Elasticsearch
episode, because it is more recent and was read as a betrayal by exactly the practitioner crowd
Lighthouse sells to.

**FSL 1.1 (Functional Source License).** Raised in this wave, not in the BDR, and it is the closest
near-miss in the list. Its Competing Use definition — *"making the Software available to others in a
commercial product or service that: 1. substitutes for the Software; 2. substitutes for any other
product or service we offer using the Software…; 3. offers the same or substantially similar
functionality"* — is a better statement of the first thing the epic set out to prevent than anything
in ELv2, and D1 borrows it. What killed it as a whole instrument is the other half: *"We hereby
irrevocably grant you an additional license to use the Software under the Apache License, Version 2.0
that is effective on the second anniversary of the date we make the Software available."* Two years
per version, against BUSL's four, on the exact axis BUSL had already been rejected for. It also
carries no licence-key clause at all and states plainly that modification and redistribution of
changes are permitted. Taking the clause without the clock was the only way to use it.

**Plain ELv2, unchanged.** The BDR's standing leaning, preferred by 2 of 3 of the second panel, and
what this wave was expected to ratify. It is off the shelf, recognised, permanent, needs no drafting,
and its clause 2 names the licence-key strip point without us having to phrase it. It was rejected
only because the epic's scope grew: ELv2 restricts nothing about modification or derivation, so under
plain ELv2 an organisation may legally build a rival forecasting product from this source provided it
never hosts it for others and never touches the key. That is half the stated goal, unprotected. Had
the goal stayed where the BDR left it — key-stripping and hosted resale — plain ELv2 would be the
right answer and this feature would be a two-day job.

**ELv2 plus a non-binding AI NOTICE.** Considered as the cheap middle path: keep the clean ELv2 SPDX
tag so scanners see a known family, and state the AI position in a `NOTICE` or `ai.txt` that is not a
licence term. Rejected because it is the worst of both — it advertises a restriction it does not
impose, and a reader who checks will find that out. A position stated without force invites exactly
the test it cannot survive. **Do not confuse this with what was chosen**: D1's second document is a
*binding* term incorporated by reference, not a notice. The difference is the whole point.

**PolyForm Strict.** Use only: no modification, no distribution, by anyone. It covers every line of
the brief and is off the shelf. Rejected because it forbids a customer patching their own instance,
which is a substantial part of why anyone self-hosts, and it forecloses the inspect-and-adapt posture
that the "your data never leaves your network" claim rests on. It would also be the one option that
genuinely reads as going closed, which the founder decision explicitly ruled out.

**PolyForm Shield, adapted, as the whole instrument.** The strongest option not taken. Shield's
Noncompete is the broadest competing-use text available — products compete across interfaces,
platforms and languages, *"even when provided free of charge"* — it is permanent, its Changes and New
Works License expressly preserves customer self-modification, and **PolyForm is the only steward that
grants permission to adapt its text**, on the condition that every mention of "PolyForm" and
polyformproject.org is stripped. It was the recommendation. Not taken because ELv2's licence-key
clause is the one piece of off-the-shelf text that names our actual strip point by name, and the
two-document shape keeps it verbatim while still reaching Shield's substance through the second file.
Shield's *wording* is used for the no-competing-use clause regardless (D2) — what was declined is
making Shield the base document.

**A licence drafted entirely from scratch.** Maximum fit to intent, zero recognition, every clause
ours to defend, highest legal cost and the longest lead time on the one item already on the critical
path. Held as the SPIKE's fallback.

**What was chosen and why it is not free.** `LICENSE` carries ELv2 verbatim; a second binding
document adds no-competing-use (Shield's wording) and the AI sentence. Permanent, no clock, whole
brief covered, and Elastic's text never edited. The cost is real: the composite is not a licence
anyone recognises, it must not be called ELv2 or carry `Elastic-2.0`, and a reader or scanner that
opens only `LICENSE` sees plain ELv2 and may never learn the second file exists — which is why the
header block is a requirement rather than a nicety. Plain ELv2 would have cost none of this and
protected roughly half as much.

### The base-text question, and why it reshaped D1

The first form of D1 adapted ELv2's text directly. Research on 2026-09-11 found no grant to do that,
and found that one licence family grants it explicitly. What was checked:

| Base text | Explicit permission to publish a **modified** version? |
|---|---|
| **ELv2** | **None.** No modified-versions clause; the seven-question FAQ never raises adaptation; no copyright notice or reuse statement on the document. "Elastic License" is a trademark. Verbatim adoption is clearly fine and common — SPDX lists `Elastic-2.0` and third-party projects ship it unchanged. |
| **FSL 1.1** | **None.** A template with `${year}` / `${licensor name}` placeholders, plainly built for adoption, but `getsentry/fsl.software` carries no `LICENSE` and no `NOTICE`, and the template says nothing about reusing its own wording. |
| **PolyForm** | **Yes, explicitly.** Its README: *"If you make changes to a PolyForm license, you must remove all mention of 'PolyForm' and polyformproject.org, as well."* Plus a contributor permission grant over the text. |
| **MPL 2.0** | Yes — §10.3 permits a renamed modified version. The model PolyForm follows. |
| **Apache-2.0** | Verbatim adoption fine; no modification grant. |

Licence texts are copyrightable works. "Nobody would sue over a licence text" may well be true and is
not a basis a two-person company should build its own licence on, particularly when the alternative
costs nothing. So the question is not answered — it is designed around. Elastic's text is reproduced
unaltered and our clauses live in a second binding document, which is structurally what BUSL does
(fixed template plus a grant the licensor writes) and what any dual-licence arrangement does.

This is research, not legal advice, and it narrows D13's review rather than replacing it.

### D12 — Why the AI clause is not the mechanism

The alternative framing, and the one the request arrived in, is that the licence should name AI
agents and prohibit them from modifying the source. It was not adopted as the load-bearing clause for
two reasons.

First, it under-reaches on the case that matters. If the prohibition attaches to the tool, then the
same person doing the same thing by hand is permitted, and the clause is trivially defeated by a
claim nobody can disprove. If it attaches to the outcome — you may not produce a substitute, strip
the key, or remove the notices — then it holds regardless of what was in the editor, because a
derivative work is a derivative work and copyright has never asked who typed it.

Second, it over-reaches on the case it appears to solve. The training-corpus scenario — source
ingested into model weights, similar code later emitted to someone who never opened the repo — is not
addressed by any tested clause anywhere. No mainstream software licence carries one: ELv2, BUSL, FSL,
PolyForm and SSPL are all silent, and the AI restrictions that do exist live in model licences
(Llama's Acceptable Use Policy, the RAIL family) and content contracts. A clause here would be novel,
contested, and undetectable in practice.

So the AI sentence ships — it states the position, it removes a "we didn't know" defence, and it is
the sentence a reader will actually look for — layered on top of an outcome clause that does the
work. The alternative of shipping it alone was rejected as protection theatre.

### D4 — Repo scope

**All three product repos.** Consistent story, no "which repo is which licence" question. Rejected
because `lighthouse-clients` is an MCP server and CLI, and the whole point of those is to be trivial
to pull into someone's toolchain. An MIT npm package clears that bar; a `LicenseRef-` one invites a
review. They carry neither the forecasting engine nor the gate, so there is nothing there worth
protecting at that price.

**Lighthouse + the Jira app, clients stay MIT.** A defensible middle — relicense the
Atlassian-distributed surface too. Rejected because Forge apps are distributed through Atlassian's
marketplace under its own terms, and adding a bespoke licence to that pipeline is a question nobody
needs to answer this quarter.

### D6 — The public term

**"Fair source."** Warmer framing, and there is an umbrella definition behind it (fair.io). Rejected
on two grounds: low recognition with this audience, and — the decisive one — the fair-source
definition requires delayed open source, i.e. a conversion clock. Using the label without the clock
would be the same category error as calling the licence ELv2.

**"Source code public and inspectable."** The most accurate phrase available, and it names the
property that actually does the work. Rejected as a *term*: it cannot go in a pricing-table cell, a
page title or a tab, and a term that needs a different phrasing on every surface is how the drift
this feature exists to prevent starts again. It survives as the *sentence* the licence and the FAQ
lead with — see the journey's "a reader concludes the source has been closed" error path.

**"Open code, not open source."** Memorable, and honest. Rejected because it defines the product by
what it is not, and leads with the word being taken away.

### D8 — Contributions

**Keep accepting PRs with an inbound-equals-outbound note.** Lowest friction, preserves the framing
`contributions.md` opens with today. Rejected on the evidence: seven commits from two people across
the project's life, Issues and Discussions already switched off. The invitation was not producing
much, and keeping it alive under a licence that prohibits derivation is a contradiction a careful
reader will spot.

**Keep accepting PRs behind a CLA.** Legally the cleanest arrangement for a source-available product
with a paid gate. Rejected because a signature step at this volume deters the drive-by docs fix and
keeps nothing else — it is ceremony sized for a community that does not exist.

**Disabling forks or pull requests outright.** Not an alternative, a fact: GitHub's forking policy
covers private and internal repositories only, and there is no setting to disable pull requests on a
public repo at all. Interaction limits were considered and rejected — they expire after at most six
months and would need re-arming forever. The `pull_request_target` workflow is not the preferred
mechanism; it is the only one.

### D9 — Sequencing

**Wait one release cycle after the Enterprise launch.** Removes the "the free thing got narrowed the
month the paid thing shipped" reading entirely, which the social lens named unprompted and is a real
effect. Rejected because the audience it would protect against is 17 stars, two outside contributors
and zero of sixteen connection records citing GitHub — the optics were sized against a crowd that is
not watching.

**Wording first, LICENSE later.** Spreads the change so neither half lands as one loud event.
Rejected because it inverts the honest order: copy claiming source-available while `LICENSE` still
says MIT is a contradiction the reader catches in the direction that reads as the licence being
unsettled. The chosen order produces the opposite inconsistency — repo correct, marketing site
lagging — which reads as ordinary and is why slices 02 and 03 follow 01 rather than precede it.

### D11 — Keeping gate hardening inside this epic

Folding Option E in would have made a single epic carry a licence text, a five-surface copy sweep, a
CI workflow, and a security-engineering design spanning server-side verification, attestation and
obfuscation. That fails this feature's own scope gate on effort and on independent-outcome count, and
it would have let the slow, uncertain half hold the fast, certain half hostage. Split to Epic #5972.
The trade accepted with the split is that until #5972 ships, `LicenseGuardAttribute.cs` remains a
one-file strip point and the relicence makes stripping *prohibited* rather than *hard*.
