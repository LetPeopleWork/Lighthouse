# Lighthouse Helm chart

Flow metrics and probabilistic forecasting for Kubernetes. Postgres-only (ADR-080); the chart
brings the whole stack up — API (SPA served in-process), bundled or external Postgres, optional MCP
workload and OIDC — with one command.

- **Chart version:** `0.1.1`
- **App image (appVersion):** `26.6.21.1`

## Install from the published Helm repo (no source checkout)

```sh
helm repo add letpeoplework https://docs.lighthouse.letpeople.work/charts
helm repo update
helm search repo lighthouse          # shows CHART 0.1.1 / APP 26.6.21.1
helm install l8e letpeoplework/lighthouse --version 0.1.1 -f values-enterprise.yaml
```

The default values render the standalone-parity shape (`frontend.mode=embedded`, one API workload,
bundled Postgres). For production, copy [`values-enterprise.yaml`](./values-enterprise.yaml), fill the
REQUIRED values (host, TLS secret, Redis when scaling, OIDC, MCP, external DB) and pass it with `-f`.

## Install from a source checkout (development)

```sh
helm install l8e ./chart -f chart/values-enterprise.yaml
```

## Versioning (ADR-083)

The **chart version** (`Chart.yaml: version`) is the single source of truth for the package and is
bumped on every publish — the publish step refuses to overwrite an already-published version. The
**appVersion** mirrors `image.tag` (the Lighthouse image the chart ships by default). The publish
guard (`scripts/version-guard.sh`) asserts both chains agree across `Chart.yaml`, this README, the
in-cluster `NOTES.txt`, the published index and `values-enterprise.yaml` before any publish.

## Publish (maintainer)

```sh
chart/scripts/publish.sh            # guard → helm package → helm repo index --merge into docs/charts/
git add docs/charts chart && git commit && git push   # pages.yml serves docs/charts/ on the existing Pages
```
