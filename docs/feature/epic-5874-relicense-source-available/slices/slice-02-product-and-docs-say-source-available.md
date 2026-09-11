# Slice 02 — The product and its docs say source available

**Feature**: `epic-5874-relicense-source-available` | **Stories**: US-02 | **ADO**: [#5969](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5969) | **Estimate**: ~5h
**Depends on**: slice 01 merged (the copy must not describe terms that are not yet in force)

## Goal

A reader who has just read the new `LICENSE` opens the running product and the documentation site and
finds them saying the same thing — so the licence change reads as one decision, not as three surfaces
disagreeing about what Lighthouse is.

## Learning hypothesis

**"Open source" is load-bearing in at least one sentence, and swapping the words leaves a claim that
no longer stands up.**

Disproves if it fails: the assumption that this is a find-and-replace. `docs/security.md:32` reads
"Everything above is checkable. Lighthouse is open source, so the paragraphs below name what to look
for." The word is doing argumentative work there — it is the warrant for the verifiability claim that
the BDR called the strongest positioning asset we own. "Source available" still supports it (the
source is still readable), but the sentence has to be rewritten to say so rather than patched.
`docs/index.md:15` has the same shape: "provided free of charge as open-source software" bundles
price and licence into one clause, and only one half of it changes.

Confirms if it succeeds: that the remaining surfaces really are mechanical, and that the grep gate
from AC-02.5 can be trusted as the check rather than as a suggestion.

## Production data

No data path. The acceptance bar is the **rendered public surface**: the deployed docs pages, and the
two strings as they appear in a running Lighthouse instance — the System Info footer and the feedback
dialog — not the source lines.

## Dogfood moment

Same day: start Lighthouse locally (`Start-DevServer.ps1`), open Settings → System Info and read the
footer, then open the feedback dialog from the header and read both paragraphs. Then open the built
docs site and walk the six changed pages.

## IN scope

- `LighthouseVersion.tsx:392` — the footer licence line, and its Vitest assertion updated rather than
  deleted (AC-02.1).
- `FeedbackDialog.tsx:141` and `:169`, and the `FeedbackDialog.test.tsx:122` assertion (AC-02.2).
- `README.md:5,17,103` and `SECURITY.md:70` (AC-02.3).
- `docs/index.md:7,15`, `docs/licensing/licensing.md:9`, `docs/security.md:32`,
  `docs/compliance/security-update-policy.md:23` (AC-02.3) — with `security.md` and `index.md`
  rewritten rather than word-swapped, per the hypothesis above.
- `docs/licensing/licensing.md` gains the source-licence section — **drafted and ready** in
  `../drafts/licensing-faq.md`, which contains both the replacement opening line and the section to
  insert (AC-02.4). It leads by disambiguating the two things that page calls a "license": the
  source-code licence, and the premium `license.json` key. Counsel wanted those kept apart, and that
  page is where they would otherwise get confused. It carries the worked allowed / not-allowed
  tables. This is one of the two rollout artefacts the BDR settled on; the other is the dated note,
  which is not a code change.
- **`docs/contributions/contributions.md`** — moved here when slice 04 was dropped. Its opening line
  is *"We develop Ligththouse as an Open Source project, so that people can actively contribute"*,
  which the claim gate flags and which the new licence contradicts. The prose fix lands here: say
  what is genuinely welcome — bug reports, feedback, docs corrections, word of mouth — without the
  open-source framing, and keep the existing contributors' thanks. It also carries a typo that has
  been live for a while. **What does not land is slice 04's enforcement mechanism** (a PR template
  and a `pull_request_target` auto-close workflow); that was judged not worth the effort.
- ~~A repeatable grep gate (AC-02.5)~~ — **built and committed ahead of this slice** as
  `Scripts/check_license_claims.sh` (`69a891579`). It is red today with 17 hits and must be green
  when this slice and slice 03 are done. Run it; do not rewrite it. If it flags a true statement
  about software that is not Lighthouse, add that hit to its `ALLOW` list with a reason rather than
  widening the pattern.

## OUT of scope

- The **contribution enforcement mechanism** — the PR template and the `pull_request_target`
  auto-close workflow that slice 04 would have carried. Dropped on 2026-09-11 as not worth the
  effort; the board item is Removed. Only the prose fix moved here. Consequence to be honest about:
  a code pull request can still be opened and nothing stops it, so someone has to notice and reply
  by hand. That was the trade accepted.
- `docs/releasenotes/releasenotes.md:935-937` — that section is about OSS *attribution* of our
  dependencies and stays accurate as written.
- `docs/Installation/configuration.md:151` ("Postgres is an open-source…") and
  `authentication.md:90` ("Keycloak is an open-source identity provider") — true statements about
  other people's software. The grep gate must not flag them.
- Everything in the website repo. Slice 03.

## Acceptance criteria

AC-02.1 through AC-02.6 as stated in `feature-delta.md`.

## Note for the crafter

Before running Biome or any build in this repo, remove any `*/docs` symlinks first — Biome `--write`
will otherwise reformat the docs tree, and this slice edits six files in it.
