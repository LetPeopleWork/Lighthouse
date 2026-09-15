# Licence follow-up — review findings, 2026-09-15

> **Status: worked through, 2026-09-15.** Four items are done, one is closed as already
> answered, one is parked on counsel. **Read the outcome table at the bottom before the items** —
> three of the six turned out to rest on premises that do not hold, and the text below each item
> is the *original* finding, kept as written so the reasoning stays auditable. Each item now
> carries a **Resolved** note recording what was actually true and what was done.
>
> **The review also missed something that outranked all six.** The drafting sources had fallen out
> of sync with the shipped `LICENSE`, so the documented way to change the licence would have
> silently reverted the `336a19747` amendment. Item 5 would have been the change that did it. See
> **Item 0** immediately below.
>
> Six items were found in a post-landing review of the shipped `LICENSE` and `NOTICE`. **None of
> them block, and none change the licensing decision.** The instrument is sound; these are
> refinements, a documentation gap, and one piece of stale housekeeping.
>
> **This was an engineering and commercial review, not a legal one.** The licence already carries
> four practitioner review rounds and two counsel opinions. Where an item below touches legal
> effect (items 1 and 3), the action is *ask counsel the specific question*, not *change the text
> on this document's say-so*.

## Background for whoever picks this up

Lighthouse relicensed off MIT on 2026-09-11 (`75d58812a`), with a post-landing amendment on
`336a19747` permitting services run on your own deployment. The instrument is the **Lighthouse
Source Available Licence 1.0**, SPDX `LicenseRef-Lighthouse-SAL-1.0`.

It is **one file in two binding parts**:

- **Part 1** — the Lighthouse Additional Terms. Our own text. Sections 1–7. The only novel drafting.
- **Part 2** — the Elastic License 2.0, reproduced byte-for-byte.

Two structural facts that constrain every change below:

1. **Part 2 must stay byte-identical to Elastic's canonical text.** Verified 2026-09-15 against
   `elastic/elasticsearch:licenses/ELASTIC-LICENSE-2.0.txt` — 93 lines, clean diff. ELv2 has no
   modified-versions provision and "Elastic License" is a trademark, so editing it is the one
   mistake the whole two-part structure exists to prevent. **Never hand-edit `/LICENSE`.**
2. **The licence is assembled, not hand-written.** Per
   `docs/feature/epic-5874-relicense-source-available/drafts/README.md`: edit `license-header.txt`
   or `license-terms.txt`, reassemble, verify the ELv2 tail is still byte-identical, then copy the
   result to `/LICENSE`. `LICENSE.draft` is a historical snapshot and will drift — do not read it
   for current terms.

Prior versions stay MIT in perpetuity (through v26.9.9.9, 9 September 2026). The boundary is the
commit that introduced `LICENSE`, not the tag. Nothing below may change that.

---

## 0. The drafting sources no longer reproduced `LICENSE` — found while working the list

**Not in the original six. It outranked all of them, and it is fixed.**

`336a19747` ("permit services run on your own deployment") edited `/LICENSE` by hand and left the
assembly sources behind. From 11 September, `license-terms.txt` was missing the whole
`PROFESSIONAL SERVICES PERFORMED ON YOUR OWN DEPLOYMENT ARE PERMITTED` paragraph and the closing
"line Part 2 draws" sentence.

The consequence is the sharp part. The drafts README documents the way to change the licence as:
edit the sources, reassemble, copy the result over `/LICENSE`. Following that documented process
would have **deleted the amendment**, quietly, as a side effect of an unrelated edit. Item 5 is the
only item on this list that touches licence text. It would have been the one that did it.

**Resolved 2026-09-15** in `db64aaf51`. Both hunks back-ported; sources reproduce `/LICENSE`
byte-for-byte again. The check that would have caught it is now in the drafts README, framed as
something to run *before* editing — the existing ELv2-tail check cannot see a hand-edit to Part 1,
because it only looks at Part 2. The README also now records why its stored `sha256` for
`elv2-verbatim.txt` never matches a local `sha256sum` on Windows: `text=auto` gives the working tree
CRLF while the committed blob is LF, which is the 3860-vs-3953 byte gap.

