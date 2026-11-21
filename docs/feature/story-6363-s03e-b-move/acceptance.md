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
