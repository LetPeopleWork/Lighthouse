> nw-research reading guide for story #5191 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 01 — Run l8e Naively (Reading Guide)

**Date**: 2026-06-07 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 6 primary, all High-tier official docs
**Doc currency**: k8s concept pages are evergreen-per-version (current nav tracks v1.36.x); image refs grounded in this repo's `Dockerfile` and `README.md` as of HEAD.

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the Deployment + Service + temporary
> NodePort) is throwaway learning scaffolding. Keep it in a personal scratch dir, e.g.
> `~/learn-k8s/story-01/`. Nothing from this story goes into the Lighthouse repo — real manifests
> first land in `chart/` at story 09 (per the planning §7 workspace map). You're deploying the
> *real* Lighthouse image, but the YAML you write here is rehearsal, not product.

## 1. Orientation

Story 00 left you with a *bare* Pod that, once deleted, stayed dead — nothing owned it. Story 01 fixes
that and runs the real app. You write a **Deployment** for the Lighthouse API (single replica,
SQLite, no persistence yet), front it with a **ClusterIP Service**, confirm the frontend is served by
that same API (no second Deployment — see §2 D4 fact), then poke a temporary **NodePort** through to
hit it in a browser. The payoff experiment: delete the Deployment-managed pod and watch it *come
back* — the opposite of story 00 — then delete the pod again and watch your SQLite data vanish,
because the database lived inside the pod's ephemeral filesystem. "Done" feels like authoring a
Deployment + Service for a real app from a blank prompt and explaining, unaided, *why* the pod
self-heals and *why* its data doesn't.

## 2. Concepts

### Deployment → ReplicaSet → Pod (the ownership chain)

**What it is.** A Deployment is the controller you almost always want. It doesn't run Pods directly —
it creates and manages a **ReplicaSet**, and the ReplicaSet creates and reconciles the Pods.

> "A *Deployment* provides declarative updates for Pods and ReplicaSets." — kubernetes.io [1]
> "You describe a *desired state* in a Deployment, and the Deployment Controller changes the actual state to the desired state at a controlled rate." — kubernetes.io [1]
> "The Deployment creates a ReplicaSet that creates three replicated Pods, indicated by the `.spec.replicas` field." — kubernetes.io [1]

This is the **ownership chain** that answers story 00's open question. In story 00 a bare Pod had no
owner, so deleting it was final. Here the Pod has an `OwnerReference` back to the ReplicaSet, whose
whole job is:

> "A ReplicaSet's purpose is to maintain a stable set of replica Pods running at any given time." — kubernetes.io [2]

So when you delete a Deployment-managed pod, the ReplicaSet notices `current < desired` and creates a
replacement. **Reconcile loop, not magic.** And you manage the ReplicaSet *through* the Deployment,
never directly:

> "a Deployment is a higher-level concept that manages ReplicaSets... we recommend using Deployments instead of directly using ReplicaSets." — kubernetes.io [2]

A Service is **not** part of this chain — it never creates or heals Pods, it only *routes* to them
(see the Service concept below). Keep the two responsibilities separate in your head: the
Deployment/ReplicaSet owns *lifecycle*; the Service owns *addressing*.

- Read: Deployments — https://kubernetes.io/docs/concepts/workloads/controllers/deployment/ [1]
- Read: ReplicaSet — https://kubernetes.io/docs/concepts/workloads/controllers/replicaset/ [2]

You should be able to answer:
- Why does a Deployment-managed pod come back after `kubectl delete pod`, when story 00's bare Pod did not? Name the object that recreates it and the field that tells it how many to keep.
- What does a Service contribute to recreating a deleted Pod? (Trick question — answer in one word.)

### The label selector is the binding (not name, not port)

**What it is.** Two separate links, both by **label**, not by name or port:
1. The ReplicaSet finds *its* Pods via the Deployment's `.spec.selector` matching the pod template's labels.
2. The Service finds *its* Pods via the Service's `.spec.selector` matching those same pod labels.

> "The `.spec.selector` field defines how the created ReplicaSet finds which Pods to manage. In this case, you select a label that is defined in the Pod template (`app: nginx`)." — kubernetes.io [1]
> "A ReplicaSet identifies new Pods to acquire by using its selector." — kubernetes.io [2]

