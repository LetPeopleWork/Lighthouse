# Slice 02: Refuse an unsafe upgrade before touching anything, and document the new path

**Goal:** every case the chart cannot upgrade safely stops before writing, with one log line naming the
reason and the way out; the Kubernetes docs describe the automatic upgrade, rollback and cleanup.

## IN scope
- Refusal with no write for: too little PVC room (AC-3.1), data ≥2 majors behind (AC-3.2), data newer
  than the image with no kept copy (AC-3.3).
- An interrupted upgrade never starts Postgres on a partial copy (AC-3.4); fixing the cause lets the next
  start proceed (AC-3.5).
- Rewrite `docs/Installation/kubernetes.md` § Upgrading the bundled PostgreSQL (AC-2.3): automatic path,
  where the old copy sits and its size, rollback semantics, the cleanup command, the manual path for
  out-of-range majors.
- `chart/values.yaml` image comment, regenerated `chart/README.md`, `Chart.yaml` version note.
- Release-notes line on #6131.

## OUT of scope
- Growing the PVC or deleting the old copy automatically.
- Compose and binary installs.

## Learning hypothesis
- **Disproves** "every unsafe case is detectable before anything is written" if any refusal scenario
  finds the data directory changed afterwards → the detection step needs redesign before release.
- **Confirms** it if all refusal scenarios leave the volume byte-identical and the fixed-cause rerun
  succeeds.

## Acceptance criteria
AC-2.3, AC-3.1 … AC-3.5 in `../feature-delta.md`.

## Dependencies
Slice 01 (detection step and layout).

## Effort
About ½–1 day. Reference class: the chart's render-time `required` guards (encryption key, ingress host).

## Dogfood
kind cluster with a deliberately small PVC: upgrade, read the refusal, grow the PVC, watch it proceed.
Then follow the rewritten docs section verbatim from a clean 0.1.17 install.
