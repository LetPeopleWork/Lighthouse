# Slice 12 — Story 6313 — Estimation is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-11 · **Estimate**: ~1 day (the ADO Story's ~2.5d assumes ~0.5d here; see the Scope Assessment) · **Depends on**: finished #6304; slice 11 (order)
**Release point**: yes — completes one setting and can be released on its own; #6313's last slice (L8).
**Locked decisions**: D7, D9–D13, D16 (one bundle), D21

## Goal
An Estimation default on the connection — field, unit, non-numeric flag and categories as one bundle — followed by
Teams and Portfolios in *Connection default* mode, taking effect on the next page load. Saving it shows the save
notice naming the followers and saying no update is needed.

## Why it is different
Estimation is read at read time (`EstimateNormalizer.cs:71-90`) and is not in the fetch fingerprint, so a change
queues nothing and slice 06 correctly does nothing. The work is the bundle and the form, not the sync.

## IN
- Default columns on the connection (one bundle for Teams, one for Portfolios — DESIGN confirms the per-grain
  split as for Parent Override); one mode column per owner for the bundle (D10 back-fill: no Estimation Field →
  follows); `EstimateNormalizer` reads the effective bundle; `EstimationFieldComponent` gains the dropdown entry
  and shows the followed unit / categories read-only.
- Shown on Jira, Azure DevOps and CSV connections (CSV reads Additional Field values,
  `CsvWorkTrackingConnector.cs:244-254`); hidden on Linear; ServiceNow per DESIGN (D21).

## OUT
- Following the field alone while owning the unit or categories (D16).

## Learning hypothesis
Disproves "estimation is the same across Teams on one connection" if dogfood Teams on the dev instance mostly
carry different estimation fields or categories — then few follow the default.

## Production data / dogfood
Dev instance: Teams on one connection with Story Points; demo data for the screenshot.

## Acceptance criteria
AC-11.1 – AC-11.4; AC-F.1 – AC-F.5.

## Changed assumptions
(filled during DELIVER)