The Deployment's `.spec.selector.matchLabels` **must** match `.spec.template.metadata.labels` — if
they disagree the API rejects the Deployment. The Service's `selector` is a separate field that must
*also* match those pod labels for traffic to land. A mismatched selector is the single most common
"my Service returns nothing" bug: the Pods are healthy, the Service just selects an empty set. The
match is purely on labels — the object *names* and the *port numbers* are irrelevant to the binding.

- Read: Service (selector / how a Service targets Pods) — https://kubernetes.io/docs/concepts/services-networking/service/ [3]

You should be able to answer:
- Which three label sets must agree, and what breaks if the Service selector is a typo away from the pod labels?
- If the Deployment is named `lighthouse` but the pod labels are `app: l8e`, what must the Service `selector` say — and does the Service name matter?

### Service: ClusterIP vs NodePort

**What it is.** A stable network front for the set of Pods its selector matches, because Pod IPs are
ephemeral.

> "A Service is a method for exposing a network application that is running as one or more Pods in your cluster." — kubernetes.io [3]

- **ClusterIP** (default): a virtual IP reachable **only inside the cluster**. This is the right,
  permanent type for the API — other in-cluster workloads (later: the MCP service, probes) reach it
  here. But you *cannot* curl a ClusterIP from your host; it isn't host-reachable. That's exactly
  why this story adds a **temporary** NodePort just to eyeball the app in a browser.
- **NodePort**: opens a static port on every node's IP (`<NodeIP>:<NodePort>`), default range
  **30000–32767** [3]. On single-node k3s the node *is* your host, so `localhost:<nodePort>` works.
  Treat it as a throwaway viewing hatch — the real exposure story is Ingress (story 03).

- Read: Service — https://kubernetes.io/docs/concepts/services-networking/service/ [3]

You should be able to answer:
- Why does the story keep ClusterIP as the Service's real type but bolt on a NodePort to view it in a browser? What's unreachable about ClusterIP from the host?
- What's the default NodePort range, and why is single-node k3s special when you curl `localhost:<nodePort>`?

### Image pull policy

**What it is.** When the kubelet (re)starts a container, `imagePullPolicy` decides whether to hit the
registry. Three values, and a default that depends on your tag:

> "`IfNotPresent`: the image is pulled only if it is not already present locally." — kubernetes.io [4]
> "`Always`: every time the kubelet launches a container, the kubelet queries the container image registry to resolve the name to an image digest..." — kubernetes.io [4]
> "`Never`: the kubelet does not try fetching the image." — kubernetes.io [4]
> "if you omit the `imagePullPolicy` field, and the tag for the container image is `:latest`, `imagePullPolicy` is automatically set to `Always`." — kubernetes.io [4]
> "if you omit the `imagePullPolicy` field, and you specify a tag for the container image that isn't `:latest`, the `imagePullPolicy` is automatically set to `IfNotPresent`." — kubernetes.io [4]
> "You should avoid using the `:latest` tag... specify a meaningful tag such as `v1.42.0` and/or a digest." — kubernetes.io [4]

Practical consequence for this story: **pin a real Lighthouse version tag** (not `:latest`). With a
pinned tag the default policy becomes `IfNotPresent`, so once the image is on the node, restarts are
fast and reproducible — and you always know *which* version is running. See §3 for the concrete image
ref and how to find the current tag.

- Read: Images (imagePullPolicy) — https://kubernetes.io/docs/concepts/containers/images/ [4]

You should be able to answer:
- With image tag `:1.2.3` and no `imagePullPolicy`, what policy applies and why? What changes if the tag were `:latest`?
- Why does pinning a version tag make a Deployment's restarts both faster and more honest?

### Environment variables (configuring the container)

**What it is.** You configure the Lighthouse container through env vars in the pod template's
`containers[].env` (later promoted to a ConfigMap/Secret in story 04+). The image already sets sane
defaults itself — `Kestrel__Endpoints__Http__Url=http://+:80` and `LIGHTHOUSE_DOCKER=true` (see §3) —
so the naive run needs almost no env. The mental model: `env:` entries become process environment
inside the container; ASP.NET maps the `__` (double-underscore) form to nested config keys.

- Read: Define Environment Variables for a Container — https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/ [6]

