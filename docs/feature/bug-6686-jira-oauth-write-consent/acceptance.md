# Acceptance brief: bug\-6686\-jira\-oauth\-write\-consent

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Connecting a Jira OAuth connection asks Atlassian for write:jira-work only when write-back mappings are saved on that connection; a connection with no write-back mappings asks for the read scopes only.
```

#### E2

##### Promises

P2

```text
Write-back on a Jira OAuth connection that was signed in without write rights sends nothing to Jira, including a connection signed in before rights were recorded.
```

#### E3

##### Promises

P2

```text
That refused write-back attempt on a Jira OAuth connection without write rights writes exactly one warning to the log.
```

#### E4

##### Promises

P2

```text
A Jira OAuth connection without write rights shows the rights as Rights Read, together with a Sign in again warning.
```

#### E5

##### Promises

P3

```text
After the admin signs in again and Atlassian grants write:jira-work, write-back on that Jira OAuth connection sends the change to Jira.
```

#### E6

##### Promises

P3

```text
Write-back on Azure DevOps OAuth connections and API-token connections keeps working as it did before this change.
```

#### E7

##### Promises

P4

```text
The write-consent warning names the connection's own tracker, Jira or Azure DevOps, and never the other one.
```

#### E8

##### Promises

P4

```text
The write-consent warning offers a Sign in again action on the connection it describes.
```

## Charter

None.

## Examination

None.
