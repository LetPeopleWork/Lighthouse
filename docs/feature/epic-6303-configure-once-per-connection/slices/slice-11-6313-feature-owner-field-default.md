# Slice 11 — Story 6313 — The Portfolios' Feature Owner Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-10 · **Estimate**: ~0.5 day · **Depends on**: finished #6304; slice 10 (order)
**Release point**: yes — completes one setting and can be released on its own (L8).
**Locked decisions**: D7, D9–D13, D21

## Goal
A "Feature Owner Field for Portfolios" default on the connection, followed by Portfolios in *Connection default*
mode. After a follower's update, its Features show their owning Team from the default field.

## Why this is thin
Read at fetch time (`JiraWorkTrackingConnector.cs:1245-1250`, `AzureDevOpsWorkTrackingConnector.cs:1200-1205`) and in
the fetch fingerprint (`FetchFingerprint.cs:50`, `:92`); resolver, save notice, queue and follow list reused. UI:
`OwnershipComponent.tsx:34-48` gains the dropdown entry.

## IN
- Default column; Portfolio mode column (D10 back-fill); new Portfolios follow (D11); dropdown entry; reads
  through the resolver in both connectors.
- Shown on Jira and Azure DevOps connections only (D21).

## OUT
- The Portfolio's owning Team itself (`OwningTeamId`) — a Team choice, not a field; not inherited.

## Learning hypothesis
Disproves "the feature owner field is per connection in practice" if dogfood Portfolios on one connection read
different fields.

## Production data / dogfood
Dev instance Portfolios that set a Feature Owner Field today.

## Acceptance criteria
AC-10.1 – AC-10.4; AC-F.1 – AC-F.5.

## Changed assumptions
(filled during DELIVER)