You should be able to answer:
- Where in a Deployment manifest do per-container env vars live, and what's the minimum env Lighthouse needs for a SQLite naive run (hint: the image already defaults the port and `LIGHTHOUSE_DOCKER`)?

## 3. Repo-grounded facts (deploy the REAL Lighthouse, not a guess)

All cited from this repo at HEAD — verify before you copy.

- **Image:** `ghcr.io/letpeoplework/lighthouse`. Confirmed in `README.md:52` (`docker pull
  ghcr.io/letpeoplework/lighthouse:latest`), `examples/postgres/docker-compose.yml:22`
  (`:dev-latest`), and `SECURITY.md:81` ("Official Docker images
  (`ghcr.io/letpeoplework/lighthouse`)"). **Pin a real release tag, not `:latest`** (per [4]).
  - **Find the current tag:** releases are at
    `https://github.com/LetPeopleWork/Lighthouse/releases/latest` (`README.md:43,58`). From a shell:
    `gh release view --repo LetPeopleWork/Lighthouse --json tagName -q .tagName`. Tag families on
    GHCR: `:latest` (newest release), `:dev-latest` (CI head — used by the E2E suite,
    `Lighthouse.EndToEndTests/package.json:13`), and per-release calver tags. Use a concrete
    release tag in your manifest.
- **Port:** the container listens on **80** (http) and **443** (https). `Dockerfile:4` `EXPOSE 80
  443`; `Dockerfile:66-67` set `Kestrel__Endpoints__Http__Url="http://+:80"` /
  `Https__Url="https://+:443"`. For a naive single-pod run, target **containerPort 80** (plain http)
  and skip TLS — the cert story is story 03.
- **`LIGHTHOUSE_DOCKER=true`** is baked into the image (`Dockerfile:68`); you don't need to set it.
- **SQLite / data path:** the data dir is **`/app/data`**, created and `chown app:app`'d in
  `Dockerfile:61` (`mkdir -p /app/logs /app/data`). The SQLite connection string points a DB file
  under the app data dir — `README.md:53` runs with `Database__ConnectionString=Data
  Source=/app/Data/LighthouseAppContext.db`. **Naive run = no volume, no PVC:** the DB lives inside
  the pod's writable layer, which is destroyed with the pod. That's the deliberate lesson of this
  story (§5.2 demo).
- **D4 fact — ONE Deployment, the API serves the SPA.** The image builds the React frontend into the
  backend's `wwwroot` and the API serves it: `Dockerfile:34-35` copies `node-builder /node/dist/.`
  into `./Lighthouse.Backend/wwwroot/`. Planning **D4** confirms: "the image builds the React
  frontend into the backend's `wwwroot` and the API serves it — one process, one container... story
  01 stands up the API (serving the SPA in `embedded` mode) as the single app workload." **Do not
  write a second frontend Deployment.** The story's "Deployment for the frontend (or confirm served
  by the API)" resolves to *confirm served by the API*.

## 4. Debug reflex (carry this through the story)

When a pod won't run, pick the right tool for the phase:

- **`kubectl describe pod <name>` → read the Events section** for *pre-run* problems: image can't be
  pulled (`ImagePullBackOff` / `ErrImagePull` — wrong tag or private registry), scheduling failures,
  failed mounts. There are no container logs yet because the container never started.
- **`kubectl logs <pod>`** *only once the container is running* (or `--previous` for a crashed prior
  instance). Logs are the app's own stdout/stderr — useless if the image never came up.

Mnemonic: **describe→Events before the container runs; logs after.** If you reflexively run `logs`
on an `ImagePullBackOff` pod you'll get nothing and waste a minute — the answer is always in
`describe`'s Events.

## 5. Hands-on — copy/paste commands

Try each block from memory first; these are the backstop. Replace `<TAG>` with a real release tag
(see §3). Work in `~/learn-k8s/story-01/`.

### 5.1 The Deployment (single replica, SQLite, no persistence)

