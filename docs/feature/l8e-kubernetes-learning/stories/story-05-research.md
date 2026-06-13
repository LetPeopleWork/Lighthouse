> nw-research reading guide for story #5195 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 05 — Auth & RBAC (Reading Guide)

**Date**: 2026-06-13 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 8 primary (6 official-doc pages + oauth2-proxy + repo)
**Doc currency**: k8s concept pages are evergreen-per-version (current nav tracks v1.36.x); the OIDC/RBAC/ServiceAccount/Secret/ConfigMap mechanics are stable API. All repo facts (the *already-implemented* OIDC + app-RBAC wiring) are line-cited from this repo's `Program.cs`, `appsettings.json`, and the `API/` controllers at HEAD. The big realisation of this story is grounded in the repo, not the k8s docs — read §3 before §5.

> **🗂 Workspace: SCRATCH — not the repo.** Everything you write here (the Keycloak Deployment, the
> Secret/ConfigMap holding the OIDC config, the ServiceAccounts, the Role/RoleBinding, the oauth2-proxy
> Deployment + Ingress annotations) is throwaway learning scaffolding. Keep it in a personal scratch dir,
> e.g. `~/learn-k8s/story-05/`. **Nothing from this story goes into the Lighthouse repo** — the real
> manifests first land in `chart/` at **story 09** (planning §7 workspace map: stories 00–08 = scratch,
> "throwaway, never committed"). You are *configuring* the real Lighthouse image's auth, but the YAML is
> rehearsal, not product. The production OIDC values (Entra, real secrets) are a story 11–12 / D2 concern.

## 1. Orientation

The word **"auth"** is about to mean **four different things** in the same cluster, and the entire point
of this story is to stop conflating them. By the time you're done you should be able to point at any
manifest and say *which* auth it is.

1. **App-level OIDC** — Lighthouse logging a human in against an identity provider (Keycloak here, Entra
   in prod). **This already exists in the C#** (`Program.cs:658-680`, §3). Story 05 doesn't *write* it —
   it **configures** it, by feeding `Authentication__*` settings into the pod via a **ConfigMap**
   (non-secret: Authority, ClientId) and a **Secret** (the ClientSecret). That ConfigMap-vs-Secret split
   *is* the "configure OIDC via Secrets and ConfigMaps" deliverable.
2. **App-level RBAC** — Lighthouse's *own* role model (system-admin / team / portfolio scopes), driven by
   the OIDC **`groups`** claim and served through `IRbacAdministrationService`. **Also already coded**
   (`Authorization` config block + `WriteGroupSnapshotOnTokenValidatedAsync`, §3). This is **not** k8s
   RBAC — it's application authorization that happens to be called "RBAC" too.
3. **k8s RBAC** — `Role` / `RoleBinding` / `ClusterRole` / `ClusterRoleBinding` + `ServiceAccount`. This
   governs **what a pod's token may do against the Kubernetes API** — create pods, read secrets, list
   nodes. It has **nothing to do** with who can log into Lighthouse. The least-privilege punchline (§3):
   **Lighthouse never calls the k8s API**, so the correct k8s-RBAC posture is *no permissions at all* —
   a dedicated ServiceAccount with `automountServiceAccountToken: false` and **no** RoleBinding.
4. **Edge auth (oauth2-proxy)** — a reverse proxy that does OIDC **at the Ingress**, before the request
   ever reaches the pod. The deliverable asks you to stand it up — but be honest (planning hypothesis #3):
   because Lighthouse **already** enforces OIDC itself, oauth2-proxy here is **partly redundant**. Its
   real value is protecting things that *don't* have built-in auth (a dashboard, Postgres' admin UI, a
   sidecar) — you learn the pattern on Lighthouse, then know when it actually earns its place.

And one **red herring** to name and discard: OIDC can *also* authenticate **humans to the kube-apiserver**
(so `kubectl` trusts your Keycloak login). That's a *third* use of the letters "OIDC", configured on the
API server, and it is **out of scope** — don't let "OIDC" + "k8s" trick you into thinking story 05 wires
your IdP into `kubectl`. It doesn't.

"Done" feels like: deploy Keycloak in-cluster, **configure** Lighthouse's existing OIDC via a ConfigMap +
Secret and log in through the browser end-to-end; create least-privilege ServiceAccounts for the
Lighthouse and Postgres pods and **explain why they need no RoleBinding**; write a namespaced Role +
RoleBinding (for a pod that *would* need API access, as the teaching exercise); stand oauth2-proxy in
front of the Ingress and verify `browser → Ingress → oauth2-proxy → Lighthouse`; and — unaided —
**place each of the four "auths" above** and say what breaks if it's missing or misconfigured.

This story builds on story 02's `lighthouse` + `lighthouse-postgres` workload and story 03's Ingress/TLS
(`lighthouse.local` over HTTPS). Don't re-teach those.

## 2. Concepts

### ServiceAccount (the pod's identity to the k8s API — not the user's identity)

**What it is.** A non-human account that gives a pod an identity *inside the cluster*, used when the pod
talks to the Kubernetes API.

> "A service account is a type of non-human account that, in Kubernetes, provides a distinct identity in a Kubernetes cluster. Application Pods, system components, and entities inside and outside the cluster can use a specific ServiceAccount's credentials to identify as that ServiceAccount." — kubernetes.io [1]

**The distinction that this whole story turns on — machine identity vs human identity:**

