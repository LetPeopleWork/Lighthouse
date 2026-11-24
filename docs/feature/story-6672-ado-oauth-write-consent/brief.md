# Product brief: story\-6672\-ado\-oauth\-write\-consent

## Request

```text
Story 6672: Azure DevOps OAuth: ask for write rights only when write-back is set up. The Azure DevOps OAuth connection always asks for vso.work_write, also when the connection is only used to read; a security review sees a read-only connection that holds a token with write rights. Ask only for read rights (vso.work) by default; ask for write rights (vso.work_write) only when write-back (Data Sync Mappings) is set up on that connection; the admin then signs in again to give the extra consent. AC: a new ADO OAuth connection without write-back asks only for read rights on work items; setting up write-back asks the admin to give write consent, and write-back works after that; a connection without write consent never tries to write; existing connections keep working.
```

## Work

M

```text
An Azure DevOps OAuth connection holds write rights only when it has write-back set up and the admin consented to write; the admin can see which rights it holds and is asked to sign in again when write-back needs more.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A new Azure DevOps OAuth connection without write-back asks only for read rights on work items
```

### P2

Outcome

```text
A connection without write consent never tries to write; a skipped write-back is logged
```

### P3

Outcome

```text
Existing Azure DevOps OAuth connections keep working, with write-back, and count as write-consented
```

### P4

Outcome

```text
Setting up write-back on a read-only connection shows the admin that it needs more rights and lets them sign in again to give write consent; write-back works after that
```

### P5

Outcome

```text
The connection's settings show which rights its sign-in holds: read, or read and write
```

## Scope

```text
Azure DevOps OAuth connections: requested scopes, stored granted rights, write-back gating, connection settings UI
```

```text
Jira OAuth scopes and Jira OAuth write-back (Bug 6686)
```

```text
Work Item Age or any metric; PAT connections, whose rights the admin sets on the token
```

## Decisions

```text
Existing ADO OAuth connections count as write-consented; the next sign-in asks only for the rights the connection needs and stores what was granted.
```

#### Status

Confirmed


```text
Adding the first write-back mapping to a read-only connection saves the mapping; the connection then shows a warning with Sign in again; no write is tried until write consent; each skipped write-back is logged.
```

#### Status

Confirmed


```text
Removing the last write-back mapping changes nothing at once; the next sign-in asks for read rights only.
```

#### Status

Confirmed


```text
UI: one 'Rights: Read work items / Read and write work items' line in the connection's Authentication section; a warning Alert with a 'Sign in again' button (ReconnectBanner popup flow) above the write-back mappings when mappings exist without write consent. Sketch in feature-delta.md.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
Sign-in asks only for the rights the connection needs, the granted rights are stored, existing connections count as write-consented, and write-back without write consent is skipped and logged
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
The connection settings show the Rights line, and a warning with Sign in again above the write-back mappings when mappings exist without write consent
```

#### Promises

P4

P5

#### Depends on

None.

#### Slice work

None.
