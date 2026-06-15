> nw-research reading guide for story #5197 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 07 — Scaling (SignalR multi-replica + HPA) (Reading Guide)

**Date**: 2026-06-14 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 4 primary (MS SignalR scale/host doc + 2 kubernetes.io pages + this repo)
**Doc currency**: the MS SignalR "production hosting and scaling" guidance is current (page dated 2026-05, covers through ASP.NET Core 11); the HPA algorithm + `autoscaling/v2` API are stable/evergreen-per-version (current nav tracks v1.36.x). The Lighthouse SignalR facts — hub path, `AddSignalR()` with **no backplane**, in-memory group fan-out, the in-memory status cache, and the singleton background updaters — are line-cited from this repo at HEAD. **Read §1 and §3 before §5 — they reframe the story from "add session affinity" to "Lighthouse is a stateful singleton today, and *three* things break when you run two of it."**

> **🗂 Workspace: SPLIT — and this one is a D3 full-nWave exception (planning §D3).** Two different
> kinds of work hide in this story, and they live in different places:
> - **The scaling *experiment* (scale to 3 → watch it break → `sessionAffinity: ClientIP` band-aid → HPA → load-test)** is **throwaway scratch**, e.g. `~/learn-k8s/story-07/`. Raw manifests, never committed. This is the light loop / `nw-spike`.
> - **The Redis backplane (and anything that makes Lighthouse actually multi-replica-safe)** is **real Lighthouse C# product code** in *this* repo → **full `DISCUSS→…→DELIVER` + the CLAUDE.md RBAC / Lighthouse-Clients / Website checklist** (planning §D3, §300). Don't sneak the backplane in as a scratch experiment — it's a product capability with tests, mutation, docs.
>
> The honest split: **`sessionAffinity` is a spike you do today; the backplane is a feature you design when you reach it.** And §1 argues the *real* fix is bigger than either.

## 1. Orientation

So far the cluster runs **one** Lighthouse pod (story 02) — `replicas: 1` — plus Postgres (02), the MCP server (06), Ingress+TLS (03), probes (04), and edge auth (05). Story 07's ADO card says: scale the API to 3 replicas, watch SignalR break, fix it with `sessionAffinity: ClientIP`, investigate the Redis backplane (spike), add an HPA on CPU, load-test. That's the *surface*.

The reframe that makes this story click — and it's grounded in the repo (§3): **Lighthouse is, today, a stateful singleton by design.** It was written to be exactly one process. Scaling the Deployment to 3 replicas doesn't just "drop some SignalR connections" — it breaks in **three independent ways**, only one of which `sessionAffinity` touches, and only one *more* of which the Redis backplane touches:

```
   replicas: 1  ──────────────▶  replicas: 3   (what actually breaks)
   ───────────────────────────────────────────────────────────────────────────
   (A) SignalR connection layer   negotiate→long-poll/SSE needs the SAME pod each
                                   request → without affinity the handshake lands
                                   on pod-2, the poll on pod-3 → connection errors.
                                   FIX: sessionAffinity: ClientIP (the band-aid).

   (B) SignalR message fan-out     groups live in EACH pod's memory. A background
                                   update computed on pod-1 does
                                   Clients.Group(key).SendAsync(...) → reaches only
                                   the clients connected to pod-1. Clients on pod-2/3
                                   never get the notification. Affinity does NOT fix
                                   this. FIX: Redis backplane (the real C# slice).

   (C) Background work runs N×     TeamUpdater, PortfolioUpdater, UpdateQueueService
       + in-memory status cache    are IHostedServices — they run in EVERY replica.
                                   3 replicas = 3 copies hammering Jira/ADO/Linear,
                                   racing on the same Postgres rows, computing every
                                   forecast 3×. And the status cache is an in-memory
                                   ConcurrentDictionary, so GetUpdateStatus answers
                                   differently depending on which pod you hit.
                                   NEITHER affinity NOR the backplane fixes this.
```

So the deliverable "what you'll learn (outcome)" — *you can scale horizontally, diagnose stateful-connection breakage, and configure CPU-based autoscaling* — is real, but the **most valuable thing** you'll learn is honest and uncomfortable: **Lighthouse is not horizontally scalable yet, and the story teaches you to *see* why.** (A) and the HPA are mechanical k8s skills. (B) is the genuine product slice (backplane). (C) is the finding that says "even with the backplane, 3 replicas is wrong until the background updaters become a single leader" — that's a *future* product decision, not story 07's job, but you must be able to name it.

