# Slice 11 — Story 6313 — The Portfolios' Feature Owner Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-10 · **Estimate**: ~0.5 day · **Depends on**: finished #6304 (05a – 06); slice 10 (order)
**Release point**: no — #6313 releases after slice 12, its last slice (L8). Alone, this slice completes one setting
and leaves `main` releasable.
**Locked decisions**: D7, D9–D13, D21

## Goal
A "Feature Owner Field for Portfolios" default on the connection, followed by Portfolios in *Connection default*
mode. After a follower's update, its Features show their owning Team from the default field.

## Why this is thin
Read at fetch time (`JiraWorkTrackingConnector.cs:1245-1250`, `AzureDevOpsWorkTrackingConnector.cs:1200-1205`) and in
the fetch fingerprint (`FetchFingerprint.cs:50`, `:92`); resolver, save dialog count, log entry and queue reused. UI:
`OwnershipComponent.tsx:34-48` gains the dropdown entry (UI-2).

## IN
- Default column; Portfolio mode column (D10 back-fill as amended: a value pointing at a deleted field becomes
  *Connection default*); new Portfolios follow (D11); dropdown entry; reads through the resolver in both connectors.
- Shown on Jira, Azure DevOps and CSV connections (D21, P-14): on CSV the extrapolation reads the Feature Owner Field
  from the Additional Field values CSV fills (`WorkItemService.cs:756-772`); the owning Team shown on a Feature stays
  Jira / ADO only.

## OUT
- The Portfolio's owning Team itself (`OwningTeamId`) — a Team choice, not a field; not inherited.

## Learning hypothesis
Disproves "the feature owner field is per connection in practice" if dogfood Portfolios on one connection read
different fields.

## Production data / dogfood
Dev instance Portfolios that set a Feature Owner Field today.

## Acceptance criteria
AC-10.1 – AC-10.5 (AC-10.5 is AC-4.4 at Portfolio grain); AC-F.1 – AC-F.5 (save dialog count and log entry).

## Release notes (finalize)
"Portfolios whose Feature Owner Field pointed at an Additional Field that had been deleted now read no owner from it,
from their next update."

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, P-14, UI-4): save dialog count and log entry instead of a save notice;
  deleted-field values repaired at the upgrade; offered on CSV too; slice 07's list no longer exists.
