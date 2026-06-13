> nw-research reading guide for story #5194 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 04 — Health, Reliability & Rollouts (Reading Guide)

**Date**: 2026-06-12 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 7 primary (6 official-doc pages + repo)
**Doc currency**: k8s concept pages are evergreen-per-version (current nav tracks v1.36.x); image/probe-target refs grounded in this repo's `Dockerfile`, `Program.cs`, and `API/VersionController.cs` at HEAD. Latest Lighthouse release is **v26.6.11.3** at write time; the rollout demo rolls **v26.6.7.1 → v26.6.11.3** (both real — tags rot, §Knowledge Gaps).

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the probe/resource patch to the
> Lighthouse Deployment, the Postgres resources block, the image-bump rollout, the deliberately-broken
> probe, the PodDisruptionBudget) is throwaway learning scaffolding. Keep it in a personal scratch dir,
> e.g. `~/learn-k8s/story-04/`. **Nothing from this story goes into the Lighthouse repo** — the real
> manifests first land in `chart/` at **story 09** (per planning §7 workspace map: stories 00–08 =
> scratch, "personal dir e.g. `~/learn-k8s/story-NN/` — throwaway, never committed"). You're hardening
> the *real* Lighthouse image, but the YAML you write here is rehearsal, not product.

## 1. Orientation

Story 03 ended on an accidental lesson. When you stood the stack up, Traefik briefly answered
`https://lighthouse.local` with **`no available server`** — because the Lighthouse pod **crash-looped on
boot**: it raced Postgres, `Database.Migrate()` threw until Postgres was actually accepting connections,
k8s restarted it a few times, and it stabilized once Postgres was up. That crash-loop-and-recover *was*
self-healing — but the **raw, unmanaged, lucky** version of it. k8s pulled a not-yet-ready pod out of
the Service endpoints and restarted a wedged process *by accident*, not because you told it how.

Story 04 makes self-healing **intentional and safe**. Six knobs, each fixing a specific failure you've
already half-seen:

- a **readiness probe** so a pod only joins the Service's endpoints once it's *actually serving* — no
  more `no available server` to a half-booted pod (that 503 was k8s correctly pulling a NotReady pod;
  now you make "ready" *mean something* instead of being accidental);
- a **startupProbe** so a slow boot (incl. EF migrations against Postgres) isn't killed by the liveness
  probe before it's even up;
- a **liveness probe** so a wedged-but-running process gets restarted *on purpose* (deadlock, hung
  thread) — not by luck;
- **resource requests/limits** so the scheduler can place pods sanely and a memory leak gets
  **OOMKilled** in its own container instead of taking down the whole node;
- a **rolling update** so a version bump swaps pods with zero downtime;
- a **PodDisruptionBudget** so *voluntary* disruptions (a node drain) respect availability.

"Done" feels like: from a blank prompt, add liveness + readiness (+ startup) probes and resource
requests/limits to the Lighthouse Deployment, roll from one **real** image tag to another and watch the
rollout happen, deliberately **break a probe** and watch k8s restart the pod, and write a PDB — and
explain, unaided, what each one does and **what fails without it**.

This story reuses story 02's backing app: assume the `lighthouse` Deployment + Service **and** the
`lighthouse-postgres` Deployment + Service (with its PVC) already exist. "**All** Deployments" for
requests/limits therefore means **both** — the Lighthouse pod *and* the Postgres pod. The **new** work
here is only the probe/resource patches, the rollout demo, and the PDB. Don't re-teach Deployments or
PVCs.

## 2. Concepts

### Liveness, readiness, and startup probes (restart vs de-endpoint vs gate)

**What it is.** Three independent health checks the kubelet runs against a container, each answering a
*different* question and each with a *different* consequence on failure.

**Liveness — "is it alive? if not, restart it."**

> "Liveness probes determine when to restart a container. For example, liveness probes could catch a deadlock, where an application is running, but unable to make progress. Restarting a container in such a state can help to make the application more available despite bugs." — kubernetes.io [1]
> "If a container fails its liveness probe more times than the configured tolerance, the kubelet restarts that container." — kubernetes.io [1]

**Readiness — "is it ready for traffic? if not, pull it from the Service — but do NOT restart it."** This
is the one that fixes story 03's `no available server`:

> "Readiness probes determine when a container is ready to accept traffic. This is useful when waiting for an application to perform time-consuming initial tasks, such as establishing network connections, loading files, and warming caches." — kubernetes.io [1]
> "If the readiness probe returns a failed state, the EndpointSlice controller removes the Pod's IP address from the EndpointSlices of all Services that match the Pod." — kubernetes.io [1]

