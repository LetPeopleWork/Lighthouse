# Slice 01 — Licence terms in force

**Feature**: `epic-5874-relicense-source-available` | **Stories**: US-01 | **ADO**: [#5968](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5968) | **Estimate**: ~3.5h
**Depends on**: slice 00 (SPIKE) complete and reviewed

## Goal

Someone opening `github.com/LetPeopleWork/Lighthouse` reads a licence that states what they may and
may not do with the source — including that deriving a substitute from it, by hand or with an AI
assistant, is not permitted — and reads it from GitHub's own sidebar, not from a file they had to go
looking for.

## Learning hypothesis

**A licence that is not on GitHub's known list degrades the repo page in ways nobody checked.**

Disproves if it fails: the assumption that swapping `LICENSE` is a one-file change. GitHub detects
licences by matching the file against its own corpus. A bespoke text will not match, so the sidebar
falls back to "View license" with no name, the API's `license` field reports `NOASSERTION` or `other`
rather than a name we chose, and any downstream consumer reading that field — dependency dashboards,
the GitHub search filter, package registries — sees something we did not pick. Better to find out
what the page actually looks like here, on the slice whose entire output is that page, than to
discover it while the website is mid-rewrite in slice 03.

Confirms if it succeeds: that a `LicenseRef-` identifier plus a named heading in the file is enough
for a human reader, and that AC-01.4's assertion can be written against a real observed value.

## Production data

No data path. The acceptance bar is the **rendered public surface**, not the diff: the GitHub repo
page as an anonymous visitor sees it, and the `gh api` licence field as an external tool reads it.
Checking the file content locally proves nothing about either.

## Dogfood moment

Same day: open the repo in a logged-out browser. Read the sidebar. Click through to the licence.
Then run `gh api repos/LetPeopleWork/Lighthouse --jq .license` and record what actually comes back.

## IN scope

- `LICENSE` replaced with: our own opening notice naming the composite licence and stating that both
  parts bind and both are in that file, then Part 1, then a delimiter, then **the Elastic License 2.0
  reproduced byte-for-byte** as Part 2 (AC-01.1, AC-01.3).
- The Lighthouse Additional Terms as **Part 1 of `LICENSE`**, above Part 2 — sections 1–6 from slice
  00, carrying no mention of "PolyForm" or polyformproject.org (AC-01.2). There is no separate terms
  file.
- A `NOTICE` file, or a clearly-headed part of `LICENSE`'s header block, recording that every version
  released before the change date remains under the MIT licence in perpetuity, with that date named
  (AC-01.4). This is what keeps the four existing forks unambiguously covered.
- The licence name and its `LicenseRef-` SPDX identifier used identically across `LICENSE`,
  `NOTICE`, `README.md` and `docs/licensing/licensing.md` (AC-01.5).
- `Lighthouse.EndToEndTests/package.json`: the `"license": "MIT"` field corrected (AC-01.7).
- `LICENSE` and `NOTICE` added to whatever packages the published artefacts — Docker image,
  standalone archives, chart (AC-01.8).
- **Licence metadata, so scanners do not misclassify the composite as `Elastic-2.0`.** Both counsel
  raised this independently. `"license": "LicenseRef-Lighthouse-SAL-1.0"` in every package manifest
  that carries one, and the same string wherever repository metadata records a licence. GitHub's own
  detector will not match the file, which is expected — what must not happen is a scanner matching
  the reproduced ELv2 text and reporting `Elastic-2.0`, since Part 1's restrictions would then be
  invisible to it.
- **A layered TDM reservation**, per counsel. The EU DSM Directive's Art. 4 text-and-data-mining
  exception applies unless the rightsholder has reserved the use "in an appropriate manner", and for
  content published online it points to machine-readable means — so the clause in `LICENSE` may bind
  a licensee and still reserve nothing against a crawler. Three layers, none sufficient alone:
  1. the express reservation in `LICENSE` Part 1 section 2 (already drafted);
  2. a W3C TDMRep file at `/.well-known/tdmrep.json` on `letpeople.work` and
     `docs.lighthouse.letpeople.work`, with `tdm-reservation` set and a `tdm-policy` URL pointing at
     the licensing page;
  3. repository-level reservation metadata on GitHub itself.
  **Layer 2 cannot reach the copy that matters.** TDMRep is served from an origin's `/.well-known/`
  path and we do not control `github.com`, which is where the source actually sits — hence layer 3.
  Counsel also notes TDMRep is a W3C Community Group report rather than a Standard, so the
  implementation must not claim certainty about its effect.
- Whatever GitHub repo-settings change the sidebar needs, once the dogfood moment shows what it
  actually renders.

## Hard constraint

**Nothing inside Elastic's text is edited, reworded, reordered or deleted.** Our words go above it,
as Part 1, behind a delimiter. No grant exists to publish a modified Elastic License, and the
two-part shape exists precisely to avoid needing one. ELv2 is always the tail of the file, so the
check is length-anchored and survives any future edit to Part 1:

```sh
diff <(tail -n "$(wc -l < elv2-verbatim.txt)" LICENSE) elv2-verbatim.txt
```

A non-empty diff fails AC-01.1.

## OUT of scope

- The rest of the "open source" copy sweep — slice 02. This slice touches `README.md` only where the
  licence name appears, and leaves its open-source sentences alone deliberately, so the two slices do
  not collide in the same paragraphs.
- The website. Slice 03.
- `contributions.md`. Slice 04.
- Anything in `lighthouse-clients` or `lighthouse-jira-app` — they stay MIT.

## Acceptance criteria

AC-01.1 through AC-01.6 as stated in `feature-delta.md`.

## Risk carried

Between this slice merging and slice 02 merging, `README.md` says both the new licence name and
"free, open-source (MIT)" a few lines apart. That window is deliberate and should be short — it is
the reason slice 02 is next in the order and not later.
