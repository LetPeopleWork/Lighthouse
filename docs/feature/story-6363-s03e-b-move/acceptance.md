# Acceptance brief: story\-6363\-s03e\-b\-move

## Slices

### S1

##### Covers

P1

```text
Look up the licence service, verifier and their interfaces in the compiled backend
```

```text
All four live in the Licensing Application namespace and none is left in the old Services Licensing namespaces; a listed name with no type fails
```

```text
Licensing application code sits in its own module folder, so its boundary can be ruled by namespace
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingLayoutArchUnitTest\.cs\:\:TheLicenseServicesAndTheirInterfaces\_LiveInTheLicensingApplication\_AndNoneIsLeftUnderServices

```text
A copy left in the old namespace is rejected; a vacuous list is rejected by the missing-name check
```

###### Obligations




##### Covers

P5

```text
Look up the licence guard attribute in the compiled backend
```

```text
It lives in the Licensing Http adapters namespace and not under the old Services Licensing namespace
```

```text
The licence gate sits in the module's adapter layer
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingLayoutArchUnitTest\.cs\:\:TheLicenseGuard\_LivesInTheLicensingHttpAdapters\_AndIsNotLeftUnderServices

```text
The guard placed in the Application namespace instead of the Http adapters is rejected
```

###### Obligations




##### Covers

P6

```text
Read the embedded resource names of the compiled backend
```

```text
Exactly one resource ends with public_key.pem and it is named Lighthouse.Backend.Modules.Licensing.Application.public_key.pem
```

```text
Licence signatures keep verifying because the key travels with its verifier
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingLayoutArchUnitTest\.cs\:\:TheLicencePublicKey\_IsEmbeddedOnce\_BesideItsVerifier

```text
Key left in the old folder or embedded twice is rejected; a moved but unreadable key is caught by the existing verifier tests
```

###### Obligations



### S2

##### Covers

P2

```text
Look up each Access Domain name and each shared Models type Access takes with it in the compiled backend, and read its namespace
```

```text
All eight live in the Access module's Domain namespace; none is left in the old Services Auth or Authorization folders or in Models Auth or Authorization; a list name that matches no type fails and names itself
```

```text
A maintainer finds the Access rules and the values they work on in the Access module's Domain, so the module boundary can be ruled by namespace
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:TheAccessRulesAndTheValuesTheyWorkOn\_LiveInTheAccessDomain\_AndNoneIsLeftUnderServicesOrModels

```text
A type moved into the right namespace while its file stays in the old folder; the folder is checked by review of the git mv, not by this namespace read
```

###### Obligations





##### Covers

P2

```text
Look up each of the sixteen Access services and interfaces in the compiled backend, and read its namespace
```

```text
All sixteen live in the Access module's Application namespace, each interface beside its service; none is left in the old Services Auth or Authorization folders
```

```text
A maintainer finds every Access service and its interface in the Access module's Application
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:TheAccessServicesAndTheirInterfaces\_LiveInTheAccessApplication\_AndNoneIsLeftUnderServices

```text
A service whose namespace line moved while its file stayed in the old folder; the folder is checked by review of the git mv, not by this namespace read
```

###### Obligations





##### Covers

P2

```text
Read every type reference made by the types in the Access module's Domain in the compiled backend
```

```text
They reach only their own Domain, the shared Models and the base library; a Domain holding no type fails instead of passing
```

```text
The Access rules can be checked by handing them values, because nothing they call can read a database, a setting or the web request
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:TheAccessDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
A Domain type reaching a Models type that itself reads the database; the kernel is trusted as a whole by this rule
```

###### Obligations



##### Covers

P2

```text
Run the same Access Domain rule over a made-up Access Domain type that takes the API-key repository, beside a twin that uses only a Models role, a sibling and the base library
```

```text
Exactly the type that takes the repository is reported, the failure names it and the repository, and the twin is examined but not reported
```

```text
A maintainer who makes an Access rule read a repository is stopped with the type and the repository named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:AnAccessDomainTypeThatReadsARepository\_IsReported\_AndItsTwinReachingOnlyTheKernelIsNot

```text
A rule that reports any non-kernel reference but names the wrong target in its message, as long as the message still contains the repository's full name
```

###### Obligations




##### Covers

