# Slice 12 — Story 6313 — Estimation is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-11 · **Estimate**: ~1 day (the ADO Story's ~2.5d assumes ~0.5d here; see the Scope Assessment) · **Depends on**: finished #6304 (05a – 06); slice 11 (order)
**Release point**: yes — completes one setting and can be released on its own; #6313's last slice (L8).
**Locked decisions**: D7, D9–D13, D16 (one bundle), D21

## Goal
An Estimation default on the connection — field, unit, non-numeric flag and categories as one bundle — followed by
Teams and Portfolios in *Connection default* mode, taking effect on the next page load. When owners follow it,
saving it shows, in the save dialog, how many follow it and that no update is needed (plain Save, "Change the
Estimation default?"); the log entry names them.

## Why it is different
Estimation is read at read time (`EstimateNormalizer.cs:71-90`) and is not in the fetch fingerprint, so a change
queues nothing and slice 06 correctly does nothing. The work is the bundle and the form, not the sync.

## IN
- Default columns on the connection (one bundle for Teams, one for Portfolios — DESIGN confirms the per-grain
  split as for Parent Override); one mode column per owner for the bundle (D10 back-fill as amended: no Estimation
  Field, or one pointing at a deleted field → follows); `EstimateNormalizer` reads the effective bundle.
- UI (UI-5): the owner's existing Estimation editor (`EstimationFieldComponent`) is reused inline on the connection
  page — field, unit, non-numeric flag, and categories when non-numeric — collapsed until a field is chosen, once for
  Teams and once for Portfolios. The owner page shows "Connection default (Story Points, Points)".
- Shown on Jira, Azure DevOps and CSV connections (CSV reads Additional Field values,
  `CsvWorkTrackingConnector.cs:244-254`); hidden on Linear and ServiceNow (D21).

## OUT
- Following the field alone while owning the unit or categories (D16).

## Learning hypothesis
Disproves "estimation is the same across Teams on one connection" if dogfood Teams on the dev instance mostly
carry different estimation fields or categories — then few follow the default.

## Production data / dogfood
Dev instance: Teams on one connection with Story Points; demo data for the screenshot.

## Acceptance criteria
AC-11.1 – AC-11.6 (AC-11.6 is AC-4.4 at Team and Portfolio grain); AC-F.1 – AC-F.5 (plain-Save dialog and log entry).

## Release notes (finalize)
"Teams and Portfolios whose Estimation Field pointed at an Additional Field that had been deleted now show no
estimation from it." (configurable terms)

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, UI-3, UI-5): plain-Save dialog count and log entry instead of a save
  notice; deleted-field values repaired at the upgrade; the inline Estimation editor is the approved UI.
