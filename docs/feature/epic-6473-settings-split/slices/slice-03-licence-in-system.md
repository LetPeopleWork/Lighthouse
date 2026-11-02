# Slice 03: The licence has a home in System (decided, M3)

Story US-03. job_id `job-operator-run-the-instance-apart-from-how-we-work`. Effort about 0.5 day. Depends on slice 01.

## Outcome a user sees

System gets a Licence tab showing what the header licence popover shows today: status, organisation, expiry, and for
System Admins import and clear. The "import a licence" links in the connection forms open it. The header icon stays.

## Learning hypothesis

If the licence has a place in System, renewal starts from a link instead of a support question. Disproved if the tab
is never opened after a release (DEVOPS decides whether to count it).

## IN

- System → Licence tab reusing the popover's content component; same rights as the popover.
- `/settings/license` and `/settings/system/licence` open it.
- Its loading and failure looks: the popover's existing ones.

## OUT

Any licence behaviour change; moving the header icon; Connections (slices 04, 05).

## Problem

Tomasz Nowak edits the Jira connection "Northwind Jira", reads that write-back needs Premium, follows "import a
licence" and gets a blank page. He finds the licence only by guessing the header icon.

## Who

- Platform operator | renews the licence once a year | wants it with the server's settings
- Team reader | wants to see whether Premium is on | read-only

## Domain examples

1. Tomasz follows the licence link from the connection form and imports the renewed licence file on System → Licence.
2. Morgan Lee opens System → Licence and sees "Premium, valid until 31 March 2027", with no import or clear button.
3. The licence status cannot be loaded; the tab shows the popover's existing could-not-load message.

## UAT scenarios

### Scenario: The licence link from a connection opens the licence

Given Tomasz is editing the connection "Northwind Jira"
When he follows the link to import a licence
Then he sees System, Licence, with the current licence status

### Scenario: An admin imports a licence from System

Given Tomasz is a System Admin and holds a renewed licence file
When he imports it on System, Licence
Then the new expiry date shows there and in the header icon

### Scenario: A reader sees the licence without changing it

Given Morgan Lee is no System Admin
When he opens System, Licence
Then he sees the status and expiry and no import or clear action

### Scenario: A licence that cannot be loaded says so

Given the licence status cannot be loaded
When Tomasz opens System, Licence
Then he sees that the licence could not be loaded

## Acceptance criteria

- [ ] System → Licence shows the popover's content with the popover's rights.
- [ ] `/settings/license` opens it.
- [ ] Import updates the header icon too (one source of licence status).
- [ ] Loading and failure looks reuse the popover's.

## Outcome KPIs

K3 (no broken link); see feature-delta.

## Technical notes

- An enforcement test (`licenseStatusOneAsker`) limits who asks for licence status; the tab must go through the same
  asker.
- Dependencies: slice 01. Maintainer's yes given (M3, 2026-11-02).
