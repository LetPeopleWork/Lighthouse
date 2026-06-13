> nw-research reading guide for story #5196 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 06 — MCP HTTP Server in-cluster (Reading Guide)

**Date**: 2026-06-13 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 8 primary (5 official-doc pages + MCP spec + oauth2-proxy(carry-over) + repo)
**Doc currency**: k8s Service concepts are evergreen-per-version (current nav tracks v1.36.x); the MCP **Streamable HTTP** transport is the current spec (rev 2025-06-18, supersedes HTTP+SSE 2024-11-05). The MCP-HTTP image/env/endpoints are line-cited from this repo's `docs/aiintegration.md` at HEAD; the downstream-auth + "no embedded MCP server" facts are line-cited from `Program.cs` / the `.csproj` / the optional-feature wiring. **Read §3 before §5 — it reframes the whole story.**

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the `mcp-http` Deployment, its ClusterIP
> Service, the API-key Secret, the optional `/mcp` Ingress route + edge-auth) is throwaway learning
> scaffolding. Keep it in a personal scratch dir, e.g. `~/learn-k8s/story-06/`. **Nothing from this story
> goes into the Lighthouse repo** — the real manifests first land in `chart/` at **story 09** (planning §7
> workspace map: stories 00–08 = scratch, "throwaway, never committed"). You're deploying a *real,
> already-published* image; the YAML is rehearsal, not product.

## 1. Orientation

So far the cluster has run **one** app: the `lighthouse` pod (SPA + API in one process, story 02) plus its
`lighthouse-postgres` backing store. Story 06 adds a **third, independent workload** — the **Lighthouse
MCP HTTP server** — and uses it to make three things concrete: running a second service, **reasoning about
internal vs external exposure**, and **securing an API endpoint with token auth**.