The crucial contrast: **liveness failure → restart; readiness failure → de-endpoint, no restart.** A pod
that's booting (or briefly overloaded) should be *pulled from traffic*, not *killed*.

**Startup — "give a slow starter room; hold the other two off until it's up."** This is the one that
keeps an aggressive liveness probe from murdering the app mid-migration:

> "Startup probes verify whether the application within a container is started. If a startup probe is configured, Kubernetes does not execute liveness or readiness probes until the startup probe succeeds, allowing the application time to finish its initialization." — kubernetes.io [1]
> "The startup probe protects slow-starting containers." — kubernetes.io [3]

**Probe mechanisms (handlers).** Each probe uses one of four mechanisms — `exec` (run a command, exit 0 =
pass), `grpc`, `httpGet` (2xx/3xx = pass), `tcpSocket` (connection opens = pass) [3].

**Timing knobs.** `initialDelaySeconds` gates the first check:

> "Number of seconds after the container has started before startup, liveness or readiness probes are initiated. If a startup probe is defined, liveness and readiness probe delays do not begin until the startup probe has succeeded." — kubernetes.io [1]

The other knobs (described by behaviour — verbatim text truncated on fetch, §Knowledge Gaps):
`periodSeconds` = how often the check runs; `failureThreshold` = how many consecutive failures before the
probe is judged failed (for a startup probe, `failureThreshold × periodSeconds` is the total budget the
app gets to start); `timeoutSeconds` = per-check timeout; `successThreshold` = consecutive passes needed
to flip back to healthy (1 for liveness/startup).

- Read: Pod lifecycle — Container probes — https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#container-probes [2]
- Read: Probes (mechanisms + timing fields) — https://kubernetes.io/docs/concepts/workloads/pods/probes/ [1]
- Read: Configure Liveness, Readiness and Startup Probes — https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/ [3]

You should be able to answer:
- Liveness vs readiness vs startup — what does **each** do *on failure* (restart / remove-from-endpoints / gate-the-others)? Which one fixes story 03's `no available server`, and which keeps the liveness probe from killing the app during EF migrations?
- What are the four probe mechanisms, and which would you pick for "Kestrel accepts connections" vs "the MVC pipeline answers"?
- A startup probe with `failureThreshold: 30`, `periodSeconds: 5` — how long does the app get to boot before the liveness probe takes over?

### Resource requests & limits (scheduler-time vs runtime; OOMKilled)

**What it is.** Two numbers per resource per container. The **request** is what the scheduler reserves to
place the pod; the **limit** is the runtime ceiling the kernel enforces.

**Requests are scheduler input:**

> "When you specify the resource _request_ for containers in a Pod, the kube-scheduler uses this information to decide which node to place the Pod on." — kubernetes.io [4]

**Limits are runtime ceilings — CPU is throttled, memory over-limit gets you OOMKilled:**

> "`cpu` limits are enforced by CPU throttling. When a container approaches its `cpu` limit, the kernel will restrict access to the CPU corresponding to the container's limit. ... Containers may not use more CPU than is specified in their `cpu` limit." — kubernetes.io [4]
> "`memory` limits are enforced by the kernel with out of memory (OOM) kills. When a container uses more than its `memory` limit, the kernel may terminate it. ... A container may use more memory than its `memory` limit, but if it does, it may get killed." — kubernetes.io [4]

The asymmetry is the lesson: **over-CPU is survivable (you just get throttled, slow); over-memory is
fatal (the container is terminated — `OOMKilled`, exit 137).**

**Units.** CPU: `1` = one core, `0.1` = `100m` ("one hundred millicpu") [4]. Memory: bytes, with suffixes
`Ei/Pi/Ti/Gi/Mi/Ki` (power-of-two) or `E/P/T/G/M/k` [4].

**QoS classes (briefly).** From the request/limit combination k8s derives a Quality-of-Service class —
**Guaranteed** (request == limit on every resource), **Burstable** (request < limit), **BestEffort**
(neither set) — which decides who gets OOM-killed first under node memory pressure. (Names not on the
fetched excerpt — §Knowledge Gaps; treat as orientation, not a quote.)

- Read: Resource Management for Pods and Containers — https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/ [4]

You should be able to answer:
- Which number does the **scheduler** read, and which does the **kernel** enforce at runtime?
- What happens to a container that exceeds its CPU limit vs its memory limit — and what exit code/signal tells you "memory limit too low"?
- Why set a request *and* a limit on every container — what goes wrong on a node if one Deployment has neither?

### Rolling update strategy (changing the image triggers a rollout)

**What it is.** A Deployment's `.spec.strategy` decides *how* pods are replaced when the template changes.

**What triggers a rollout — only a template change:**