---

## 1. Make the TDM / AI reservation machine-readable

**Priority: highest of the six.** This is the one item where the current state may not achieve what
the text intends.

**What's there.** Part 1 §2 reserves TDM rights in prose: *"The licensor expressly reserves the
right to reproduce and extract the software, and any part of it, for text and data mining,
including for the training, fine-tuning or evaluation of any automated or machine-learning
system."*

**The problem.** Under the EU DSM Directive Art. 4(3), a rights-holder opt-out from the TDM
exception must be expressed in a **machine-readable** form for content made publicly available
online. A paragraph inside `LICENSE` is human-readable. It is at best unsettled whether it
satisfies the machine-readable requirement, and the crawlers this clause is aimed at do not parse
licence prose.

**What to do.**

- Add a repository-root machine-readable signal alongside the prose. At minimum a `robots.txt`;
  consider an `ai.txt` / TDM reservation file. Keep the prose clause — this supplements it, it does
  not replace it.
- Mirror the reservation anywhere the source is served that is not the Git host.
- The prose in §2 does not need to change.

**Ask counsel.** *Does our prose reservation satisfy DSM Art. 4(3), and what machine-readable form
would they want alongside it?* This is a narrow, cheap question with a concrete answer.

**Done when:** a machine-readable reservation is live at the repository root and referenced from
the licensing docs page.

**Resolved 2026-09-15 — no action taken, and the proposed action should not be taken.** This was
already worked in review round 3 and the answer is in the drafts README. Three corrections:

- **The mechanism is wrong.** It is W3C TDMRep at `/.well-known/tdmrep.json` with `tdm-reservation`
  and `tdm-policy`, not `robots.txt` or `ai.txt` at the repository root.
- **A repository-root file cannot work.** TDMRep is served from an *origin's* `/.well-known/` path.
  We control `letpeople.work` and `docs.lighthouse.letpeople.work`; we do not control `github.com`,
  which is where the source sits and where a crawler meets it. A file in the repo root is not
  served at any origin's `/.well-known/`.
- **The counsel question already exists**, as **C2**, and it is sharper than the one proposed here.
  C2(c) asks whether a reservation that cannot reach the GitHub copy leaves us *worse off* than the
  clean contractual prohibition — and C2(d) asks whether a W3C Community Group Report is
  "appropriate" for Art. 4(3) at all.

Publishing anything now pre-empts a decision that was deliberately deferred. The §2 prose stays as
it is, which this item already agreed. **Next step is counsel answering C2, not an engineering
change.**

---

## 2. Write the "we built our own internal dashboard" worked example

**Priority: high — this is the clause a buyer will ask about.**

**What's there.** Part 1 §1 prohibits building a separate substitute product, and explicitly
extends to internal use: *"This applies whether you make that product available to others or deploy
it only inside your company."*

**Why it matters.** That is broader than ELv2 alone and broader than a typical BUSL production-use
limit. The realistic scenario: a customer's platform team, having run Lighthouse, builds an internal
forecasting dashboard. Is providing *"substantially the same core functionality"* its *"primary
purpose"*? Arguably yes — which means a sophisticated engineering org reading carefully will stop
here and raise it in procurement. Our ICP skews exactly that way.

§1.1 already carves out a lot (plugins, extensions, integrations, tools consuming output, modified
deployments, and a broader product with incidental overlap). The gap is not the drafting — it is
that **no worked example covers the internal-dashboard case**, which is the first one a platform
team will think of.

**What to do.** Add a worked example to the licensing docs page — *not* to `LICENSE` itself. The
licence already points there: *"For worked examples of what this license allows and does not allow,
see the licensing page in the Lighthouse documentation."* Examples are guidance and expressly do not
expand or restrict the grant, which is exactly the right place for a judgement call like this.

Cover at least:

- Internal dashboard that **reads Lighthouse output** (forecasts, exports, API) → permitted, it is a
  tool consuming output under §1.1.