The reframe that makes this story click (and it's grounded in the repo, §3): **the MCP HTTP server is not
part of the Lighthouse backend.** It's a **separate, already-published Node container**
(`ghcr.io/letpeoplework/lighthouse-clients/mcp-http`) that does exactly one job — speak the **MCP protocol
over HTTP** to an LLM client, and translate those calls into ordinary **Lighthouse REST API** calls,
authenticating with a **Lighthouse API key**. You are not *writing* an MCP server; you are *deploying* one
and wiring its two network hops:

```
   LLM client ──(MCP over HTTP, POST /mcp)──▶ [edge?] ──▶ mcp-http Service (ClusterIP) ──▶ mcp-http pod
                                                                                               │
                                                          (in-cluster, X-Api-Key, story 05)    ▼
                                                              lighthouse Service ──▶ Lighthouse API
```

Two hops, **two different auth questions** — and conflating them is the trap:

1. **Downstream (mcp-http → Lighthouse): already solved.** The container holds a `LIGHTHOUSE_API_KEY` and
   reaches Lighthouse over the **in-cluster Service DNS**; Lighthouse's smart-auth recognises the key via
   the `X-Api-Key` header (story 05, `Program.cs:614`). You just supply the key as a **Secret**.
2. **Inbound (LLM client → mcp-http): the part you must reason about.** The published server authenticates
   *downstream* but does **not** ship inbound auth of its own. And the MCP spec is explicit that an HTTP
   transport **SHOULD** authenticate and **MUST** validate `Origin`. So a `/mcp` endpoint that holds a
   powerful API key and answers **anyone** is a hole: whoever reaches it drives your Lighthouse. That makes
   **"internal vs external exposure" a security decision, not a convenience one**:
   - **Default-safe: ClusterIP-only** — reachable only from inside the cluster; LLM clients that run
     in-cluster (or via `kubectl port-forward` for a human) talk to it, nothing external can.
   - **External (Ingress `/mcp`): only WITH an edge-auth layer in front** — oauth2-proxy / Traefik
     ForwardAuth / an API-key middleware (story 05's edge pattern). Exposing it raw is the anti-pattern.

"Done" feels like: a `mcp-http` **Deployment** + **ClusterIP Service** running in-cluster, fed its
`LIGHTHOUSE_URL` (the in-cluster Lighthouse Service DNS) and `LIGHTHOUSE_API_KEY` (a Secret); an LLM client
(or `curl`) successfully **initializing an MCP session against the cluster endpoint**; and — unaided — you
can **justify your exposure choice** (why ClusterIP-only is the safe default and what an Ingress route
would *require* first), explain the **two-hop auth**, and find the MCP server via **in-cluster DNS**.

This builds on story 02 (multi-service + `__` env config), story 03 (Ingress path routing, `/mcp`), story
04 (probes — and note the happy surprise in §3), and story 05 (X-Api-Key smart-auth + edge auth). Don't
re-teach those.

## 2. Concepts

### MCP over HTTP (the Streamable HTTP transport)

**What it is.** MCP (Model Context Protocol) lets an LLM client call tools on a server. It's **JSON-RPC**,
carried over one of two standard transports — **stdio** (local subprocess) or **Streamable HTTP** (a
network endpoint). Story 06 is the HTTP one.

> "MCP uses JSON-RPC to encode messages. JSON-RPC messages MUST be UTF-8 encoded. The protocol currently defines two standard transport mechanisms for client-server communication: stdio ... Streamable HTTP" — modelcontextprotocol.io [1]

**The endpoint shape — one path, POST + GET, SSE optional:**

> "In the Streamable HTTP transport, the server operates as an independent process that can handle multiple client connections. This transport uses HTTP POST and GET requests. Server can optionally make use of Server-Sent Events (SSE) to stream multiple server messages." — modelcontextprotocol.io [1]
> "The server MUST provide a single HTTP endpoint path (hereafter referred to as the MCP endpoint) that supports both POST and GET methods. For example, this could be a URL like `https://example.com/mcp`." — modelcontextprotocol.io [1]

That `…/mcp` example is exactly the route the Lighthouse server uses (`POST /mcp`, §3). Client POSTs carry
JSON-RPC requests; the server answers with either `application/json` (one object) or `text/event-stream`
(an SSE stream).

**The security warning — this is the spine of story 06's exposure decision:**

> "1. Servers MUST validate the `Origin` header on all incoming connections to prevent DNS rebinding attacks. 2. When running locally, servers SHOULD bind only to localhost (127.0.0.1) rather than all network interfaces (0.0.0.0). 3. Servers SHOULD implement proper authentication for all connections." — modelcontextprotocol.io [1]

Note point 2 vs your Docker env (`HOST=0.0.0.0`, §3): binding to all interfaces is fine *inside a pod*
(the pod network is the isolation boundary, and a ClusterIP Service is the only door) — but it's precisely
why you must **not** then staple it to a public Ingress without auth (point 3).

- Read: MCP — Transports (stdio + Streamable HTTP + the security warning) — https://modelcontextprotocol.io/docs/concepts/transports [1]

You should be able to answer:
- MCP's two standard transports — which one is story 06, and what's the *other* one used for (story-stdio, the local clients)?
- What HTTP methods must the single MCP endpoint support, and when does the server answer with SSE vs a single JSON object?
- The three Streamable-HTTP security rules — which one (`SHOULD implement proper authentication`) makes "expose externally" a decision you can't make casually?

### Service types — ClusterIP (internal) vs NodePort / LoadBalancer (external)

**What it is.** A Service gives a stable name + virtual IP in front of a set of pods; its `type` decides
*who can reach it*.

> "In Kubernetes, a Service is a method for exposing a network application that is running as one or more Pods in your cluster." — kubernetes.io [2]

**ClusterIP is the default — and it's internal-only:**

> "Applying this manifest creates a new Service ... with the default ClusterIP service type." — kubernetes.io [2]

A `ClusterIP` Service is reachable **only from within the cluster** — there is no external door. That's the
property you *want* for the MCP server's default posture (verbatim "only from within the cluster" sentence
truncated on fetch — §Knowledge Gaps; this is the documented, well-known behaviour of ClusterIP). The
other `type` values open external doors and form a ladder of increasing exposure (described by their
well-known behaviour — full verbatim text truncated, §Gaps):
- **`ClusterIP`** — internal-only virtual IP (default). The MCP server's safe home.
- **`NodePort`** — opens a port on **every node** so external traffic can hit it directly (L4). Blunt;
  rarely what you want for an app endpoint.
- **`LoadBalancer`** — provisions an external L4 load balancer (cloud). External, L4.
- **`ExternalName`** — a CNAME alias to an external DNS name; no proxying.

**Where Ingress fits.** None of those is how you normally expose HTTP externally — that's **Ingress**
(story 03), an **L7** router that terminates TLS and path/host-routes to a **ClusterIP** Service behind it.
So "expose the MCP server externally" = keep it `ClusterIP` **and** add an Ingress route (`/mcp`) — *with
auth* — not flip it to `NodePort`/`LoadBalancer`.

- Read: Service (what a Service is; ClusterIP default; the type ladder) — https://kubernetes.io/docs/concepts/services-networking/service/ [2]

You should be able to answer:
- What's the **default** Service type, and who can reach a `ClusterIP` Service from where?
- Order `ClusterIP` / `NodePort` / `LoadBalancer` by exposure — and say why "expose the MCP server" means *Ingress in front of a ClusterIP*, not switching the Service type.
- Given the MCP server holds an API key and has no inbound auth of its own, which `type` is the correct default and why?

### Multi-service architecture & in-cluster DNS (how mcp-http finds Lighthouse)

**What it is.** A second/third workload in the same namespace, discovered by name through **cluster DNS** —
not through the public Ingress.

Every Service gets a stable DNS name: `<service>.<namespace>.svc.cluster.local` (and the short forms
`<service>` / `<service>.<namespace>` from within the same/any namespace). So the MCP pod reaches
Lighthouse at **`http://lighthouse.lighthouse.svc.cluster.local`** (or just `http://lighthouse`
same-namespace) — **plain HTTP, in-cluster**, never the public `https://lighthouse.local`.

This matters for two reasons, both echoing earlier stories:
- **Don't hairpin through the Ingress.** If you set `LIGHTHOUSE_URL=https://lighthouse.local`, the MCP pod
  would leave the cluster, re-enter through Traefik, do TLS, and (with `/etc/hosts` tricks) maybe not even
  resolve `lighthouse.local` from inside a pod. Use the **internal Service DNS** — faster, no TLS, no
  split-horizon (the same lesson as story 05's `MetadataAddress`).
- **TLS terminates at the edge (story 03).** Pod-to-pod traffic is plain HTTP on the cluster network;
  you don't need (and `lighthouse`'s in-cluster Service doesn't serve) HTTPS internally.

- Read (DNS for Services) — https://kubernetes.io/docs/concepts/services-networking/dns-pod-service/ [3]

You should be able to answer:
- What's the in-cluster DNS name for the `lighthouse` Service in the `lighthouse` namespace, and what does the MCP pod use for `LIGHTHOUSE_URL` — the public host or the Service DNS? Why?
- Why is hitting `https://lighthouse.local` from inside a pod the *wrong* answer (two reasons)?
- HTTP or HTTPS for the mcp-http → lighthouse hop, and why (where did TLS terminate)?

### API auth patterns — the two-hop split (downstream key vs inbound edge auth)

**What it is.** The MCP server sits **between** an untrusted client and a privileged backend, so it has
*two* trust boundaries — and they're secured differently.

- **Downstream (mcp-http → Lighthouse): a bearer API key.** A long-lived secret token the server presents
  on every call. Lighthouse's smart-auth routes any request carrying the `X-Api-Key` header to the API-key
  handler (story 05, `Program.cs:612-617`). This is the classic **service-to-service API key** pattern:
  simple, but the key is **powerful and long-lived**, so it lives in a **Secret** and never in a ConfigMap
  or image (story 05's Secret-vs-ConfigMap lesson).
- **Inbound (LLM client → mcp-http): edge auth, or don't expose.** The published server doesn't
  authenticate inbound callers, and the MCP spec says it **SHOULD**. The pattern menu, in order of how this
  course handles it:
  - **No inbound exposure (ClusterIP-only)** — the boundary *is* the cluster network. Default.
  - **Edge auth via the Ingress** — oauth2-proxy / Traefik ForwardAuth / basic-auth / an API-key
    middleware in front of the `/mcp` route (story 05's edge pattern). Required before any external route.
  - **(Anti-pattern) raw external `/mcp`** — a privileged proxy answering the open internet. Don't.

The teaching point: **a key that authenticates you to the backend is not the same as authenticating the
caller to you.** The MCP server having `LIGHTHOUSE_API_KEY` protects *Lighthouse from random pods* — it
does **nothing** to protect *the MCP server from random clients*. Two boundaries, two mechanisms.

- Read (carry-over from story 05): oauth2-proxy / edge auth — https://oauth2-proxy.github.io/oauth2-proxy/ [4]

You should be able to answer:
- The MCP server's **two** trust boundaries — name the auth mechanism for each, and which one the published image already handles vs which one you must add.
- Why does `LIGHTHOUSE_API_KEY` belong in a **Secret**, and what would go wrong if you baked it into the image or a ConfigMap?
- "An API key to the backend ≠ authenticating the caller" — restate that in terms of the `/mcp` endpoint and what an attacker who reaches an unauth'd external `/mcp` can do.

## 3. Repo-grounded facts (the MCP HTTP server is a SEPARATE container — you DEPLOY it, you don't BUILD it)

All cited from this repo at HEAD — verify before you copy. **Read this first; it reframes the story.**

- **The MCP HTTP server is the `lighthouse-clients` Node container, not the backend.** Per
  `docs/aiintegration.md:199-213`, the published image is
  **`ghcr.io/letpeoplework/lighthouse-clients/mcp-http:latest`** (pin a real tag in practice), run with
  `-e HOST=0.0.0.0 -e PORT=3000 -e LIGHTHOUSE_URL=… -e LIGHTHOUSE_API_KEY=…`. It exposes
  **`GET /health`** and **`POST /mcp`** (`docs/aiintegration.md:181-184`). Clients point at
  `http://<host>:3000/mcp` (`:213`).
- **The Lighthouse *backend* does NOT host an MCP server (despite a dangling reference).** The backend
  `.csproj` references `ModelContextProtocol.AspNetCore` v1.4.0
  (`Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj:42`) and there's an `McpServer`
  optional-feature **flag** (`Models/OptionalFeatures/OptionalFeatureKeys.cs:9`, seeded in
  `Services/Implementation/Seeding/OptionalFeatureSeeder.cs:33`) — **but `Program.cs` has no `MapMcp` /
  `AddMcpServer` wiring** (grep'd at HEAD → zero matches). So the package is **vestigial/aspirational** and
  the flag toggles UI affordances, *not* an in-process MCP endpoint. **The thing you deploy in this story
  is the separate clients container** — don't go hunting for an MCP endpoint inside the Lighthouse image;
  it isn't there. (If a future story embeds MCP in the backend, that's product C# → full nWave, planning
  §D3 — out of scope here.)
- **Downstream auth is story 05's X-Api-Key, end to end.** The MCP container authenticates to Lighthouse
  with `LIGHTHOUSE_API_KEY`; Lighthouse's smart-auth recognises an API request by the **`X-Api-Key`**
  header (`Program.cs:614`) and routes it to the API-key handler (`Program.cs:612-617`). Create the key in
  the UI: **System Settings → API Keys** (`docs/aiintegration.md:52`). The key is a **Secret** (story 05).
- **Happy surprise — the MCP server HAS a health endpoint (Lighthouse didn't).** Story 04's whole "no
  `/health`, pick a probe target carefully" problem **doesn't apply here**: `GET /health`
  (`docs/aiintegration.md:183`) is purpose-built for exactly this. Point the **liveness/readiness probes**
  straight at `httpGet /health` on `PORT` — clean, unauthenticated, no `/api`-401 trap to dodge.
- **`/mcp` Ingress route = story 03 path-routing, no rewrite needed.** The ADO card's `lighthouse.local/mcp`
  is **path-based** routing to the `mcp-http` Service. Because the server already serves at the path
  **`/mcp`** (`docs/aiintegration.md:184`), a `pathType: Prefix` `/mcp` route needs **no path rewrite** —
  the path the client sends is the path the server expects. (Contrast: if it served at `/` you'd need a
  rewrite. Confirm by curling the Service directly first, §5.)
- **The downstream URL is the in-cluster Service DNS, NOT the public host.** Set
  `LIGHTHOUSE_URL=http://lighthouse.lighthouse.svc.cluster.local` (plain HTTP, in-cluster) — TLS terminated
  at the Ingress in story 03, and hitting `https://lighthouse.local` from a pod hairpins through Traefik
  and may not even resolve (§2 DNS, echoing story 05's split-horizon).

> **Forward hook — productization is OUT OF SCOPE (planning §D3).** Pinning a real image tag + digest,
> sealing the API-key Secret (encryption-at-rest / external-secrets), the *final* exposure decision
> (ClusterIP-only vs an authenticated public `/mcp`), and whether the chart even ships the MCP server are
> **story 09 (chart) / 11–12 (SaaS)** concerns. Story 06 proves the *mechanism* with a throwaway Deployment.

## 4. Debug reflex (carry this through the story)

Carry forward the prior rules (describe→Events before run, logs after; `get endpoints` to see if a Service
has backends — story 03; `/api` 401 = unauth — story 05), then add the two-hop MCP failure shapes — note
that **most failures are the *downstream* hop, not the MCP protocol**:

- **MCP client can't reach the endpoint at all** → it's the **inbound** hop / routing. ClusterIP-only? then
  an *external* client simply can't (that's correct — use `kubectl port-forward svc/mcp-http 3000:3000` or
  an in-cluster pod). Behind an Ingress? `kubectl describe ingress`, `kubectl get endpoints mcp-http`
  (story 03 reflex). `curl http://<host>/mcp` with no `Accept` header → 4xx is *expected* (the client must
  send `Accept: application/json, text/event-stream`).
- **MCP session initializes but every tool call fails with an auth error** → it's the **downstream** hop:
  `LIGHTHOUSE_API_KEY` is wrong/missing → mcp-http reaches Lighthouse and gets a hard **401**
  (`Program.cs:636`). Check the *effective* env in the pod:
  `kubectl exec deploy/mcp-http -- env | grep -i LIGHTHOUSE`. Re-issue the key in System Settings → API Keys
  if unsure.
- **Tools fail with a connection/timeout error (not 401)** → `LIGHTHOUSE_URL` is wrong: either you used the
  **public** `https://lighthouse.local` (hairpin / unresolved from a pod) or a typo'd Service DNS. Fix to
  `http://lighthouse.lighthouse.svc.cluster.local`. Confirm DNS from inside:
  `kubectl exec deploy/mcp-http -- wget -qO- http://lighthouse.lighthouse.svc.cluster.local/api/v1/version/current`.
- **`403 Forbidden` mentioning Origin** → the MCP spec's **`Origin` validation** ([1]) rejecting the
  caller. Configure the allowed origin / front it correctly rather than disabling the check.
- **"Anyone on the network can call it"** → you exposed it externally (Ingress `/mcp` or a NodePort)
  **without edge auth**. That's the §2 anti-pattern: add oauth2-proxy/ForwardAuth (story 05) or drop back
  to ClusterIP-only. `kubectl get svc mcp-http` (is it really ClusterIP?) + `kubectl get ingress`.
- **Pod never Ready** → probe the **MCP** server's `GET /health`, not Lighthouse's API. If `/health` 404s,
  the container didn't boot its HTTP server (check `LIGHTHOUSE_URL`/`PORT` env and `kubectl logs`).

Mnemonic: **can't-connect = inbound/routing (or correctly ClusterIP-blocked); init-OK-but-tools-401 =
downstream API key; tools-timeout = wrong LIGHTHOUSE_URL (use Service DNS, not the public host);
Origin-403 = MCP DNS-rebind guard; anyone-can-call = you exposed it without edge auth. Probe `/health`.**

## 5. Hands-on — copy/paste manifests

Try each block from memory first; these are the backstop. Work in `~/learn-k8s/story-06/`, namespace
`lighthouse`. Assumes story 02's `lighthouse` + `lighthouse-postgres` and (for Option B) story 03's
Ingress exist. First create a Lighthouse **API key** in the UI (System Settings → API Keys).

### 5.1 The API key as a Secret (downstream auth — story 05)

```bash
kubectl create secret generic mcp-lighthouse-key -n lighthouse \
  --from-literal=LIGHTHOUSE_API_KEY='<paste-the-key-from-System-Settings>'
# Secret = base64 at rest, NOT encrypted (story 05) — fine for local; productization seals it (story 11-12).
```

### 5.2 The mcp-http Deployment (probes point at /health — story 04 made easy)

```yaml
# mcp-http.yaml
apiVersion: apps/v1
kind: Deployment
metadata: { name: mcp-http, namespace: lighthouse }
spec:
  replicas: 1
  selector: { matchLabels: { app: mcp-http } }
  template:
    metadata: { labels: { app: mcp-http } }
    spec:
      automountServiceAccountToken: false      # doesn't call the k8s API (story 05 least-privilege)
      containers:
        - name: mcp-http
          image: ghcr.io/letpeoplework/lighthouse-clients/mcp-http:latest   # pin a real tag, never :latest
          env:
            - { name: HOST, value: "0.0.0.0" }   # bind all interfaces — fine INSIDE a pod (§2 point 2)
            - { name: PORT, value: "3000" }
            # in-cluster Service DNS + plain HTTP — NOT the public https://lighthouse.local (§3)
            - { name: LIGHTHOUSE_URL, value: "http://lighthouse.lighthouse.svc.cluster.local" }
            - name: LIGHTHOUSE_API_KEY
              valueFrom: { secretKeyRef: { name: mcp-lighthouse-key, key: LIGHTHOUSE_API_KEY } }
          ports: [{ containerPort: 3000 }]
          readinessProbe:                        # the MCP server HAS /health (unlike Lighthouse) — story 04
            httpGet: { path: /health, port: 3000 }
            periodSeconds: 10
            failureThreshold: 3
          livenessProbe:
            httpGet: { path: /health, port: 3000 }
            periodSeconds: 10
            failureThreshold: 3
          resources:
            requests: { cpu: "50m",  memory: "64Mi" }
            limits:   { cpu: "250m", memory: "128Mi" }
```

### 5.3 ClusterIP Service (internal-only — the safe default)

```yaml
# mcp-http-svc.yaml
apiVersion: v1
kind: Service
metadata: { name: mcp-http, namespace: lighthouse }
spec:
  type: ClusterIP            # DEFAULT + internal-only (§2). This IS the exposure decision: in-cluster only.
  selector: { app: mcp-http }
  ports: [{ port: 3000, targetPort: 3000 }]
```

```bash
kubectl apply -f mcp-http.yaml -f mcp-http-svc.yaml
kubectl rollout status deploy/mcp-http
kubectl get endpoints mcp-http        # must list the pod IP:3000 (story 03 reflex)
```

### 5.4 Test it — from INSIDE the cluster (Option A: ClusterIP-only)

```bash
# health check from another pod (proves in-cluster reachability):
kubectl run curl --rm -it --image=curlimages/curl --restart=Never -- \
  curl -s http://mcp-http.lighthouse.svc.cluster.local:3000/health

# initialize an MCP session (POST /mcp; the Accept header is REQUIRED by the spec [1]):
kubectl run curl --rm -it --image=curlimages/curl --restart=Never -- \
  curl -s -X POST http://mcp-http.lighthouse.svc.cluster.local:3000/mcp \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json, text/event-stream' \
    -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"curl","version":"0"}}}'

# for a human/LLM client on your laptop, port-forward instead of exposing externally:
kubectl port-forward svc/mcp-http 3000:3000 -n lighthouse
# then point your MCP client at http://127.0.0.1:3000/mcp
```

### 5.5 (Option B) External via Ingress `/mcp` — ONLY WITH edge auth

Do this **only** after wiring an edge-auth layer (story 05's oauth2-proxy / Traefik ForwardAuth middleware).
Path-based route, no rewrite (server already serves `/mcp`, §3):

```yaml
# mcp-ingress.yaml — adds /mcp to the existing host; reuses story 05's ForwardAuth middleware.
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: mcp-http
  namespace: lighthouse
  annotations:
    # REQUIRED: gate /mcp behind edge auth (story 05). Without this, do NOT create this Ingress.
    traefik.ingress.kubernetes.io/router.middlewares: lighthouse-oauth2-forwardauth@kubernetescrd
spec:
  tls:
    - hosts: [lighthouse.local]
      secretName: lighthouse-tls        # story 03
  rules:
    - host: lighthouse.local
      http:
        paths:
          - path: /mcp
            pathType: Prefix
            backend:
              service:
                name: mcp-http
                port: { number: 3000 }
```

```bash
kubectl apply -f mcp-ingress.yaml
# verify the edge challenge fires BEFORE the MCP server answers:
curl -sk https://lighthouse.local/mcp -X POST \
  -H 'Accept: application/json, text/event-stream' -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' -i | head -20
# expect a redirect/401 from oauth2-proxy, NOT a raw MCP response.
```

### 5.6 Teardown

```bash
kubectl delete -f mcp-ingress.yaml --ignore-not-found
kubectl delete -f mcp-http-svc.yaml -f mcp-http.yaml
kubectl delete secret mcp-lighthouse-key -n lighthouse
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *the MCP HTTP server runs in-cluster and is reachable from an LLM client; you can run a
second service, reason about internal vs external exposure, and secure an API endpoint with token auth.*
Unaided, you should be able to:

- [ ] Explain that the MCP HTTP server is a **separate published container** (`lighthouse-clients/mcp-http`), not part of the backend — and that the backend's `ModelContextProtocol.AspNetCore` reference is **not** wired (`Program.cs` has no `MapMcp`).
- [ ] Deploy it as a **third workload** + **ClusterIP Service**, wire `LIGHTHOUSE_URL` to the **in-cluster Service DNS** (not the public host) and `LIGHTHOUSE_API_KEY` from a **Secret**, and probe **`GET /health`**.
- [ ] Describe the **two-hop auth**: downstream `X-Api-Key` to Lighthouse (already handled) vs **inbound** auth to the MCP endpoint (the part you must add or avoid by not exposing).
- [ ] **Justify the exposure decision**: why **ClusterIP-only** is the safe default; why an Ingress `/mcp` route **requires** edge auth first; and why you do *not* reach for `NodePort`/`LoadBalancer`.
- [ ] Initialize an MCP session against the cluster endpoint (`POST /mcp` with the required `Accept` header) from inside the cluster / via `port-forward` — and read the MCP spec's **`Origin` / authentication** security rules.
- [ ] Find the MCP server (and Lighthouse) via **in-cluster DNS** (`<svc>.<ns>.svc.cluster.local`) and explain why pod→pod is **plain HTTP** (TLS terminated at the edge, story 03).
- [ ] Connect the failure modes: `init-OK-but-tools-401` = downstream key; `tools-timeout` = wrong `LIGHTHOUSE_URL`; `anyone-can-call` = exposed without edge auth.

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick a throwaway experiment that *tests the lesson at its edges*. Form a hypothesis **before** you run it:

> **(a) Prove ClusterIP is a wall.** With §5.3 applied (ClusterIP), try to reach `mcp-http` from your
> laptop **without** port-forward (e.g. `curl http://<node-ip>:3000/mcp`). Predict *before*: does it
> connect, refuse, or hang? Then `kubectl port-forward` and watch it work. The lesson: ClusterIP has **no
> external door** — that *is* the exposure control, not an accident.
>
> **(b) Break the downstream hop two different ways and read the two different errors.** First set
> `LIGHTHOUSE_API_KEY` to garbage → predict the MCP `initialize` succeeds but a *tool call* fails, and with
> what status (expect a downstream **401**, `Program.cs:636`). Revert, then set
> `LIGHTHOUSE_URL=https://lighthouse.local` (the public host) → predict a **timeout/connection** error, not
> a 401. Confirm both in `kubectl logs deploy/mcp-http`. The lesson: **401 = wrong key; timeout = wrong
> URL/hairpin** — two hops, two symptoms.
>
> **(c) Expose it raw, feel the hole, then close it.** Apply §5.5 **without** the ForwardAuth annotation,
> and `curl https://lighthouse.local/mcp` from your laptop — predict whether you can now drive Lighthouse
> through it with no credentials of your own (you can — the pod's API key does the talking). Then add the
> annotation back and watch the edge challenge appear. The lesson, in your hands: **a backend API key does
> not authenticate the caller** — exposure without edge auth hands your Lighthouse to the internet.

Investigate, don't look it up: knock on ClusterIP from outside and feel the wall; break key-vs-URL and read
the two errors; expose-raw then gate it. The refused connection is the ClusterIP lesson; the 401-vs-timeout
is the two-hop lesson; the credential-free drive-through is the edge-auth lesson.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | MCP — Transports (JSON-RPC; stdio + Streamable HTTP; single POST/GET endpoint; SSE; Origin/auth security warning) | modelcontextprotocol.io | High (1.0) | Official spec | 2026-06-13 | Y (quotes confirmed verbatim) |
| 2 | Service (what a Service is; ClusterIP default; type ladder) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Partial (Service + default-ClusterIP quotes confirmed; per-type descriptions + "only within the cluster" sentence truncated — §Gaps) |
| 3 | DNS for Services and Pods (`<svc>.<ns>.svc.cluster.local`) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Pointer (DNS naming is well-known documented behaviour; not re-quoted here) |
| 4 | oauth2-proxy (edge auth — carried over from story 05) | oauth2-proxy.github.io | High (0.9) | Official project docs | 2026-06-13 | Y (definition confirmed in story 05) |
| — | Lighthouse repo (`docs/aiintegration.md`, `Program.cs`, `.csproj`, optional-feature wiring) | this repo | Primary | First-party source | 2026-06-13 | Y (line-cited at HEAD; "no MapMcp in backend" = grep-confirmed zero matches; image/env/endpoints from the docs page) |

Primary sources: **8** (the MCP spec transport page + 3 kubernetes.io doc pages + the oauth2-proxy
carry-over + the line-cited Lighthouse repo, counting the truncated [2]/[3] pieces). The repo facts are the
strongest authority: they establish that the MCP HTTP server is a *separate published container* wrapping
Lighthouse's REST API — which reframes the story from "build/embed MCP" to "deploy a second service and
reason about its exposure + its two auth hops".

## Knowledge Gaps

- **Service-type descriptions + the "ClusterIP only within the cluster" sentence truncated on fetch.** [2]
  confirmed "what a Service is" and "default ClusterIP" verbatim, but the per-type (ClusterIP / NodePort /
  LoadBalancer / ExternalName) descriptions and the explicit cluster-internal-only sentence fell past the
  fetch truncation. They're described from well-known documented behaviour; eyeball the "Publishing
  Services (ServiceTypes)" section of [2] if you want them verbatim. Confidence High.
- **In-cluster DNS [3] cited as a pointer, not re-quoted.** The `<service>.<namespace>.svc.cluster.local`
  naming is stable, documented k8s behaviour; this guide states it from that knowledge rather than a
  fetched quote. Verify on the DNS page if you want the literal form.
- **The mcp-http inbound-auth surface is asserted from the docs' silence + the MCP spec.** `docs/aiintegration.md`
  documents only the *downstream* `LIGHTHOUSE_API_KEY`; it does **not** describe an inbound auth option for
  the HTTP server. The guide therefore treats "no built-in inbound auth → secure via exposure choice + edge
  auth" as the safe reading. **Verify against the `@letpeoplework/lighthouse-mcp-http` package README** when
  you deploy — if a newer version adds an inbound token/Origin-allowlist env var, prefer that over (or in
  addition to) the edge layer.
- **The exact `/mcp` path + `GET /health` are from the docs page, not the running image.** `docs/aiintegration.md:181-184`
  states `GET /health` and `POST /mcp`; **confirm by curling the Service directly** (§5.4) before trusting
  the Ingress path/probe — package routes can change between versions. If `/mcp` isn't the served path,
  you'll need an Ingress path rewrite.
- **Backend `ModelContextProtocol.AspNetCore` is unused at HEAD.** Confirmed by grep (no `MapMcp`/
  `AddMcpServer` in `Program.cs`). If a later release wires an *embedded* MCP server into the backend, this
  story's "deploy the separate container" framing would need revisiting — but that would be product C# →
  full nWave (planning §D3), tracked separately.
- **Image tag rots.** `mcp-http:latest` is illustrative — pin whatever
  `gh release list --repo LetPeopleWork/lighthouse-clients` (clients repo) shows when you deploy; never
  `:latest` in anything you keep.

## Full Citations

[1] Anthropic / MCP contributors. "Transports". Model Context Protocol. https://modelcontextprotocol.io/docs/concepts/transports. Accessed 2026-06-13. (Spec rev 2025-06-18; Streamable HTTP supersedes the 2024-11-05 HTTP+SSE transport.)
[2] The Kubernetes Authors. "Service". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/service/. Accessed 2026-06-13 (per-type descriptions truncated on fetch).
[3] The Kubernetes Authors. "DNS for Services and Pods". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/dns-pod-service/. Accessed 2026-06-13 (pointer).
[4] OAuth2 Proxy Authors. "OAuth2 Proxy". oauth2-proxy.github.io. https://oauth2-proxy.github.io/oauth2-proxy/. Accessed 2026-06-13 (carry-over from story 05).
[5] LetPeopleWork. Lighthouse repository — `docs/aiintegration.md` (lines 23, 52, 181-184, 199-213: the `ghcr.io/letpeoplework/lighthouse-clients/mcp-http` image, its `HOST`/`PORT`/`LIGHTHOUSE_URL`/`LIGHTHOUSE_API_KEY` env, and the `GET /health` + `POST /mcp` endpoints), `Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj` (line 42: `ModelContextProtocol.AspNetCore` 1.4.0, unused), `Lighthouse.Backend/Lighthouse.Backend/Program.cs` (lines 612-617, 614, 636: `X-Api-Key` smart-auth + `/api` 401; no `MapMcp` anywhere), `Models/OptionalFeatures/OptionalFeatureKeys.cs` (line 9: `McpServerKey`), `Services/Implementation/Seeding/OptionalFeatureSeeder.cs` (line 33: seeded flag). Accessed 2026-06-13.
