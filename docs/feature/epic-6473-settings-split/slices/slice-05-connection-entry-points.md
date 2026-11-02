# Slice 05: Every way to a connection leads to System

Story US-05. job_id `job-operator-run-the-instance-apart-from-how-we-work`. Effort about 1 day. Depends on slice 04.
Decided by M4 (2026-11-02); Q7 and Q8 open for the DISTILL walk-through.

## Outcome a user sees

Creating, opening and fixing a connection all start from, and end in, System → Connections: the onboarding stepper's
"Add {connection}", the "Create a {connection} before adding a {team}" hint, the Task Manager's connections section,
the reconnect warning, and old bookmarks. Saving or cancelling a connection page returns to System → Connections.

## Learning hypothesis

If a first-time admin starts on the Overview and the stepper takes them to System → Connections and back, they set up
the first connection without getting lost, which is the path a connect-first onboarding will later build on (M4).

## IN

- Onboarding stepper step 1 opens the add page under System (Q8 shape); the stepper stays on the Overview.
- The "Add {team}" disabled hint gains a link to System → Connections.
- Task Manager connections section and `ReconnectBanner` link there; Q7 decides whether one banner stays on the
  Overview.
- Connection add and edit pages return to System → Connections on Save, Cancel and wizard completion.
- Redirects (replace): `/connections/new` and `/connections/:id/edit` to the Q8 addresses; unknown ids behave as today.
- Docs list for finalize: `worktrackingsystems.md`, getting-started and Overview pages (see feature-delta checklists).

## OUT

A connect-first onboarding interview (ADR-271 U5, later); any change to the connection wizard's steps.

## Problem

Priya sets up a new Lighthouse for Northwind Logistics. The stepper's "Add {connection}" opens the wizard, and after
saving she lands on an Overview that no longer shows connections, so she cannot see that it worked.

## Who

- First-time System Admin | follows the onboarding stepper | wants to see the connection where it lives
- Platform operator | follows a Task Manager warning or a runbook link | wants to land on the right connection

## Domain examples

1. Priya clicks "Add {connection}" in the stepper, creates "Northwind Jira", and lands on System → Connections
   showing it; the stepper on the Overview now offers "Add {team}".
2. Tomasz follows his runbook link `/connections/4/edit` and lands on the edit page of "Northwind ADO" under System.
3. The Task Manager says "Northwind Jira" stopped working; Tomasz follows it to that connection under System.
4. Priya cancels the wizard halfway; she is back on System → Connections, nothing created.

## UAT scenarios

### Scenario: The first connection from the onboarding stepper

Given Priya is a System Admin on an instance with no connection
When she adds "Northwind Jira" from the onboarding stepper and saves it
Then she sees "Northwind Jira" on Settings, System, Connections

### Scenario: An old edit link opens the same connection

Given Tomasz's runbook links "/connections/4/edit" for "Northwind ADO"
When he opens the link
Then he sees the edit page of "Northwind ADO" under System

### Scenario: Cancelling returns to the connections

Given Priya is halfway through adding a connection
When she cancels
Then she is back on Settings, System, Connections, and no connection was created

### Scenario: A warning leads to the connection

Given the Task Manager reports that "Northwind Jira" stopped working
When Tomasz follows it
Then he reaches "Northwind Jira" under System

### Scenario: The hint on a disabled Add Team says where to go

Given an instance with no connection
When Priya points at the disabled "Add {team}" button
Then the hint tells her to create a connection first and links to Settings, System, Connections

## Acceptance criteria

- [ ] Stepper, hint, Task Manager and reconnect warning lead to System → Connections.
- [ ] Save, Cancel and wizard completion return to System → Connections.
- [ ] `/connections/new` and `/connections/:id/edit` redirect (replace) to the new addresses.
- [ ] New paths raise no usage-data page event, as the old ones did not.

## Outcome KPIs

K3 (no broken link, now including connection URLs); see feature-delta.

## Technical notes

- Docs and screenshots at finalize; connection screenshot file names stay.
- Dependencies: slice 04; Q7 and Q8 at the DISTILL walk-through.
