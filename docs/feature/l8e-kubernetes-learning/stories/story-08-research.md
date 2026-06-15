> nw-research reading guide for story #5198 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 08 — Namespaces & Multi-Env (Kustomize base+overlays + NetworkPolicy) (Reading Guide)

**Date**: 2026-06-15 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 4 primary (3 kubernetes.io pages + k3s docs + this repo)
**Doc currency**: the namespaces / NetworkPolicy / Kustomize pages are stable, evergreen-per-version k8s concepts (`kubernetes.io/metadata.name` auto-label is stable since k8s 1.22; Kustomize built into kubectl since 1.14). The **k3s NetworkPolicy fact is the load-bearing environment detail**: your k3s ships an **embedded kube-router netpol controller, enabled by default** — so unlike a bare flannel cluster, the policies you write here **actually enforce**. Read §1 and the k3s note in §2 before you assume a policy "didn't work."

> **🗂 Workspace: SCRATCH — light loop, NOT a D3 exception.** All of this is throwaway YAML in
> `~/learn-k8s/story-08/`, never committed to the repo. No product C#/TS changes, no tests, no
> CLAUDE.md checklist. This is the classic light loop: read → build it yourself → `nw-spike` →
> Socratic review. (The Kustomize layout you build here is *rehearsal* for the real Helm chart in
> story 09 — see the forward hook in §3. Don't try to make it production-grade; make it teach you.)

## 1. Orientation

So far you've run **one** Lighthouse in **one** namespace (`lighthouse`): Deployment + Service (02), Postgres + PVC (02), Ingress + TLS (03), probes (04), edge auth (05), the MCP workload (06), and you've scaled + autoscaled it (07). Story 08's card asks for something deceptively small: stand up **two** environments — `lighthouse-dev` and `lighthouse-prod` — that differ only in **image tag, replicas, and hostname**, *without* copy-pasting (and drifting) two near-identical manifest trees, and then **firewall them off from each other** with a NetworkPolicy.

Three concepts carry the story, and each has a "gotcha" that's the actual lesson:

```
   Namespace        scopes NAMES and is the unit of multi-env / multi-tenant — BUT it is
                    NOT a network boundary. Two namespaces can talk to each other freely
                    by default. (This is why step 5 exists.)

   Kustomize        base + overlays: write the manifests ONCE (base), express each env as a
   (base+overlay)   thin diff (overlay) — namespace, image tag, replica count, hostname.
                    No templating language; it's typed transformers + strategic-merge patches.
                    GOTCHA: it's not Helm. No `{{ }}`, no values.yaml, no conditionals. That's
                    deliberate — story 09 replaces this with Helm; here you feel why you'd want it.

   NetworkPolicy    the thing that makes a namespace an actual isolation boundary. GOTCHA #1:
                    pods are ALLOW-ALL until a policy selects them. GOTCHA #2: a policy only
                    does anything if the CNI enforces it — and your k3s DOES (embedded
                    kube-router), so you'll see real blocking, not a no-op.
```

The honest framing: **a namespace is an organizational boundary, not a security one.** "Multi-env" is two things stacked — *namespaces* give you separate copies with separate names and separate DNS; *Kustomize* keeps those copies DRY; *NetworkPolicy* is what stops dev from reaching prod's database. The card bundles them because that's the real shape of "run more than one of this thing safely."

"Done" feels like: `kubectl apply -k overlays/dev` and `kubectl apply -k overlays/prod` each stand up a complete, **independently-configured** Lighthouse (different tag, replicas, host) in its own namespace from **one** shared base; you can explain what the `namespace`/`images`/`replicas` transformers did by diffing `kubectl kustomize overlays/dev` vs `overlays/prod`; and you applied a NetworkPolicy that lets a pod reach Services **in its own namespace** but **blocks** it from reaching the other env's Service — and you *proved* the block with a `wget` that hangs, having predicted it would.

This is also the **dress rehearsal for two big things downstream**: story 09's Helm chart does what your Kustomize overlays do (parameterize one manifest set), and the SaaS north-star's **namespace-per-tenant + NetworkPolicy isolation** (planning §4 architecture) is *exactly* this pattern with `tenant-<name>` instead of `dev`/`prod`. Learn it here on two envs; apply it later to N tenants.

## 2. Concepts

### Namespaces — scope for names, unit of multi-env, NOT a network boundary

**What it is.** A namespace partitions one cluster into named slices.

> "In Kubernetes, _namespaces_ provide a mechanism for isolating groups of resources within a single cluster." — kubernetes.io [1]

> "Namespaces provide a scope for names. Names of resources need to be unique within a namespace, but not across namespaces." — kubernetes.io [1]

So `lighthouse-dev/lighthouse` and `lighthouse-prod/lighthouse` are two different Deployments that can share the name `lighthouse` because the namespace disambiguates them.

**The "not everything is namespaced" fact** (matters when you wonder why a Namespace or PV ignores your `namespace:` setting):

> "Namespace-based scoping is applicable only for namespaced objects _(e.g. Deployments, Services, etc.)_ and not for cluster-wide objects _(e.g. StorageClass, Nodes, PersistentVolumes, etc.)_." — kubernetes.io [1]

`kubectl api-resources --namespaced=true` / `--namespaced=false` lists each set. Namespaces themselves, Nodes, PVs, StorageClasses are **cluster-scoped**.

**Cross-namespace DNS** (you'll use this to *test* the NetworkPolicy):

> "This entry is of the form `<service-name>.<namespace-name>.svc.cluster.local`, which means that if a container only uses `<service-name>`, it will resolve to the service which is local to a namespace." — kubernetes.io [1]

> "If you want to reach across namespaces, you need to use the fully qualified domain name (FQDN)." — kubernetes.io [1]

So inside `lighthouse-dev`, `postgres` resolves to dev's Postgres; to *attempt* to reach prod's Lighthouse you'd target `lighthouse.lighthouse-prod.svc.cluster.local` — which is precisely what your NetworkPolicy should block.

**The automatic label** (lets a NetworkPolicy/`namespaceSelector` pick a namespace by name without you labelling it):

> "The Kubernetes control plane sets an immutable label `kubernetes.io/metadata.name` on all namespaces. The value of the label is the namespace name." — kubernetes.io [1] (stable since 1.22)

**The thing the doc does NOT say but you must internalize:** a namespace, by itself, gives you **zero** network isolation. Pods in `lighthouse-dev` can open connections to pods/Services in `lighthouse-prod` with no restriction until a NetworkPolicy says otherwise. Namespace = names + a scope for quotas/RBAC, **not** a firewall.

- Read: Namespaces (concept; scope-for-names; not-all-namespaced; DNS; auto-label) — https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/ [1]

You should be able to answer:
- Why can both envs have a Deployment literally named `lighthouse`? What disambiguates them, and what's the FQDN of dev's Postgres Service vs prod's?
- Name three resources that ignore a `namespace:` setting because they're cluster-scoped.
- Inside `lighthouse-dev`, what does the bare name `postgres` resolve to, and what would you type to (try to) reach prod's Lighthouse?
- True/false: putting prod in its own namespace stops dev from talking to it. (False — and that's why step 5 exists.)

### Kustomize — base + overlays: write manifests once, diff per environment

**What it is.** Template-free customization built into kubectl: a **base** holds the canonical manifests; each **overlay** references the base and layers typed transformations on top.

> "Since 1.14, kubectl also supports the management of Kubernetes objects using a kustomization file." — kubernetes.io [2]

Invoke it with `kubectl kustomize <dir>` (render to stdout — *always diff with this before applying*) or `kubectl apply -k <dir>` (render + apply).

**Base vs overlay.** A base is a directory with a `kustomization.yaml` + resource files. An overlay is a directory whose `kustomization.yaml` lists the base under `resources:` and then customizes:

```yaml
# overlays/dev/kustomization.yaml
resources:
  - ../../base
namespace: lighthouse-dev      # stamp every namespaced resource into this ns
```

**The transformers the card needs** (these are the whole point — they replace copy-paste):

- `namespace:` — sets `metadata.namespace` on every namespaced resource in the build → this is how one base becomes "the dev copy" vs "the prod copy."
- `images:` — rewrites an image's name/tag/digest without editing the Deployment. This is your **"different image tag"** lever (dev tracks `latest`/`main`; prod pins a release).
- `replicas:` — overrides a workload's replica count. Your **"different replicas"** lever (dev 1, prod 2).
- `patches:` (strategic-merge or JSON6902) — targeted edits for anything without a dedicated transformer, e.g. the **Ingress hostname** per env.
- `configMapGenerator:` / `secretGenerator:` — generate a ConfigMap/Secret from literals or files; the generated object gets a **content-hash name suffix**:

  > "a content hash suffix appended. This ensures that a new ConfigMap or Secret is generated when the contents are changed" — kubernetes.io [2]

  which **auto-triggers a rolling update** when config changes (the pod template references the new hashed name). This is a genuinely nice property Helm makes you wire by hand.

**The gotcha that IS the lesson:** Kustomize has **no templating language** — no `{{ .Values.x }}`, no conditionals, no loops. You can only *override what already exists* in the base. When you find yourself wishing you could say "if prod, add a sidecar," you've found the wall that **story 09 (Helm)** exists to climb. Feel that wall here; don't fight it.

- Read: Declarative Management with Kustomize (kubectl `-k`; bases/overlays; transformers; generators) — https://kubernetes.io/docs/tasks/manage-kubernetes-objects/kustomization/ [2]

You should be able to answer:
- What does `kubectl kustomize overlays/dev` do vs `kubectl apply -k overlays/dev`, and why run the first one first?
- Which transformer changes the image tag, which changes replicas, which sets the namespace — and which mechanism (not a simple transformer) do you use for the per-env Ingress hostname?
- Why does editing a value in a `configMapGenerator` cause pods to roll, while editing a hand-written ConfigMap of the same name does not?
- Name one thing you *cannot* express in Kustomize that you'd want for prod-vs-dev — i.e. the reason story 09 switches to Helm.

### NetworkPolicy — turning a namespace into an actual boundary

**What it is.** A namespaced resource that says which connections to/from selected pods are allowed. The two facts that trip everyone:

**(1) Pods are allow-all until selected.**

> "By default, a pod is non-isolated for ingress; all inbound connections are allowed." — kubernetes.io [3]

> "A pod is isolated for ingress if there is any NetworkPolicy that both selects the pod and has 'Ingress' in its `policyTypes`." — kubernetes.io [3]

Once *any* ingress policy selects a pod, that pod flips to **default-deny ingress** — only what the policy's `ingress` list allows gets through:

> "When a pod is isolated for ingress, the only allowed connections into the pod are those from the pod's node and those allowed by the `ingress` list of some NetworkPolicy that applies to the pod for ingress." — kubernetes.io [3]

**(2) A policy is inert unless the CNI enforces it.**

> "Network policies are implemented by the network plugin… Creating a NetworkPolicy resource without a controller that implements it will have no effect." — kubernetes.io [3]

**This is where your environment matters — and the good news:** **k3s enforces NetworkPolicy out of the box.**

> "K3s includes an embedded network policy controller. The underlying implementation is kube-router's netpol controller library." — docs.k3s.io [4]

(You can disable it with `--disable-network-policy`, but the default is ON.) So on your cluster, policies **bite** — a blocked connection genuinely hangs/fails. If you'd been on a bare-flannel cluster without a netpol controller, the same YAML would silently do nothing, and you'd waste an hour. Know which world you're in.

**Selecting across namespaces:**

> "**namespaceSelector**: This selects particular namespaces for which all Pods should be allowed as ingress sources or egress destinations." — kubernetes.io [3]

Combined with the auto-label `kubernetes.io/metadata.name`, you can allow exactly one namespace. But for "isolate the envs," the simpler shape is: in prod, **allow ingress only from prod itself** (an empty `podSelector: {}` in `from` means "all pods in *this policy's* namespace"), and **deny everything else** — which by construction blocks dev.

**The egress trap (why the card asks only about ingress):** if you ever add a default-deny **egress** policy, you will also block the pod's **DNS lookups** (to kube-dns/CoreDNS on UDP/TCP 53) and its outbound calls to Jira/ADO/Linear — Lighthouse will look "broken" for non-obvious reasons. The card scopes you to **restricting traffic between namespaces** = **ingress** isolation; leave egress open here, and treat "lock down egress without breaking DNS + connectors" as a deliberate spike (§7), not the main path.

- Read: Network Policies (default non-isolation; policyTypes; namespaceSelector; default-deny) — https://kubernetes.io/docs/concepts/services-networking/network-policies/ [3]
- Read (pointer): k3s networking — embedded netpol controller — https://docs.k3s.io/networking/networking-services [4]

You should be able to answer:
- What is a pod's ingress posture with **no** NetworkPolicy, and what changes the instant *one* ingress policy selects it?
- Why does the same NetworkPolicy YAML enforce on your k3s but would be a no-op on some other clusters — what's doing the enforcing?
- How does `from: [{ podSelector: {} }]` differ from `from: [{ namespaceSelector: {} }]` — which one means "same namespace only"?
- Why does the card restrict **between-namespace (ingress)** traffic and stay away from egress — what would a careless default-deny-egress break first?

## 3. Repo-grounded facts (what actually differs per env for Lighthouse)

These ground the overlays. From the earlier stories' notes (02/03) — verify the current tag yourself.

- **Image:** `ghcr.io/letpeoplework/lighthouse`. Pin a release tag for prod via `gh release view --repo LetPeopleWork/Lighthouse` (story 01). **Dev tracks a floating tag** (e.g. `latest`), **prod pins** a specific release — that contrast *is* the `images:` transformer demo.
- **One container serves both SPA + API (embedded frontend, D4).** There is no separate frontend workload to template here — `frontend.mode` stays `embedded` through Bands A–C (planning Q4). So "deploy Lighthouse" = one Deployment + one Service + one Ingress per env (plus its Postgres). Container port **80**; data dir **/app/data**.
- **Per-env config is just env vars (story 02).** Postgres is selected by `Database__Provider=postgres` + `Database__ConnectionString=Host=postgres;Database=lighthouse;Username=postgres;Password=…` (the `__` → `:` ASP.NET mapping; migrations auto-run on boot). Give **each namespace its own Postgres** so the envs are truly isolated (and so the NetworkPolicy has something meaningful to protect) — with Postgres-per-namespace the connection string is *identical* in both envs (bare `postgres` short-DNS resolves locally), so the only real per-env differences end up being **image tag, replicas, Ingress host**, exactly the card's list.
- **Hostnames come from the Ingress (story 03).** Dev = e.g. `lighthouse-dev.local`, prod = `lighthouse.local`, each with a `/etc/hosts` entry pointing at the cluster (mkcert TLS as in 03). The host string is the per-env **`patches:`** demo (Ingress has no dedicated Kustomize transformer for host).
- **The SignalR hub rides `/api/updateNotificationHub`** (story 07 §3) — no extra Ingress rule needed; `/api/*` already routes to the backend. Nothing to do here, just don't forget it when copying the Ingress.

> **Forward hook — this Kustomize tree is a throwaway rehearsal for story 09 (Helm).** Don't invest in making it pretty or committing it. The moment you wish for a conditional or a values file, that's story 09. And the moment you think "dev/prod is just two values of one parameter," you've previewed the SaaS model where **each tenant is a namespace** (`tenant-<name>`) with the **same** NetworkPolicy isolation you write here (planning §4). Same pattern, swap `dev|prod` for tenant names.

## 4. Debug reflex (carry this through the story)

Carry forward the prior rules (`describe`→Events before `logs`; `get endpoints` to see Service backends — 03; `/api` 401 = unauth — 05; probe the right path — 04), then add the namespace/kustomize/netpol shapes. **Before each symptom, name whether it's a names problem, a render problem, or a policy problem.**

- **`kubectl get pods` shows "No resources found"** after you applied an overlay → you're looking in the wrong **namespace**. `kubectl get pods -n lighthouse-dev`. (Set a default with `kubectl config set-context --current --namespace=lighthouse-dev`.) Names problem.
- **`kubectl apply -k` errors before anything is created** → render problem. Run `kubectl kustomize overlays/dev` alone to see the rendered YAML and the kustomize error (bad `resources:` path, transformer typo). Never debug a kustomization by applying it — render it.
- **The image/replicas didn't change between envs** → the transformer didn't match. The `images: name:` must equal the **exact** image string in the base (`ghcr.io/letpeoplework/lighthouse`), and `replicas: name:` must equal the workload name. `kubectl kustomize overlays/prod | grep -E 'image:|replicas:'` to confirm the render, not the cluster.
- **Both envs answer on the same hostname / one overwrites the other** → your Ingress host patch didn't apply, so both Ingresses claim the same host. Diff the rendered Ingress per overlay.
- **A connection you *expected* to work now hangs after applying a NetworkPolicy** → you flipped a pod to default-deny and forgot to allow a legitimate path (most often: Lighthouse→its own Postgres, or the Ingress controller→Lighthouse). The Ingress controller lives in **another namespace** (`kube-system`/`traefik`), so a "same-namespace-only" ingress rule will **also block Traefik** → your site 502s. You must allow ingress from the ingress-controller namespace too. Policy problem — and the most common real one.
- **The NetworkPolicy seems ignored (cross-namespace still works)** → first confirm enforcement exists: on k3s it's on by default (kube-router), but check you didn't start k3s with `--disable-network-policy`. Then confirm a policy actually **selects** the target pod (`podSelector` matches its labels) and has `Ingress` in `policyTypes`. An unselected pod stays allow-all.
- **A `wget` test "fails" but for the wrong reason** → distinguish *blocked by policy* (connect **hangs** then times out) from *DNS not found* (resolves instantly to "bad address") from *nothing listening* (instant "connection refused"). Only the hang/timeout is your policy working.

Mnemonic: **"No resources found" = wrong namespace; apply-time error = render it with `kubectl kustomize`; tag/replicas unchanged = transformer name mismatch; site 502 after a netpol = you blocked Traefik (allow the ingress-controller namespace); cross-ns still works = policy doesn't select the pod or k3s netpol is disabled; hang≠refused≠NXDOMAIN.**

## 5. Hands-on — copy/paste manifests & commands

Try each block from memory first; these are the backstop. Scratch dir `~/learn-k8s/story-08/`. Layout:

```
~/learn-k8s/story-08/
├── base/
│   ├── kustomization.yaml
│   ├── postgres.yaml          # Deployment + Service + PVC (per-namespace DB)
│   └── lighthouse.yaml        # Deployment + Service + Ingress
└── overlays/
    ├── dev/
    │   ├── kustomization.yaml
    │   └── ingress-host.yaml   # strategic-merge patch: dev host
    └── prod/
        ├── kustomization.yaml
        ├── ingress-host.yaml   # strategic-merge patch: prod host
        └── netpol.yaml         # isolation policies (prod)
```

### 5.0 Create the two namespaces

```bash
kubectl create namespace lighthouse-dev
kubectl create namespace lighthouse-prod
kubectl get ns --show-labels | grep lighthouse   # note the kubernetes.io/metadata.name label
```

### 5.1 base/lighthouse.yaml — one Deployment + Service + Ingress (embedded SPA+API)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata: { name: lighthouse, labels: { app: lighthouse } }
spec:
  replicas: 1                                  # overlays override this
  selector: { matchLabels: { app: lighthouse } }
  template:
    metadata: { labels: { app: lighthouse } }
    spec:
      containers:
        - name: lighthouse
          image: ghcr.io/letpeoplework/lighthouse   # overlays set the tag via images:
          ports: [{ containerPort: 80 }]
          env:
            - { name: Database__Provider, value: postgres }
            - { name: Database__ConnectionString, value: "Host=postgres;Database=lighthouse;Username=postgres;Password=devpassword" }
          resources: { requests: { cpu: "100m", memory: "256Mi" }, limits: { cpu: "500m", memory: "512Mi" } }
          volumeMounts: [{ name: data, mountPath: /app/data }]
      volumes: [{ name: data, emptyDir: {} }]   # scratch: emptyDir; Postgres holds the real state
---
apiVersion: v1
kind: Service
metadata: { name: lighthouse, labels: { app: lighthouse } }
spec:
  selector: { app: lighthouse }
  ports: [{ port: 80, targetPort: 80 }]         # ClusterIP
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata: { name: lighthouse }
spec:
  rules:
    - host: PLACEHOLDER.local                   # overlays patch this
      http:
        paths:
          - path: /
            pathType: Prefix
            backend: { service: { name: lighthouse, port: { number: 80 } } }
```

### 5.2 base/postgres.yaml — a per-namespace Postgres (so the envs are truly isolated)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata: { name: postgres, labels: { app: postgres } }
spec:
  replicas: 1
  selector: { matchLabels: { app: postgres } }
  template:
    metadata: { labels: { app: postgres } }
    spec:
      containers:
        - name: postgres
          image: postgres:16-alpine             # pin a digest in anything you keep
          env:
            - { name: POSTGRES_DB, value: lighthouse }
            - { name: POSTGRES_USER, value: postgres }
            - { name: POSTGRES_PASSWORD, value: devpassword }   # use a Secret for real (story 13)
          ports: [{ containerPort: 5432 }]
          volumeMounts: [{ name: pgdata, mountPath: /var/lib/postgresql/data }]
      volumes: [{ name: pgdata, emptyDir: {} }]  # scratch only — real PVC is story 02's job
---
apiVersion: v1
kind: Service
metadata: { name: postgres, labels: { app: postgres } }
spec:
  selector: { app: postgres }
  ports: [{ port: 5432, targetPort: 5432 }]      # bare "postgres" resolves to THIS namespace's DB
```

### 5.3 base/kustomization.yaml

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - lighthouse.yaml
  - postgres.yaml
```

### 5.4 overlays/dev — floating tag, 1 replica, dev host

```yaml
# overlays/dev/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../../base
namespace: lighthouse-dev
images:
  - name: ghcr.io/letpeoplework/lighthouse
    newTag: latest                 # dev tracks the floating tag
replicas:
  - name: lighthouse
    count: 1
patches:
  - path: ingress-host.yaml
    target: { kind: Ingress, name: lighthouse }
```

```yaml
# overlays/dev/ingress-host.yaml  (strategic-merge patch — just the host)
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata: { name: lighthouse }
spec:
  rules:
    - host: lighthouse-dev.local
      http:
        paths:
          - path: /
            pathType: Prefix
            backend: { service: { name: lighthouse, port: { number: 80 } } }
```

### 5.5 overlays/prod — pinned tag, 2 replicas, prod host, + isolation

```yaml
# overlays/prod/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../../base
  - netpol.yaml
namespace: lighthouse-prod
images:
  - name: ghcr.io/letpeoplework/lighthouse
    newTag: "v25.x.x"              # PIN a real release tag (gh release view --repo LetPeopleWork/Lighthouse)
replicas:
  - name: lighthouse
    count: 2
patches:
  - path: ingress-host.yaml
    target: { kind: Ingress, name: lighthouse }
```

```yaml
# overlays/prod/ingress-host.yaml  (prod host)
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata: { name: lighthouse }
spec:
  rules:
    - host: lighthouse.local
      http:
        paths:
          - path: /
            pathType: Prefix
            backend: { service: { name: lighthouse, port: { number: 80 } } }
```

```yaml
# overlays/prod/netpol.yaml — isolate prod: deny cross-namespace ingress, allow same-ns + the ingress controller
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: { name: default-deny-ingress }
spec:
  podSelector: {}                  # selects ALL pods in this (prod) namespace → flips them to default-deny ingress
  policyTypes: [Ingress]
  # no ingress: rules → nothing allowed in, yet (the next policy opens the legitimate paths)
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: { name: allow-same-namespace-and-ingress-controller }
spec:
  podSelector: {}
  policyTypes: [Ingress]
  ingress:
    - from:
        - podSelector: {}          # any pod IN THIS namespace (prod) — Lighthouse<->its own Postgres
    - from:
        - namespaceSelector:       # the Traefik/ingress controller lives in another namespace
            matchLabels: { kubernetes.io/metadata.name: kube-system }
```

> Two real-world notes baked in above: (1) `podSelector: {}` in `from` = "same namespace only" — that's what blocks `lighthouse-dev`. (2) Without the second `from` (the ingress-controller namespace), your default-deny would also block **Traefik**, and `lighthouse.local` would 502. On stock k3s Traefik runs in `kube-system`; confirm with `kubectl get pods -A | grep traefik` and fix the namespace label match if it differs.

### 5.6 Render, then apply, then verify both envs stand up independently

```bash
# ALWAYS render first — never debug by applying:
kubectl kustomize overlays/dev  | grep -E 'namespace:|image:|replicas:|host:'
kubectl kustomize overlays/prod | grep -E 'namespace:|image:|replicas:|host:'   # different tag/replicas/host

kubectl apply -k overlays/dev
kubectl apply -k overlays/prod
kubectl get deploy,po,svc,ingress -n lighthouse-dev
kubectl get deploy,po,svc,ingress -n lighthouse-prod    # prod shows 2 lighthouse replicas
```

### 5.7 Prove the NetworkPolicy: same-namespace allowed, cross-namespace blocked

```bash
# from a throwaway pod IN dev, try to reach PROD's Lighthouse (should HANG → time out = blocked):
kubectl run probe --rm -it --image=busybox --restart=Never -n lighthouse-dev -- \
  wget -qO- --timeout=5 http://lighthouse.lighthouse-prod.svc.cluster.local/api/v1/version/current
# expect: wget: download timed out   (the policy did its job)

# from a throwaway pod IN prod, reach prod's OWN Lighthouse (should WORK — same namespace allowed):
kubectl run probe --rm -it --image=busybox --restart=Never -n lighthouse-prod -- \
  wget -qO- --timeout=5 http://lighthouse.lighthouse-prod.svc.cluster.local/api/v1/version/current
# expect: a JSON/text response   (same-namespace traffic still flows)

# and the site itself through Traefik must still load (proves you didn't block the ingress controller):
curl -k https://lighthouse.local/api/v1/version/current   # /etc/hosts → cluster (story 03)
```

### 5.8 Teardown

```bash
kubectl delete -k overlays/dev  --ignore-not-found
kubectl delete -k overlays/prod --ignore-not-found
kubectl delete namespace lighthouse-dev lighthouse-prod --ignore-not-found
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *You can isolate environments and remove config duplication with Kustomize base + overlays, and you understand NetworkPolicy.* Unaided, you should be able to:

- [ ] Create `lighthouse-dev` and `lighthouse-prod` and explain why both can hold a Deployment named `lighthouse`, and give the FQDN of each env's Postgres Service.
- [ ] Stand up **both** envs from **one base** with `kubectl apply -k`, differing only in **image tag, replicas, hostname** — and prove the diff with `kubectl kustomize` (not by inspecting the cluster).
- [ ] Say which transformer did the tag (`images:`), the replica count (`replicas:`), the namespace (`namespace:`), and which mechanism (`patches:`) did the Ingress host — and why config in a `configMapGenerator` rolls the pods.
- [ ] State the two NetworkPolicy gotchas: pods are **allow-all until selected**, and a policy is **inert without an enforcing CNI** — and confirm your k3s enforces (embedded kube-router, on by default).
- [ ] Write a policy that **blocks** dev→prod while **allowing** prod-internal traffic **and** the Traefik ingress controller, and **prove** the block with a `wget` that times out (distinguishing a policy hang from refused/NXDOMAIN).
- [ ] Explain why the card restricts **ingress** between namespaces and why a careless default-deny **egress** would break DNS + the Jira/ADO/Linear connectors first.
- [ ] Name the one thing you couldn't express in Kustomize that you'd want for prod (the reason story 09 is Helm), and how this exact pattern becomes **namespace-per-tenant + NetworkPolicy** in the SaaS north-star.

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick throwaway experiments that *test the lesson at its edges*. Form a hypothesis **before** each run:

> **(a) Prove a namespace is not a boundary — then make it one.** *Before* applying any NetworkPolicy, `wget` prod's Lighthouse from a pod in dev. Predict: it **works** (allow-all). Then apply `netpol.yaml` and repeat. Predict: it **hangs/times out**. The lesson in your hands: **isolation is a policy you add, not a property a namespace has.**
>
> **(b) Feel the default-deny flip.** Apply only `default-deny-ingress` (the first policy, no allow). Predict: prod's site 502s (Traefik blocked) **and** Lighthouse can't reach its own Postgres. Add the second policy's same-namespace `from` → Postgres works but the site still 502s. Add the ingress-controller `from` → site loads. The lesson: **once selected, a pod denies everything; you must enumerate every legitimate caller — including ones in other namespaces (Traefik).**
>
> **(c) Break the image transformer on purpose.** In `overlays/prod`, change `images: name:` to a string that *doesn't* match the base image. Run `kubectl kustomize overlays/prod | grep image:`. Predict: the tag is **unchanged** (no match = no-op, silently). The lesson: **Kustomize overrides by exact match; a typo'd transformer fails open, not loud.**
>
> **(d) Touch the egress trap (carefully).** Add a default-deny **egress** policy to dev (`policyTypes: [Egress]`, no egress rules). Predict *before*: Lighthouse can't resolve DNS or reach connectors → it looks broken. Then add an egress allow for DNS (UDP/TCP 53 to kube-system) and watch DNS return. The lesson, unprompted: **egress lockdown is a different, sharper knife — DNS first, then named destinations — which is why the card stayed on ingress.**

Investigate, don't look it up: open the door (no policy), shut it (default-deny), prove the shut door times out, enumerate the callers you forgot (Traefik, Postgres), and feel the egress knife. The hang is the policy working; the 502 is a caller you didn't allow; the unchanged tag is a transformer that didn't match.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Namespaces (scope-for-names; not-all-namespaced; `<svc>.<ns>.svc.cluster.local` DNS; `kubernetes.io/metadata.name` auto-label) | kubernetes.io | High (1.0) | Official | 2026-06-15 | Y (quotes confirmed verbatim) |
| 2 | Declarative Management with Kustomize (kubectl `-k` since 1.14; bases/overlays; transformers; generator hash-suffix → rolling update) | kubernetes.io | High (1.0) | Official | 2026-06-15 | Y (quotes confirmed verbatim) |
| 3 | Network Policies (allow-all-by-default; "no effect" without an implementing controller; default-deny once selected; namespaceSelector) | kubernetes.io | High (1.0) | Official | 2026-06-15 | Y (quotes confirmed verbatim; default-deny-all *example* truncated on fetch — §Gaps) |
| 4 | k3s networking — embedded netpol controller (kube-router), `--disable-network-policy` | docs.k3s.io | High (1.0) | Official | 2026-06-15 | Y (quote confirmed verbatim) |
| — | Lighthouse facts (image `ghcr.io/letpeoplework/lighthouse`; port 80; `/app/data`; `Database__Provider`/`__ConnectionString` env; `/api/updateNotificationHub`; embedded SPA) | this repo / prior stories 02–07 | Primary | First-party | 2026-06-15 | Carried from stories 02/03/07 notes — re-verify the current release tag with `gh release view` |

Primary sources: **4** official docs + the carried-forward Lighthouse facts. The load-bearing, non-obvious one is **[4]**: on a *bare* flannel cluster the NetworkPolicy YAML would be a silent no-op, but **k3s's embedded kube-router controller is on by default**, so the policies in §5 genuinely enforce — which is the whole reason step 5 is a real exercise and not theater.

## Knowledge Gaps

- **The verbatim default-deny-all-ingress *example* YAML on [3] was past the fetch truncation.** The *behavior* (an ingress policy with `podSelector: {}` and no `ingress:` rules denies all inbound) is quoted and well-documented; the §5 `default-deny-ingress` manifest follows that documented shape. Eyeball [3]'s "Default policies" section if you want the canonical example verbatim. Confidence High.
- **The egress-breaks-DNS warning is general, well-known behavior, not a verbatim quote from the fetched excerpt.** A default-deny egress blocks the pod's lookups to CoreDNS (UDP/TCP 53) — documented across the k8s NetworkPolicy recipes and CoreDNS docs. Treat §2's egress trap and spike (d) as carry-this-knowledge, and confirm against the "Default deny all egress traffic" recipe if you pursue it. Confidence High.
- **The Traefik/ingress-controller namespace is assumed `kube-system` (stock k3s).** Verify with `kubectl get pods -A | grep traefik`; if your install runs Traefik elsewhere, fix the `namespaceSelector` match in §5.5 or the allow rule won't match and prod will 502. Confidence Medium (install-dependent).
- **The exact `patches:` (vs the older `patchesStrategicMerge:`) field name and the `images:`/`replicas:` schema can drift across kustomize versions bundled in kubectl.** The shapes in §5 are current; if `kubectl kustomize` complains, `kubectl version` + the kustomize reference resolves it. Confidence High.
- **Postgres-per-namespace with `emptyDir` is scratch-only.** It throws away data on pod restart — fine for "watch two envs run," wrong for anything you keep. The real PVC story is 02; the real per-env secret/password story is 13 (SealedSecrets). Don't carry the inline `devpassword` anywhere real.
- **This whole tree is deliberately NOT the chart.** It's the rehearsal for story 09 (Helm). Resist hardening it; its job is to make you feel Kustomize's ceiling, not to be kept.

## Full Citations

[1] The Kubernetes Authors. "Namespaces". kubernetes.io. https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/. Accessed 2026-06-15. (Quoted: "namespaces provide a mechanism for isolating groups of resources within a single cluster"; "Namespaces provide a scope for names…"; "Namespace-based scoping is applicable only for namespaced objects…not for cluster-wide objects"; the `<service-name>.<namespace-name>.svc.cluster.local` DNS form + "use the fully qualified domain name (FQDN)"; "The Kubernetes control plane sets an immutable label `kubernetes.io/metadata.name` on all namespaces".)
[2] The Kubernetes Authors. "Declarative Management of Kubernetes Objects Using Kustomize". kubernetes.io. https://kubernetes.io/docs/tasks/manage-kubernetes-objects/kustomization/. Accessed 2026-06-15. (Quoted: "Since 1.14, kubectl also supports the management of Kubernetes objects using a kustomization file."; the generator "content hash suffix appended…ensures that a new ConfigMap or Secret is generated when the contents are changed". Used: `kubectl kustomize` / `kubectl apply -k`; base/overlay `resources:`; `namespace`/`namePrefix`/`commonLabels`/`images`/`replicas`/`patches`/`configMapGenerator`/`secretGenerator` transformers.)
[3] The Kubernetes Authors. "Network Policies". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/network-policies/. Accessed 2026-06-15. (Quoted: "By default, a pod is non-isolated for ingress; all inbound connections are allowed."; "A pod is isolated for ingress if there is any NetworkPolicy that both selects the pod and has 'Ingress' in its `policyTypes`"; "the only allowed connections into the pod are those from the pod's node and those allowed by the `ingress` list…"; "Network policies are implemented by the network plugin… Creating a NetworkPolicy resource without a controller that implements it will have no effect."; "namespaceSelector: This selects particular namespaces…". Default-deny-all *example* YAML truncated on fetch — §Gaps.)
[4] The k3s Authors / SUSE. "Networking Services" (embedded network policy controller). docs.k3s.io. https://docs.k3s.io/networking/networking-services. Accessed 2026-06-15. (Quoted: "K3s includes an embedded network policy controller. The underlying implementation is kube-router's netpol controller library."; the `--disable-network-policy` flag to turn it off.)
