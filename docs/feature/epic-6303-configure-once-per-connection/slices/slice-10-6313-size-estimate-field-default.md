# Slice 10 — Story 6313 — The Portfolios' Size Estimate Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-09 · **Estimate**: ~0.5 day · **Depends on**: finished #6304 (05a – 06); slice 09 (order)
**Release point**: no — #6313 releases after slice 12, its last slice (L8). Alone, this slice completes one setting
and leaves `main` releasable.
**Locked decisions**: D7 (one slice per setting), D9–D13, D21

## Goal
A "Size Estimate Field for Portfolios" default on the connection, followed by Portfolios in *Connection default*
mode. After a follower's update, its Features take their estimated size from the default field.

## Why this is thin
The setting is read at fetch time (`JiraWorkTrackingConnector.cs:1311-1316`, `AzureDevOpsWorkTrackingConnector.cs:1207-1218`)
and is in the fetch fingerprint (`FetchFingerprint.cs:51`, `:93`), so the resolver, the save dialog count, the log
entry and the queue all apply unchanged. UI: `FeatureSizeComponent.tsx:178` gains the dropdown entry (UI-2).

## IN
- Default column; Portfolio mode column (D10 back-fill as amended: a value pointing at a deleted field becomes
  *Connection default*); new Portfolios follow (D11); dropdown entry; reads through the resolver in both connectors.
- Shown on Jira and Azure DevOps connections only (D21).

## OUT
- Feature Owner Field (slice 11). Estimation (slice 12).

## Learning hypothesis
Disproves "the size estimate field is per connection in practice" if dogfood Portfolios on one connection read
different fields — then few follow the default.

## Production data / dogfood
Dev instance Portfolios that set a Size Estimate Field today; demo data for the screenshot.

## Acceptance criteria
AC-9.1 – AC-9.5 (AC-9.5 is AC-4.4 at Portfolio grain); AC-F.1 – AC-F.5 (save dialog count and log entry).

## Release notes (finalize)
"Portfolios whose Size Estimate Field pointed at an Additional Field that had been deleted now read no size estimate
from it, from their next update."

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, UI-4): save dialog count and log entry instead of a save notice;
  deleted-field values repaired at the upgrade; slice 07's list no longer exists.

## Carried from slice 06 review
See the note of that name in [slice 08](slice-08-6313-portfolio-parent-override-default.md): distinct-owner counts across changed defaults, and this default's own body sentence in the update question.
