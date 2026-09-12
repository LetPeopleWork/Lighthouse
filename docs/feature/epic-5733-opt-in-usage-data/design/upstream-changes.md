# Upstream changes requested — DESIGN → DISCUSS, 2026-09-12

The emission redesign (ADR-190, ADR-191) invalidates parts of the DISCUSS delta that DESIGN cannot
fix on its own authority. Each item below is a **request for back-propagation**, not a change already
made. Nothing in `## Wave: DISCUSS / ...` has been edited.

The cause in every case is the same one maintainer decision: **the payload carries user and feature
counts only, with no instance identifier**. Install counts are therefore permanently unanswerable —
not approximately, not with a wide error bar, but not at all, because the information is never sent.

---

## U1 — US-04 is half-satisfiable and its title promises the half that is dead

**Where**: `feature-delta.md:404`, *"US-04 — Know how many instances exist and which versions they
run"*.

**What breaks**: the first clause. Nothing in the payload distinguishes one instance with five
consenting browsers from five instances with one each.

**What survives, transformed**: the second clause. Version adoption is answerable as *the
distribution of consenting browsers by version*. A three-browser instance counts three times.

**Why that is arguably the better number, and still a different one**: the job this story serves is
picking a deprecation date that does not strand people, and a deprecation strands people rather than
databases. But a reader who sees "installed base" will assume the old meaning, so the substitution
has to be explicit rather than quiet.

**Requested**: retitle to something the design can deliver — *"Know which Lighthouse versions the
people using it are actually on"* — and rewrite the elevator pitch's "count of distinct instance
identifiers" accordingly.

---

## U2 — Four of US-04's seven acceptance criteria no longer describe anything

| AC | Requested change |
|---|---|
| AC-04.1 "a heartbeat at most once per day per instance" | **Delete.** There is no heartbeat |
| AC-04.2 "payload is exactly: instance identifier, version, deployment mode, licence tier, timestamp" | **Replace** with the closed event-name and property contract in ADR-190 |
| AC-04.3 / AC-04.4 instance identifier, random, minted on first consent, stable across restarts, derived from nothing | **Relocate** to the per-browser pseudonym. "Derived from nothing" and "minted only on a grant" carry over verbatim. "Stable across restarts" becomes "stable for as long as the browser keeps its token" |
| AC-04.5 silent degradation | **Keep unchanged** |
| AC-04.6 the dogfood instance appears in the dashboard on ship day | **Keep**, retargeted from the heartbeat to the first product event |
| AC-04.7 "the docs state that the instance count means *instances with at least one consenting user*" | **Rewrite.** The unit is now a consenting browser and no instance count is produced at all. This is the most important of the seven, because it governs what the usage data page claims |

---

## U3 — `OUT-usagedata-instances-reporting` is unmeasurable as written

**Where**: `feature-delta.md:641`, *"≥ 25 distinct instance identifiers report in a rolling 24h window
within 90 days of slice 02"*.

There are no instance identifiers to count.

**Requested**: a new unit — distinct consenting browsers — and a **re-set target**, because 25
browsers is not 25 instances and the maintainer's original number was chosen against installs. This
needs a person's judgement, not a conversion factor, which is why DESIGN proposes nothing.

---

## U4 — D3's second paragraph states a census that will not exist

**Where**: `feature-delta.md:103-115`, D3.

D3 says *"the installed-base census counts instances with at least one consenting user, not
instances. A biased denominator that is named is usable; an unnamed one is not."* The reasoning is
right and the subject is gone. Its third paragraph — *"the instance identifier is minted lazily on
first consent"* — describes a mechanism that is being removed.

**Requested**: keep D3's naming principle and change what it names. The denominator to disclose is
now "consenting browsers, which over-counts multi-browser installations and under-counts everyone who
declined".

---

## U5 — AC-08.4 names the wrong identifier

**Where**: `feature-delta.md:560`, *"Every event carries the instance identifier and is subject to the
same consent and master switch as the heartbeat."*

**Requested**: *"Every event carries the browser's analytics pseudonym and is subject to the same
consent check and master switch, evaluated fresh at ingest and again at forward."* The second half is
strengthened rather than weakened — there is no longer a heartbeat to be *"the same as"*, and the
check now runs twice.

---

## U6 — The Story Map's "Learn from it" row has no slice 01b

**Where**: `feature-delta.md:590-601`.

The map allocates `US-04 heartbeat + census` to slice 01b and `US-08 product events` to slice 04.
Slice 01b is superseded.

**Requested**: the walking skeleton's emit half becomes slice 01c (the event pipe, carrying exactly
one route event), and slice 04 moves up to immediately after it. The two copy corrections — S3
`SurveyNudge.tsx:114` and S4 the CRA self-assessment row 1.7 — travel with 01c for exactly the reason
they travelled with 01b: it is the slice that first makes them false.

---

## Not an upstream change, but it belongs in the same conversation

**`docs/settings/usagedata.md` still describes the daily heartbeat, and the shipped consent dialog
links to it as the authoritative account.** The dialog deliberately carries no field list of its own
(`UsageDataDialog.tsx` says so in as many words), so for as long as that page is wrong, the product's
most load-bearing privacy disclosure is wrong.

The maintainer has judged the page not release-ready and deferred the rewrite. It was **not touched
by this DESIGN**. It is recorded here because U2's AC-04.7 and U4's denominator both land on it, and
because the rewrite must happen inside slice 01c rather than after it.
