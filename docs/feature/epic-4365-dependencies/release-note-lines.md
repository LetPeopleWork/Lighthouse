# Release note lines — Epic #4365 "Show Feature Dependencies"

Draft for the next release. Slices 01-04 are all on `main`; ADO Stories #5782/#5783/#5786 are Closed,
#5787 is Resolved. Epic #4365 carries the `Release Notes` tag.

Terminology: *Feature*, *Work Item*, *Portfolio* and *Team* render as whatever the instance calls
them. Keep the seeded defaults in this copy.

---

## See What Each Feature Is Waiting On

Your teams already record dependencies — a Predecessor link in Azure DevOps, an *is blocked by* link
in Jira, a relation between two Linear Projects. Lighthouse never read any of it. So the one place
your whole delivery is laid out in order was also the one place a Feature's blockers were invisible,
and you found out about them in the meeting where the date slipped.

Lighthouse now reads those links and names them. Every Feature list — the [Features
page](https://docs.lighthouse.letpeople.work/features/features.html), your Portfolios and your Teams —
has a **Dependencies** column listing what that Feature waits on, one per line, each linking straight
into your work tracking system. Nothing to configure and nothing to declare: if the link is in your
tracker, it is on the row.

Lighthouse does not let you author a dependency, and it never will. What it shows you is what your
tracker already says.

Where a dependency cannot be taken at face value, the row says so rather than staying quiet. One
warning icon per Feature, with the reasons in its tooltip:

- the Feature it waits on sits in no Portfolio they share
- the two are waiting on each other, so neither can go first
- the Feature it waits on has no measured delivery to forecast from
- it waits on something below it in the order — not a problem, but worth knowing before you re-plan

This is available on **every** Lighthouse instance, community edition included.

### When the links are not where Lighthouse looks

Two settings live under *Dependency Settings* on your
[Portfolio's settings page](https://docs.lighthouse.letpeople.work/portfolios/edit.html):

**Dependency Field** (Azure DevOps) — if your teams keep dependencies in a custom field rather than the
tracker's own link, name that field once and Lighthouse reads it instead, splitting on commas or
semicolons. It must be defined as an Additional Field on the connection first. Entries that resolve to
nothing are passed over; the rest still count.

On every other tracker the selector is visible but does nothing yet — naming a field on a Jira or
Linear Portfolio is accepted and saved, and the Portfolio goes on reading the tracker's own links.
Jira support is being built now. Linear cannot support it at all: that connector has no custom-field
reading of its own, which is also why the parent, Feature-owner and size settings beside it are equally
inert there.

**Ignore Dependencies** — set a Portfolio's dependencies aside without hiding or deleting one. They
keep being read and keep being listed; they simply stop counting. It takes effect on the next read, so
there is nothing to re-download, and flipping it back restores exactly the picture you had.

### What reads what

| | Where dependencies come from |
|---|---|
| Azure DevOps | the *Predecessor* link, or a field of your own (see above) |
| Jira Cloud & Data Center | the *is blocked by* end of a **Blocks** link — if your administrator renamed it, Lighthouse tells you what it looked for and what it found instead |
| Linear | a relation between two Projects |
| CSV | a column naming what each row waits on, set on the connection |
| ServiceNow | not available — it has no Features for a dependency to run between |

### Your forecasts have not moved

This release **shows** dependencies. It does not feed them into the simulation — every forecast date is
the one you would have got before, to the trial. Making the forecast honour a dependency is the next
step, and it is deliberately separate: one change to forecasting at a time.

---

## Open before this ships

- [ ] Docs updated (see the documentation gap list) and screenshots regenerated.
- [ ] Community reporter attribution for the *Contributions ❤️* section — Epic #4365 is
      `Community`-tagged; confirm who asked for this and add them.
- [ ] Confirm the release-notes wording against the shipped UI once #5787 is reviewed.
- [ ] **Carry forward to the release that ships Jira support (slice 05):** a Portfolio that named a
      Jira dependency field under this release has that setting saved and ignored. The first refresh
      after Jira support ships starts reading it, so its dependencies change with nobody touching a
      setting — including dependencies appearing on a Portfolio whose users had concluded it had none.
      That release owes a note naming who is affected and what they will see; this one does not.
- [ ] **Decide the Dependency Field's connector scope.** Only `AzureDevOpsWorkTrackingConnector`
      consults `DependencyOverrideAdditionalFieldDefinitionId`; Jira and Linear read their native links
      and ignore the setting, while the settings form offers the selector on every Portfolio. Either
      wire the other two connectors, or qualify the selector in the UI, or say "Azure DevOps only" in
      the copy — this draft says the last of the three.
