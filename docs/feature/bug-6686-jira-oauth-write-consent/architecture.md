# Architecture brief: bug\-6686\-jira\-oauth\-write\-consent

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProviderTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkTrackingSystemConnectionDtoTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/OAuth\/OAuthCredentialTest\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/JiraOAuthWriteConsent\/JiraOAuthWriteConsentTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~JiraOAuthWriteConsent
```

#### Supports

None.

#### Public oracle

```text
An admin signs in a Jira OAuth connection with and without write-back mappings, Atlassian answers the token request with a granted scope, and a write-back runs on that connection
```

```text
Without write-back the sign-in asks Atlassian for the read scopes and offline_access only; with write-back it also asks for write:jira-work. A grant naming write:jira-work lets write-back send the change to Jira; a grant without it, or a Jira sign-in stored before rights were recorded, sends nothing, logs one warning, and the connection list says Rights Read and needsWriteConsent true. Azure DevOps OAuth and API-token connections behave as before.
```

```text
The sign-in URL for a Jira connection with write-back lacks write:jira-work, or a read-only Jira grant (or an old Jira sign-in with no recorded rights) still sends a write-back to Jira, or the connection list shows no rights for a Jira OAuth connection
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.trackerName\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Connections/WriteConsentAlert.trackerName.test.tsx
```

#### Supports

None.

#### Public oracle

```text
The connection settings page shows a Jira OAuth connection and an Azure DevOps OAuth connection whose sign-in may only read while write-back is set up
```

```text
The Jira connection's warning reads: Write-back needs more rights. Lighthouse can only read from Jira with this sign-in. Sign in again to allow Lighthouse to write the mapped fields. The Azure DevOps connection's warning names Azure DevOps. Both keep the Sign in again button.
```

```text
A Jira connection's warning names Azure DevOps, or names no tracker, or the Sign in again button is gone
```

##### Promises

P4

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
