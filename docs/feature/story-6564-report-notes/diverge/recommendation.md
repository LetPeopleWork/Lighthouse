# Recommendation — story-6564-report-notes (DIVERGE, rev. 1)

Inputs: `job-analysis.md`, `competitive-research.md`, `options-raw.md`, `taste-evaluation.md`.

## Decision statement for DISCUSS

Proceed with notes to reports, built like the Deliveries notes: a list of notes kept by the report, each with text and
a day, showing who wrote it when auth is on and no name when auth is off, shown in the live report for every report
type, and kept out of print and PDF for now. How large the Team is goes in as ordinary note text. A number for how
large the Team is [new] held on the Team is not part of this step. Assumption: free text with the note's own day is
enough for both workshop cases (score 4.10).

## Recommended: notes to reports (4.10)

A report shows its notes, newest first. Each note has text and the day it was written. It shows who wrote it when auth
is on. People change only their own notes, as on Deliveries. The list sits in the live report and is not drawn in the
print or PDF version. The list is keyed on the report, and one report model serves every template, so it works for
every report type and for both Team and Portfolio reports.

Why it ranks first: it leads the next option by 0.80 under the locked weights. It scores higher on Desirability and
Feasibility (DVF 4.33 against 3.00), Subtraction (4 against 3), Concept count (4 against 3) and Progressive disclosure
(4 against 3), and lower on Speed-as-trust (4 against 5). It is a copy of a shipped seam, with one new idea for the
reader.

## The next two options, by mechanism

### A wizard step that stores fixed context fields on the report [new] — 3.30
- **Why it scores well**: the context is stored in the report's own payload, so it opens with the report (T4 5); fixed
  questions for how large the Team is and what happened give structure.
- **Core trade-off**: what happened is frozen at creation unless the report is edited; `Report.cs` has no creator
  field today, so showing who wrote it needs a new field; a payload change per template.
- **Key risk**: the creator does not know the context at creation, or it changes after.
- **Hire when**: the context is the same for the life of the report and a fixed form is wanted.

### A store of dated events outside the reports, kept for a Team or the organisation and listed by date [new] — 3.15
- **Why it scores well**: write what happened once; every report whose period holds the date lists it. It has a
  market precedent (a sheet as the annotation source for Grafana).
- **Core trade-off**: three new ideas for the reader (a store outside the report, a date rule, a place to manage it).
- **Key risk**: a Portfolio report lists organisation events only by assumption; DISCUSS must settle this.
- **Hire when**: the same things that happened on a date explain many reports.

Removed before scoring, by the one constraint rule in `taste-evaluation.md`: a number for how large the Team is held
on the Team (fails "generic": a Portfolio report has no single Team to say how large it is) and event marks on the charts [new] (a comparison
panel has no day axis). A generated context line [new] failed the DVF filter (5, below 6): no member count or assignee
was found in `Models/`.

## Answers to the two open questions from discovery

- **How large the Team is**: write it as note text. Jessie asked to show how large the Team is; nobody asked to
  compute with it, so a stored number is not needed to meet the request. The trigger to hold a number is a request to
  compute from it (for example Throughput per person). A number held on the Team would then serve Team reports only,
  because it fails the "generic" rule. Turning old note text into a value would be a migration, not free.
- **Context that always applies, or something that happened on a date**: one note concept for now. A note shows the
  day it was written; the writer puts the date of what happened in the text if it differs. A store of dated events
  outside the reports, listed by date [new] (3.15), adds three ideas. It is the direction to revisit if readers need
  the event's own date.

## Dissent (second place): a wizard step that stores fixed context fields on the report [new], 3.30

The margin to the recommended option is 0.80. It would win if DISCUSS decides the context is stated once, by whoever
builds the report, in fixed fields, and that a list needing upkeep is a cost. It opens fastest (T4 5) and needs no list.
It loses on Desirability (what happened is frozen), on a new creator field, and on a payload change per template.

## Open points for DISCUSS

1. Does an event's own date matter enough to add a date field to a note?
2. Who may add notes: any user with write access to the report's owner, or only the report's author?
3. Do notes go when a report is deleted? (Deliveries notes freeze on archive.)
4. Print and PDF: out for now (maintainer decision). The ADO item says "probably yes". The job
   `job-share-numbers-outside-lighthouse` stays unserved by this Story: a reader who gets only the PDF or a
   screenshot sees no context.

## For the requester

**What we looked at.** Six ways to put context next to the numbers in a report. Three failed a check or a filter. We scored three.

**What we recommend.** Notes to reports, like the notes on Deliveries.
- A note has text and a day.
- It shows who wrote it when auth is on. When auth is off, it shows no name.
- Notes show in the live report. They stay out of print and PDF for now.
- It works for every report type, not only Then & Now.

**The two open questions from discovery.**
- How large the Team is: write it as a note. A number held on the Team [new] would serve Team reports only.
  We would add it only if you want to compute with it.
- Context that always applies, or something that happened on a date: use one kind of note for both. A note shows the
  day it was written. If you need the day of what happened (for example a change that started or a training that took
  place on a date), put it in the text. A separate list of events kept once [new] is the next step if that is not enough.

**What this does not do.** Notes stay out of print and PDF. A reader who gets only the PDF sees no context.

**What we are unsure about.** Two people at one workshop asked for this. The scores are desk judgement, not measured.

**Second choice.** A context step in the report-creation wizard [new]. The creator answers a few fixed questions when
the report is made. It opens fast, but what happened is fixed at creation. It scores 0.80 lower.
