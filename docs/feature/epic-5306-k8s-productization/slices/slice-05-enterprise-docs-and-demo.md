# Slice 05 — Enterprise docs: architecture, quick-start, config reference, demo walkthrough

> Story: #5200 · Personas: platform-operator (self-hoster reads to deploy) + prospect (reads to evaluate) · Size: ≤1 day · Release: R2 (pitch-ready)

## Learning hypothesis
We believe a self-hoster (and a prospect) can both self-host AND evaluate Lighthouse from the docs
alone — no sales call, no support ticket. We will know it is true when a reader follows the published
quick-start to a running instance and the demo walkthrough (install -> auth -> MCP -> scaling) runs
verbatim.

## Elevator Pitch
- **Before:** the production self-host story is tribal knowledge; a prospect can't evaluate without a call.
- **After:** the reader opens the published docs page and sees the architecture diagram (Ingress -> oauth2-proxy -> API + MCP + Postgres), a copy-paste quick-start, a full config reference, and a runnable demo walkthrough.
- **Decision enabled (self-hoster):** "I can deploy this myself from the docs." **(prospect):** "This is real and demoable — I'd pitch it internally."

## In scope
- Architecture diagram: Ingress -> oauth2-proxy -> API + MCP + Postgres.
- README/docs: prerequisites, quick-start, full config reference (every values-enterprise.yaml option documented with comments).
- Demo walkthrough: install -> auth -> MCP -> scaling, reproducible step by step.
- Publish chart + README to the LetPeopleWork GitHub org (docs surface of the publish step).
- Config reference is cross-checked against values.yaml comments (single source of truth) to prevent drift.

## Out of scope
- Chart code itself (slices 01-04). Private gitops/SaaS ops docs (out of feature scope). In-app product UX docs (unaffected).

## Production-data acceptance (real, not synthetic)
- A reader following the published quick-start verbatim reaches a responding instance using the real published chart.
- The demo walkthrough's four stages (install, auth, MCP, scaling) each execute against the real image and produce the documented observable output.
- Every option in the config reference maps to an actual chart value (no documented-but-nonexistent knobs).

## Dogfood moment
A fresh reader (Claude/maintainer playing the self-hoster) runs the quick-start + demo walkthrough top to bottom on a clean cluster.

## Embedded AC (Given/When/Then)
- Given the published docs, When a reader views the architecture section, Then a rendered diagram shows Ingress -> oauth2-proxy -> API + MCP + Postgres.
- Given the quick-start, When a reader follows it verbatim on a conformant cluster, Then they reach a responding Lighthouse instance.
- Given the config reference, When each documented option is checked against the chart, Then every option corresponds to a real values key (no drift).
- Given the demo walkthrough, When a reader runs install -> auth -> MCP -> scaling, Then each stage produces the documented observable output (pods Running, OIDC login, an MCP call, replicas scaled).
- Given a values key is renamed in the chart, When finalization runs, Then the docs-vs-values drift check flags the stale reference (maintainer finalization gate).
