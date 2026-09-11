# Slice 03 — The website says source available

**Feature**: `epic-5874-relicense-source-available` | **Stories**: US-03 | **ADO**: [#5970](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5970) | **Estimate**: ~5h
**Repo**: `LetPeopleWork/website` (separate checkout) | **Depends on**: slice 01 merged

## Goal

A prospect who arrives from a search for a self-hosted alternative reads a page that describes the
licence accurately and still tells them the thing they came for — the data stays on their
infrastructure — so the page survives the check they will run on it.

## Learning hypothesis

**The wording change is not cosmetic: this page is an acquisition asset, and one of the sentences
being rewritten is the answer to the search query that brings people here.**

Disproves if it fails: the assumption that slice 03 is slice 02 in a different repo.
`public/compare/index.html` carries a **schema.org FAQPage** whose first question is literally "Is
there an open-source alternative to ActionableAgile and Nave?" answered "Yes." That structured data
is what makes the page eligible for a rich result on that query. Rewriting the answer to "Lighthouse
is source-available and self-hosted" is honest and may cost the match; deleting the question forfeits
it outright; leaving it is a lie in machine-readable form. There is no option that is free, and K4
exists to measure which way it actually went.

Confirms if it succeeds: that the positioning survives on "self-hosted, your data never leaves your
network, flat-rate" — the three claims the BDR said were doing the real work — without the word the
page currently leads on.

## Production data

No data path. The acceptance bar is the **rendered, deployed page**, and afterwards the analytics:
K4 tracks organic sessions to `/compare` over the 60 days following, against the 60 days before.
That number is the slice's real verdict and arrives long after the merge — record the pre-change
baseline **before** merging, or it cannot be compared.

## Dogfood moment

Same day: open the preview deploy, read the hero, scroll the pricing table to the licence row, open
`/compare`, and run the deployed URLs through Google's Rich Results Test to confirm the FAQ and
Product structured data still parse.

## IN scope

- `src/components/Hero.tsx:27` — the headline (AC-03.1).
- `src/components/LighthouseSection.tsx:131` — the "100% open source" strapline (AC-03.1).
- `src/components/SEO.tsx:26` (keyword list) and `:60` (organisation description) (AC-03.1).
- `src/pages/Index.tsx:24,59,68,118` — **including the JSON-LD blocks**, which are the easiest thing
  in this slice to miss because they do not render as visible text (AC-03.1).
- `src/pages/Lighthouse.tsx:1307` — the pricing comparison row, which currently reads "100% Open
  Source (MIT License)" as a feature of all three tiers (AC-03.2).
- `public/compare/index.html` — meta description, og:description, the three FAQ answers, the body
  copy at 85 and 113, and the comparison table row at 97 (AC-03.3).
- `public/manifest.json:4` and `README.md:82`, which mirror each other (AC-03.4).

## OUT of scope

- **`src/pages/AI.tsx` and `src/components/AIIntegrationSection.tsx`.** These say "open source" about
  the LetPeopleWorkShop and LetPeopleGrow Claude Code plugins, which are MIT and stay MIT. Changing
  them would introduce a false statement. AC-03.5 asks for this to be *verified* at the end of the
  slice, not assumed — a repo-wide replace is exactly how these get caught in the blast.
- The Terms and Conditions behind `#lighthouse-license`. That is the paid licence agreement, a
  different document from the source-code licence. Named as an exclusion rather than left silent;
  DESIGN should confirm the two do not now contradict each other.
- Anything in the `Lighthouse` repo. Slices 01, 02 and 04.

## Acceptance criteria

AC-03.1 through AC-03.6 as stated in `feature-delta.md`.

## Note for the crafter

The docs-site screenshots this website hot-links from `@main` via jsDelivr are untouched by this
slice — no image changes here. But the same rule applies in reverse: do not rename or delete an asset
this repo points at.
