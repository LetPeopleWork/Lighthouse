# Slice 10 — Story 6313 — The Portfolios' Size Estimate Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-09 · **Estimate**: ~0.5 day · **Depends on**: finished #6304; slice 09 (order)
**Release point**: yes — completes one setting and can be released on its own (L8).
**Locked decisions**: D7 (one slice per setting), D9–D13, D21

## Goal
A "Size Estimate Field for Portfolios" default on the connection, followed by Portfolios in *Connection default*
mode. After a follower's update, its Features take their estimated size from the default field.

## Why this is thin
The setting is read at fetch time (`JiraWorkTrackingConnector.cs:1311-1316`, `AzureDevOpsWorkTrackingConnector.cs:1207-1218`)
and is in the fetch fingerprint (`FetchFingerprint.cs:51`, `:93`), so the resolver, the save notice, the queue and
the follow list all apply unchanged. UI: `FeatureSizeComponent.tsx:178` gains the dropdown entry.

## IN
- Default column; Portfolio mode column (D10 back-fill); new Portfolios follow (D11); dropdown entry; reads
  through the resolver in both connectors.
- Shown on Jira and Azure DevOps connections only (D21).

## OUT
- Feature Owner Field (slice 11). Estimation (slice 12).

## Learning hypothesis
Disproves "the size estimate field is per connection in practice" if dogfood Portfolios on one connection read
different fields — then few follow the default.

## Production data / dogfood
Dev instance Portfolios that set a Size Estimate Field today; demo data for the screenshot.

## Acceptance criteria
AC-9.1 – AC-9.4; AC-F.1 – AC-F.5.

## Changed assumptions
(filled during DELIVER)