> "Service accounts are different from user accounts, which are authenticated human users in the cluster. By default, user accounts don't exist in the Kubernetes API server; instead, the API server treats user identities as opaque data." — kubernetes.io [1]

So a **ServiceAccount** = a *pod's* identity to the *k8s API*. A **user account** = a *human's* identity
to the *k8s API* (how *you* are `kubectl`-authenticated). **Neither** is the Keycloak login a Lighthouse
*end-user* does — that's app-level OIDC (§1.1), a fourth thing the k8s API never sees.

**Every pod gets one automatically — and that's the risk:**

> "Every namespace gets a `default` ServiceAccount upon creation." — kubernetes.io [1]
> "If you deploy a Pod in a namespace, and you don't manually assign a ServiceAccount to the Pod, Kubernetes assigns the `default` ServiceAccount for that namespace to the Pod." — kubernetes.io [1]

By default that account's **token is mounted into the pod** at `/var/run/secrets/kubernetes.io/serviceaccount/`.
A compromised app could use it to talk to the API. Least privilege says: if the app doesn't need the API,
**don't mount the token**:

> "To prevent Kubernetes from automatically injecting credentials for a specified ServiceAccount or the `default` ServiceAccount, set the `automountServiceAccountToken` field in your Pod specification to `false`." — kubernetes.io [1]

- Read: ServiceAccounts — https://kubernetes.io/docs/concepts/security/service-accounts/ [1]

You should be able to answer:
- A ServiceAccount vs a user account vs a Lighthouse end-user's Keycloak login — which two does the **k8s API** know about, and which does it never see?
- What does *every* pod get if you don't assign a ServiceAccount, and what gets mounted into it by default?
- Lighthouse never calls the k8s API. What is the least-privilege ServiceAccount for it — and which one field makes it least-privilege?

### Role, ClusterRole, RoleBinding, ClusterRoleBinding (k8s RBAC — additive, namespaced vs cluster)

**What it is.** k8s' own authorization: a *Role* is a set of allowed verbs on resources; a *binding*
grants that Role to a subject (a user, group, or **ServiceAccount**).

> "Role-based access control (RBAC) is a method of regulating access to computer or network resources based on the roles of individual users within your organization. RBAC authorization uses the `rbac.authorization.k8s.io` API group to drive authorization decisions, allowing you to dynamically configure policies through the Kubernetes API." — kubernetes.io [2]

**Role vs ClusterRole — the namespaced/cluster split (mirrors Issuer-vs-ClusterIssuer from story 03):**

> "A Role always sets permissions within a particular namespace; when you create a Role, you have to specify the namespace it belongs in. ClusterRole, by contrast, is a non-namespaced resource." — kubernetes.io [2]

**RoleBinding vs ClusterRoleBinding — who gets the permission, and how wide:**

> "A role binding grants the permissions defined in a role to a user or set of users. It holds a list of _subjects_ (users, groups, or service accounts), and a reference to the role being granted. A RoleBinding grants permissions within a specific namespace whereas a ClusterRoleBinding grants that access cluster-wide." — kubernetes.io [2]

**The safety property that makes least-privilege the *default* posture — RBAC only ever adds:**

> "Permissions are purely additive (there are no \"deny\" rules)." — kubernetes.io [2]

No deny rules means: a ServiceAccount with **no** binding can do **nothing** to the API. You don't lock it
down by denying — you lock it down by *granting nothing*. That's why story 05's Lighthouse SA needs no
RoleBinding at all.

- Read: Using RBAC Authorization — https://kubernetes.io/docs/reference/access-authn-authz/rbac/ [2]

You should be able to answer:
- Role vs ClusterRole — which is namespaced, which is cluster-wide? Same question for RoleBinding vs ClusterRoleBinding.
- "Permissions are purely additive (there are no deny rules)" — given that, how do you make a ServiceAccount least-privilege? (Hint: it's about what you *don't* write.)
- A binding's subject can be a user, a group, **or a ServiceAccount** — which of those is the pod, and which is the human at `kubectl`?

### Secret vs ConfigMap (where the OIDC config actually lives — and the encryption caveat)

**What they are.** Two ways to inject config into a pod, split by sensitivity.

> "A ConfigMap is an API object used to store non-confidential data in key-value pairs." — kubernetes.io [4]
> "A Secret is an object that contains a small amount of sensitive data such as a password, a token, or a key." — kubernetes.io [3]
> "Secrets are similar to ConfigMaps but are specifically intended to hold confidential data." — kubernetes.io [3]

**The split for *this* story's OIDC config:** `Authority`, `ClientId`, `Enabled`, `GroupClaimName` are
**non-secret → ConfigMap**; the **`ClientSecret` → Secret**. The docs are explicit that a ConfigMap is the
wrong home for the secret:

> "ConfigMap does not provide secrecy or encryption. If the data you want to store are confidential, use a Secret rather than a ConfigMap, or use additional (third party) tools to keep your data private." — kubernetes.io [4]

