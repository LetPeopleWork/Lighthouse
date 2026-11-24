# Acceptance brief: bug\-6686\-jira\-oauth\-write\-consent

## Slices

### S1

##### Covers

P1

```text
An admin starts the Jira OAuth sign-in on a connection without write-back mappings
```

```text
The sign-in asks Atlassian for the read scopes and offline_access only
```

```text
The admin is not asked for write permission in Jira when write-back is not set up
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:A\_connection\_without\_write\_back\_asks\_Atlassian\_only\_to\_read

```text
The write scope is requested for every Jira connection, or a read scope is dropped
```

###### Obligations

None.

##### Covers

P1

```text
An admin starts the Jira OAuth sign-in on a connection with a write-back mapping
```

```text
The sign-in asks for the read scopes plus write:jira-work
```

```text
The consent screen in Atlassian names the write right once write-back is saved
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:A\_connection\_with\_write\_back\_asks\_Atlassian\_to\_read\_and\_write\_work\_items

```text
The sign-in still asks only for read scopes
```

###### Obligations

None.

##### Covers

P2

```text
Atlassian grants only read rights, or an empty, absent or lookalike write scope, or a refresh answers with write scope on a read-only sign-in; a write-back then runs; the connection list is read
```

```text
Nothing is sent to Jira, one warning naming the connection and telling the admin to sign in again is logged, the stored value is unchanged, and the connection list says Rights Read and needsWriteConsent true
```

```text
The admin sees the Sign in again warning and no write is attempted on Jira
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:Write\_back\_on\_a\_connection\_signed\_in\_only\_to\_read\_sends\_nothing\_and\_logs\_one\_warning

```text
Write-back is still sent on a read-only Jira grant, logs no or several warnings, or the list shows no rights
```

###### Obligations

None.

##### Covers

P2

```text
A Jira sign-in stored before rights were recorded is used for a write-back
```

```text
Nothing is sent to Jira; the sign-in counts as Rights Read with needsWriteConsent true and one warning is logged
```

```text
A Jira sign-in made before this fix asks to be signed in again once write-back is used
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:A\_connection\_signed\_in\_before\_rights\_were\_recorded\_counts\_as\_read\_only

```text
A missing record of rights is treated as write allowed, as for Azure DevOps
```

###### Obligations

None.

##### Covers

P3

```text
After a read-only grant the admin signs in again and Atlassian grants write:jira-work; a write-back runs
```

```text
The change is sent to Jira once, the list says Rights ReadAndWrite with no warning
```

```text
After signing in again, write-back reaches Jira and the warning disappears
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:After\_the\_admin\_signs\_in\_again\_with\_write\_consent\_write\_back\_sends\_the\_change

```text
The old read-only record stays after a new sign-in
```

###### Obligations

None.

##### Covers

P3

```text
A Jira API-token connection with write-back runs a write-back
```

```text
The change is sent to Jira and the connection list shows no rights and no warning
```

```text
Jira API-token connections keep writing back as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs\:\:A\_connection\_signed\_in\_with\_an\_API\_token\_keeps\_writing\_back

```text
API-token connections are blocked or flagged for write consent
```

###### Obligations

None.

### S2

##### Covers

P4

```text
The connection settings page shows a Jira OAuth connection and an Azure DevOps OAuth connection whose sign-in may only read while write-back is set up
```

```text
The Jira connection's warning reads that Lighthouse can only read from Jira with this sign-in and never carries the Azure DevOps sentence; the Azure DevOps connection's warning names Azure DevOps and never the Jira sentence; both keep the Sign in again button
```

```text
A Jira admin is told the sign-in can only read from Jira and can sign in again to give write rights
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.trackerName\.test\.tsx

```text
The Azure DevOps warning keeps naming Azure DevOps while a Jira warning is shown only for one tracker type
```

###### Obligations

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
