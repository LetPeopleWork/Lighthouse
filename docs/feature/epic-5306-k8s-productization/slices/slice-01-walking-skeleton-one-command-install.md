# Slice 01 — Walking Skeleton: one-command install brings the stack up

> Story: #5199 · Persona: platform-operator (self-hoster) · Size: ≤1 day · Release: Walking Skeleton

## Learning hypothesis
We believe a self-hoster can go from "a pile of raw manifests" to "a running Lighthouse" with a
single `helm install` against a local chart directory. We will know it is true when a real
`helm install l8e ./chart` of the real published image yields all pods Ready and a NOTES.txt
that names the access URL.

## Elevator Pitch
- **Before:** the self-hoster hand-writes Deployment + Service + Ingress YAML and wires them up by hand.
- **After:** they run `helm install l8e ./chart` and see `kubectl get pods` report `l8e-api  1/1  Running`, and Helm prints NOTES.txt with the access URL.
- **Decision enabled:** "Lighthouse is installable as a unit — I can adopt it for my org without bespoke YAML."

## In scope
- Minimal `chart/` skeleton: Chart.yaml, values.yaml, templates for API Deployment + Service + Ingress + NOTES.txt.
- `frontend.mode: embedded` default (the sacrosanct single-container shape) — API serves the SPA.
- API wired to the epic-5305 readiness/liveness/startup probes (pre-req, already shipped) so rollout gates on real health.
- NOTES.txt prints the resolved access URL + next-step hint.

## Out of scope
- Postgres / MCP / OIDC values (slice 03). Parameterising image tag / replicas / host beyond defaults (slice 02). Publishing to a repo (slice 04). Docs (slice 05). `frontend.mode: split` path (deferred; toggle stub only).

## Production-data acceptance (real, not synthetic)
- `helm install l8e ./chart` runs against the REAL published Lighthouse image (current release tag), not a stub.
- `kubectl get pods` shows the API pod `1/1 Running`; readiness probe gates it into rotation.
- The configured ingress hostname serves the app over HTTP(S).
- NOTES.txt output contains the access URL.

## Dogfood moment
Claude/maintainer runs the actual `helm install` against a local k3s with the real image and opens the URL.

## Embedded AC (Given/When/Then)
- Given a clean k3s cluster and the `chart/` directory, When the operator runs `helm install l8e ./chart`, Then all chart workloads reach Ready and Helm exits 0.
- Given the install succeeded, When the operator reads the install output, Then NOTES.txt names the access URL and the next step.
- Given default values (no overrides), When the chart renders, Then `frontend.mode` is `embedded` and exactly one API workload is defined (standalone gate preserved).