**The caveat that keeps you honest — a k8s Secret is NOT encrypted by default (it's base64, which is not encryption):**

> "Kubernetes Secrets are, by default, stored unencrypted in the API server's underlying data store (etcd). Anyone with API access can retrieve or modify a Secret, and so can anyone with access to etcd." — kubernetes.io [3]

So a Secret is *better* than a ConfigMap for the ClientSecret (intent, separate RBAC surface, not printed
in `describe`), but it is **not** a vault. Real protection needs encryption-at-rest + RBAC on the Secret
(both noted in [3]) — a productization (story 11–12) concern, not something you solve locally.

**Both are consumed the same handful of ways:**

> "Pods can consume ConfigMaps as environment variables, command-line arguments, or as configuration files in a volume." — kubernetes.io [4]

For Lighthouse you'll inject as **environment variables**, because ASP.NET maps `Authentication__Authority`
(env) → `Authentication:Authority` (config) — see §3.

- Read: ConfigMaps — https://kubernetes.io/docs/concepts/configuration/configmap/ [4]
- Read: Secrets — https://kubernetes.io/docs/concepts/configuration/secret/ [3]

You should be able to answer:
- Which OIDC settings go in a ConfigMap and which in a Secret — and *why* the ClientSecret cannot live in the ConfigMap?
- Is a k8s Secret encrypted at rest by default? What is base64, and what would you actually need to protect it?
- Three ways a pod can consume a ConfigMap/Secret — which one will you use for Lighthouse, and why (think `__`)?

### oauth2-proxy (edge auth — authenticate at the Ingress, before the pod)

**What it is.** A reverse proxy that performs the OIDC dance itself and only forwards already-authenticated
requests upstream.

> "A reverse proxy and static file server that provides authentication using Providers (Google, GitHub, and others) to validate accounts by email, domain or group." — oauth2-proxy [5]

**Where it sits.** `browser → Ingress → oauth2-proxy → Lighthouse`. The two common wirings:
- **As the upstream's gatekeeper** — Traefik/NGINX calls oauth2-proxy via an *external-auth* hook
  (`auth_request` / Traefik `ForwardAuth` middleware) on every request; a 202 lets it through, a 401
  bounces the browser to Keycloak.
- **In-line proxy** — oauth2-proxy *is* the Ingress backend and proxies authenticated traffic to
  Lighthouse's Service.

**The honest reconciliation (planning hypothesis #3).** Lighthouse **already** does OIDC against the same
Keycloak. So putting oauth2-proxy in front means **two** OIDC layers — the user could be challenged twice,
and you now maintain two client registrations. That's belt-and-suspenders. oauth2-proxy *earns its keep*
when the thing behind it has **no** auth of its own (a raw dashboard, pgAdmin, an internal tool). Learn
the pattern on Lighthouse because the demo is cheap, but record *when you'd actually reach for it* — and
note the alternative: for an app that already authenticates, you usually **don't** layer oauth2-proxy; you
just expose it. (Decision deferred to the chart/SaaS boundary, stories 09/11–12.)

- Read: oauth2-proxy — https://oauth2-proxy.github.io/oauth2-proxy/ [5]
- Read (mechanism): k8s authentication, "OpenID Connect Tokens" (the **red herring** — OIDC to the *kube-apiserver*, not the app) — https://kubernetes.io/docs/reference/access-authn-authz/authentication/#openid-connect-tokens [6]

You should be able to answer:
- Draw `browser → Ingress → oauth2-proxy → Lighthouse`. Where does the OIDC redirect to Keycloak happen, and what does oauth2-proxy forward upstream once the user is authenticated?
- Lighthouse already does OIDC. So what does oauth2-proxy add *here*, honestly — and what kind of backend would make it genuinely necessary?
- The red herring: name the *third* use of OIDC (humans → kube-apiserver) and say why story 05 is **not** about it.

## 3. Repo-grounded facts (Lighthouse ALREADY does OIDC + app-RBAC — you CONFIGURE, you don't BUILD)

All cited from this repo at HEAD — verify before you copy. **This is the section that reframes the whole
story: the C# is already written. Read it first.**

- **App-level OIDC is fully implemented.** `Program.cs:658-680` registers `.AddOpenIdConnect(...)` and
  binds every value from the `Authentication` config block: `Authority` (660), `ClientId` (661),
  `ClientSecret` (662), `code` + PKCE (663-664), `CallbackPath` (667), `SignedOutCallbackPath` (668),
  `RequireHttpsMetadata` (670), `MetadataAddress` (671), and `Scopes` (673-677). **You are not writing
  OIDC — you are filling these in.**
- **The config block + its env-var names.** `appsettings.json:49` `"Authentication"`: `Enabled` (false by
  default), `Authority` (""), `ClientId` (""), `ClientSecret` (""), `CallbackPath`
  (`/api/auth/callback`), `SignedOutCallbackPath` (`/api/auth/signout-callback`), `Scopes`
  (`["openid","profile","email"]`), `SessionLifetimeMinutes` (480). ASP.NET maps **`__` → `:`**, so the
  k8s env vars are `Authentication__Enabled`, `Authentication__Authority`, `Authentication__ClientId`,
  `Authentication__ClientSecret`, etc. (same `__` mechanic story 02 used for `Database__*`).
- **App-level RBAC is also implemented, and it is FED BY the OIDC `groups` claim.** Separate config block
  `appsettings.json` `"Authorization"`: `Enabled` (false), `EmergencySystemAdminSubjects` (`[]`),
  `GroupClaimName` (`"groups"`). On every successful login, `OnTokenValidated` →
  `WriteGroupSnapshotOnTokenValidatedAsync` (`Program.cs:679,683`) reads the stable subject (`sub`/`oid`,
  692) and the configured group claim (`GroupClaimName`, 700), parses it (`GroupClaimParser`, 706), and
  persists a snapshot via `IOidcGroupSnapshotWriter` (712; registered 949). The UI gating then derives
  from `IRbacAdministrationService` →
  `AuthorizationController` `GET /api/v1/authorization/my-summary` (`API/AuthorizationController.cs:10,24`).
  **This is the "app-level auth vs k8s RBAC" contrast made concrete — neither of these touches a k8s
  Role.**
- **Auth can be turned fully off — and that's the default.** When `Authentication:Enabled` is false (or
  Authority/ClientId are blank), Program.cs installs a `DisabledAuthenticationHandler`
  (`Program.cs:574-578`) and *skips* OIDC middleware (588-594), with `AuthModeResolver` reporting
  `Misconfigured` so the SPA shows an error page rather than crashing. So your **first** failure mode if
  the env vars are wrong is *not* a crash — it's "auth silently disabled / misconfigured". Check
  `GET /api/v1/auth/mode` (`API/AuthController.cs:14-15,26-27`, `[AllowAnonymous]`).
- **The `/api` 401 trap still applies (story 04).** The cookie scheme returns a hard **401** for
  unauthenticated `/api/*` (`Program.cs:636`) and **403** on access-denied (648). The anonymous routes you
  can safely probe through oauth2-proxy / health are `GET /api/v1/version/current` (story 04) and
  `GET /api/v1/auth/mode|login|session|me` (all `[AllowAnonymous]`, `AuthController.cs:26-90`).
- **Smart-auth means oauth2-proxy must not strip the cookie.** Requests with `X-Api-Key` route to the API
  key handler; everything else (browser sessions) routes to the cookie scheme (`Program.cs:612-617`). If
  oauth2-proxy sits in-line, it must **pass through** Lighthouse's `.Lighthouse.Session` cookie and the
  `Authorization`/`X-Api-Key` headers, or you'll break the app's own auth while "adding" auth.
- **Lighthouse does NOT call the Kubernetes API.** There is no k8s client SDK dependency and nothing reads
  the in-pod ServiceAccount token (grep the backend for `kubernetes`/`KubernetesClient` → none). Therefore
  the least-privilege k8s-RBAC posture for both the `lighthouse` and `lighthouse-postgres` pods is a
  dedicated ServiceAccount with **`automountServiceAccountToken: false` and no RoleBinding** — they need
  *zero* API access. The Role/RoleBinding you write in §5.4 is a **teaching exercise** for a hypothetical
  pod that *would* need API access, not something Lighthouse itself requires.

> **Forward hook — the production OIDC values are OUT OF SCOPE here (planning §D2/§D3).** Locally you use
> **Keycloak + HTTP/self-signed**; production is **Entra ID + real secrets + encryption-at-rest on the
> Secret**. Wiring the *real* IdP, sealing secrets (SealedSecrets/external-secrets), and the
> oauth2-proxy-or-not decision are **story 11–12 / chart** concerns. Story 05 proves the *mechanism* with
> throwaway Keycloak; it does not productionize it.

## 4. Debug reflex (carry this through the story)

Carry forward the prior rules (describe→Events before run, logs after; `/api` 401 = wrong/unauth target),
then add the auth-specific failure shapes — most of which are **not** k8s errors at all but **OIDC
mismatches**:

- **Login redirects to Keycloak then fails with `invalid issuer` / `IDX10205` / `Unable to obtain
  configuration`** → the **split-horizon issuer trap** (the #1 in-cluster OIDC bug). The browser reaches
  Keycloak at one URL (`https://keycloak.local`) but the **pod** reaches it at the in-cluster Service DNS
  (`http://keycloak.keycloak.svc:8080`). The `iss` in the token must match what `Authority` expects. Fix:
  pin Keycloak's frontend hostname (`KC_HOSTNAME`) so the issuer is **stable regardless of who asks**, and
  use `MetadataAddress` (bound at `Program.cs:671`) to point the pod's *discovery fetch* at the internal
  URL while `Authority` stays the public issuer. Read it: `kubectl logs deploy/lighthouse` shows the OIDC
  middleware exception with the expected-vs-actual issuer.
- **Browser loops back to login forever / cookie never sticks** → over HTTP, the session cookie is
  `SecurePolicy = Always` (`Program.cs:628`), so it's **dropped on plain http**. You went through story
  03 for exactly this reason — log in over **HTTPS** (`https://lighthouse.local`), not http. If
  oauth2-proxy is in-line, also confirm it's **forwarding** the `.Lighthouse.Session` cookie (§3).
- **Everyone is anonymous / "auth disabled" even though you set the env vars** → `GET /api/v1/auth/mode`
  says `Misconfigured` or disabled. Either `Authentication__Enabled` isn't `"true"`, or
  `Authority`/`ClientId` is still blank (`Program.cs:591-594` short-circuits). Check the *effective* env in
  the pod: `kubectl exec deploy/lighthouse -- env | grep -i Authentication`.
- **Secret/ConfigMap value not arriving** → `kubectl exec deploy/lighthouse -- env | grep -i Auth` shows
  nothing → the `envFrom`/`valueFrom.secretKeyRef` key name doesn't match, or the key has a `:` instead of
  `__` (env vars can't contain `:` — you MUST use `__`). `kubectl describe pod` Events show
  `CreateContainerConfigError` if a referenced Secret/key is missing.
- **`kubectl drain` / API call from the pod gets `Forbidden`** (only if you gave a pod API access in §5.4)
  → that's k8s RBAC working: the ServiceAccount has no (or insufficient) RoleBinding. `kubectl auth
  can-i <verb> <resource> --as=system:serviceaccount:<ns>:<sa>` tells you exactly what a SA may do.
- **oauth2-proxy returns 500 / redirect mismatch** → its `--redirect-url` must equal the registered
  Keycloak client redirect URI **exactly** (scheme + host + path), and its `--oidc-issuer-url` must match
  the issuer (same split-horizon rule as above). `kubectl logs deploy/oauth2-proxy`.

Mnemonic: **most "k8s auth" failures are OIDC mismatches, not RBAC** — issuer mismatch (split-horizon),
cookie dropped on http, env not `__`-mapped, or auth silently disabled. True k8s-RBAC `Forbidden` only
appears if a pod actually calls the API — and Lighthouse doesn't. Verify a SA's powers with
`kubectl auth can-i ... --as=system:serviceaccount:...`.

## 5. Hands-on — copy/paste manifests

Try each block from memory first; these are the backstop. Work in `~/learn-k8s/story-05/`, namespace
`lighthouse` (reuse story 02–03). Replace placeholders (`<CLIENT_SECRET>`, hostnames) with your values.
**Keycloak realm/client setup is done in the Keycloak admin UI** — the manifests stand the server up; the
realm `lighthouse` + client `lighthouse` (confidential, redirect `https://lighthouse.local/api/auth/callback`)
you create through its console.

### 5.1 Keycloak in-cluster (throwaway dev mode)

```yaml
# keycloak.yaml — DEV MODE ONLY (in-memory, no TLS). Never production.
apiVersion: apps/v1
kind: Deployment
metadata: { name: keycloak, namespace: lighthouse }
spec:
  replicas: 1
  selector: { matchLabels: { app: keycloak } }
  template:
    metadata: { labels: { app: keycloak } }
    spec:
      containers:
        - name: keycloak
          image: quay.io/keycloak/keycloak:26.0   # pin a real tag; never :latest
          args: ["start-dev"]
          env:
            - { name: KC_BOOTSTRAP_ADMIN_USERNAME, value: admin }
            - { name: KC_BOOTSTRAP_ADMIN_PASSWORD, value: admin }   # dev only
            - { name: KC_HOSTNAME, value: "https://keycloak.local" } # STABLE issuer — beats split-horizon (§4)
            - { name: KC_HTTP_ENABLED, value: "true" }
            - { name: KC_PROXY_HEADERS, value: "xforwarded" }
          ports: [{ containerPort: 8080 }]
---
apiVersion: v1
kind: Service
metadata: { name: keycloak, namespace: lighthouse }
spec:
  selector: { app: keycloak }
  ports: [{ port: 8080, targetPort: 8080 }]
```

Give Keycloak its own Ingress host (`keycloak.local`, story 03 pattern) so the browser *and* the issuer
agree on `https://keycloak.local`. Then in the admin UI: create realm `lighthouse`, a confidential client
`lighthouse` with redirect URI `https://lighthouse.local/api/auth/callback`, a `groups` client-scope/mapper,
and a test user in a group.

### 5.2 OIDC config split across a ConfigMap (non-secret) and a Secret (the ClientSecret)

```yaml
# oidc-config.yaml — the ConfigMap/Secret split IS the deliverable (§2 Secret vs ConfigMap).
apiVersion: v1
kind: ConfigMap
metadata: { name: lighthouse-oidc, namespace: lighthouse }
data:
  Authentication__Enabled: "true"
  Authentication__Authority: "https://keycloak.local/realms/lighthouse"   # public issuer
  Authentication__ClientId: "lighthouse"
  # point the POD's discovery fetch at the in-cluster Service while Authority stays the public issuer (§4)
  Authentication__MetadataAddress: "http://keycloak.keycloak.svc.cluster.local:8080/realms/lighthouse/.well-known/openid-configuration"
  Authentication__RequireHttpsMetadata: "false"   # dev only — internal fetch is plain http
  Authorization__Enabled: "true"
  Authorization__GroupClaimName: "groups"
---
apiVersion: v1
kind: Secret
metadata: { name: lighthouse-oidc-secret, namespace: lighthouse }
type: Opaque
stringData:                                  # stringData = plaintext in, base64 at rest (NOT encrypted, §2)
  Authentication__ClientSecret: "<CLIENT_SECRET>"   # from the Keycloak client's Credentials tab
```

Wire both into the existing `lighthouse` Deployment with `envFrom` (keeps story 02's `Database__*` env):

```yaml
# patch the lighthouse container
spec:
  template:
    spec:
      containers:
        - name: lighthouse
          envFrom:
            - configMapRef: { name: lighthouse-oidc }
            - secretRef: { name: lighthouse-oidc-secret }
```

### 5.3 Least-privilege ServiceAccounts (no token, no binding — because Lighthouse never calls the API)

```yaml
# serviceaccounts.yaml — least privilege = a dedicated SA with the token NOT mounted, and NO RoleBinding (§3).
apiVersion: v1
kind: ServiceAccount
metadata: { name: lighthouse, namespace: lighthouse }
automountServiceAccountToken: false      # Lighthouse doesn't talk to the k8s API → drop the token [1]
---
apiVersion: v1
kind: ServiceAccount
metadata: { name: lighthouse-postgres, namespace: lighthouse }
automountServiceAccountToken: false
```

Assign them in each Deployment's pod spec:

```yaml
spec:
  template:
    spec:
      serviceAccountName: lighthouse            # (lighthouse-postgres for the DB Deployment)
      automountServiceAccountToken: false       # belt-and-suspenders at the pod level too
```

Prove the posture — the SA can do nothing, and the token isn't mounted:

```bash
kubectl auth can-i --list --as=system:serviceaccount:lighthouse:lighthouse -n lighthouse
# expect only the baseline self-review verbs; no get/list on real resources.
kubectl exec deploy/lighthouse -- ls /var/run/secrets/kubernetes.io/serviceaccount/ 2>&1 || echo "no token mounted — correct"
```

### 5.4 A namespaced Role + RoleBinding (TEACHING exercise — for a pod that WOULD need API access)

Lighthouse doesn't need this; you write it to *understand* k8s RBAC. Example: a SA allowed to read
ConfigMaps in its own namespace.

```yaml
# rbac-demo.yaml — Role is namespaced; RoleBinding grants it to a ServiceAccount in THIS namespace [2].
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata: { name: configmap-reader, namespace: lighthouse }
rules:
  - apiGroups: [""]            # core API group
    resources: ["configmaps"]
    verbs: ["get", "list", "watch"]   # additive only — no deny rules exist [2]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata: { name: configmap-reader-binding, namespace: lighthouse }
subjects:
  - kind: ServiceAccount
    name: lighthouse          # the POD identity, not a human [1]
    namespace: lighthouse
roleRef:
  kind: Role
  name: configmap-reader
  apiGroup: rbac.authorization.k8s.io
```

```bash
kubectl apply -f rbac-demo.yaml
# BEFORE binding it returned "no"; AFTER, "yes" — additive permissions, live:
kubectl auth can-i list configmaps -n lighthouse --as=system:serviceaccount:lighthouse:lighthouse   # yes
kubectl auth can-i list secrets   -n lighthouse --as=system:serviceaccount:lighthouse:lighthouse   # no — never granted
kubectl auth can-i list pods      -n kube-system --as=system:serviceaccount:lighthouse:lighthouse  # no — Role is namespaced [2]
```

### 5.5 oauth2-proxy in front of the Ingress (edge auth) — Traefik ForwardAuth

```yaml
# oauth2-proxy.yaml — edge OIDC. Honestly partly redundant with Lighthouse's own OIDC (§2) — learn the pattern.
apiVersion: apps/v1
kind: Deployment
metadata: { name: oauth2-proxy, namespace: lighthouse }
spec:
  replicas: 1
  selector: { matchLabels: { app: oauth2-proxy } }
  template:
    metadata: { labels: { app: oauth2-proxy } }
    spec:
      containers:
        - name: oauth2-proxy
          image: quay.io/oauth2-proxy/oauth2-proxy:v7.6.0   # pin a real tag
          args:
            - --provider=oidc
            - --oidc-issuer-url=https://keycloak.local/realms/lighthouse   # MUST match the issuer (§4)
            - --client-id=oauth2-proxy                # a SEPARATE Keycloak client from "lighthouse"
            - --redirect-url=https://lighthouse.local/oauth2/callback   # MUST equal the registered redirect exactly
            - --email-domain=*
            - --upstream=static://202                  # ForwardAuth mode: just say yes/no, don't proxy
            - --http-address=0.0.0.0:4180
            - --reverse-proxy=true
            - --cookie-secure=true
          env:
            - { name: OAUTH2_PROXY_CLIENT_SECRET, valueFrom: { secretKeyRef: { name: oauth2-proxy-secret, key: client-secret } } }
            - { name: OAUTH2_PROXY_COOKIE_SECRET,  valueFrom: { secretKeyRef: { name: oauth2-proxy-secret, key: cookie-secret } } }
          ports: [{ containerPort: 4180 }]
---
apiVersion: v1
kind: Service
metadata: { name: oauth2-proxy, namespace: lighthouse }
spec:
  selector: { app: oauth2-proxy }
  ports: [{ port: 4180, targetPort: 4180 }]
---
apiVersion: traefik.io/v1alpha1
kind: Middleware
metadata: { name: oauth2-forwardauth, namespace: lighthouse }
spec:
  forwardAuth:
    address: http://oauth2-proxy.lighthouse.svc.cluster.local:4180/
    trustForwardHeader: true
    authResponseHeaders: ["X-Auth-Request-User", "X-Auth-Request-Email"]
```

Attach the middleware to the **Lighthouse Ingress** (story 03) so every request is gated at the edge:

```yaml
# add to the lighthouse Ingress metadata.annotations (Traefik):
metadata:
  annotations:
    traefik.ingress.kubernetes.io/router.middlewares: lighthouse-oauth2-forwardauth@kubernetescrd
```

```bash
cookie_secret=$(head -c32 /dev/urandom | base64)   # oauth2-proxy needs a 32-byte cookie secret
kubectl create secret generic oauth2-proxy-secret -n lighthouse \
  --from-literal=client-secret='<OAUTH2_PROXY_CLIENT_SECRET>' \
  --from-literal=cookie-secret="$cookie_secret"
kubectl apply -f oauth2-proxy.yaml
# verify the chain: a fresh browser to https://lighthouse.local now bounces to Keycloak FIRST (edge),
# THEN Lighthouse's own OIDC — observe the DOUBLE challenge that proves the redundancy point (§2).
```

### 5.6 Teardown

```bash
kubectl delete -f oauth2-proxy.yaml -f rbac-demo.yaml -f keycloak.yaml
kubectl delete secret oauth2-proxy-secret lighthouse-oidc-secret -n lighthouse
kubectl delete configmap lighthouse-oidc -n lighthouse
# revert the lighthouse Deployment to story 03 (drop envFrom + serviceAccountName), and remove the
# Ingress ForwardAuth annotation:
kubectl apply -f lighthouse.yaml
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *OIDC auth works end-to-end; k8s RBAC is understood and applied; you can distinguish
app-level auth from k8s RBAC and wire least-privilege ServiceAccounts plus edge auth enforcement.* Unaided,
you should be able to:

- [ ] **Name and place the four "auths"**: app-level OIDC (Lighthouse↔Keycloak, already coded), app-level RBAC (the `groups` claim → `IRbacAdministrationService`, already coded), k8s RBAC (Role/RoleBinding/SA — pod↔kube-apiserver), oauth2-proxy edge auth — and identify the OIDC-to-kube-apiserver **red herring** as out of scope.
- [ ] **Configure** Lighthouse's *existing* OIDC via a ConfigMap (Authority/ClientId/Enabled/GroupClaimName) + a Secret (ClientSecret), explain **why the ClientSecret can't be in the ConfigMap**, and explain that the env keys use `__` because ASP.NET maps `__`→`:`.
- [ ] Log in **end-to-end** through the browser over **HTTPS**, and explain the two ways it breaks: the **split-horizon issuer** mismatch (fix with stable `KC_HOSTNAME` + `MetadataAddress`) and the **cookie dropped on http** (`SecurePolicy=Always`).
- [ ] **ServiceAccount vs RoleBinding** least privilege: why Lighthouse's SA needs `automountServiceAccountToken: false` and **no RoleBinding** — grounded in "permissions are purely additive, no deny rules" and "Lighthouse never calls the k8s API".
- [ ] Write a namespaced **Role + RoleBinding**, explain **Role vs ClusterRole** (namespaced vs cluster) and **RoleBinding vs ClusterRoleBinding**, and verify a SA's powers with `kubectl auth can-i ... --as=system:serviceaccount:...`.
- [ ] Stand up **oauth2-proxy** for `browser → Ingress → oauth2-proxy → Lighthouse`, and **honestly assess its redundancy** here (Lighthouse already does OIDC) vs when it genuinely earns its place (a backend with no built-in auth).
- [ ] Explain why a k8s **Secret is not encryption** (base64 at rest in etcd) and what real protection needs (encryption-at-rest + RBAC) — deferred to productization (story 11–12).

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick a throwaway experiment that *tests the lesson at its edges*. Form a hypothesis **before** you run it:

> **(a) Break the issuer on purpose — see the split-horizon failure.** Set
> `Authentication__Authority` to the **internal** Service URL (`http://keycloak.keycloak.svc:8080/realms/lighthouse`)
> instead of the public one, keep `MetadataAddress` pointing internally, and log in. Predict *before* you
> run it: does discovery succeed but token validation fail, or does the browser redirect break? Read
> `kubectl logs deploy/lighthouse` for the expected-vs-actual `iss`. Then fix it with the stable
> `KC_HOSTNAME` + public `Authority` and watch it pass. (Expected: the browser-issued token's `iss` is the
> public host; validation against the internal `Authority` fails — *that* is split-horizon.)
>
> **(b) Prove the ServiceAccount is powerless — and that a binding is purely additive.** Before applying
> §5.4, run `kubectl auth can-i list configmaps --as=system:serviceaccount:lighthouse:lighthouse` (expect
> **no**). Apply the Role+RoleBinding, re-run (expect **yes**), then check `list secrets` and
> `list pods -n kube-system` (both still **no**). Predict the three answers first. The lesson: you grant by
> *adding*, never by denying, and a namespaced Role can't reach another namespace.
>
> **(c) Is oauth2-proxy redundant? Measure it.** With §5.5 in place, open a fresh private window to
> `https://lighthouse.local` and count the Keycloak challenges. Predict: one (edge) or two (edge + app)?
> Then remove the ForwardAuth annotation and confirm Lighthouse *still* requires login on its own. The
> point: for an app that already authenticates, the edge proxy adds a second challenge for little gain —
> name the backend type where it *would* be the only thing standing between the internet and an unauth'd
> service.

Investigate, don't look it up: break the issuer and read the `iss` mismatch; grant-then-probe to feel
"additive, no deny"; double-challenge to feel the redundancy. The issuer log is the OIDC lesson; the
`can-i` matrix is the RBAC lesson; the challenge count is the edge-auth lesson.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | ServiceAccounts (non-human identity; default SA; automountServiceAccountToken; SA vs user account) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Y (quotes confirmed) |
| 2 | Using RBAC Authorization (RBAC + API group; Role vs ClusterRole; RoleBinding vs ClusterRoleBinding; additive/no-deny) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Y (quotes confirmed) |
| 3 | Secrets (sensitive data; similar-to-ConfigMap-but-confidential; unencrypted-at-rest caution; consumed as env/files) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Y (quotes confirmed) |
| 4 | ConfigMaps (non-confidential key-value; no-secrecy warning; consumed as env/args/files) | kubernetes.io | High (1.0) | Official | 2026-06-13 | Y (quotes confirmed) |
| 5 | oauth2-proxy (reverse proxy doing provider auth) | oauth2-proxy.github.io | High (0.9) | Official project docs | 2026-06-13 | Partial (definition quote confirmed; forward-mechanism described by behaviour — §Gaps) |
| 6 | k8s authentication — OpenID Connect Tokens (the red herring: OIDC → kube-apiserver) | kubernetes.io | High (1.0) | Official | 2026-06-13 | N (section truncated on fetch — described by behaviour, §Gaps) |
| — | Lighthouse repo (Program.cs, appsettings.json, AuthController.cs, AuthorizationController.cs) | this repo | Primary | First-party source | 2026-06-13 | Y (line-cited at HEAD; OIDC + app-RBAC already implemented = read, not guessed; no-k8s-client = grep confirmed) |

Primary sources: **8** (5 official kubernetes.io doc pages + the oauth2-proxy project docs + the line-cited
Lighthouse repo, counting the truncated [6] as a named-but-unverified pointer). The repo facts are the
strongest authority here: they establish that OIDC and app-RBAC are *already coded*, which reframes the
whole story from "build auth" to "configure auth + understand the orthogonal k8s-RBAC layer".

## Knowledge Gaps

- **k8s-API OIDC section [6] truncated on fetch.** The "OpenID Connect Tokens" subsection of the
  authentication reference would not render (page truncated twice). It's cited as the **red herring**
  pointer only — the claim "OIDC can also authenticate humans to the kube-apiserver" is well-established
  k8s behaviour, but treat [6] as a navigation pointer, not a verbatim quote. Read the live section if you
  want the `--oidc-issuer-url` kube-apiserver flags verbatim (you do **not** need them for this story).
- **oauth2-proxy forward/upstream mechanism described, not quoted.** [5] confirmed the one-line definition
  verbatim; the ForwardAuth-vs-inline wiring and header pass-through are described from the project's
  documented behaviour, not a single quote. Confirm against the oauth2-proxy "Configuration" and
  "Integration" pages when you wire §5.5 — and confirm the exact Traefik `ForwardAuth` middleware fields
  against current Traefik docs (the CRD shape drifts between Traefik majors).
- **Keycloak flags rot between majors.** `KC_BOOTSTRAP_ADMIN_*`, `KC_HOSTNAME`, `start-dev` are current
  for Keycloak 26.x; older charts used `KEYCLOAK_ADMIN`/`KEYCLOAK_ADMIN_PASSWORD`. Pin the tag you
  actually pull and check its docs. `start-dev` is **explicitly dev-only** (in-memory H2, no TLS) — never
  production.
- **Image tags rot.** `quay.io/keycloak/keycloak:26.0` and `quay.io/oauth2-proxy/oauth2-proxy:v7.6.0` are
  illustrative — pin whatever current stable tag you pull; never `:latest`.
- **The split-horizon issuer fix has two valid shapes.** This guide uses stable `KC_HOSTNAME` + public
  `Authority` + internal `MetadataAddress`. An equally valid alternative is to route the pod's discovery
  through the same Ingress (`https://keycloak.local`) with in-cluster DNS/hostAliases so *one* URL works
  for both — pick whichever you find clearer in your spike; both make `iss` consistent.
- **Production secret-sealing is out of scope.** The §2 caution (Secret = base64, not encrypted) is
  flagged but its real fix (encryption-at-rest, SealedSecrets/external-secrets, RBAC on the Secret) is a
  story 11–12 / chart concern, not solved here.

## Full Citations

[1] The Kubernetes Authors. "Service Accounts". kubernetes.io. https://kubernetes.io/docs/concepts/security/service-accounts/. Accessed 2026-06-13.
[2] The Kubernetes Authors. "Using RBAC Authorization". kubernetes.io. https://kubernetes.io/docs/reference/access-authn-authz/rbac/. Accessed 2026-06-13.
[3] The Kubernetes Authors. "Secrets". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/secret/. Accessed 2026-06-13.
[4] The Kubernetes Authors. "ConfigMaps". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/configmap/. Accessed 2026-06-13.
[5] OAuth2 Proxy Authors. "OAuth2 Proxy". oauth2-proxy.github.io. https://oauth2-proxy.github.io/oauth2-proxy/. Accessed 2026-06-13.
[6] The Kubernetes Authors. "Authenticating" (OpenID Connect Tokens). kubernetes.io. https://kubernetes.io/docs/reference/access-authn-authz/authentication/#openid-connect-tokens. Accessed 2026-06-13 (section truncated on fetch — pointer only).
[7] LetPeopleWork. Lighthouse repository — `Lighthouse.Backend/Lighthouse.Backend/Program.cs` (lines 574-578, 588-594, 612-617, 628, 636, 648, 658-680, 683, 692, 700, 706, 712, 949), `Lighthouse.Backend/Lighthouse.Backend/appsettings.json` (Authentication block at line 49; Authorization block: Enabled/EmergencySystemAdminSubjects/GroupClaimName), `Lighthouse.Backend/Lighthouse.Backend/API/AuthController.cs` (lines 14-15, 26-90; all `[AllowAnonymous]`), `Lighthouse.Backend/Lighthouse.Backend/API/AuthorizationController.cs` (lines 10-11, 14, 24 — `GET /api/v1/authorization/my-summary` via `IRbacAdministrationService`). Accessed 2026-06-13.
