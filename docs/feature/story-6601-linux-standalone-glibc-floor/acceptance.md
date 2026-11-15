# Acceptance brief: story\-6601\-linux\-standalone\-glibc\-floor

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
The Linux standalone release build runs on a CI runner image that is not retired, so a release is never blocked by a retired image.
```

#### E2

##### Promises

P1

```text
Before a runner image is retired, a visible alarm reports it, early enough that a release is not blocked by the retirement unannounced.
```

#### E3

##### Promises

P2

```text
A written decision states the minimum glibc version and Linux distribution that the AppImage requires.
```

#### E4

##### Promises

P2

```text
A build whose AppImage needs a newer glibc than the written floor fails in CI, so such an AppImage is never released.
```

#### E5

##### Promises

P3

```text
The produced AppImage starts on Ubuntu 24.04 LTS.
```

## Charter

None.

## Examination

None.