> "A Deployment's rollout is triggered if and only if the Deployment's Pod template (that is, `.spec.template`) is changed, for example if the labels or container images of the template are updated. Other updates, such as scaling the Deployment, do not trigger a rollout." — kubernetes.io [5]

So `kubectl set image …` (which edits the template's image) **is** a rollout; `kubectl scale …` is not.

**The two strategies, and the default:**

> ".spec.strategy.type can be \"Recreate\" or \"RollingUpdate\". \"RollingUpdate\" is the default value." — kubernetes.io [5]
> "All existing Pods are killed before new ones are created." — kubernetes.io [5] (the `Recreate` strategy)

**The two RollingUpdate knobs (both default 25%):**

> "maxUnavailable: The maximum number of Pods that can be unavailable during the update process. It defaults to 25%." — kubernetes.io [5]
> "maxSurge: The maximum number of Pods that can be created over the desired number of Pods. It defaults to 25%." — kubernetes.io [5]

**At `replicas: 1`** the 25% defaults *round* so a rolling update briefly runs a **second (surge) pod**,
waits for it to become Ready, then terminates the old one — momentary 2-pod overlap, then back to 1. And
that overlap is the catch with a shared database: the **new** pod boots and runs `Database.Migrate()`
against the *same* Postgres while the **old** pod is still Ready and serving against the *old* schema. A
re-run of an already-applied migration is idempotent and harmless — but a **destructive** new migration
(rename/drop/retype a column) mutates the schema out from under the still-running old pod, which then
throws until it's terminated. So even at `replicas: 1`, rolling is only safe if migrations are
**backward-compatible**.

**Project guideline (the chosen approach): migrations are expand-only / non-destructive.** Follow the
expand/contract (parallel-change) pattern so a rolling update is always safe with a shared DB: per release,
**additive only** — add nullable columns, add tables/indexes; never rename/drop/retype a column in the same
release whose code depends on the new shape. A destructive cleanup is its **own later release**, shipped
once no pod runs the old code. This is preferred over `Recreate` because it keeps zero downtime *and* is
safe. (`Recreate` — kill the old pod before starting the new, accepting a brief outage — remains the
fallback only for a singleton that genuinely cannot tolerate two live instances, e.g. an exclusive lock /
non-shared embedded store; not needed here.) At `replicas > 1`, also move `Database.Migrate()` out of every
pod into a one-shot migration `Job`/initContainer (Band B / story 07) so booting pods don't race to apply.
Track and reverse a rollout with `kubectl rollout status` / `kubectl rollout undo`.

- Read: Deployments — Strategy / Rolling Update — https://kubernetes.io/docs/concepts/workloads/controllers/deployment/#strategy [5]

You should be able to answer:
- Which Deployment edits trigger a rollout and which don't — why is `kubectl set image` a rollout but `kubectl scale` not?
- `maxSurge` vs `maxUnavailable` — what does each cap, and what are their defaults? At `replicas: 1`, what do you actually *see* happen?
- When would you choose `Recreate` over `RollingUpdate`, and what do you give up?

### PodDisruptionBudget (voluntary vs involuntary disruption)

**What it is.** A budget that limits how many pods of a replicated app may be *voluntarily* taken down at
once — so a node drain can't evict your whole app.

**The distinction that makes a PDB make sense — voluntary vs involuntary:**

> "We call these unavoidable cases _involuntary disruptions_ to an application. Examples are: a hardware failure of the physical machine backing the node ... a kernel panic ... eviction of a pod due to the node being out-of-resources." — kubernetes.io [6]
> "We call other cases _voluntary disruptions_. ... Typical application owner actions include: deleting the deployment or other controller that manages the pod ... directly deleting a pod. ... Cluster administrator actions include: Draining a node for repair or upgrade. Draining a node from a cluster to scale the cluster down. Removing a pod from a node to permit something else to fit on that node." — kubernetes.io [6]

A PDB only governs the **voluntary** kind (the eviction API honours it); a kernel panic ignores your PDB.

> "A PDB limits the number of Pods of a replicated application that are down simultaneously from voluntary disruptions." — kubernetes.io [6]

**The two ways to express the budget (pick exactly one):**

> ".spec.minAvailable which is a description of the number of pods from that set that must still be available after the eviction, even in the absence of the evicted pod. `minAvailable` can be either an absolute number or a percentage." — kubernetes.io [7]
> ".spec.maxUnavailable ... which is a description of the number of pods from that set that can be unavailable after the eviction. It can be either an absolute number or a percentage." — kubernetes.io [7]
> "You can specify only one of `maxUnavailable` and `minAvailable` in a single `PodDisruptionBudget`." — kubernetes.io [7]

**The honest caveat (planning Band B):** a PDB with **`minAvailable: 1` on a single-replica Deployment**
means a *voluntary* disruption (e.g. `kubectl drain`) **cannot evict the only pod** without `--force` —
draining the node would block on it. So the PDB's real payoff arrives at **`replicas > 1`**, which is
**Band B scaling (story 07)**. Learn it now, demo it now, but don't oversell it: at `replicas: 1` it's a
correctly-configured handbrake that mostly just *blocks* drains rather than gracefully sequencing them.

- Read: Disruptions (voluntary vs involuntary) — https://kubernetes.io/docs/concepts/workloads/pods/disruptions/ [6]
- Read: Specifying a Disruption Budget — https://kubernetes.io/docs/tasks/run-application/configure-pdb/ [7]

You should be able to answer:
- Voluntary vs involuntary disruption — give two examples of each, and say which kind a PDB can actually constrain.
- `minAvailable` vs `maxUnavailable` — can you set both? What does `minAvailable: 1` mean for a `kubectl drain` at `replicas: 1` vs `replicas: 3`?
- Why is the PDB's real value a **Band B** (story 07) thing, even though you write it here?

## 3. Repo-grounded facts (probe the REAL Lighthouse — from the source, not a guess)

All cited from this repo at HEAD — verify before you copy. **This grounds *what you point a probe at*,
which is itself a design choice because Lighthouse ships no health endpoint.**

- **Container ports.** `Dockerfile:4` `EXPOSE 80 443`; Kestrel listens `http://+:80` (`Dockerfile:66`)
  and `https://+:443` (`Dockerfile:67`). Probes target the **HTTP port 80** (TLS terminates at the
  Ingress, story 03 — the pod speaks plain http internally).
- **Embedded single workload — ONE Deployment + ONE Service serves SPA + API.** The SPA is served by the
  same backend process via `app.UseSpa(...)` (`Program.cs:229-233`). This is the Deployment you add
  probes/resources to. The backing app is story 02's, so **"all Deployments" = both** the `lighthouse`
  Deployment **and** the `lighthouse-postgres` Deployment.
- **CRUCIAL — Lighthouse has NO dedicated health endpoint.** There is no `/health` / `/healthz`;
  `Program.cs` wires **no** `AddHealthChecks()` / `MapHealthChecks()` (grep'd at HEAD — zero matches in
  the whole backend). So the probe **target is a design decision**. The menu, with trade-offs:
  - **`tcpSocket` on `:80`** — simplest **liveness** target; proves Kestrel accepts a TCP connection.
    Doesn't prove app logic works, but a wedged process that stops accepting connections still fails it.
  - **`httpGet /`** — the SPA index (anonymous, served by `UseSpa`, `Program.cs:229`); returns 200 once
    Kestrel + static files are up. Fine for **liveness**; it's just static-file serving, so it doesn't
    exercise the API.
  - **`httpGet /api/v1/version/current`** — the **best readiness-ish** target: it runs the MVC pipeline,
    not just static files. `VersionController` is class-level `[AllowAnonymous]`, route
    `[Route("api/v1/[controller]")]` + `[HttpGet("current")]` → `/api/v1/version/current`
    (`API/VersionController.cs:9,12,22`). **Caveat (be honest):** it returns **404 if the version string
    is empty** (`VersionController.cs:24,29-30`) — on a real *pinned release* image the version is set so
    it returns 200, but a dev/unstamped build could 404. Confirm with `curl` before trusting it as a
    probe target (§4).
  - **DO NOT** point a probe at an **auth-gated** `/api/*` route. Unauthenticated `/api` requests get a
    hard **401** (`Program.cs:636`: `OnRedirectToLogin` returns `401` for paths under `/api`; e.g.
    `AuthorizationController` is `[Authorize]`). A probe there **fails permanently** → the pod never
    becomes Ready (readiness) or restart-loops forever (liveness). This is the trap. The reason
    `/api/v1/version/current` is safe is precisely that it's `[AllowAnonymous]`.
- **Two real image tags for the rollout.** Image `ghcr.io/letpeoplework/lighthouse`. Roll from
  **`v26.6.7.1`** → **`v26.6.11.3`** (both real releases; `v26.6.11.3` is latest as of 2026-06-12).
  **Never `:latest`.** Tags rot — discover current ones with
  `gh release list --repo LetPeopleWork/Lighthouse` (or `gh release view … -q .tagName`).

> **Forward hook — the production-correct fix is OUT OF SCOPE here (planning §D3).** The *right* answer
> long-term is to add a real health endpoint in the C#: `AddHealthChecks().AddDbContextCheck<…>()`
> (a Postgres-connectivity **readiness** check) + `MapHealthChecks("/healthz")`. But that is **product
> C# code → full nWave** (DISCUSS→…→DELIVER + the RBAC/Clients/Website checklist), not this infra
> light-loop. So for story 04 you probe the **real image as-is** (tcp / `/` / `/api/v1/version/current`)
> and note the proper `/health` endpoint as a deferred enhancement — the same honest deferral story 03
> made for sticky-sessions (→ story 07) and ACME (→ story 12).

## 4. Debug reflex (carry this through the story)

Carry forward the prior rule, then add the probe/rollout failure shapes:

- **describe→Events before the container runs; logs after** (story 01). `kubectl describe pod` → Events
  for pre-run problems and **probe failures** (they show up here verbatim); `kubectl logs` once it's up.
- **Pod stuck `0/1 Running`, never Ready** → the **readiness probe** is failing.
  `kubectl describe pod` Events show `Readiness probe failed: HTTP probe failed with statuscode: 401`
  (or `404`, or `connection refused`). Punchline: a probe pointed at an **auth-gated `/api` path 401s
  forever** — the pod is healthy, your probe target is wrong (§3 trap). A `404` here is the
  empty-version edge on `/api/v1/version/current`; `connection refused` means Kestrel isn't up yet
  (your `initialDelaySeconds` is too low or there's no startup probe).
- **`CrashLoopBackOff` with climbing RESTARTS** → the **liveness probe is too aggressive** —
  `initialDelaySeconds` too low (or no startup probe), so liveness kills a still-booting app before it
  finishes (exactly story 03's migration race, but now *self-inflicted*). Fix: a **startupProbe** to gate
  liveness until boot completes. Read it: `kubectl describe pod` → `Liveness probe failed` Events + a
  rising restart count.
- **`OOMKilled` (exit 137)** → the **memory limit is too low**. `kubectl describe pod` → last state
  `Terminated`, reason `OOMKilled`. Raise `resources.limits.memory` (or fix the leak).
- **Rollout stuck / never completes** → `kubectl rollout status deploy/lighthouse` (it'll say what it's
  waiting on), `kubectl get rs` (old vs new ReplicaSet, who has the pods), and back out with
  `kubectl rollout undo deploy/lighthouse`. A surge pod that never becomes Ready usually means the
  *new* image fails its readiness probe (bad tag, bad probe target).
- **A `kubectl drain` that hangs** → a **PDB is blocking eviction**. `kubectl get pdb` (check
  `ALLOWED DISRUPTIONS`); at `replicas: 1` with `minAvailable: 1` it's **0**, so the drain waits forever
  unless you `--force`. That's the PDB doing its (single-replica-limited) job (§3 caveat).

Mnemonic: **never-Ready → describe pod for the readiness-probe line (401 = wrong target);
CrashLoop → liveness too eager, add a startupProbe; exit 137 → OOMKilled, raise the memory limit;
rollout stuck → rollout status + undo; drain hangs → get pdb.**

## 5. Hands-on — copy/paste manifests

Try each block from memory first; these are the backstop. Replace `<OLD_TAG>`/`<NEW_TAG>` with real
release tags (§3: `v26.6.7.1` → `v26.6.11.3`). Work in `~/learn-k8s/story-04/`. Assumes story 02's
`lighthouse` and `lighthouse-postgres` Deployments/Services exist.

### 5.1 Lighthouse Deployment — startup + liveness + readiness probes AND resources

Full container spec excerpt so it's copy-paste-able into story 02's `lighthouse` Deployment. The startup
probe gates the other two so a slow boot (incl. EF migrations) isn't killed; liveness is a cheap
`tcpSocket`; readiness exercises the MVC pipeline via the **anonymous** version route (§3).

```yaml
# patch the containers[] entry of the lighthouse Deployment (story 02)
spec:
  template:
    spec:
      containers:
        - name: lighthouse
          image: ghcr.io/letpeoplework/lighthouse:<OLD_TAG>   # e.g. v26.6.7.1 — pinned, never :latest
          ports:
            - containerPort: 80
          # STARTUP: give boot (Kestrel + EF migrations vs Postgres) up to 30×5s=150s before
          # liveness/readiness even begin. Disables the other two until it passes [1][3].
          startupProbe:
            httpGet:
              path: /api/v1/version/current   # anonymous (§3) — proves the app is actually serving
              port: 80
            periodSeconds: 5
            failureThreshold: 30              # 30 × 5s = 150s startup budget
          # LIVENESS: cheap "is Kestrel accepting connections?" — restart on failure. tcpSocket so a
          # transient slow request doesn't trigger a needless restart.
          livenessProbe:
            tcpSocket:
              port: 80
            periodSeconds: 10
            failureThreshold: 3               # 3 consecutive fails → restart
          # READINESS: exercises the MVC pipeline; failure de-endpoints (no restart) [1].
          readinessProbe:
            httpGet:
              path: /api/v1/version/current   # [AllowAnonymous] → 200 on a pinned release image (§3)
              port: 80
            periodSeconds: 10
            failureThreshold: 3
          resources:
            requests:                          # scheduler reserves this to place the pod [4]
              cpu: "100m"
              memory: "256Mi"
            limits:                            # runtime ceiling: CPU throttled, memory over → OOMKilled [4]
              cpu: "1000m"
              memory: "512Mi"
```

### 5.2 Postgres Deployment — resources too ("all Deployments")

```yaml
# patch the containers[] entry of the lighthouse-postgres Deployment (story 02)
spec:
  template:
    spec:
      containers:
        - name: postgres
          resources:
            requests:
              cpu: "100m"
              memory: "256Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
```

### 5.3 Explicit RollingUpdate strategy (with the Recreate alternative)

Add to the `lighthouse` Deployment's `spec:` (a sibling of `replicas`/`selector`/`template`):

```yaml
  strategy:
    type: RollingUpdate          # the default [5]; named here for clarity
    rollingUpdate:
      maxSurge: 25%              # at replicas:1 this rounds to a 2nd (surge) pod during the roll [5]
      maxUnavailable: 25%        # rounds to 0 at replicas:1 → the old pod stays until the new is Ready
  # ALTERNATIVE — use Recreate when a singleton CANNOT tolerate two live instances at once
  # (exclusive lock / non-shared embedded store). It kills the old pod BEFORE starting the new,
  # accepting a brief outage. Not needed here: Postgres is a separate Deployment (story 02), so two
  # transient Lighthouse pods sharing one Postgres is fine.
  #   strategy:
  #     type: Recreate
```

### 5.4 The rolling-update demo — bump the image, watch it roll

```bash
kubectl apply -f lighthouse.yaml -f postgres.yaml     # apply 5.1–5.3 first
# watch the ReplicaSets and pods while you roll:
kubectl get rs -w &                                   # see a NEW rs scale up as the OLD scales down
kubectl set image deploy/lighthouse \
  lighthouse=ghcr.io/letpeoplework/lighthouse:<NEW_TAG>   # e.g. v26.6.11.3 — edits .spec.template → ROLLOUT [5]
kubectl rollout status deploy/lighthouse              # blocks until the new pod is Ready (or fails)
kubectl get pods -w                                   # surge pod appears, becomes Ready, old terminates
kill %1
# back it out (rolls to the PREVIOUS template):
kubectl rollout undo deploy/lighthouse
kubectl rollout status deploy/lighthouse
```

### 5.5 Break a liveness probe — watch k8s restart the pod

Point liveness at a path that will never answer 2xx, apply, and watch RESTARTS climb / `CrashLoopBackOff`.

```bash
# Temporarily break liveness: a path the app doesn't serve as 2xx (httpGet, so 404 = probe failure).
kubectl patch deploy/lighthouse --type=json -p='[
  {"op":"replace","path":"/spec/template/spec/containers/0/livenessProbe",
   "value":{"httpGet":{"path":"/definitely-not-here","port":80},"periodSeconds":5,"failureThreshold":2}}
]'
kubectl get pods -w        # RESTARTS climbs; STATUS → CrashLoopBackOff as liveness keeps failing
kubectl describe pod -l app=lighthouse | sed -n '/Events/,$p'   # read "Liveness probe failed" Events
# REVERT to the good liveness probe (re-apply 5.1) and confirm it stabilizes:
kubectl apply -f lighthouse.yaml
kubectl rollout status deploy/lighthouse
```

### 5.6 PodDisruptionBudget for the API (with the replicas:1 caveat called out)

```yaml
# pdb.yaml — limits VOLUNTARY disruptions (e.g. kubectl drain) [6][7].
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: lighthouse
spec:
  minAvailable: 1                # at least 1 lighthouse pod must stay up through a voluntary disruption [7]
  selector:
    matchLabels:
      app: lighthouse            # MUST match the Deployment's pod labels (story 02)
# CAVEAT (§3): at replicas:1, minAvailable:1 means a drain CANNOT evict the only pod without --force —
# ALLOWED DISRUPTIONS = 0. The real payoff is at replicas > 1 (Band B / story 07). Concept now, payoff later.
```

```bash
kubectl apply -f pdb.yaml
kubectl get pdb lighthouse        # read ALLOWED DISRUPTIONS — 0 at replicas:1 (that's the caveat, live)
# Demo the budget holding an eviction (single-node k3s — cordon+delete shows the block without nuking the node):
kubectl drain $(kubectl get node -o name | head -1) --ignore-daemonsets --delete-emptydir-data --dry-run=client
# A real `kubectl drain <node>` (no --dry-run) would HANG on the lighthouse pod because the PDB forbids
# evicting the last replica. Ctrl-C it, or see 7(c) for the replicas:2 version where the PDB shines.
```

### 5.7 Teardown / revert

```bash
kubectl delete -f pdb.yaml
# revert probes/resources/strategy by re-applying story 02's plain manifests (or strip the added blocks):
kubectl apply -f lighthouse.yaml -f postgres.yaml
kubectl uncordon $(kubectl get node -o name | head -1)   # if you cordoned/drained in 5.6
kubectl rollout undo deploy/lighthouse                   # if you want to drop back to <OLD_TAG>
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *make k8s self-heal and roll out safely, and explain probes, resource requests/limits,
rolling-update strategy, and disruption budgets.* Unaided, you should be able to:

- [ ] Explain **liveness vs readiness vs startup** by what each does *on failure* — **restart** / **remove from Service endpoints (no restart)** / **gate the other two until boot finishes**.
- [ ] Choose a **probe target for the real image** and justify it (tcp:80 or `/` for liveness; `/api/v1/version/current` for readiness because it's `[AllowAnonymous]` and exercises the MVC pipeline) — and explain **why NOT** an auth-gated `/api` path (permanent 401).
- [ ] Set **requests vs limits** and explain **scheduler-time (request) vs runtime (limit)**, what CPU-over-limit does (throttle) vs memory-over-limit (`OOMKilled`, exit 137).
- [ ] **Trigger and read a rolling update**: which edit triggers it (`set image` vs `scale`), what `maxSurge`/`maxUnavailable` do at `replicas:1`, and back it out with `kubectl rollout undo`.
- [ ] **Break a liveness probe** and explain the restart loop — and the **startupProbe** as the fix when the cause is an aggressive `initialDelaySeconds` killing a still-booting app.
- [ ] **Write a PDB** and explain **voluntary vs involuntary** disruption, `minAvailable` vs `maxUnavailable`, and the **`replicas:1` caveat** (drain blocks; payoff is at `replicas>1` / story 07).
- [ ] **Connect it back to story 03's crash-loop**: which probe makes self-heal *intentional* (liveness for the restart, readiness for the de-endpoint, startup so boot isn't killed) instead of accidental.

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick a throwaway experiment that *tests the lesson at its edges*. Form a hypothesis **before** you run it:

> **(a) Starve memory — predict the signal.** Set `resources.limits.memory: 32Mi` on the Lighthouse
> container and re-apply. Predict *before* applying: does it fail to schedule, get throttled, or get
> killed — and with what `kubectl describe pod` reason and exit code? Watch `kubectl get pods -w` and
> `kubectl describe pod`. (Expected: `OOMKilled`, exit 137 — the kernel terminates it for exceeding the
> memory limit [4]. CPU would *throttle*; memory *kills*.)
>
> **(b) Liveness with no runway — the startup lesson in the negative.** Set `livenessProbe.initialDelaySeconds: 0`
> with a low `failureThreshold` and **remove the startupProbe**. Predict whether liveness kills the app
> mid-boot before Kestrel + EF migrations finish. Watch RESTARTS climb / `CrashLoopBackOff`, then add the
> startupProbe back and watch it stabilize — *that* is what the startup probe buys you.
>
> **(c) Scale to 2, then drain — the PDB doing its real job.** `kubectl scale deploy/lighthouse --replicas=2`,
> confirm `kubectl get pdb` now shows `ALLOWED DISRUPTIONS: 1`, then `kubectl drain <node>` (single-node:
> cordon + `kubectl delete pod` the two in turn) and observe the **ordering**: the PDB lets one go but
> holds the second until the first is replaced and Ready. Contrast with 5.6's `replicas:1` block — *this*
> is where `minAvailable` earns its keep (Band B / story 07).

Investigate, don't look it up: starve it and read the exit code; remove the runway and watch the restart
loop; scale up and watch the PDB sequence the drain. The exit-137 is the memory lesson; the restart loop
is the startup-probe lesson; the sequenced drain is the PDB lesson.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Probes (liveness/readiness/startup definitions, mechanisms, initialDelaySeconds) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed; periodSeconds/failureThreshold/timeoutSeconds/successThreshold text truncated — described by behaviour, §Gaps) |
| 2 | Pod lifecycle — Container probes (entry point / lifecycle context) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (probe definitions corroborated; restart/de-endpoint behaviour confirmed) |
| 3 | Configure Liveness, Readiness and Startup Probes (task page, "protects slow-starting containers") | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (startup-protection quote confirmed; full timing-field table truncated) |
| 4 | Resource Management for Pods and Containers (requests=scheduler, limits=runtime, CPU throttle / memory OOM, units) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed; QoS class names not on excerpt — §Gaps) |
| 5 | Deployments — Strategy / RollingUpdate (template-change triggers rollout, Recreate/RollingUpdate, maxSurge/maxUnavailable 25%) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (template-trigger quote confirmed in body; strategy/maxSurge/maxUnavailable confirmed via the #strategy section fetch) |
| 6 | Disruptions (voluntary vs involuntary, PDB limits voluntary disruptions) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| 7 | Specifying a Disruption Budget (minAvailable / maxUnavailable, only-one rule) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| — | Lighthouse repo (Dockerfile, Program.cs, VersionController.cs) | this repo | Primary | First-party source | 2026-06-12 | Y (line-cited at HEAD; no-health-endpoint = grep confirmed zero matches) |

Primary sources: **7** (6 official kubernetes.io doc pages [1–7 across the probes/lifecycle/resources/
deployment/disruptions/pdb set] plus the line-cited Lighthouse repo). All doc sources are first-party
Kubernetes project documentation (High tier). Repo facts are line-cited from this repository's own source
files — the strongest authority for what the image serves and where a probe can safely point.

## Knowledge Gaps

- **Probe timing-field verbatim text truncated.** [1]/[3] confirmed `initialDelaySeconds` verbatim and
  the liveness/readiness/startup definitions verbatim, but the fetched bodies truncated *before* the full
  `periodSeconds` / `failureThreshold` / `timeoutSeconds` / `successThreshold` table. Those are described
  by their well-known behaviour (and the `failureThreshold × periodSeconds` startup-budget arithmetic);
  confidence High, but eyeball the "Configure Probes" table at [3] if you want the literal one-liners.
- **The `/api/v1/version/current` 404 edge is real but conditional.** `VersionController.cs:24,29-30`
  returns 404 when the version string is empty. On a **pinned release image** the version is stamped so it
  returns 200 — but a dev/`:dev-latest`/unstamped build could 404 and break a readiness probe. **Verify
  with `curl` against your actual image** before trusting it as a probe target; fall back to `httpGet /`
  or `tcpSocket :80` if it 404s.
- **QoS class names not quoted.** [4]'s fetched excerpt confirmed requests/limits/units verbatim but did
  not include the Guaranteed/Burstable/BestEffort names — treated as orientation, not a quote. Verify on
  the "Configure Quality of Service for Pods" task page if you need them verbatim.
- **`maxSurge`/`maxUnavailable` rounding at `replicas:1` is described, not quoted.** [5] confirms the 25%
  defaults verbatim; the *rounding behaviour* (25% of 1 → surge 1 / unavailable 0) is the documented
  algorithm applied, not a literal sentence. Confirm by watching `kubectl get rs -w` during the 5.4 roll.
- **Image tags rot.** `v26.6.7.1` → `v26.6.11.3` are real and current at write time (2026-06-12); pin
  whatever `gh release list --repo LetPeopleWork/Lighthouse` shows when you actually run the demo.
- **No real `/health` endpoint exists.** Confirmed by grep (zero `Add/MapHealthChecks` in the backend).
  Adding one is the production-correct fix but is **full-nWave product code (planning §D3)** — out of
  scope here; this story probes the image as-is.

## Full Citations

[1] The Kubernetes Authors. "Probes". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/pods/probes/. Accessed 2026-06-12.
[2] The Kubernetes Authors. "Pod Lifecycle" (Container probes). kubernetes.io. https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#container-probes. Accessed 2026-06-12.
[3] The Kubernetes Authors. "Configure Liveness, Readiness and Startup Probes". kubernetes.io. https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/. Accessed 2026-06-12.
[4] The Kubernetes Authors. "Resource Management for Pods and Containers". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/. Accessed 2026-06-12.
[5] The Kubernetes Authors. "Deployments". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/controllers/deployment/. Accessed 2026-06-12.
[6] The Kubernetes Authors. "Disruptions". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/pods/disruptions/. Accessed 2026-06-12.
[7] The Kubernetes Authors. "Specifying a Disruption Budget for your Application". kubernetes.io. https://kubernetes.io/docs/tasks/run-application/configure-pdb/. Accessed 2026-06-12.
[8] LetPeopleWork. Lighthouse repository — `Dockerfile` (lines 4, 66-67), `Lighthouse.Backend/Lighthouse.Backend/Program.cs` (lines 229-233, 636; no `AddHealthChecks`/`MapHealthChecks` anywhere), `Lighthouse.Backend/Lighthouse.Backend/API/VersionController.cs` (lines 9, 12, 22, 24, 29-30). Accessed 2026-06-12.
