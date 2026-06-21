# Slice 02 — Configure the install via values (image, replicas, hostname)

> Story: #5199 · Persona: platform-operator (self-hoster) · Size: ≤1 day · Release: R1 (configurable)

## Learning hypothesis
We believe a self-hoster will configure the deployment by editing values, not by editing templates.
We will know it is true when changing image tag, replica count and ingress hostname in a values file
visibly changes the rendered/running deployment with no template edits.

## Elevator Pitch
- **Before:** to change the image or hostname the operator has to hand-edit raw manifests.
- **After:** they run `helm install l8e ./chart --set image.tag=v26.6.x --set ingress.host=lh.acme.internal` and `kubectl get deploy l8e-api -o yaml` shows the new tag; the app answers on the new host.
- **Decision enabled:** "I can pin a version and place Lighthouse on my own hostname — it fits my environment."

## In scope
- Parameterise: `image.repository` / `image.tag`, `replicaCount`, `ingress.host`, `ingress.tls`, resource requests/limits.
- Sensible defaults in values.yaml; omitting any of these yields a working install.
- Scaffold `values-enterprise.yaml` as the production-grade reference file (keys present, commented; deeper sections filled in slice 03).

## Out of scope
- Postgres / MCP / OIDC (slice 03). Publishing (slice 04). Docs prose (slice 05).

## Production-data acceptance (real, not synthetic)
- A real `helm install ... --set image.tag=<a real published tag>` runs and the running pod uses that exact image.
- Setting `replicaCount: 2` (with the epic-5305 Redis backplane pre-req present) yields 2 Ready API pods that don't double-sync.
- Setting `ingress.host` makes the app reachable on that host.

## Dogfood moment
Install once with defaults, once with an overridden tag + host + replicaCount=2; confirm both come up.

## Embedded AC (Given/When/Then)
- Given the operator sets `image.tag` to a published release, When they install, Then the running API pod's image equals that tag.
- Given the operator sets `ingress.host`, When they install, Then the app is reachable on that host and NOTES.txt prints that host.
- Given the operator omits all overrides, When they install, Then defaults apply and the install still succeeds (no required-without-default knob in this slice).
- Given the operator sets `replicaCount: 2`, When they install, Then 2 API pods are Ready and a sync runs once across the fleet (epic-5305 pre-req honoured).
