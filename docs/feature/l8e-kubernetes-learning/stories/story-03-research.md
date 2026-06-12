> nw-research reading guide for story #5193 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 03 — Ingress & TLS (Reading Guide)

**Date**: 2026-06-12 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 8 primary (6 official-doc + repo + mkcert README)
**Doc currency**: k8s concept pages are evergreen-per-version (current nav tracks v1.36.x); cert-manager latest is **v1.20.2** at write time (tags rot — §Knowledge Gaps); image/routing refs grounded in this repo's `Dockerfile` and `Program.cs` at HEAD.

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the Ingress, the hand-made TLS Secret,
> the cert-manager Issuers/Certificate) is throwaway learning scaffolding. Keep it in a personal
> scratch dir, e.g. `~/learn-k8s/story-03/`. **Nothing from this story goes into the Lighthouse
> repo** — the real manifests first land in `chart/` at **story 09** (per planning §7 workspace map:
> stories 00–08 = scratch, "personal dir e.g. `~/learn-k8s/story-NN/` — throwaway, never
> committed"). You're exposing the *real* Lighthouse image at a real hostname, but the YAML you write
> here is rehearsal, not product.

## 1. Orientation

Story 01 ran the app behind a **ClusterIP** Service that's only reachable inside the cluster, so to
eyeball it you bolted on a throwaway **NodePort** (port `30080`) — a viewing hatch, explicitly *not*
the real exposure story. Story 02 gave Postgres a PVC so its data outlives any pod. Story 03 deletes
the NodePort hack and replaces it with the real thing: a **hostname** (`lighthouse.local`, mapped via
`/etc/hosts`) fronted by an **Ingress**, with **TLS terminated at the Ingress**.

Two lessons braid together. First, **Ingress routing**: an Ingress is an HTTP(S) router that maps a
hostname (and optionally a path) to a Service — but it does *nothing* on its own; a separate **Ingress
controller** (on k3s: **Traefik**, shipped and running by default) is what actually fulfils it.
Second, **managed TLS**: you'll do it the hard way first — hand-make a self-signed cert, stuff it into
a `kubernetes.io/tls` Secret, and reference that Secret from the Ingress's `tls:` block — then install
**cert-manager** and let it *issue and renew* that cert for you automatically via a **ClusterIssuer +
Certificate**.

"Done" feels like: from a blank prompt, write an Ingress that routes `https://lighthouse.local` to the
`lighthouse` Service, first with a hand-rolled cert in a TLS Secret, then with cert-manager
issuing/renewing that cert on its own — and explain, unaided, **host-vs-path routing** and **how
cert-manager's issue/renew loop works**.

This story reuses story 02's backing app: assume the `lighthouse` Service (and its Postgres) from
story 02 already exist. The **new** manifests here are just the Ingress, the manual TLS Secret, and
then the cert-manager Issuer/ClusterIssuer + Certificate. Don't re-teach Deployments.

## 2. Concepts

### Ingress + IngressClass (the HTTP router, and why it's inert without a controller)

**What it is.** An Ingress is a routing rule object: it maps external HTTP(S) requests — by host and
path — to in-cluster Services. It is *declarative routing only*; the thing that turns those rules into
real traffic handling is a separate **Ingress controller**.

> "An API object that manages external access to the services in a cluster, typically HTTP." — kubernetes.io [1]
> "Ingress exposes HTTP and HTTPS routes from outside the cluster to services within the cluster. Traffic routing is controlled by rules defined on the Ingress resource." — kubernetes.io [1]

The single most important fact — the Ingress is **inert without a controller**:

> "You must have an Ingress controller to satisfy an Ingress. Only creating an Ingress resource has no effect." — kubernetes.io [1]
> "An Ingress controller is responsible for fulfilling the Ingress, usually with a load balancer, though it may also configure your edge router or additional frontends to help handle the traffic." — kubernetes.io [1]

**Rules carry an optional host and a list of paths** — this is the host-vs-path mechanism:

> "Each HTTP rule contains the following information: An optional host. In this example, no host is specified, so the rule applies to all inbound HTTP traffic through the IP address specified. If a host is provided (for example, foo.bar.com), the rules apply to that host. A list of paths (for example, /testpath), each of which has an associated backend defined with a service.name and a service.port.name or service.port.number." — kubernetes.io [1]
> ```yaml
> spec:
>   ingressClassName: nginx-example
>   rules:
>   - http:
>       paths:
>       - path: /testpath
>         pathType: Prefix
>         backend:
>           service:
>             name: test
>             port:
>               number: 80
> ```
> — kubernetes.io [1]

**Which controller picks up the Ingress** is decided by `ingressClassName` (→ an `IngressClass`
object), with a fallback to the cluster's *default* IngressClass:

> "If the `ingressClassName` is omitted, a default Ingress class should be defined." — kubernetes.io [1]
> "Even if you use an ingress controller that is able to operate without any IngressClass, the Kubernetes project still recommends that you define a default IngressClass." — kubernetes.io [1]

**On k3s specifically:** k3s ships **Traefik** and marks it the **default IngressClass**, so a bare
Ingress with **no** `ingressClassName` is handled by Traefik automatically (confirm with `kubectl get
ingressclass` — Traefik should be marked `(default)`). Being explicit (`ingressClassName: traefik`) is
good hygiene anyway: it documents intent and survives a cluster where the default differs. (If you'd
*disabled* Traefik you'd install ingress-nginx instead and name its class — an aside, not this story.)

- Read: Ingress — https://kubernetes.io/docs/concepts/services-networking/ingress/ [1]

> **Note — Ingress is frozen; the Gateway API is its successor.** The Kubernetes project now
> recommends the **Gateway API** over Ingress, and the Ingress API has been **frozen**: it's GA and
> stable (subject to GA stability guarantees, no plans to remove it), but it gets **no new features**.
> "Frozen" means *stable, not dying* — Ingress is a completely valid thing to learn and run, and it's
> still what most clusters, tutorials, and **Helm charts** (including the one Lighthouse ships at
> story 09) use today. The mental model transfers cleanly to Gateway: `IngressClass` → `GatewayClass`,
> the Ingress + its host/path rules → `Gateway` + `HTTPRoute`, and the cert-manager TLS-Secret loop is
> *identical* (cert-manager has a Gateway shim with the same annotation pattern). So learning Ingress
> here is the on-ramp, not wasted motion. On k3s the bundled Traefik (v3) backs both, but the Gateway
> API CRDs aren't enabled by default — Ingress is the lower-friction path for this fundamentals story.
> **The real Ingress-vs-Gateway decision belongs later**, where it pays off: the public Helm chart
> (**story 09**) and the SaaS per-tenant routing + wildcard TLS (**stories 11–12**). See planning §5
> (revisable hypotheses).

You should be able to answer:
- Why does creating an Ingress resource alone do nothing, and what object actually makes it work? On k3s, which controller is that and how do you confirm it's the default?
- In a rule, what's the difference between the `host` field and a `path` entry — which one selects by hostname and which by URL prefix?
- `ingressClassName` is omitted. What decides which controller handles the Ingress, and why is naming the class explicitly still good hygiene?
- Ingress is "frozen" — what does that precisely mean for whether you should still learn/use it, and where in this epic does the Ingress-vs-Gateway-API choice actually get decided?

### Host-based vs path-based routing (and why embedded Lighthouse needs only host-based)

**What it is.** Two axes of an Ingress rule. **Host-based**: route by the `Host:` header
(`lighthouse.local` → some Service). **Path-based**: within a host, route by URL prefix (`/api` → one
Service, `/` → another). You combine them as needed.

The crux for *this* story is Lighthouse's topology. In **embedded mode** (planning §D4/Q4 — the
default through Bands A–C) there is **ONE workload and ONE Service** (`lighthouse:80`) serving
*everything*: the SPA and the API come out of the same backend process (`UseSpa`, §3). So host-based
routing to that single Service is *all you need* — no path rules, because there's nothing to split to.

**Path-based** routing only becomes necessary in the future **split** topology (planning Q4, Band D),
where a separate nginx Deployment serves the SPA and the backend serves only the API. And that split
is *clean* precisely because of Lighthouse's URL map (§3): the SignalR hub is mounted **under `/api`**,
so `/api` → backend (REST **and** the WebSocket hub) and `/` → the nginx frontend, with no overlap. So
in this story you **DO host-based** and **EXPLAIN path-based** as the split-mode future.

WebSocket note for the debug reflex: SignalR rides WebSockets *through* the Ingress; **Traefik proxies
WebSockets by default** — no special annotation for basic single-replica use. Sticky-session /
multi-replica concerns are deliberately **out of scope** here (`replicas: 1`); that's **story 07**
(SignalR scaling — `sessionAffinity` band-aid, Redis backplane real fix, planning §D3). Don't
rat-hole on it now.

- Read: Ingress (rules / host / path) — https://kubernetes.io/docs/concepts/services-networking/ingress/ [1]

You should be able to answer:
- In embedded mode, why is a single host-based rule to `lighthouse:80` sufficient — what makes path-based routing unnecessary here?
- When *does* path-based routing become necessary, and why does SignalR living under `/api` make the eventual `/api`→backend, `/`→frontend split clean?
- Which story owns the SignalR-behind-multiple-replicas problem, and why is it explicitly not this one?

### TLS termination at the Ingress (`tls:` → a `kubernetes.io/tls` Secret)

**What it is.** You secure an Ingress by pointing its `tls:` block at a Secret that holds a cert +
private key. The Ingress controller terminates TLS at the edge — the connection from controller to the
backend pod is plain HTTP (which is why §3 targets the Service on port 80, not 443).

The Secret must be of type **`kubernetes.io/tls`** with keys **`tls.crt`** and **`tls.key`**:

> "The kubernetes.io/tls Secret type is for storing a certificate and its associated key that are typically used for TLS. One common use for TLS Secrets is to configure encryption in transit for an Ingress, but you can also use it with other resources or directly in your workload." — kubernetes.io [4]
> "When using this type of Secret, the tls.key and the tls.crt key must be provided in the data (or stringData) field of the Secret configuration." — kubernetes.io [4]

The Ingress assumes **TLS terminates at the ingress point**, on the single port 443, with multiple
hosts multiplexed by SNI:

> "The Ingress resource only supports a single TLS port, 443, and assumes TLS termination at the ingress point. If the TLS configuration section in an Ingress specifies different hosts, they are multiplexed on the same port according to the hostname specified through the SNI TLS extension." — kubernetes.io [1]

You make the Secret either by hand from a cert/key pair —

> "Create a TLS secret from the given public/private key pair." — kubernetes.io (`kubectl create secret tls`) [6]

— (story 03's "manual first" step), or you let cert-manager populate it (the cert-manager half below).

- Read: Secret types / `kubernetes.io/tls` — https://kubernetes.io/docs/concepts/configuration/secret/ [4]
- Read: Ingress TLS section — https://kubernetes.io/docs/concepts/services-networking/ingress/ [1]

You should be able to answer:
- What two keys must a `kubernetes.io/tls` Secret contain, and what type must it be?
- "TLS terminates at the Ingress" — what does that mean for the protocol between the controller and the pod, and why does the Ingress target the Service on port 80 here?
- How does one Ingress serve TLS for several hostnames on a single port 443?

### cert-manager: Issuer vs ClusterIssuer, and the Certificate→Secret→renewal loop

**What it is.** cert-manager automates the manual cert step. An **Issuer**/**ClusterIssuer**
represents a CA that can sign certs; a **Certificate** resource declares *what* cert you want; and
cert-manager keeps the resulting Secret populated and **auto-renewed**.

> "`Issuers`, and `ClusterIssuers`, are Kubernetes resources that represent certificate authorities (CAs) that are able to generate signed certificates by honoring certificate signing requests." — cert-manager.io [5]

**Issuer vs ClusterIssuer = scope** (namespaced vs cluster-wide):

> "An `Issuer` is a namespaced resource, and it is not possible to issue certificates from an `Issuer` in a different namespace." — cert-manager.io [5]
> "If you want to create a single `Issuer` that can be consumed in multiple namespaces, you should consider creating a `ClusterIssuer` resource. This is almost identical to the `Issuer` resource, however is non-namespaced so it can be used to issue `Certificates` across all namespaces." — cert-manager.io [5]

The **Certificate** is the request, and cert-manager stores the result in a Secret and renews it:

> "In cert-manager, the `Certificate` resource represents a human readable definition of a certificate request." — cert-manager.io [7]
> "The signed certificate and private key are then stored in the specified `Secret` resource." — cert-manager.io [7]
> "cert-manager will ensure that the certificate is auto-renewed before it expires and re-issued if requested." — cert-manager.io [7]

The mental model: **Certificate (what I want) → Issuer/ClusterIssuer (who signs) → Secret (where the
result lands, kept fresh).** That Secret is the same `kubernetes.io/tls` Secret the Ingress's `tls:`
block references — so once cert-manager owns it, the Ingress just keeps serving whatever cert-manager
keeps current.

- Read: Issuer / ClusterIssuer — https://cert-manager.io/docs/concepts/issuer/ [5]
- Read: Certificate — https://cert-manager.io/docs/concepts/certificate/ [7]

You should be able to answer:
- Issuer vs ClusterIssuer — what's the one-word difference (scope), and when do you reach for each?
- Trace the loop: which resource says *what cert I want*, which says *who signs it*, and where does the issued cert land — and who keeps it from expiring?
- The Ingress references Secret `lighthouse-tls`. How does cert-manager taking over change what's *in* that Secret without the Ingress YAML changing?

### SelfSigned / CA issuer (the LOCAL issuer path — ACME is story 12)

**What it is.** Two issuer *types* that need no public DNS, so they fit local learning. **SelfSigned**:
certs sign themselves with a given key — useful to bootstrap a root CA. **CA**: signs with a CA
keypair you store in a Secret.

> "The `SelfSigned` issuer doesn't represent a certificate authority as such, but instead denotes that certificates will 'sign themselves' using a given private key." — cert-manager.io [8]
> "useful for bootstrapping a root certificate for a custom PKI (Public Key Infrastructure), or for otherwise creating simple ad-hoc certificates for a quick test." — cert-manager.io [8]
> "The CA issuer represents a Certificate Authority whose certificate and private key are stored inside the cluster as a Kubernetes `Secret`." — cert-manager.io [9]

**The reconciliation you must get right (planning §D2):** local TLS uses **SelfSigned/CA (or mkcert)**,
**NOT ACME**. The **ACME DNS-01 wildcard** issuer (Let's Encrypt for `*.lighthouse.letpeoplework.com`)
is the **PROD** path and is deferred to **story 12** — it can't work locally because there's no public
DNS to solve a DNS-01 challenge against. So in this story: ACME is *not used*, full stop.

**The mkcert-as-CA bridge.** A bare SelfSigned/CA issuer gives you *auto-renewal* but the browser still
distrusts the cert (the CA isn't in your trust store). The elegant local move: generate a CA with
**mkcert** (whose root is installed into your OS/browser trust store), hand that mkcert root CA to a
**CA ClusterIssuer** as its signing Secret, and let cert-manager issue `lighthouse.local` certs from
it. Now you get **both** browser trust (mkcert root is trusted) **and** auto-renewal (cert-manager owns
the issuing loop). That's the bridge between "manual self-signed" and "fully managed."

- Read: SelfSigned issuer — https://cert-manager.io/docs/configuration/selfsigned/ [8]
- Read: CA issuer — https://cert-manager.io/docs/configuration/ca/ [9]

You should be able to answer:
- Why does local TLS use SelfSigned/CA and not ACME — what does ACME DNS-01 need that a local machine lacks, and which story brings it?
- What does a SelfSigned issuer give you, and how is a CA issuer different (where does its signing keypair live)?
- The mkcert-as-CA bridge: what does it buy you that a plain SelfSigned ClusterIssuer does not?

### Ingress ↔ cert-manager integration (ingress-shim annotation)

**What it is.** The shortcut. Instead of hand-writing a Certificate resource, annotate the Ingress and
cert-manager's **ingress-shim** auto-creates one for the hosts in your `tls:` block.

> "ingress-shim watches `Ingress` resources across your cluster. If it observes an `Ingress` with annotations described in the Supported Annotations section, it will ensure a `Certificate` resource with the name provided in the `tls.secretName` field and configured as described on the `Ingress` exists in the `Ingress`'s namespace." — cert-manager.io [10]
> "`cert-manager.io/cluster-issuer`: the name of a `cert-manager.io` `ClusterIssuer` to acquire the certificate required for this `Ingress`." — cert-manager.io [10]

So adding `cert-manager.io/cluster-issuer: <name>` to the Ingress is functionally equivalent to writing
the Certificate yourself — fewer moving parts, and the Secret named in `tls.secretName` is what
cert-manager fills.

- Read: cert-manager Ingress usage / ingress-shim — https://cert-manager.io/docs/usage/ingress/ [10]

You should be able to answer:
- What does the `cert-manager.io/cluster-issuer` annotation on an Ingress cause cert-manager to create, and in which namespace?
- When would you prefer the explicit `Certificate` resource over the annotation, and vice-versa?

## 3. Repo-grounded facts (point the Ingress at the REAL Lighthouse — from the source, not a guess)

All cited from this repo at HEAD — verify before you copy. **This grounds what the Ingress backend
actually is and why embedded mode needs only host-based routing.**

- **Container ports.** `Dockerfile:4` `EXPOSE 80 443`; Kestrel listens `http://+:80`
  (`Dockerfile:66`) and `https://+:443` (`Dockerfile:67`). For this story TLS is terminated at the
  **Ingress**, not the pod — so cluster-internally you target the `lighthouse` Service on **port 80**
  (plain http). The pod's 443 is irrelevant here.
- **Embedded frontend — ONE workload, ONE Service serves everything.** The SPA is served by the *same*
  backend process via `app.UseSpa(...)` (`Program.cs:229-233`, `SourcePath = "wwwroot"`,
  `DefaultPage = "/index.html"`). There is no second frontend Deployment — this is
  `frontend.mode=embedded` per planning §D4/Q4. Consequence: the Ingress routes to the single
  `lighthouse:80` Service, host-based, no path split.
- **The routing map of that single service** (the crux of the host-vs-path lesson):
  - `/` and all non-API paths → the SPA (the `UseSpa` fallback, `Program.cs:229`).
  - `/api/*` → REST controllers (`app.MapControllers()`, `Program.cs:210`). The app itself branches on
    `context.Request.Path.StartsWithSegments("/api")` for auth redirect-vs-401/403
    (`Program.cs:636,648`), confirming `/api` is the API surface.
  - **SignalR hub is mounted UNDER `/api`**: `app.MapHub<UpdateNotificationHub>("api/updateNotificationHub")`
    (`Program.cs:212`). So `/api/*` carries **both** REST **and** the WebSocket hub.
  - Teaching consequence: in **embedded mode** (this story) host-based routing is all you need.
    **Path-based** only matters in the future **split** topology (planning Q4, Band D), and because
    SignalR lives under `/api`, that split is clean: `/api` → backend (REST + hub), `/` → a separate
    nginx frontend.
- **Image pin.** `ghcr.io/letpeoplework/lighthouse`; **pin a real release tag, never `:latest`**
  (story 01 §3, [2]). Latest at write time: **`v26.6.7.1`** — but the exact tag rots; discover it with
  `gh release view --repo LetPeopleWork/Lighthouse --json tagName -q .tagName`.
- **Backing app is story 02's.** Assume the `lighthouse` Deployment + Service (and Postgres) from story
  02 already exist; this story's NEW manifests are only the Ingress, the manual TLS Secret, and the
  cert-manager Issuer/ClusterIssuer + Certificate. **Don't re-teach Deployments.**

## 4. Debug reflex (carry this through the story)

Carry forward the prior stories' rule, then add the Ingress/TLS failure shapes:

- **describe→Events before the container runs; logs after** (story 01). `kubectl describe` →
  Events for *pre-run* problems; `kubectl logs` only once the container is up. New surfaces this story:
  `kubectl describe ingress`, `kubectl describe certificate`, `kubectl describe certificaterequest`,
  `kubectl get events -n cert-manager`.
- **404 / 503 from the Ingress** → the controller got the request but couldn't route it. Walk it:
  (1) does the **host rule match** the `Host:` you sent (`lighthouse.local`)? (2) is the backend
  `service.name` / `port` correct? (3) does that Service have **endpoints**
  (`kubectl get endpoints lighthouse` — empty = selector mismatch, story 01's classic bug)? A 503
  usually means "no healthy backend endpoints"; a 404 usually means "no matching host/path rule."
- **Confirm Traefik picked up the Ingress** → `kubectl get ingress` (ADDRESS should populate),
  `kubectl get ingressclass` (Traefik `(default)`), Traefik pod logs in `kube-system`.
- **Browser cert warning** → the cert chain isn't trusted or doesn't match the name. Causes: the
  self-signed/CA root isn't in your trust store (**mkcert root not installed** — `mkcert -install`), or
  the cert's **CN/SAN doesn't include `lighthouse.local`**. `curl -k` bypasses trust to isolate
  routing from trust; if `curl -k` works but the browser warns, it's a *trust* problem, not routing.
- **cert-manager not issuing** → `kubectl describe certificate lighthouse-tls` (Conditions/Events show
  why), then `kubectl describe certificaterequest`. For **SelfSigned/CA** check the **Issuer is Ready**
  and the **CA signing Secret exists**. (`kubectl describe order` / `challenge` is **ACME-only** →
  *not relevant here*, that's story 12.) Also `kubectl get events -n cert-manager`.
- **WebSocket/SignalR through the Ingress** → works by default on Traefik (no annotation needed for
  basic single-replica use). If real-time updates stall with `replicas: 1`, suspect routing/trust, not
  stickiness. **Sticky-session / multi-replica is story 07, not now** — don't rat-hole.

Mnemonic: **404/503 → describe ingress + check endpoints; cert warning → curl -k to split routing from
trust; cert not issuing → describe certificate + is the Issuer Ready?**

## 5. Hands-on — copy/paste manifests

Try each block from memory first; these are the backstop. Replace `<TAG>` with a real release tag
(§3, e.g. `v26.6.7.1`) and `<NODE_IP>` with your node's IP. Work in `~/learn-k8s/story-03/`. Assumes
the `lighthouse` Service from story 02 exists.

### 5.1 Point `lighthouse.local` at the node

The Ingress routes by `Host:`, so your machine must resolve `lighthouse.local` to the node.

```bash
# Find the node IP (single-node k3s: often 127.0.0.1 works, or use the real node IP):
kubectl get nodes -o wide        # read INTERNAL-IP  → that's <NODE_IP>
# Add the hosts entry (single-node k3s: 127.0.0.1 is usually fine):
echo "127.0.0.1 lighthouse.local" | sudo tee -a /etc/hosts
# (or:  echo "<NODE_IP> lighthouse.local" | sudo tee -a /etc/hosts )
```

> Planning §D2: local DNS = `/etc/hosts`; the wildcard `*.lighthouse.letpeoplework.com` is **story 12**.

### 5.2 Manual self-signed cert FIRST → a `kubernetes.io/tls` Secret

The hard way first, so you understand what cert-manager later automates. **mkcert** (browser-trusted)
is preferred; an `openssl` fallback follows.

```bash
# --- Option A: mkcert (preferred — its root is installed into your trust store, so the browser trusts it) ---
mkcert -install                                   # one-time: add mkcert's root CA to the OS/browser store
mkcert lighthouse.local                            # writes lighthouse.local.pem (cert) + lighthouse.local-key.pem (key)
kubectl create secret tls lighthouse-tls \
  --cert=lighthouse.local.pem \
  --key=lighthouse.local-key.pem
```

```bash
# --- Option B: openssl fallback (NOT browser-trusted → expect a warning unless you import the cert) ---
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout tls.key -out tls.crt \
  -subj "/CN=lighthouse.local" \
  -addext "subjectAltName=DNS:lighthouse.local"    # SAN MUST include the host or the browser rejects it
kubectl create secret tls lighthouse-tls --cert=tls.crt --key=tls.key
```

> The Secret is type `kubernetes.io/tls` with keys `tls.crt`/`tls.key` [4]; `kubectl create secret
> tls` builds exactly that from the pair [6].

### 5.3 The Ingress — host-based, TLS via the Secret (path-based shown but disabled)

Host `lighthouse.local`, single path `/` → Service `lighthouse:80`, `tls:` referencing the Secret.

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: lighthouse
spec:
  ingressClassName: traefik         # explicit even though k3s makes Traefik the default (good hygiene)
  tls:
    - hosts:
        - lighthouse.local          # must match the rule host below (and the cert's SAN)
      secretName: lighthouse-tls    # the kubernetes.io/tls Secret from 5.2 (later: cert-manager owns it)
  rules:
    - host: lighthouse.local        # HOST-BASED: route by Host header
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: lighthouse    # the single embedded-mode Service (story 02)
                port:
                  number: 80        # TLS terminates HERE at the Ingress; backend is plain http
# ---------------------------------------------------------------------------
# SPLIT-MODE FUTURE (planning Q4 / Band D) — NOT used in embedded mode. For when
# the SPA moves to its own nginx Deployment. SignalR lives under /api, so the split is clean:
#   rules:
#     - host: lighthouse.local
#       http:
#         paths:
#           - path: /api            # REST + the SignalR hub (api/updateNotificationHub) → backend
#             pathType: Prefix
#             backend:
#               service: { name: lighthouse-api, port: { number: 80 } }
#           - path: /               # everything else → the static nginx frontend
#             pathType: Prefix
#             backend:
#               service: { name: lighthouse-frontend, port: { number: 80 } }
# ---------------------------------------------------------------------------
```

### 5.4 Apply + verify routing and TLS

```bash
kubectl apply -f ingress.yaml
kubectl get ingress lighthouse                 # ADDRESS should populate; HOSTS=lighthouse.local
kubectl get ingressclass                       # confirm traefik is (default)
kubectl describe ingress lighthouse            # if 404/503 later: read here + check endpoints
kubectl get endpoints lighthouse               # NOT empty — else selector mismatch (story 01)

curl -k https://lighthouse.local/ | head       # -k skips trust: proves ROUTING works
curl https://lighthouse.local/ | head          # no -k: trusted IFF mkcert (5.2 Option A)
curl -k https://lighthouse.local/api/projects | head   # prove /api routes through the SAME host (embedded)
```

### 5.5 Install cert-manager (pinned)

Pin a concrete version — the tag rots; current latest is **v1.20.2** (check
https://cert-manager.io/docs/installation/ for today's tag).

```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.20.2/cert-manager.yaml
kubectl get pods -n cert-manager -w            # wait for cert-manager, -cainjector, -webhook → Running/Ready
```

### 5.6 Let cert-manager own the cert: SelfSigned→CA ClusterIssuer + Certificate

Bootstrap a CA with a SelfSigned issuer, then a CA ClusterIssuer signs `lighthouse.local`. Two ways to
wire it in — the explicit `Certificate` and the shorter ingress annotation. **The annotation is
recommended for brevity.**

```yaml
# issuers.yaml — SelfSigned bootstraps a CA; the CA ClusterIssuer then signs leaf certs.
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: selfsigned-bootstrap
spec:
  selfSigned: {}                    # signs with the cert's own key — only to mint the root CA below
---
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: local-ca
  namespace: cert-manager           # the CA cert/key Secret must live where the CA issuer reads it
spec:
  isCA: true
  commonName: lighthouse-local-ca
  secretName: local-ca-secret       # cert-manager writes the CA keypair here
  issuerRef:
    name: selfsigned-bootstrap
    kind: ClusterIssuer
---
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: local-ca-issuer
spec:
  ca:
    secretName: local-ca-secret     # signs leaf certs with the CA keypair above [9]
# BRIDGE for browser trust: instead of the SelfSigned-bootstrapped CA above, create local-ca-secret
# FROM A mkcert ROOT so the browser already trusts the chain (mkcert -install put its root in your store):
#   kubectl -n cert-manager create secret tls local-ca-secret \
#     --cert="$(mkcert -CAROOT)/rootCA.pem" --key="$(mkcert -CAROOT)/rootCA-key.pem"
# then skip selfsigned-bootstrap + the local-ca Certificate, keep only local-ca-issuer.
```

```yaml
# certificate.yaml — EXPLICIT approach: declare the cert; cert-manager fills Secret lighthouse-tls.
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: lighthouse-tls
spec:
  secretName: lighthouse-tls        # SAME Secret the Ingress tls: references — cert-manager takes it over
  dnsNames:
    - lighthouse.local
  issuerRef:
    name: local-ca-issuer
    kind: ClusterIssuer
```

```yaml
# OR — ANNOTATION approach (recommended, fewer moving parts): add to the Ingress metadata and DROP
# certificate.yaml. ingress-shim auto-creates the Certificate for the tls hosts [10].
#   metadata:
#     annotations:
#       cert-manager.io/cluster-issuer: local-ca-issuer
```

```bash
kubectl apply -f issuers.yaml
kubectl apply -f certificate.yaml          # (skip if using the annotation approach instead)
```

### 5.7 Verify cert-manager took over

```bash
kubectl get certificate                          # lighthouse-tls → READY=True
kubectl describe certificate lighthouse-tls      # Conditions/Events: Issued; if not READY, read here
kubectl get secret lighthouse-tls -o yaml | head # cert-manager (re)populated tls.crt/tls.key
# Reload https://lighthouse.local in the browser — trusted IFF the CA chain is in your store (mkcert).
```

### 5.8 Tear down

```bash
kubectl delete -f certificate.yaml -f issuers.yaml -f ingress.yaml
kubectl delete secret lighthouse-tls            # the hand-made / cert-manager TLS Secret
sudo sed -i '/lighthouse.local/d' /etc/hosts    # remove the hosts entry
# Leave cert-manager installed for later stories. To remove it later:
#   kubectl delete -f https://github.com/cert-manager/cert-manager/releases/download/v1.20.2/cert-manager.yaml
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *expose Lighthouse on a real hostname with managed TLS, and explain host-vs-path
routing and how cert-manager issues/renews certs.* Unaided, you should be able to:

- [ ] Write an Ingress from scratch routing host `lighthouse.local` to Service `lighthouse:80`, with matching `tls.hosts` and rule `host`.
- [ ] Explain why an Ingress does **nothing** without a controller, and that on k3s the controller is **Traefik** (default IngressClass — `kubectl get ingressclass`).
- [ ] Explain host-vs-path routing and **why embedded mode needs only host-based** (one Service serves SPA + API), and **when path-based is needed** — the split topology, where `/api` carries **REST + SignalR** and `/` the nginx frontend.
- [ ] Make a `kubernetes.io/tls` Secret **by hand** (mkcert or openssl, SAN = `lighthouse.local`) and wire it into the Ingress `tls:` block.
- [ ] Explain that **TLS terminates at the Ingress** — so the controller→pod hop is plain http and the Ingress targets port 80.
- [ ] Install cert-manager and explain **Issuer vs ClusterIssuer** (namespaced vs cluster scope) and the **Certificate → Issuer → Secret → auto-renewal** loop.
- [ ] Explain why local TLS uses **SelfSigned/CA, not ACME**, and which story (**12**) brings ACME DNS-01 wildcard — and why ACME can't run locally.
- [ ] Explain the **mkcert-as-CA bridge**: browser trust *and* auto-renewal at once.
- [ ] Diagnose a **404/503** (host rule? service name/port? endpoints?) and a **cert-trust warning** (`curl -k` to split routing from trust; mkcert root not installed / wrong SAN).

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick one throwaway experiment that *tests the lesson at its edges*:

> **Delete the `lighthouse-tls` Secret out from under a live Ingress — does cert-manager re-issue it?**
> With the cert-manager Certificate in place (5.6), `kubectl delete secret lighthouse-tls` and watch
> `kubectl get secret lighthouse-tls -w` and `kubectl describe certificate lighthouse-tls`. Form a
> hypothesis *before* you run it: who, if anyone, recreates that Secret, and why? (This is the
> reconcile loop that makes "auto-renewed before it expires" [7] real — the Certificate is the desired
> state; the Secret is the actual state cert-manager keeps matching.) **Bonus:** point the Ingress
> backend at a **wrong service port** (e.g. `port: 8080`) and read *exactly* how Traefik reports the
> failure — is it a 404 or a 503, and what does `kubectl get endpoints lighthouse` say? That proves
> the controller→endpoints binding: a 503 means the rule matched but no healthy backend answered.

Investigate, don't look it up: delete the Secret, observe; break the port, observe. The re-issue *is*
the renewal lesson; the 503-vs-404 *is* the routing lesson.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Ingress (controller-required, rules, host/path, IngressClass, TLS-port/SNI) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed; TLS-port/SNI line via kubernetes.io search index — page body truncates before TLS) |
| 4 | Secrets (`kubernetes.io/tls`, tls.crt/tls.key) | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quotes via kubernetes.io search index; page body truncated before TLS-Secrets section) |
| 5 | Issuer / ClusterIssuer (scope) | cert-manager.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| 6 | kubectl create secret tls | kubernetes.io | High (1.0) | Official | 2026-06-12 | Y (quote confirmed) |
| 7 | Certificate (request → Secret → renewal) | cert-manager.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| 8 | SelfSigned issuer | cert-manager.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| 9 | CA issuer | cert-manager.io | High (1.0) | Official | 2026-06-12 | Y (quote confirmed) |
| 10 | cert-manager Ingress usage (ingress-shim, cluster-issuer annotation) | cert-manager.io | High (1.0) | Official | 2026-06-12 | Y (quotes confirmed) |
| 3 | k3s networking services (Traefik deployed by default) | docs.k3s.io | High (1.0) | Official | 2026-06-12 | Y (quote confirmed) |
| 2 | mkcert (local CA, `-install`, trusted root) | github.com/FiloSottile/mkcert | Medium-High (0.8) | Official project README | 2026-06-12 | Referenced (well-known tool; commands standard) |
| — | Lighthouse repo (Dockerfile, Program.cs) | this repo | Primary | First-party source | 2026-06-12 | Y (line-cited at HEAD) |

Primary sources: **8** (6 official k8s/cert-manager doc pages [1,4,5,6,7,8,9,10] across kubernetes.io
and cert-manager.io counted as the doc set, plus the k3s page [3], plus the line-cited Lighthouse
repo). All doc sources are first-party project documentation (High tier). mkcert [2] is its official
GitHub README (Medium-High — community tool, not a standards body). Repo facts are line-cited from this
repository's own source files — the strongest authority for what the image and routing actually do.

## Knowledge Gaps

- **k8s Ingress/Secret TLS quotes verified via the search index, not a clean page fetch.** Both
  kubernetes.io pages [1][4] are long enough that the fetched body truncates *before* the TLS sections.
  The verbatim TLS-Secret keys (`tls.crt`/`tls.key`, type `kubernetes.io/tls`) and the "single TLS port,
  443 … TLS termination at the ingress point … SNI" sentence were confirmed as literal strings returned
  by the kubernetes.io search index (and corroborated by the `kubectl create secret tls` reference page
  [6], fetched cleanly). Confidence High, but if you want to eyeball them in-page, open [1]/[4] and
  scroll to the "TLS" / "TLS secrets" subsections.
- **Default-IngressClass annotation not quoted verbatim.** The page [1] confirms the *default
  IngressClass* concept verbatim ("If the `ingressClassName` is omitted, a default Ingress class should
  be defined"), but the specific annotation `ingressclass.kubernetes.io/is-default-class` wasn't on the
  fetched excerpt — described by behaviour instead (k3s marks Traefik `(default)`; confirm with
  `kubectl get ingressclass`). Don't invent the annotation string; verify in-page if you need it.
- **k3s "default *ingress controller*" phrasing.** [3] confirms verbatim "Traefik is deployed by default
  when starting the server" and that it's a HelmChart addon, but doesn't use the exact words "default
  ingress controller." The IngressClass-default behaviour is the operational truth; verify locally with
  `kubectl get ingressclass`.
- **cert-manager release tag rots.** **v1.20.2** is latest at write time; pin whatever
  https://cert-manager.io/docs/installation/ shows at install time. Same caveat as the Lighthouse image
  tag (`v26.6.7.1` now — discover with `gh release view …`).
- **`$(VAR)` / SAN-vs-CN browser nuances are environment-dependent.** Modern browsers validate the SAN,
  not the CN — the openssl block (5.2 Option B) sets `subjectAltName` for that reason; a cert with only
  a CN may still warn. Verify on first run; mkcert (Option A) sidesteps this by setting SANs and trust
  for you.
- **k3s Traefik chart version not pinned here.** k3s bundles a specific Traefik version per k3s release;
  this doc treats "Traefik is present and default" as the only fact that matters. If a Traefik-specific
  annotation behaves oddly, check the bundled version via the `traefik` HelmChart in `kube-system`.

## Full Citations

[1] The Kubernetes Authors. "Ingress". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/ingress/. Accessed 2026-06-12.
[2] FiloSottile. "mkcert — a simple zero-config tool to make locally trusted development certificates". GitHub. https://github.com/FiloSottile/mkcert. Accessed 2026-06-12.
[3] The k3s Authors / SUSE. "Networking Services". docs.k3s.io. https://docs.k3s.io/networking/networking-services. Accessed 2026-06-12.
[4] The Kubernetes Authors. "Secrets". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/secret/. Accessed 2026-06-12.
[5] The cert-manager Authors. "Issuer". cert-manager.io. https://cert-manager.io/docs/concepts/issuer/. Accessed 2026-06-12.
[6] The Kubernetes Authors. "kubectl create secret tls". kubernetes.io. https://kubernetes.io/docs/reference/kubectl/generated/kubectl_create/kubectl_create_secret_tls/. Accessed 2026-06-12.
[7] The cert-manager Authors. "Certificate". cert-manager.io. https://cert-manager.io/docs/concepts/certificate/. Accessed 2026-06-12.
[8] The cert-manager Authors. "SelfSigned". cert-manager.io. https://cert-manager.io/docs/configuration/selfsigned/. Accessed 2026-06-12.
[9] The cert-manager Authors. "CA". cert-manager.io. https://cert-manager.io/docs/configuration/ca/. Accessed 2026-06-12.
[10] The cert-manager Authors. "Securing Ingress Resources". cert-manager.io. https://cert-manager.io/docs/usage/ingress/. Accessed 2026-06-12.
[11] LetPeopleWork. Lighthouse repository — `Dockerfile` (lines 4, 66-67), `Lighthouse.Backend/Lighthouse.Backend/Program.cs` (lines 210, 212, 229-233, 636, 648). Accessed 2026-06-12.