- Internal dashboard that **reimplements the forecasting** to replace Lighthouse → this is the
  prohibited case; say so plainly.
- Internal tool that does something adjacent and happens to overlap → permitted, unless
  substituting is its primary purpose.

**Done when:** the licensing docs page answers "can we build our own internal dashboard?" without
the reader needing to interpret §1 themselves.

**Resolved 2026-09-15** in `0a6c4e7c9`. One correction to the premise: two of the three cases were
already on the page — "a dashboard that consumes the output" under Allowed, and "a separate
forecasting product … even inside one company" under Not allowed. The real gap was that they sat in
two different tables, so finding the answer meant reconstructing the rule rather than reading it,
and the middle case (adjacent tool with incidental overlap) had no row at all.

The page now asks the question as a question, in a note beside the consultant one, and splits it
the three ways it splits. The missing middle row is added. It closes with the test itself — built
to replace, or built to work with — so a case none of the three covers is still decidable.

---

## 3. Know where Part 1's contract-formation reach is thin

**Priority: medium. Likely no action beyond a counsel confirmation — this is a "know the limit" item.**

**What's there.** Part 1 is candid that some prohibitions bind by contract rather than copyright:
*"Where a prohibited use would not, absent a license, infringe any such right, the prohibition still
binds you as a term of this license… Nothing in this license purports to make conduct infringing
that would not otherwise be so."* That honesty is good drafting.

**The tension.** Part 1 applies *"from the moment you obtain any part of the software… They do not
depend on your running the software."* But the acceptance hook is ELv2's *"By using the software,
you agree to all of the terms and conditions below."* Against someone who only ever **read or
cloned** the source and never ran it, the contractual terms rest on much weaker formation. The
copyright-based prohibitions are unaffected; it is the contract-only branch that is exposed.

**What to do.** Nothing to the text unless counsel says otherwise. Confirm with counsel that they
considered the obtain-but-never-run case and are comfortable. Then **record the answer** in the
drafting record so it is not rediscovered later.

**Done when:** the drafting record states the position on contract formation for non-running
recipients.

**Resolved 2026-09-15** in `810e6e8c3` — the question is now asked; the answer is counsel's.
This was a genuine gap: A2 covers scope-versus-covenant, and nothing in the question set covered
formation. Added as **A6**, with the point sharpened. It is narrower than it first looks, and that
is what makes it answerable: where the prohibited act infringes copyright, A2's scope sentence does
the work and formation is beside the point. It bites only on A2's deliberately preserved
contract-only branch — and the clearest instance of that branch is the training-data case in C2,
where the recipient is least likely to have run anything at all. A6 cross-references C2 for that
reason.

