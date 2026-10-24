# Slice 02: Choosing a Jira Release no longer waits in series (ADO #6398)

*(File name kept for the links to it. Re-cut after the maintainer decisions of 2026-10-24: reusing the header's
licence is dropped, MD-12; the Release list waits for the licence, MD-15.)*

**Goal:** the dialog reuses the Portfolio's delivery sources the Deliveries page already fetched, and starts the
Release list as soon as the licence is known premium rather than when the tab is drawn, so the wait for the sources
disappears and the Release list no longer starts last.

## IN scope
- The dialog takes the sources from the Deliveries page's answer (`useDeliveryManagement`), asking only when the page
  has no successful answer yet, and asking again on opening when the page's last answer failed (MD-23, AC-2.1 as
  amended; how: DESIGN, inside the Deliveries folder; keyed TanStack queries are the house pattern).
- The Release list request starts while the dialog is open, as soon as the sources include the source and the
  licence is known premium, whichever tab is on screen (MD-7, MD-15: N1 answered (b)). No request for a licence that
  is unknown or known not to be premium, so the server is never asked for something it would refuse.
- None on page load.
- Timing assumption, not a dependency (MD-15): Epic 6400 slice 02 is in by release, so the licence is one fast shared
  query and waiting for it costs almost nothing. Before that, the licence wait and the Release list run in series.
- A late answer for a Portfolio or dialog no longer on screen is dropped.
- Closing the dialog still forgets the list (today's behaviour).

## OUT of scope
- Anything Epic 6400 changes (MD-11, MD-12): `GetById`, `/teams`, `/portfolios`, `hooks/useLicenseRestrictions.ts`,
  a `["licenseStatus"]` query, a call to `licensingService.getLicenseStatus`, reusing the header's licence. The
  licence speed-up comes from 6400 slice 02.
- Making the Jira sweep cheaper (slice 03 and follow-up F2).
- Fetching on page load or hover (MD-7).

## Why it still stands alone
It shrank to about ¾ day, but it ships a different outcome from slice 01 (speed, not honesty) with its own hypothesis,
and shipped alone it already removes the sources wait and moves the Release list to the front. Merged with 01 it would
be about 1¾ days, past the one-day slice cap.

## Learning hypothesis
- **Disproves** "the waits are long mainly because they run in series" if, with the sources reused and the Release
  list started on open, the time to a usable picker on the throttled dev instance stays close to today's sum.
- **Confirms** it if the time from the click to a usable picker on the dev instance (Jira Cloud, licence answered, warm
  cache) drops from CA-6's 2.70 s to 1.2 s or less (K1a), with exactly one request on the critical path (K3); and, after
  release, to 5 s or less on the reporter's Data Center instance (K1b; assumes Epic 6400 slice 02 is in, MD-15).

## Acceptance criteria
US-02 AC-2.1 to AC-2.6 in `feature-delta.md`.

## Dependencies
Technically independent of slice 01: it can ship alone. It is ordered after 01 as a sequencing preference only, because
shipped first it would show today's guesses for a shorter time (F-L1). No file overlap with Epic 6400 (AC-2.6, checked at
review by diff against the named list).

## Effort
About ¾ day. Reference class: passing one already-fetched list down and lifting one fetch out of a tab body.

## Dogfood
On the dev instance throttled to "Slow 4G", open the Deliveries page, then Add Delivery, and read the DevTools
waterfall: no new sources request, the options request starts as soon as the licence has answered premium, and the
picker is ready when the options have answered. On a Community licence: no options request at all. Visit the page
without opening the dialog: no options request.
