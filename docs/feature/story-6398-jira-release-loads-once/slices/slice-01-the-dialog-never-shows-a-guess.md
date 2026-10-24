# Slice 01: The Delivery dialog never shows a guess (ADO #6398)

**Goal:** while the delivery sources or the licence are unknown, the Add and Edit Delivery dialog shows that it is
loading; the premium notice appears only once the licence is known not to be premium; a failed sources call shows a
notice asking to reopen the dialog (MD-23; was Try again), and a failed Release list says honestly how to retry.

## IN scope
- A busy placeholder tab in the tab row while the sources are unknown: a disabled button named exactly "Loading…"
  with `aria-busy="true"` (DISTILL sketch A, MD-21).
- The dialog reads `licenseStatus === null` from the unchanged `useLicenseRestrictions()` as "not known yet": no
  premium notice, Rule-Based lock or premium tooltip meanwhile; Rule-Based stays enabled, and both premium tabs' bodies
  show a loading look (MD-12, MD-14). The change is local to `DeliveryCreateModal`.
- Sources failure: a `warning` notice "Couldn't check what this Portfolio's connection offers. Close this dialog and
  open it again to retry." under the tab row, no button (MD-22, MD-23; supersedes MD-8's Try again). Manual and
  Rule-Based stay usable, and opening the dialog again asks for the sources again.
- Release-list failure: the existing Alert, reworded to "The list of {sourceName} entries could not be loaded. Close
  this dialog and open it again to retry.", no button (MD-13).
- Edit mode: a Delivery bound to a source never shows another tab's body while the sources load (MD-6).
- While the sources or the licence are unknown, no blocking message ("Delivery name is required") under the form
  (AC-1.9, F-M1).

## OUT of scope
- Fetching earlier or reusing the page's sources (slice 02).
- Any backend change; the server's `LicenseGuard` and `RbacGuard` stay as they are.
- Anything Epic 6400 changes (MD-12): `hooks/useLicenseRestrictions.ts`, a `["licenseStatus"]` query, a call to
  `licensingService.getLicenseStatus`.
- A licence failure look: follow-up F1, after Epic 6400 slice 02. Known limit until then: a failed licence check shows
  the loading look for as long as the dialog is open.
- A Try again button on either failure look (MD-13 for the Release list, MD-23 for the sources).

## Learning hypothesis
- **Disproves** "an honest loading look is enough for the user to stop reading the dialog as broken" if, with the
  waits still in series, the reporter still reads a wait as "no Jira Release here" or "licence lapsed".
- **Confirms** it if the reporter's next feedback is about speed only, not about wrong answers.

## Acceptance criteria
US-01 AC-1.1 to AC-1.9 in `feature-delta.md` (AC-1.7 narrowed to the source-tree rule, AC-1.9 added after the
final-wave review).

## Dependencies
None; no file overlap with Epic 6400. Looks: DISTILL sketches A–F, MD-21 (the two failure looks made consistent by
MD-23). The licence's missing failure look is a maintainer-accepted exception that ends with F1; DISTILL pins it with
one scenario.

## Effort
About a day. Reference class: one dialog's state handling plus its Vitest scenarios.

## Dogfood
On the dev instance with the browser throttled to "Slow 4G", open Add Delivery on a Portfolio with a Jira connection
and see the busy tab, then the tab's spinner, never the premium notice. Block the sources route in DevTools (request
blocking) and see the notice; unblock it, close and reopen the dialog, and the Jira Release tab appears (MD-23). Block the options route and see the reworded Alert; reopening the
dialog recovers. Block `/license` and see the loading look stay, with Manual usable.