"Done" feels like: you scaled to 3, **reproduced** the breakage and can say *which* of (A)/(B)/(C) each symptom is; you applied `sessionAffinity: ClientIP` and watched the *connection* errors stop (while understanding it did nothing for fan-out); you can **explain** the Redis backplane and have a throwaway sketch of the C# change; you configured an **HPA on CPU** (and know it needs resource *requests*, set in story 04-ish), load-tested, and watched replicas scale; and — unaided — you can argue **why `sessionAffinity` is a band-aid, why the backplane is the real messaging fix, and why neither makes the background-updater layer safe.**

This builds on story 02 (the single app workload + Postgres), story 04 (resource requests/limits + probes — the HPA *needs* requests), story 05 (the hub is `[Authorize]`), and story 06 (a second workload; here Redis would be a *third* backing store like Postgres). Don't re-teach those.

## 2. Concepts

### Sticky sessions / session affinity — why SignalR needs the same pod (the (A) fix)

**What it is.** SignalR pins a logical connection to one server process. Across a farm, the load balancer must keep sending a given client back to the same backend — "sticky sessions" / "session affinity."

> "SignalR requires the same server process handle all HTTP requests for a specific connection. When SignalR runs on a server farm (multiple servers), 'sticky sessions' must be used. 'Sticky sessions' are also called *session affinity*." — Microsoft Learn [1]

**Why your Lighthouse install needs it (repo fact, §3).** The frontend builds its connection with `.withUrl(\`${baseUrl}/updateNotificationHub\`, { withCredentials: true })` and **nothing else** — no `skipNegotiation`, no transport restriction. So it **negotiates** and may settle on **long polling** or **SSE**, where each HTTP request is a *separate* request the LB could route anywhere. The MS doc lists the only three cases where you can skip affinity — and Lighthouse hits **none** of them:

> "There are three scenarios where sticky sessions aren't required for an app: Hosting on a single server in a single process … Using the Azure SignalR Service … All clients are configured to use WebSockets **only** and the client configuration enables [SkipNegotiation]." — Microsoft Learn [1]

And the kicker — affinity is required **even with the backplane**:

> "In all other scenarios (including when the Redis backplane is used), the server environment must be configured for sticky sessions." — Microsoft Learn [1]

That single sentence is the spine of the whole story: **affinity (A) and the backplane (B) are not alternatives — you need *both*.** Affinity keeps a connection's HTTP requests on one pod; the backplane lets a message raised on *any* pod reach clients on *all* pods.

**How k8s does affinity.** A Service can pin a client to a backend by source IP: set `service.spec.sessionAffinity: ClientIP` (default is `None`). It routes connections from a given client IP to the same Pod (verbatim sentence truncated on fetch — §Gaps; this is the documented, well-known behaviour). It's an **L4 / source-IP** mechanism with a timeout — note its two real-world holes you must be able to name: it breaks when many clients sit behind **one NAT/proxy IP** (they all pin to one pod → uneven load), and the **Ingress** in front (story 03, Traefik) terminates the client connection, so the IP the Service sees may be Traefik's, not the real client's — cookie-based stickiness at the *Ingress* is often the better lever. So `sessionAffinity: ClientIP` on the Service is the *textbook* answer the card asks for, and you should apply it and watch it work — but you should also be able to say where it's fragile.

- Read: SignalR production hosting & scaling (sticky sessions; scale-out; Nginx `ip_hash`) — https://learn.microsoft.com/aspnet/core/signalr/scale [1]
- Read (pointer): Service session affinity (`sessionAffinity: ClientIP`) — https://kubernetes.io/docs/reference/networking/virtual-ips/ [3]

