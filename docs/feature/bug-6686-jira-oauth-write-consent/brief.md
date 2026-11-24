# Product brief: bug\-6686\-jira\-oauth\-write\-consent

## Request

```text
Bug 6686 — Jira OAuth: write-back cannot write with read-only scopes. The Jira OAuth connection asks only for read scopes (read:jira-work, read:jira-user, board/sprint/issue/project read scopes, offline_access) in JiraOAuthProvider.cs. Write-back (Data Sync Mappings) on a Jira OAuth connection therefore cannot write: Jira refuses the update for lack of write:jira-work. Jira API-token connections are not affected. To check: confirm the failure on a Jira OAuth connection with a write-back mapping; then decide whether Jira OAuth should ask for write:jira-work only when write-back is set up, the same way as the Azure DevOps change in Story 6672. Found while preparing Story 6672. Maintainer decisions 2026-11-24: mirror the Azure DevOps change (ask write:jira-work only when write-back mappings are saved, record what Atlassian granted, show the Rights line and the Sign in again warning for Jira too); a Jira OAuth sign-in stored before rights were recorded counts as read-only; the write-consent warning names the connection's tracker (Jira or Azure DevOps).
```

## Work

S

```text
Write-back works on a Jira OAuth connection: Lighthouse asks Atlassian for write rights only when write-back is set up, and shows when a Jira sign-in may only read.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A Jira OAuth connection asks Atlassian for write:jira-work only when write-back mappings are saved on it; otherwise it asks for the read scopes only
```

### P2

Outcome

```text
Lighthouse records what Atlassian granted; write-back on a Jira OAuth sign-in without write rights, including a sign-in made before rights were recorded, sends nothing to Jira and logs one warning, and the connection shows Rights Read with the Sign in again warning
```

### P3

Outcome

```text
After the admin signs in again and Atlassian grants write:jira-work, write-back on the Jira OAuth connection sends the change; Azure DevOps OAuth and API-token connections keep their current behaviour
```

### P4

Outcome

```text
The write-consent warning names the connection's own tracker (Jira or Azure DevOps) and keeps its Sign in again action
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A Jira OAuth connection with write-back asks Atlassian for write:jira-work, records what Atlassian granted, and write-back on a Jira sign-in that may only read is not sent and is shown as needing a new sign-in
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S2

```text
The write-consent warning names the connection's tracker, so a Jira OAuth connection is told it can only read from Jira
```

#### Promises

P4

#### Depends on

None.

#### Slice work

None.