```yaml
# lighthouse-deploy.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lighthouse
  labels:
    app: lighthouse
spec:
  replicas: 1
  selector:
    matchLabels:
      app: lighthouse          # ← must match template labels below (the ReplicaSet binding)
  template:
    metadata:
      labels:
        app: lighthouse        # ← the Service will select on this same label
    spec:
      containers:
        - name: lighthouse
          image: ghcr.io/letpeoplework/lighthouse:<TAG>   # pin a real release tag, never :latest
          imagePullPolicy: IfNotPresent                   # implicit for a non-:latest tag; explicit for clarity
          ports:
            - containerPort: 80     # Kestrel http (Dockerfile EXPOSE 80 / Kestrel__...Http__Url=:80)
          # No env needed for the naive SQLite run: the image already sets the port,
          # LIGHTHOUSE_DOCKER=true, and a default SQLite connection string.
          # No volumes → SQLite at /app/data is ephemeral (that's the lesson in 5.4).
```

```bash
kubectl apply -f lighthouse-deploy.yaml
kubectl get deploy,rs,pods            # see Deployment → its ReplicaSet → 1 Pod (the ownership chain)
kubectl rollout status deploy/lighthouse
kubectl describe pod -l app=lighthouse   # if not Running: read Events (ImagePull? scheduling?)
kubectl logs -l app=lighthouse           # once Running: app stdout
```

### 5.2 The ClusterIP Service (the real, permanent exposure)

```yaml
# lighthouse-svc.yaml
apiVersion: v1
kind: Service
metadata:
  name: lighthouse
spec:
  type: ClusterIP            # default; cluster-internal only — NOT reachable from your host
  selector:
    app: lighthouse          # ← must match the pod labels — THIS is the binding, not the name/port
  ports:
    - port: 80               # the Service's cluster-internal port
      targetPort: 80         # the container port it forwards to
```

```bash
kubectl apply -f lighthouse-svc.yaml
kubectl get svc lighthouse
kubectl get endpoints lighthouse      # should list the pod IP — empty here = selector mismatch
# prove it's reachable INSIDE the cluster (and that you can't curl it from the host):
kubectl run curl --rm -it --image=curlimages/curl --restart=Never -- curl -s http://lighthouse/ | head
```

### 5.3 Temporary NodePort to view it in a browser

ClusterIP isn't host-reachable, so add a throwaway NodePort just to look (delete it after).

```yaml
# lighthouse-nodeport.yaml  (temporary — viewing hatch only)
apiVersion: v1
kind: Service
metadata:
  name: lighthouse-view
spec:
  type: NodePort
  selector:
    app: lighthouse          # same label binding
  ports:
    - port: 80
      targetPort: 80
      nodePort: 30080        # must be in 30000–32767
```

```bash
kubectl apply -f lighthouse-nodeport.yaml
kubectl get svc lighthouse-view          # see TYPE=NodePort and 80:30080/TCP
curl -s http://localhost:30080/ | head   # single-node k3s: node == host
# open http://localhost:30080 in a browser → the Lighthouse SPA, served BY the API (D4: one Deployment)
```

### 5.4 The two experiments

