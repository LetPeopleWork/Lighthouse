# Slice 04 — Package & publish the chart to the public Helm repo

> Story: #5199 · Personas: lighthouse-maintainer (publishes) + platform-operator (consumes) · Size: ≤1 day · Release: R2 (publicly installable)

## Learning hypothesis
We believe publishing the chart to a GitHub Pages Helm repo makes it installable by any external
self-hoster with `helm repo add` — with low enough ceremony that the maintainer does it at
finalization, not as a release-day scramble. We will know it is true when an external user runs
`helm repo add` + `helm install l8e letpeoplework/lighthouse` and gets the running stack.

## Elevator Pitch
- **Before:** the chart only exists in the repo; an external self-hoster has to clone the source to install.
- **After:** the maintainer runs the package+publish step; an external user runs `helm repo add letpeoplework https://letpeoplework.github.io/<repo>` then `helm search repo lighthouse` and sees the published version.
- **Decision enabled (self-hoster):** "I can install Lighthouse straight from a public Helm repo, no source clone." **(maintainer):** "I can ship a new chart version as a cheap, repeatable step."

## In scope
- `helm package` + Chart.yaml version bump (explicit, single-source).
- Helm repo index generation + push to the GitHub Pages branch under the LetPeopleWork GitHub org.
- A finalization-gate check: refuse to overwrite an existing chart version silently.
- Self-hoster path: `helm repo add` / `helm repo update` / `helm search repo` resolve the published chart.

## Out of scope
- Docs prose / architecture diagram / demo walkthrough (slice 05). Private gitops repo / ArgoCD (out of feature scope).

## Production-data acceptance (real, not synthetic)
- A real `helm package` produces a versioned .tgz; the published index lists that exact version.
- From a machine WITHOUT the source repo, `helm repo add` + `helm install l8e letpeoplework/lighthouse -f values-enterprise.yaml` brings the real stack up Ready.
- `helm search repo lighthouse` shows the published version string equal to Chart.yaml.

## Dogfood moment
On a clean machine with no source checkout, add the public repo and install end to end from it.

## Embedded AC (Given/When/Then)
- Given the chart is packaged with a bumped version, When the maintainer publishes, Then the GitHub Pages Helm index lists that version and `helm search repo` shows it.
- Given the published repo, When an external user (no source) runs `helm repo add` then `helm install`, Then the stack comes up Ready.
- Given the chart version was NOT bumped, When the maintainer attempts to publish, Then the finalization-gate check flags it and publish does not silently overwrite.
- Given the chart version, When compared across Chart.yaml, the published index, NOTES.txt and the README install snippet, Then all four agree (shared-artifact consistency).
