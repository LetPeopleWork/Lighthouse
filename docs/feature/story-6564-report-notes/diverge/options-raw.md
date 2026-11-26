# Options (raw) — story-6564-report-notes (DIVERGE, rev. 1)

No evaluation language in this file; scoring is in `taste-evaluation.md`. rev. 1 replaces the variant of the note
list (a list with a kind and an event date) and three more members of the free-text family, per review.

## HMW

How might we let a reader of any report see the facts that explain its numbers?

## SCAMPER, with a probability per lens (each lens sums to 1; stated estimates, not measurements)

| Lens | Candidates (probability within the lens) |
|---|---|
| S Substitute | a Team size value held on the Team 0.35; context picked from a tag list 0.10; free text 0.55 |
| C Combine | context asked in the report-creation wizard 0.15; one list holding standing notes and dated events 0.85 |
| A Adapt | event marks on the charts 0.15; a sheet as the source 0.05; a notes list under the report 0.80 |
| M Magnify | notes written like the Deliveries notes 0.70; replies and mentions 0.10; rich text 0.20 |
| P Put to other use | events kept once for a Team or the organisation and listed in every report whose period holds the date 0.20; notes also in print and PDF 0.80 |
| E Eliminate | a line generated from data Lighthouse holds 0.55; one context text, no list 0.30; no author, no date 0.15 |
| R Reverse | the owner holds notes and reports pull them 0.20; the reader adds context 0.05; the report author writes 0.75 |

Crazy 8s supplements (sum 1): a "what changed" line generated from membership changes 0.45; a pinned note per metric
panel 0.35; a reader-side private note 0.20.

## Deriving the curated-six probabilities

Each curated option takes the stated probability of the lens candidate it came from (the one marked below). The six
raw values are then divided by their sum (2.10) so they form one distribution over the six, summing to 1. These are
stated estimates, used to read clustering, never to rank.

| Option | Source lens candidate | Raw | Curated (raw / 2.10) |
|---|---|---|---|
| Notes to reports | M, 0.70 | 0.70 | 0.333 |
| Team size value on the Team | S, 0.35 | 0.35 | 0.167 |
| Event marks on the charts | A, 0.15 | 0.15 | 0.071 |
| Context step in the report-creation wizard | C, 0.15 | 0.15 | 0.071 |
| Events kept once and listed by date | P, 0.20 | 0.20 | 0.095 |
| Generated context line | E, 0.55 | 0.55 | 0.262 |

Tail rule applied to this one distribution: a tail candidate has probability at most 0.2. Tails: the Team size value
(0.167), event marks (0.071), the wizard step (0.071), events kept once (0.095). Heads: notes to reports (0.333) and
the generated line (0.262), together 0.595.

## Curated six

### Option 1: Notes to reports

**Core idea**: A report shows a list of notes, newest first, each with text, day, and the author when auth is on; people add, edit and delete their own notes.
**Key mechanism**: A copy of the Deliveries notes seam, keyed on the report (a list kept by the report).
**Key assumption**: Free text carries both workshop cases; the note's own day is enough.
**SCAMPER origin**: Magnify (and Adapt)
**Closest competitor**: Deliveries notes; a text box beside a chart
**name_origin**: user-words ("notes to reports")
**stated_probability**: 0.333 (curated; 0.70 in lens M)

### Option 2: Team size value on the Team

**Core idea**: A Team holds a size number; every report of that Team shows it in its header.
**Key mechanism**: A new field on the Team, edited in Team settings, read by the report. A value, not text.
**Key assumption**: People keep the number current, and a Team has one size over the whole report period.
**SCAMPER origin**: Substitute
**Closest competitor**: none found in the pages read (TeamRhythm names team size as context, offers no field)
**name_origin**: coined (this wave minted it; names the mechanism)
**stated_probability**: 0.167 (0.35 in lens S)

### Option 3: Event marks on the charts

**Core idea**: An event with a date appears as a mark on the day axis of each report chart that has one.
**Key mechanism**: An event list per report plus a mark layer drawn in the chart panels.
**Key assumption**: The context that matters is datable and the panels share a day axis.
**SCAMPER origin**: Adapt
**Closest competitor**: Grafana annotations, Datadog annotations
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.071 (0.15 in lens A)

### Option 4: Context step in the report-creation wizard

**Core idea**: When someone creates a report, the wizard asks a few fixed questions (how large the Team is, what happened); the answers show as a block at the top of the live report.
**Key mechanism**: Fixed context fields stored in the report's own payload, edited with the report, frozen otherwise.
**Key assumption**: The creator knows the context at creation and it changes rarely.
**SCAMPER origin**: Combine (the job merged with report creation)
**Closest competitor**: a caption typed when a slide is built
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.071 (0.15 in lens C)

### Option 5: Events kept once and listed by date

**Core idea**: Dated events (a change that started, a training) are written once for a Team or the organisation; each report lists the ones whose date falls in its period.
**Key mechanism**: An event store outside the reports, selected by date overlap with the report's period.
**Key assumption**: Context is a fact about the Team or organisation on a date, not about one report.
**SCAMPER origin**: Put to other use
**Closest competitor**: a sheet as the annotation source for Grafana; Grafana tag queries
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.095 (0.20 in lens P)

### Option 6: Generated context line

**Core idea**: Each report shows a line the system writes from data it holds (for example the number of closed Work Items and the length of the period).
**Key mechanism**: Computation from tracker data; nobody writes anything.
**Key assumption**: The context a reader needs can be derived, for example from distinct assignees if a tracker gives them.
**SCAMPER origin**: Eliminate (the writer)
**Closest competitor**: none found in the pages read
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.262 (0.55 in lens E)

## Diversity test

| Option | Different mechanism | Different assumption | Different cost |
|---|---|---|---|
| 1 | list kept by the report | free text is enough | low: a copy of a shipped seam |
| 2 | a number on the Team | people maintain it | medium: Team schema, settings, migration |
| 3 | marks on a time axis | context is datable | high: every panel |
| 4 | fixed fields at creation | the creator knows it now | medium: a payload per template, a wizard step |
| 5 | a store outside reports, picked by date | context belongs to the Team or organisation | medium: a new entity and a selection rule |
| 6 | computed, nothing written | context can be derived | unknown: data not found in the models searched |

Family read: the mechanism "a text list kept by the report" holds one option (0.333 of the six). The other five use a
number, a mark layer, fixed fields, an outside store and computation.

## Eliminated or merged

- One list that holds standing notes and dated events with a kind and an event date: a degree variant of option 1
  (same mechanism, added fields). The first revision carried it as a sixth option.
- One context text on the report, no list; notes held by the Team or Portfolio (a list keyed on the owner, merged into
  option 5's store); rich text, replies and mentions (degree of option 1); notes also in print and PDF (a maintainer
  constraint, not an option); a tag list (merged into option 2); a sheet as the source (needs an outside document);
  private reader notes (another job).