P2

P6

```text
Read the public members of the one RBAC administration service interface in the compiled backend, each as name, return type and parameter types
```

```text
Exactly the 44 members fixed from the compiled interface before the move, compared both ways; a second or missing interface of that name fails
```

```text
Every controller and test double calls the same RBAC administration members after the move as before it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:TheRbacAdministrationService\_KeepsEveryPublicMember\_ExactlyAsBeforeTheMove

```text
A changed default value of an optional parameter, or a renamed parameter, with names, return and parameter types unchanged
```

###### Obligations




##### Covers

P2

```text
Compare a made-up interface that kept one member, gained one, lost one and changed one member's return type with its list from before
```

```text
The member comparison fails naming the added member, the lost member, and the re-typed member with both its old and its new type, and does not name the kept member
```

```text
A maintainer who changes the RBAC administration surface during a move is stopped with the changed member named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessApplicationLayoutArchUnitTest\.cs\:\:AnInterfaceWithAMemberAddedRemovedAndRetyped\_FailsTheMemberPin\_NamingEachChange

```text
A comparison that also flagged a reordered member list; the comparison is by set, so order is free
```

###### Obligations




##### Covers

P5

```text
Run the existing architecture rules whose lists name a moved Access type by its full name, after the move
```

```text
Each list names the moved type in its new namespace and is still compared both ways, so a stale old name fails as no longer offending and a new edge fails as unlisted
```

```text
The module boundary rules keep guarding the Access types after they move, none quietly loosened
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture \(existing both\-ways module boundary\, access\-table\, every\-action\-declares\-access and test\-host lists\)\, run by the declared full filtered gate

```text
A list entry deleted together with the edge it allowed, if the edge itself also disappeared; both-ways comparison accepts that
```

###### Obligations


##### Covers

P5

```text
Look at where the unit tests of the moved Access types sit after the move
```

```text
Each moved unit test sits under the test project's Modules Access folder of its type's layer, namespace equal to folder
```

```text
A maintainer finds the tests of an Access type beside its layer in the test project
```

##### Disposition

NotTested

```text
No placement rule for unit tests exists yet; the design puts that rule in the later feature that adds the Access and Licensing module rules, and in this slice the git mv of each test file is checked in review
```

feature C of story 6363 \(module rules and unit\-test placement rule\)\; slice reviewer for this slice

##### Covers

P6

```text
Run the whole existing backend suite, live connectors left out, after the move
```

```text
Every existing test stays green, edited only in usings, full-name strings and moves; the composition root registers the same services with the same lifetimes in the same order
```

```text
Nothing a user, an API client, a tracker or the database sees changes: sign-in, RBAC answers, API keys and licence status behave as before
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend with the declared live\-connector exclusion filter \(existing suite\)

```text
A behaviour change in a path no existing test covers; the move is checked to be namespace-only by review of the diff
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
After the move, a reader of the licensing code finds every licensing type in the Licensing module under its designed layer, with namespace matching folder, and no licensing type is still reachable under its old namespace; the licence still verifies against its embedded public key.
```

#### E2

##### Promises

P2

```text
The Access domain and application types sit in the Access module's Domain and Application folders, the Access domain depends on nothing but the shared kernel and the base library, and the RBAC administration service offers exactly the same public operations as before the move.
```

#### E3

##### Promises

P3

```text
The Access guards, markers, filters and authentication handlers sit in the Access HTTP adapter; the RBAC guard attribute and the four attributes are declared and behave as before, the count of guarded actions equals the count measured at the start of B3, and the development log setting still silences the authentication handler by its full type name.
```

#### E4

##### Promises

P4

```text
The licensing test data and licensing integration test sit under the Licensing tests folder, and every build, CI and configuration reference to them points there, so licensing tests and the live authentication runs still find the licence files.
```

#### E5

##### Promises

P5

```text
The unit tests of moved types sit in the matching tests module folder, and the architecture rules and lists name moved types at their new place and still detect a mismatch in either direction.
```

#### E6

##### Promises

P6

```text
A user or API client sees no change: auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys and licence status (before and after an import) answer as before, the database model is unchanged, the live authentication, RBAC and proxy-auth journeys pass and all existing tests stay green.
```

## Charter

None.

## Examination

None.