A6 also asks the follow-on this item did not: whether a second acceptance hook in Part 1 ("by
obtaining, copying or distributing…") would help, or whether a second, differently-worded trigger
alongside Part 2's costs more than it gains. Part 2 cannot be edited, so any such hook lives in
Part 1 alone.

---

## 4. Reduce procurement friction

**Priority: medium — commercial, not legal.**

**The problem.** `LICENSE` is ~17 KB and Part 1 is dense. In a self-service motion at CHF 2,000
nobody reads it. The people who *do* read it are enterprise legal, who bill hours against it — and
those are the deals worth most.

**What to do.**

- Link the licensing docs page **from the top of `LICENSE`**, not only from the closing line before
  Part 2. A reader who opens the file should see the plain-language route immediately.
- Give the licensing docs page a short "what this means for you" summary above the worked examples,
  segmented by reader: *running it internally* / *consulting on a client's deployment* / *building
  on top of it* / *evaluating it*.
- Make sure it is reachable from the pricing page and the trial flow, not just the docs tree.

**Related work in flight:** Focusrite have an open internal IT/security review. That is the first
real test of this friction with a live account — worth using their questions as the source list for
the FAQ rather than guessing.

**Done when:** a non-lawyer can answer "what am I allowed to do" in under two minutes without
opening `LICENSE`.

**Resolved 2026-09-15**, in `bfe39fd7a` (the `LICENSE` link) and `0a6c4e7c9` (the docs page).

- The docs link is now at the **top** of `LICENSE`, above the terms, instead of ~12 KB in at the
  last paragraph before Part 2. Deliberately a signpost only: the operative statement that the
  examples neither expand nor restrict the grant stays in its one existing place rather than being
  restated, so the two cannot drift and leave a question about which governs.
- The docs page opens with four labelled reader segments — running it internally, consulting with
  it, building on it, evaluating it — instead of one section addressed to one of those four while
  the other three inferred their answer from tables written for everyone. Same rules, no new
  permissions.
- The README now links the readable version; it named the licence but offered no route to it.

**Not done, and out of this repository:** the pricing page and the trial flow live on
`letpeople.work`. Still worth doing, and still worth sourcing the FAQ from Focusrite's actual
questions rather than guessing.

---

## 5. Add a "Part 1 never expands Part 2" clarifier

**Priority: low. Drafting nit, one sentence.**

**What's there.** The header says Part 1 *"limit[s] the rights granted in Part 2"* and that *"Where
the two parts conflict, these Additional Terms govern."* Meanwhile §1.1 carries permissive-sounding
headers — `SERVICES DELIVERED TO SOMEONE ELSE'S DEPLOYMENT ARE PERMITTED`, `PROFESSIONAL SERVICES
PERFORMED ON YOUR OWN DEPLOYMENT ARE PERMITTED`.

The drafting is actually careful here — the managed-service paragraph explicitly defers to Part 2
(*"That is a hosted or managed service, and Part 2 prohibits it separately"*), so the trap was
avoided. But a reader looking for room could pair the conflict rule with a `...ARE PERMITTED` header
and argue Part 1 grants something Part 2 forbids.

**What to do.** One sentence in `license-header.txt`, near the existing conflict rule, to the effect
that **Part 1 only ever narrows Part 2 and grants nothing**; where Part 1 says something is
permitted, it means Part 1 does not restrict it, not that Part 2's limits fall away.

Reassemble per the drafts README, re-verify the ELv2 tail is byte-identical, then copy to `/LICENSE`.

**Done when:** the header forecloses the "Part 1 expands Part 2" reading explicitly.

**Resolved 2026-09-15** in `bfe39fd7a`. One placement change: the conflict rule is not in
`license-header.txt`, it is the opening of `license-terms.txt`. The clarifier went there, beside it,
where the misreading would start:

> *Part 1 only ever narrows Part 2. It grants nothing. Where Part 1 says that something is
> permitted, that means Part 1 does not restrict it - not that Part 2's limits fall away. A use must
> be permitted by both parts to be licensed.*

Assembled through the documented process, not hand-edited — and only because item 0 was fixed
first. Both checks pass: sources reproduce `/LICENSE`, ELv2 tail byte-identical.

---

## 6. Regenerate the published SBOM

**Priority: low, but it is published and compliance-adjacent.**

**What's there.** `Lighthouse.Frontend/public/sbom/backend.cdx.json` carries a metadata timestamp of
**2026-03-31** — roughly six months stale, predating both the relicence and a good deal of
dependency churn. It is served under `public/` and referenced from
`docs/compliance/security-update-policy.md`.

**The good news:** its own `metadata.component` declares **no** licence, so there is no false MIT
claim for Lighthouse itself. The MIT strings in that file are genuine third-party dependency
licences. This is staleness, not a misstatement.

**What to do.**

- Regenerate the SBOM against current dependencies.
- Set `metadata.component.licenses` to `LicenseRef-Lighthouse-SAL-1.0` so the published SBOM states
  the licence positively rather than omitting it.
- Note `metadata.component.version` is `0.0.0` — worth wiring to the real version while in there.
- If SBOM generation is not already in the release pipeline, that is the actual fix; a manual
  regeneration will just go stale again.

**Done when:** the published SBOM reflects current dependencies and declares the correct licence,
ideally generated on release rather than by hand.

**Resolved 2026-09-15** in `f4a23e7d1` — but not as proposed. The premise is wrong in a way worth
recording, because it is the kind of wrong that wastes an afternoon:

- **The committed files are placeholders, by design.** `ci_sbom.yml` generates real SBOMs and CI
  overwrites these from the build artifacts. Regenerating them by hand fixes nothing that ships and
  re-stales immediately. The item's own closing line — "if SBOM generation is not already in the
  release pipeline, that is the actual fix" — was the right instinct; it *is* in the pipeline.
- **The packaging gap it half-detected is already closed.** When Docker packaging shipped the
  placeholders it was RCA'd in `docs/analysis/docker-sbom-stale-placeholders-rca.md`, and
  `ci_docker.yml` now downloads and copies like the other three.
- **The frontend placeholder is worse than the backend one** and went unmentioned: one component,
  for an entire React app.

What survived is the licence point, moved to where it takes effect. Both generators describe
Lighthouse itself as `0.0.0` with no licence, because they read that from the `.sln` and
`package.json`. `Scripts/stamp-sbom-metadata.js` now stamps the real version and
`LicenseRef-Lighthouse-SAL-1.0`'s name and URL onto `metadata.component` after generation —
restamping `bom-ref` and `purl` with it, since both embed the version. Written as a script rather
than inline `jq` so it could be run against both real SBOMs before shipping, which is how the
`purl` handling was verified.

---

## Outcome, 2026-09-15

| # | Item | Outcome | Where |
|---|---|---|---|
| 0 | Sources no longer reproduced `LICENSE` | **Fixed.** Not on the original list; outranked everything on it | `db64aaf51` |
| 1 | Machine-readable TDM reservation | **Closed, no action.** Already answered in review round 3; proposed mechanism does not work, and the counsel question exists as C2 | — |
| 2 | Internal-dashboard worked example | **Done.** Two of three cases were already there; the gap was findability plus the middle case | `0a6c4e7c9` |
| 3 | Contract formation, non-running recipient | **Asked.** Genuine gap; now question A6. Answer is counsel's | `810e6e8c3` |
| 4 | Procurement friction | **Done in-repo.** Pricing page and trial flow are on `letpeople.work`, out of scope here | `bfe39fd7a`, `0a6c4e7c9` |
| 5 | Part 1 / Part 2 clarifier | **Done.** Placed with the conflict rule in `license-terms.txt`, not the header | `bfe39fd7a` |
| 6 | Regenerate SBOM | **Done differently.** Committed files are CI-overwritten placeholders; the licence fix belongs in the generator | `f4a23e7d1` |

### What is actually left

1. **Counsel, one conversation:** C2 (does the prose reservation satisfy Art. 4(3), and does a
   reservation that cannot reach the GitHub copy help or hurt) and A6 (formation against a
   recipient who obtained but never ran). Nothing downstream of item 1 moves until C2 is answered.
2. **`letpeople.work`:** link the licensing page from the pricing page and the trial flow. Source
   the FAQ from Focusrite's IT/security review questions rather than guessing.

### What this review got wrong, and the pattern in it

Three of six items rested on premises that did not hold, and all three failed the same way: **the
answer was already recorded somewhere in the repository, and the review did not look.** Item 1 was
settled in the drafts README's round-3 table. Item 6's placeholders are explained by an RCA in
`docs/analysis`. Item 2's missing examples were two-thirds present on the page it proposed to
change.

Meanwhile the one problem that genuinely threatened the licence — item 0 — was invisible to a
review that read the shipped `LICENSE` and the drafting record but never checked them *against each
other*. Reading both is not the same as diffing them. The check is one command, and it is now in
the drafts README.

## Do not

- Hand-edit `/LICENSE`, and above all do not touch Part 2. Byte-identity with Elastic's text is
  load-bearing — verify it after any reassembly. **This has already happened once** (item 0); the
  reproduce-check now in the drafts README is what catches it, and it has to be run *before*
  editing, not after.
- Change the version boundary or anything about the MIT grant for prior releases.
- Treat any of the above as blocking. The licence is live and sound; this is a refinement pass.