You should be able to answer:
- Why does Lighthouse's SignalR need sticky sessions today — which of the MS doc's "three scenarios" would let you *skip* affinity, and why does the current frontend (`withUrl` with no `skipNegotiation`) not qualify?
- Restate the "(including when the Redis backplane is used)" sentence: are affinity and the backplane alternatives or both-required?
- Two ways `sessionAffinity: ClientIP` on the Service can misbehave (NAT'd clients; the Ingress hiding the real client IP) — and what you'd reach for instead at scale.

### SignalR scale-out & the Redis backplane — why a message on one pod misses clients on another (the (B) fix)

**What it is.** With no backplane, each server only knows *its own* connections, so a broadcast only reaches *its own* clients.

> "Add a server, and it gets new connections that the other servers don't know about. … When SignalR on one of the servers wants to send a message to all clients, the message only goes to the clients connected to that server." — Microsoft Learn [1]

That is **exactly** Lighthouse's (B): `UpdateQueueService` does `hubContext.Clients.Group(updateKey.ToString()).SendAsync(...)` and `Clients.Group("GlobalUpdates").SendAsync(...)` (§3). Group membership is per-process in-memory, so on 3 replicas a "team X refreshed" event computed on pod-1 reaches only the browsers stuck (by affinity) to pod-1. The Redis backplane fixes this:

> "The SignalR Redis backplane uses the publish/subscribe feature to forward messages to other servers. … When a server wants to send a message to all clients, it sends it to the backplane. The backplane knows all connected clients and which servers they're on. It sends the message to all clients via their respective servers." — Microsoft Learn [1]

> "The Redis backplane is the recommended scale-out approach for apps hosted on your own infrastructure." — Microsoft Learn [1]

So the **product slice** is: add `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, change `builder.Services.AddSignalR()` (Program.cs:269, §3) to `.AddStackExchangeRedis(redisConnectionString)`, deploy a Redis workload (a *third* backing store, like Postgres in 02), and wire the connection string by config (`__`-style env, story 02). Because it's product C#, it's **full nWave** (§D3): tests for the wiring, the RBAC/clients/website checklist (the backplane has no RBAC effect, no API contract change → likely "N/A, because…" on all three, but you *record* that), docs, mutation. The throwaway scratch version is fine for the spike; the committed version goes through DELIVER.

- Read: SignalR scale-out + Redis backplane (same page) — https://learn.microsoft.com/aspnet/core/signalr/scale [1]

You should be able to answer:
- In Lighthouse terms, trace a "team refreshed" notification from `UpdateQueueService` on pod-1 to a browser pinned to pod-2 — where exactly does it get lost **without** a backplane, and how does Redis pub/sub restore it?
- Why is the backplane the **product** slice (full nWave) while `sessionAffinity` is a scratch spike?
- The backplane fixes message fan-out — does it fix the background-updaters-run-3× problem (C)? (No — name what would.)

### HorizontalPodAutoscaler — CPU-based autoscaling (the card's last ask)

**What it is.** A controller that changes a Deployment's replica count to track a target metric (here, CPU).

> "In Kubernetes, a *HorizontalPodAutoscaler* automatically updates a workload resource (such as a Deployment or StatefulSet), with the aim of automatically scaling capacity to match demand." — kubernetes.io [2]

**The algorithm (know this cold):**

> "desiredReplicas = ceil[ currentReplicas × ( currentMetricValue / desiredMetricValue ) ]" — kubernetes.io [2]

> "if the current metric value is `200m`, and the desired value is `100m`, the number of replicas will be doubled … If the current value is instead `50m`, you'll halve the number of replicas." — kubernetes.io [2]

**The gotcha that bites everyone — CPU% is a percentage of the *request*, so requests are mandatory:**

> "if a target utilization value is set, the controller calculates the utilization value as a percentage of the equivalent resource request on the containers in each Pod." — kubernetes.io [2]

> "if some of the Pod's containers do not have the relevant resource request set, CPU utilization for the Pod will not be defined and the autoscaler will not take any action for that metric." — kubernetes.io [2]

So: **no `resources.requests.cpu` on the API container → the HPA silently does nothing.** (Story 04 set requests/limits — confirm they're on the API Deployment, §5.) Also: the HPA reads metrics from the **metrics-server**, which is **not installed by default** on a bare k3s in some configs — `kubectl top pods` failing is the tell (§4). Pair the HPA with the affinity Service (A) and ideally the backplane (B), or the HPA will *scale Lighthouse into its own (B)/(C) bugs* — autoscaling a not-scale-safe app just multiplies the breakage faster.

- Read: Horizontal Pod Autoscaling (concept + algorithm + requests requirement) — https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/ [2]
- Read (pointer): HPA walkthrough (`kubectl autoscale`, load-gen) — https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale-walkthrough/ [2a]

You should be able to answer:
- Reproduce the HPA formula and compute desired replicas for currentReplicas=2, current CPU=300m vs request-derived target=150m.
- Why does an HPA do **nothing** if the container has no CPU **request** set — and what's the one-command symptom (`kubectl top` fails / `<unknown>` in `kubectl get hpa`)?
- Why is autoscaling a *stateful-singleton* app (Lighthouse today) a foot-gun — what do (B) and (C) do as replicas climb?

### The deeper lesson — Lighthouse is a stateful singleton (the (C) finding)

**What it is.** Three pieces of in-process state make today's Lighthouse a single-instance app, and only one of them is SignalR:

1. **In-memory SignalR groups** — fixed by the backplane (B).
2. **In-memory status cache** — `UpdateNotificationHub` holds a `ConcurrentDictionary<UpdateKey, UpdateStatus>` (§3). `GetUpdateStatus` reads it; on N pods there are N *different* caches → inconsistent answers. A distributed cache (Redis again) or sourcing status from Postgres would fix it.
3. **Singleton background updaters** — `TeamUpdater`, `PortfolioUpdater`, and the `UpdateQueueService` (`UpdateServiceBase : BackgroundService`) are registered with `AddHostedService` (§3) and run **in every replica**. 3 replicas = 3 concurrent sync loops against Jira/ADO/Linear and 3× the forecast computation, racing on the same rows. The fix is **leader election** (only one replica runs the updaters) or extracting the updaters into a **separate single-replica Deployment / Job** — a real architectural decision, explicitly **out of scope for story 07** (it's future product work; planning §D3 rule-of-thumb), but you must be able to name it.

The teaching point: **"scale horizontally" is a property an app *has*, not a flag you set.** Story 07's honest outcome is that you can scale the *connection* layer (affinity) and the *messaging* layer (backplane), and you can *see* that the *work* layer (background updaters + caches) isn't ready — and you can articulate the fix without being asked to build it.

You should be able to answer:
- Name the three pieces of in-process state that make Lighthouse single-instance, and the fix class for each (backplane / distributed cache / leader-election).
- Which of the three does story 07 actually *fix*, which does it *spike*, and which does it only *surface for later*?
- Why is "leader election or a separate updater Deployment" the right shape for (C), and why is it **not** story 07's job?

## 3. Repo-grounded facts (Lighthouse is a single-instance app today — verify before you copy)

All cited from this repo at HEAD. **Read this first; it grounds §1's three-way reframe.**

- **One SignalR hub, mapped under `/api`, `[Authorize]`-gated.** `app.MapHub<UpdateNotificationHub>("api/updateNotificationHub")` (`Program.cs:212`); the class is `[Authorize] public class UpdateNotificationHub : Hub` (`Services/Implementation/BackgroundServices/Update/UpdateNotificationHub.cs`). **The path is `/api/updateNotificationHub`, NOT `/hub`** — the planning north-star's `/hub` shorthand is wrong for routing; the Ingress already path-routes `/api/*` to the backend (story 03), so the hub rides the **existing** `/api` route, no new Ingress rule.
- **SignalR has NO backplane today — in-memory only.** `builder.Services.AddSignalR().AddJsonProtocol(...)` (`Program.cs:269`) — there is **no** `.AddStackExchangeRedis(...)`, and the `.csproj` has **no** `SignalR.StackExchangeRedis` package (grep'd at HEAD → zero matches). This is the gap the (B) product slice fills.
- **The fan-out is group-based and therefore per-process.** `UpdateQueueService` (`…/Update/UpdateQueueService.cs`) injects `IHubContext<UpdateNotificationHub>` and broadcasts: `hubContext.Clients.Group(updateKey.ToString()).SendAsync(updateKey.ToString(), status)` and `hubContext.Clients.Group("GlobalUpdates").SendAsync("GlobalUpdateNotification")` (lines ~201-203). Groups are in each pod's memory → without a backplane, cross-pod delivery silently fails (§1 B).
- **There is an in-memory status cache in the hub.** `UpdateNotificationHub` holds `ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses` and serves `GetUpdateStatus(...)` from it. Per-process → inconsistent across replicas (§1 C, piece 2).
- **The background updaters are `AddHostedService` — one per replica.** `builder.Services.AddHostedService<TeamUpdater>()` (`Program.cs:872`), `AddHostedService<PortfolioUpdater>()` (`Program.cs:875`), and `UpdateServiceBase : BackgroundService` (`…/Update/UpdateServiceBase.cs:13`) backing the update queue. Every replica runs every loop → N× external sync + racing writes (§1 C, piece 3). **This is the strongest argument that naive 3-replica scaling is unsafe beyond SignalR.**
- **The frontend negotiates (no `skipNegotiation`, no transport pin) → affinity is required.** `UpdateSubscriptionService.ts:69-72`: `new signalR.HubConnectionBuilder().withUrl(\`${baseUrl}/updateNotificationHub\`, { withCredentials: true }).build()`. No WebSockets-only, no `skipNegotiation` → fails the MS doc's only affinity-free path (§2 A).
- **Resource requests exist (story 04) — the HPA depends on them.** The API container should carry `resources.requests.cpu` (set when you did probes/limits in story 04). **Confirm it's present** before creating the HPA, or the HPA no-ops (§2 HPA gotcha). If story 04's manifest didn't set a CPU *request*, add one in the scratch manifest for this story.

> **Forward hook — the real fixes are product/architecture work, scoped OUT of the story-07 scratch.** The Redis backplane is the one *in-scope* product slice (full nWave). The distributed status cache (C-2) and leader-election / separate-updater-Deployment (C-3) are **later** product decisions — name them in your review, don't build them here. Whether the *chart* (story 09) ships Redis + an HPA by default, and how the SaaS (11–13) runs per-tenant replicas, are downstream (planning §2 inflection points).

## 4. Debug reflex (carry this through the story)

Carry forward the prior rules (`describe`→Events before `logs`; `get endpoints` to see backends — story 03; `/api` 401 = unauth — story 05; probe the right path — story 04), then add the scaling failure shapes. The discipline: **before each symptom, predict which of (A)/(B)/(C) it is.**

- **Browser console shows SignalR reconnect loops / "Error: Connection disconnected" right after scaling to >1** → **(A)** the negotiate handshake and the follow-up poll landed on different pods. `kubectl get svc lighthouse -o yaml | grep sessionAffinity` (is it `None`?). Fix: `sessionAffinity: ClientIP`. Watch the loop stop.
- **Connection is stable but live updates are flaky / only refresh sometimes** → **(B)** fan-out. The update fired on a pod your browser isn't pinned to. `kubectl logs` the *other* replicas and you'll see the `SendAsync` there. Affinity won't fix it; this is the backplane. Tell: stable connection + missing notifications = (B), not (A).
- **`kubectl get hpa` shows `TARGETS: <unknown>/50%`** → metrics or requests. Either **metrics-server isn't installed** (`kubectl top pods` errors → install metrics-server; on k3s it's usually bundled but verify) or the **container has no CPU request** (§2). Fix the cause, not the HPA.
- **HPA never scales up under load** → check `kubectl describe hpa` Events + `kubectl top pods` (is CPU actually rising?). Often the load test isn't hitting CPU, or requests are set so high that utilization% stays low. Also check `behavior`/stabilization windows before assuming it's broken.
- **External APIs (Jira/ADO/Linear) get rate-limited or you see duplicate/racey updates after scaling** → **(C)** every replica runs `TeamUpdater`/`PortfolioUpdater`. This is *expected* and is the finding, not a bug to patch in story 07. `kubectl logs -l app=lighthouse --prefix` across pods shows the same sync running N×. Name it; defer the fix (leader election).
- **`kubectl top pods` / `kubectl top nodes` errors with "Metrics API not available"** → metrics-server missing; the HPA can't function. Install/enable it first.

Mnemonic: **reconnect-loop-on-scale = (A) affinity; stable-but-missing-updates = (B) backplane; `<unknown>` HPA target = metrics-server or missing CPU request; duplicate external syncs = (C) background updaters run per-replica (the finding, defer the fix). Predict the letter before you fix.**

## 5. Hands-on — copy/paste manifests & commands

Try each block from memory first; these are the backstop. Scratch dir `~/learn-k8s/story-07/`, namespace `lighthouse`. Assumes story 02's `lighthouse` Deployment + Service exist with story-04 resource **requests** on the container.

### 5.1 Scale to 3 and watch (A) break (no affinity yet)

```bash
kubectl scale deploy/lighthouse -n lighthouse --replicas=3
kubectl rollout status deploy/lighthouse -n lighthouse
kubectl get pods -n lighthouse -l app=lighthouse -o wide   # 3 pods, different IPs
kubectl get svc lighthouse -n lighthouse -o jsonpath='{.spec.sessionAffinity}{"\n"}'  # → None
# Now open the Lighthouse UI through the Ingress and watch the browser console:
#   expect SignalR negotiate/reconnect churn — predict it BEFORE you look.
```

### 5.2 The (A) band-aid — `sessionAffinity: ClientIP`

```bash
kubectl patch svc lighthouse -n lighthouse -p \
  '{"spec":{"sessionAffinity":"ClientIP","sessionAffinityConfig":{"clientIP":{"timeoutSeconds":10800}}}}'
kubectl get svc lighthouse -n lighthouse -o jsonpath='{.spec.sessionAffinity}{"\n"}'  # → ClientIP
# Reload the UI: the reconnect loop should stop. Connection is now stable...
# ...but trigger a background update and watch (B): a client on pod-1 may miss an
# update computed on pod-2. Affinity fixed the CONNECTION, not the FAN-OUT.
```

> Note: if all your browser traffic enters through Traefik, the Service may see Traefik's IP for every client (all pin to one pod). That's the §2 caveat — cookie-stickiness at the Ingress is the better lever at real scale. For the learning loop, ClientIP on the Service is enough to *see* the effect.

### 5.3 (B) the real fix — Redis backplane (SCRATCH SKETCH; the committed version is full nWave)

Deploy a throwaway Redis (a *third* backing store), then wire SignalR to it. **This C# change belongs in a full-nWave DELIVER slice, not committed from here** — the snippet is to *understand* the shape.

```yaml
# redis.yaml — throwaway, single replica, no persistence (backplane is pub/sub, not a store of record)
apiVersion: apps/v1
kind: Deployment
metadata: { name: redis, namespace: lighthouse }
spec:
  replicas: 1
  selector: { matchLabels: { app: redis } }
  template:
    metadata: { labels: { app: redis } }
    spec:
      containers:
        - name: redis
          image: redis:7-alpine          # pin a digest in anything you keep
          ports: [{ containerPort: 6379 }]
          resources: { requests: { cpu: "25m", memory: "32Mi" }, limits: { cpu: "100m", memory: "64Mi" } }
---
apiVersion: v1
kind: Service
metadata: { name: redis, namespace: lighthouse }
spec:
  selector: { app: redis }
  ports: [{ port: 6379, targetPort: 6379 }]   # ClusterIP — internal only (story 06)
```

```csharp
// Program.cs (the product slice — DELIVER, not scratch). Reads the conn string from config (story 02 __ env).
// builder.Services.AddSignalR()
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")
        ?? "redis.lighthouse.svc.cluster.local:6379")   // in-cluster Service DNS (story 06), plain, internal
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

```bash
# requires the NuGet package on the backend:
#   dotnet add Lighthouse.Backend/Lighthouse.Backend package Microsoft.AspNetCore.SignalR.StackExchangeRedis
kubectl apply -f redis.yaml
# After deploying a Lighthouse image with the backplane wired + affinity ON:
#   a notification computed on ANY pod now reaches clients on ALL pods. Verify by
#   forcing an update and watching a browser pinned (affinity) to a DIFFERENT pod receive it.
```

### 5.4 (HPA) autoscale on CPU — needs resource *requests*

```bash
# 0) sanity: metrics must work, container must have a CPU request
kubectl top pods -n lighthouse                       # must return numbers, not an error
kubectl get deploy lighthouse -n lighthouse -o jsonpath='{.spec.template.spec.containers[0].resources.requests.cpu}{"\n"}'  # must be non-empty
```

```yaml
# hpa.yaml — autoscaling/v2, CPU target 60% of the request
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata: { name: lighthouse, namespace: lighthouse }
spec:
  scaleTargetRef: { apiVersion: apps/v1, kind: Deployment, name: lighthouse }
  minReplicas: 2          # keep affinity meaningful + survive a pod loss
  maxReplicas: 5
  metrics:
    - type: Resource
      resource:
        name: cpu
        target: { type: Utilization, averageUtilization: 60 }
```

```bash
kubectl apply -f hpa.yaml
kubectl get hpa lighthouse -n lighthouse -w     # TARGETS should show a % once metrics flow (not <unknown>)
```

### 5.5 Load-test and watch it scale

```bash
# simple in-cluster load generator hammering the app through its in-cluster Service:
kubectl run load --rm -it --image=busybox --restart=Never -n lighthouse -- \
  /bin/sh -c "while true; do wget -q -O- http://lighthouse.lighthouse.svc.cluster.local/api/v1/version/current >/dev/null; done"
# in another terminal, watch the HPA react:
kubectl get hpa lighthouse -n lighthouse -w
kubectl get pods -n lighthouse -l app=lighthouse -w    # replica count should climb toward maxReplicas
# stop the load (Ctrl-C the `load` pod) and watch it scale back down after the stabilization window.
```

### 5.6 Teardown

```bash
kubectl delete hpa lighthouse -n lighthouse --ignore-not-found
kubectl delete -f redis.yaml --ignore-not-found
kubectl patch svc lighthouse -n lighthouse -p '{"spec":{"sessionAffinity":"None"}}'
kubectl scale deploy/lighthouse -n lighthouse --replicas=1
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *Lighthouse can scale horizontally; you understand what breaks and how to fix it — diagnose stateful-connection breakage, and configure CPU-based autoscaling.* Unaided, you should be able to:

- [ ] Scale the API to 3 replicas and **reproduce** the breakage, labelling each symptom as (A) connection / (B) fan-out / (C) background-work.
- [ ] Apply `sessionAffinity: ClientIP` and explain it fixes the **connection** layer (A) only — and name its two real-world holes (NAT'd clients; Ingress hiding the client IP).
- [ ] State the MS rule that affinity is required **even with the Redis backplane** — affinity and backplane are *both*, not either/or.
- [ ] Explain the Redis backplane in **Lighthouse terms**: where `UpdateQueueService`'s `Clients.Group(...).SendAsync` loses cross-pod delivery, and how pub/sub restores it — and why this is the **full-nWave product slice**, not a scratch edit.
- [ ] Configure an **HPA on CPU**, state the `ceil(currentReplicas × current/target)` algorithm, and explain why it **no-ops without a CPU request** and needs **metrics-server**.
- [ ] Load-test and observe the HPA scale up then down; read `kubectl get hpa` / `kubectl top pods` to confirm.
- [ ] Name the **(C)** finding — singleton background updaters + in-memory status cache run per-replica — and the fix class (leader election / separate updater Deployment / distributed cache) **and** why it's **out of scope** for story 07.
- [ ] Run the CLAUDE.md cross-cutting checklist on the backplane slice (RBAC: N/A because…? Lighthouse-Clients: N/A because no API contract change? Website: N/A?) and record each as an explicit "N/A, because…".

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick throwaway experiments that *test the lesson at its edges*. Form a hypothesis **before** each run:

> **(a) Make (A) and (B) visibly different.** Scale to 3 with `sessionAffinity: None`. Predict the browser symptom (reconnect churn). Flip to `ClientIP` → predict the churn stops **but** a notification still sometimes doesn't arrive. Confirm by tailing all three pods' logs while forcing an update: you'll see the `SendAsync` on a pod your browser isn't pinned to. The lesson in your hands: **affinity fixed the pipe, not the broadcast.**
>
> **(b) Prove the backplane closes (B) — and that affinity is *still* needed.** Stand up the scratch Redis (§5.3) against a backplane-wired image, keep affinity ON, and watch a cross-pod notification finally land. Then turn affinity OFF *with* the backplane on → predict (per the MS "(including when the Redis backplane is used)" line) that the **connection** breaks again even though fan-out works. The lesson: **both, not either.**
>
> **(c) Feel the HPA's request dependency.** Create the HPA, then `kubectl edit` the Deployment to *remove* the CPU request. Predict `kubectl get hpa` → `TARGETS: <unknown>` and zero scaling. Put it back → it works. The lesson: **CPU% is a percentage of the request; no request, no autoscaling.**
>
> **(d) Watch (C) bite.** With 3 replicas, tail `kubectl logs -l app=lighthouse --prefix --all-containers` and trigger a team/portfolio refresh. Predict *before*: how many times does the sync against your connector run? (Three.) The lesson, unprompted: **Lighthouse isn't scale-safe just because SignalR is — the work layer needs leader election, which story 07 only *names*.**

Investigate, don't look it up: scale and break it, fix the pipe and watch the broadcast still leak, add the backplane and watch affinity still matter, starve the HPA of a request, and count the duplicate syncs. The reconnect loop is (A); the missing notification is (B); the `<unknown>` target is the request gotcha; the triple sync is (C).

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | SignalR production hosting & scaling (sticky sessions; "including when the Redis backplane is used"; scale-out; Redis pub/sub backplane; Nginx `ip_hash`) | learn.microsoft.com | High (1.0) | Official | 2026-06-14 | Y (quotes confirmed verbatim) |
| 2 | Horizontal Pod Autoscaling (definition; `ceil` algorithm; CPU%-of-request; "no request → no action") | kubernetes.io | High (1.0) | Official | 2026-06-14 | Y (quotes confirmed verbatim) |
| 2a | HPA walkthrough (`kubectl autoscale`, load-gen) | kubernetes.io | High (1.0) | Official | 2026-06-14 | Pointer (procedure is well-known; not re-quoted) |
| 3 | Service session affinity (`sessionAffinity: ClientIP`) | kubernetes.io | High (1.0) | Official | 2026-06-14 | Partial (TOC confirmed; the verbatim ClientIP sentence truncated on fetch — §Gaps) |
| — | Lighthouse repo (`Program.cs`, `UpdateNotificationHub.cs`, `UpdateQueueService.cs`, `UpdateServiceBase.cs`, `UpdateSubscriptionService.ts`, `.csproj`) | this repo | Primary | First-party source | 2026-06-14 | Y (line-cited at HEAD; "no backplane" / "no StackExchangeRedis pkg" = grep-confirmed zero matches) |

Primary sources: **4** (the MS SignalR scale page + 2 kubernetes.io pages + the line-cited Lighthouse repo). The repo facts are the strongest authority: they establish that Lighthouse is a *single-instance app by construction* (in-memory groups + in-memory status cache + per-replica background updaters), which reframes the story from "add session affinity" to "see the three independent ways a stateful singleton breaks under replicas, fix the two that are in scope, and name the third."

## Knowledge Gaps

- **The verbatim `sessionAffinity: ClientIP` sentence truncated on fetch.** [3]'s TOC confirms the "Session affinity" / "Session stickiness timeout" sections exist, but the body fell past the fetch truncation. The behaviour (route a client to the same Pod by source IP, with a timeout) is well-known, documented k8s behaviour; eyeball the "Session affinity" section of [3] if you want it verbatim. Confidence High.
- **Story 04's CPU *request* on the API container is assumed, not re-verified here.** §2/§3 assert it exists from story 04; **confirm with the `kubectl get deploy ... resources.requests.cpu` one-liner in §5.4** before creating the HPA — if it's missing, the HPA no-ops. (This is the single most common HPA failure.)
- **metrics-server on your k3s is assumed present.** k3s usually bundles it, but some minimal installs don't. `kubectl top pods` failing is the tell — install/enable metrics-server before expecting the HPA to function.
- **The backplane package version / exact `AddStackExchangeRedis` overload may drift.** The snippet shows the common shape; confirm the current `Microsoft.AspNetCore.SignalR.StackExchangeRedis` API for the backend's .NET version when you do the real DELIVER slice. The connection-string-from-config wiring (story 02 `__` env) is the durable part.
- **The (C) fix (leader election / separate updater Deployment / distributed status cache) is named, not designed.** This guide deliberately scopes it out of story 07 (planning §D3 rule-of-thumb: it's future product/architecture work). When you reach it, it's its own full-nWave effort — don't let it leak into the story-07 scratch.
- **Whether the frontend *should* switch to WebSockets-only + `skipNegotiation` (avoiding affinity entirely) is an open product question.** The MS doc's third affinity-free path is exactly that. It's a viable alternative to `sessionAffinity` for (A) — worth raising in review, but a frontend change with its own trade-offs (proxies that block WS), so out of story 07's scratch scope.

## Full Citations

[1] Stanton-Nurse, A.; Gaster, B.; Dykstra, T. "ASP.NET Core SignalR production hosting and scaling". Microsoft Learn. https://learn.microsoft.com/aspnet/core/signalr/scale. Accessed 2026-06-14. (Page dated 2026-05; covers through ASP.NET Core 11. Quoted: "SignalR requires the same server process handle all HTTP requests…"; the three affinity-free scenarios; "In all other scenarios (including when the Redis backplane is used), the server environment must be configured for sticky sessions."; the scale-no-backplane "the message only goes to the clients connected to that server"; the Redis pub/sub backplane mechanism; "recommended scale-out approach for apps hosted on your own infrastructure".)
[2] The Kubernetes Authors. "Horizontal Pod Autoscaling". kubernetes.io. https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/. Accessed 2026-06-14. (Quoted: HPA definition; `desiredReplicas = ceil[currentReplicas × (currentMetricValue / desiredMetricValue)]`; "utilization value as a percentage of the equivalent resource request"; "if some of the Pod's containers do not have the relevant resource request set, CPU utilization for the Pod will not be defined and the autoscaler will not take any action".)
[2a] The Kubernetes Authors. "HorizontalPodAutoscaler Walkthrough". kubernetes.io. https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale-walkthrough/. Accessed 2026-06-14 (pointer — `kubectl autoscale` + load-generator procedure).
[3] The Kubernetes Authors. "Virtual IPs and Service Proxies" (Service session affinity / `sessionAffinity: ClientIP`). kubernetes.io. https://kubernetes.io/docs/reference/networking/virtual-ips/. Accessed 2026-06-14 (ClientIP sentence truncated on fetch — §Gaps).
[4] LetPeopleWork. Lighthouse repository — `Lighthouse.Backend/Lighthouse.Backend/Program.cs` (line 212: `MapHub<UpdateNotificationHub>("api/updateNotificationHub")`; line 269: `AddSignalR().AddJsonProtocol(...)` with no backplane; lines 872/875: `AddHostedService<TeamUpdater>()` / `AddHostedService<PortfolioUpdater>()`), `Services/Implementation/BackgroundServices/Update/UpdateNotificationHub.cs` (`[Authorize]` hub; `ConcurrentDictionary<UpdateKey, UpdateStatus>` in-memory cache; group subscribe/unsubscribe), `…/Update/UpdateQueueService.cs` (lines ~201-203: `Clients.Group(...).SendAsync` + `Clients.Group("GlobalUpdates").SendAsync`), `…/Update/UpdateServiceBase.cs` (line 13: `BackgroundService, IUpdateService`), `Lighthouse.Frontend/src/services/UpdateSubscriptionService.ts` (lines 69-72: `HubConnectionBuilder().withUrl(.../updateNotificationHub, { withCredentials: true })` — no `skipNegotiation`), `Lighthouse.Backend.csproj` (no `SignalR.StackExchangeRedis` package — grep-confirmed). Accessed 2026-06-14.