**(a) Delete the pod — watch the ReplicaSet bring it back** (contrast with story 00's bare Pod):

```bash
kubectl get pods -l app=lighthouse -w &        # watch
kubectl delete pod -l app=lighthouse           # delete the Deployment-managed pod
# the watch shows the old pod Terminating AND a NEW pod appear — the ReplicaSet reconciled desired=1
kubectl get rs -l app=lighthouse               # DESIRED=1 CURRENT=1 — the controller did it, not you
kill %1
```

**(b) Delete the pod — watch SQLite data vanish** (no persistence):

```bash
# 1. Add some state in the browser (create a team/project) via http://localhost:30080
# 2. Delete the pod so a fresh one replaces it:
kubectl delete pod -l app=lighthouse
kubectl rollout status deploy/lighthouse
# 3. Reload the browser → your team/project is GONE.
# Why: the SQLite file lived at /app/data inside the pod's writable layer, which is destroyed with
# the pod. The ReplicaSet gave you a brand-new pod with a brand-new empty filesystem. Persistence
# (a PVC) is story 02/04 — this story is meant to FEEL the loss.
```

### 5.5 Tear down

```bash
kubectl delete -f lighthouse-nodeport.yaml -f lighthouse-svc.yaml -f lighthouse-deploy.yaml
kubectl get deploy,rs,svc,pods            # confirm all gone (incl. the ReplicaSet)
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *author a Deployment + Service for a real app and explain ReplicaSets, image pull
policy, env vars, and why pod-local storage is ephemeral.* Unaided, you should be able to:

- [ ] Write a Deployment (replicas: 1) for `ghcr.io/letpeoplework/lighthouse:<real-tag>` from scratch, with matching selector/template labels, and explain why those labels must agree.
- [ ] Write a ClusterIP Service that lands traffic on the pod, and explain why its `selector` (not name, not port) is the binding.
- [ ] Explain why you keep ClusterIP as the real type but add a NodePort to view in a browser — and why `localhost:<nodePort>` works on single-node k3s.
- [ ] Delete the pod and explain, in your own words, what recreates it (ReplicaSet, desired-count reconcile) — and contrast with story 00's bare Pod.
- [ ] State the default `imagePullPolicy` for a pinned tag vs `:latest`, and why pinning is the honest choice.
- [ ] Point to where env vars live in the manifest and say why the naive SQLite run barely needs any (image defaults).
- [ ] Demonstrate and explain why creating state then deleting the pod loses your data (SQLite in `/app/data` lives in the ephemeral pod layer).
- [ ] Confirm — and justify from the Dockerfile — that the frontend is served by the API, so there's no second Deployment (D4).

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick one throwaway experiment that *tests the ownership lesson*:

> **Scale the Deployment to 2 replicas (`kubectl scale deploy/lighthouse --replicas=2`) and watch
> `kubectl get pods -w`. What does the ReplicaSet do? Then delete ONE of the two pods — what happens,
> and how is that different from deleting the bare Pod back in story 00? Form a hypothesis about what
> the ReplicaSet is comparing on each loop *before* you read the answer. Bonus: with 2 replicas both
> running SQLite-in-pod, what now happens to your data consistency — and what does that tell you
> about why story 02 moves to Postgres?**

Investigate, don't look it up: scale, observe, delete a pod, observe. The 2-replica SQLite mess is
the experiential argument for the next story.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Deployments | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 2 | ReplicaSet | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 3 | Service | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (corroborated, story-00 verified) |
| 4 | Images (imagePullPolicy) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 5 | Lighthouse repo (Dockerfile, README, docker-compose, SECURITY) | this repo | Primary | First-party source | 2026-06-07 | Y (line-cited) |
| 6 | Define Environment Variables for a Container | kubernetes.io | High (1.0) | Official | 2026-06-07 | Referenced (standard task page) |

All k8s sources are primary, first-party project documentation (High tier). Repo facts are line-cited
from this repository's own source files — the strongest possible authority for what the image does.

## Knowledge Gaps

- **Exact current release tag not pinned in this doc.** Tags move; §3 gives the discovery command
  (`gh release view ... -q .tagName`) and the releases URL rather than hard-coding a tag that would
  rot. Pin the tag you find at build time.
- **Default SQLite connection string when no env is passed not exhaustively traced through C#.**
  `README.md:53` shows the documented `Data Source=/app/Data/LighthouseAppContext.db`; the image
  applies a sane default for a bare run. If the naive pod fails to find a DB path, set
  `Database__ConnectionString` explicitly via env (§2 env-vars). Verify on first run via
  `kubectl logs`.
- **HTTPS/cert path (443) intentionally out of scope.** The image exposes 443 with a default cert,
  but TLS/Ingress is story 03; this story uses plain http on 80.

## Full Citations

[1] The Kubernetes Authors. "Deployments". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/controllers/deployment/. Accessed 2026-06-07.
[2] The Kubernetes Authors. "ReplicaSet". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/controllers/replicaset/. Accessed 2026-06-07.
[3] The Kubernetes Authors. "Service". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/service/. Accessed 2026-06-07.
[4] The Kubernetes Authors. "Images". kubernetes.io. https://kubernetes.io/docs/concepts/containers/images/. Accessed 2026-06-07.
[5] LetPeopleWork. Lighthouse repository — `Dockerfile` (lines 4, 34-35, 61, 66-68), `README.md` (lines 43, 52-53, 58), `examples/postgres/docker-compose.yml` (line 22), `SECURITY.md` (line 81). Accessed 2026-06-07.
[6] The Kubernetes Authors. "Define Environment Variables for a Container". kubernetes.io. https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/. Accessed 2026-06-07.
