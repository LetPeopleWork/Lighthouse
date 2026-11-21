# Acceptance brief: story\-6363\-s03e\-b\-move

## Slices

None.

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
