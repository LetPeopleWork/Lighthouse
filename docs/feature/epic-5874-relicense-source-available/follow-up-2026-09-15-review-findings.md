# Licence follow-up — review findings, 2026-09-15

> **Status: work list, ready to hand off.** Six items found in a post-landing review of the shipped
> `LICENSE` and `NOTICE`. **None of them block, and none change the licensing decision.** The
> instrument is sound; these are refinements, a documentation gap, and one piece of stale
> housekeeping.
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

---

## Suggested order

| # | Item | Why this position |
|---|---|---|
| 1 | Machine-readable TDM reservation | Only item where intent may not currently be achieved |
| 2 | Internal-dashboard worked example | First question a real buyer asks; docs-only, no licence change |
| 6 | Regenerate SBOM | Cheap, published, compliance-adjacent |
| 4 | Procurement friction | Commercial upside, pairs with the Focusrite review |
| 5 | Part 1 / Part 2 clarifier | One sentence, but needs the full reassemble-and-verify cycle |
| 3 | Contract-formation confirmation | Bundle with counsel alongside item 1 |

Items 1 and 3 are one conversation with counsel. Items 2 and 4 are one documentation pass. Item 5
is the only one that touches the licence text, and it must go through the assembly process.

## Do not

- Hand-edit `/LICENSE`, and above all do not touch Part 2. Byte-identity with Elastic's text is
  load-bearing — verify it after any reassembly.
- Change the version boundary or anything about the MIT grant for prior releases.
- Treat any of the above as blocking. The licence is live and sound; this is a refinement pass.
