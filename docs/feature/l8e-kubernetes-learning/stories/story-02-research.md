> nw-research reading guide for story #5192 — read the concepts, try it yourself first, the copy/paste commands in the Hands-on section are there when you want them.

# Story 02 — Persistence & Config (Reading Guide)

**Date**: 2026-06-07 | **Step**: nw-research (instructor + reference, not implementer) | **Sources**: 7 primary, all High-tier official docs
**Doc currency**: k8s concept pages are evergreen-per-version (current nav tracks v1.36.x); image/config refs grounded in this repo's `Dockerfile`, `examples/postgres/docker-compose.yml`, `DatabaseConfigurator.cs`, and `appsettings.json` at HEAD.

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the Postgres Deployment + its PVC, the
> ConfigMap/Secret, the rewired Lighthouse Deployment) is throwaway learning scaffolding. Keep it in
> a personal scratch dir, e.g. `~/learn-k8s/story-02/`. **Nothing from this story goes into the
> Lighthouse repo** — the real manifests first land in `chart/` at **story 09** (per planning §7
> workspace map: "`lighthouse` (this repo) … **+ the Helm chart** (`chart/`) … Established by story
> 09"). You're driving the *real* Lighthouse image at a real Postgres backend, but the YAML you write
> here is rehearsal, not product.

## 1. Orientation

Story 01 ended on a deliberate sting: you created state in the UI, deleted the Deployment-managed pod,
the ReplicaSet handed you a brand-new pod — and your SQLite data was **gone**, because the DB file
lived at `/app/data` inside the pod's ephemeral writable layer. Story 02 fixes that, and does it the
way the product is actually meant to run in production: **switch the backend from SQLite to Postgres,
give Postgres a `PersistentVolumeClaim` so its data outlives any pod, and lift configuration and
credentials out of the image into a `ConfigMap` and a `Secret`.**

Two lessons braid together here. First, **persistence**: a PVC is storage with a lifecycle
*independent of the pod*, so deleting the Postgres pod no longer destroys the database. Second,
**config hygiene** (the twelve-factor "config in the environment" idea): the connection string and DB
password stop being baked into a manifest's `env:` literals and become a ConfigMap (non-secret) and a
Secret (the password), injected via `envFrom`/`secretKeyRef`. "Done" feels like standing up Postgres
with durable storage from a blank prompt, flipping Lighthouse to it with the *correct* env vars
(traced from the repo, not guessed — see §3), and proving — by deleting the Postgres pod and watching
the data survive — that you understand exactly why this works where story 01 didn't.

This is "lighter than its label" (planning §A, story 02): `Lighthouse.Migrations.Postgres` already
exists and `pg_dump` is already in the image — **Postgres is a supported backend today**. The slice
is mostly *config*: provider selection, connection string, and durable storage.

## 2. Concepts

### PersistentVolume / PersistentVolumeClaim / StorageClass (the claim/binding/dynamic-provisioning model)

**What it is.** Three objects, one job: durable storage that doesn't die with a pod. A **PV** is the
actual piece of storage; a **PVC** is a *request* for storage; a **StorageClass** describes a *kind*
of storage and lets the cluster create a PV on demand.

> "A *PersistentVolume* (PV) is a piece of storage in the cluster that has been provisioned by an administrator or dynamically provisioned using Storage Classes... PVs are volume plugins like Volumes, but have a lifecycle independent of any individual Pod that uses the PV." — kubernetes.io [1]
> "A *PersistentVolumeClaim* (PVC) is a request for storage by a user. It is similar to a Pod. Pods consume node resources and PVCs consume PV resources... Claims can request specific size and access modes (e.g., they can be mounted ReadWriteOnce, ReadOnlyMany, ReadWriteMany, or ReadWriteOncePod...)." — kubernetes.io [1]

The **binding** is the heart of it — a control loop pairs a PVC to a PV, one-to-one and exclusive:

> "A control loop in the control plane watches for new PVCs, finds a matching PV (if possible), and binds them together. If a PV was dynamically provisioned for a new PVC, the loop will always bind that PV to the PVC... Once bound, PersistentVolumeClaim binds are exclusive... A PVC to PV binding is a one-to-one mapping, using a ClaimRef which is a bi-directional binding between the PersistentVolume and the PersistentVolumeClaim." — kubernetes.io [1]

**Dynamic provisioning** is why you don't hand-author a PV on k3s. When no static PV matches, the
StorageClass conjures one:

> "When none of the static PVs the administrator created match a user's PersistentVolumeClaim, the cluster may try to dynamically provision a volume specially for the PVC. This provisioning is based on StorageClasses: the PVC must request a storage class and the administrator must have created and configured that class for dynamic provisioning to occur." — kubernetes.io [1]
> "A StorageClass provides a way for administrators to describe the *classes* of storage they offer. Different classes might map to quality-of-service levels, or to backup policies, or to arbitrary policies determined by the cluster administrators." — kubernetes.io [2]
> "When a PVC does not specify a `storageClassName`, the default StorageClass is used." — kubernetes.io [2]

**On k3s specifically:** the default StorageClass is **`local-path`**, backed by Rancher's
**local-path-provisioner**, which ships built into k3s. A PVC with no `storageClassName` is satisfied
by `local-path`, which carves a directory on the node's disk and binds a PV to your claim
automatically — no PV YAML, no cloud volume. (Verify with `kubectl get storageclass` — `local-path`
should be marked `(default)`.) Caveat to keep in mind for later bands: `local-path` is node-local, so
the data lives on *one* node — fine for single-node k3s learning, a constraint you'll revisit when
multi-node/replication shows up.

- Read: PersistentVolumes — https://kubernetes.io/docs/concepts/storage/persistent-volumes/ [1]
- Read: Storage Classes — https://kubernetes.io/docs/concepts/storage/storage-classes/ [2]

You should be able to answer:
- What does "a lifecycle independent of any individual Pod" buy you, in terms of story 01's data-loss demo?
- A PVC stays `Pending`. Name the two most likely causes (no matching PV / no provisioner for the requested class) and how dynamic provisioning normally avoids this.
- On k3s, which StorageClass satisfies a PVC that omits `storageClassName`, and which provisioner backs it? Where does the data physically live?

### ConfigMap (non-secret config out of the image)

**What it is.** A place to keep *non-confidential* configuration as key/value pairs, decoupled from
the container image so the same image runs in dev, staging, and prod with different config.

> "A ConfigMap is an API object used to store non-confidential data in key-value pairs." — kubernetes.io [3]
> "ConfigMap does not provide secrecy or encryption. If the data you want to store are confidential, use a Secret rather than a ConfigMap, or use additional (third party) tools to keep your data private." — kubernetes.io [3]
> "A ConfigMap is not designed to hold large chunks of data. The data stored in a ConfigMap cannot exceed 1 MiB." — kubernetes.io [3]

For this story the ConfigMap carries the **non-secret** pieces of Lighthouse's DB wiring — e.g.
`Database__Provider=postgres` and the *non-credential* parts of the connection. The password does
**not** go here (see Secret).

- Read: ConfigMap — https://kubernetes.io/docs/concepts/configuration/configmap/ [3]

You should be able to answer:
- What's the one-sentence reason `Database__Provider` belongs in a ConfigMap but the DB password does not?
- What's the hard size limit on a ConfigMap, and what does k8s tell you to use instead for large data?

### Secret (and the honest caveat: base64 ≠ encrypted)

**What it is.** The object for *small amounts of sensitive data* — here, the Postgres password.

> "A Secret is an object that contains a small amount of sensitive data such as a password, a token, or a key." — kubernetes.io [4]
> "Using a Secret means that you don't need to include confidential data in your application code." — kubernetes.io [4]

**The caveat you must internalize.** A Secret's `data:` is **base64-encoded, which is not
encryption** — anyone who can read the object (or etcd) can trivially decode it:

> "Kubernetes Secrets are, by default, stored unencrypted in the API server's underlying data store (etcd). Anyone with API access can retrieve or modify a Secret, and so can anyone with access to etcd." — kubernetes.io [4]
> "In order to safely use Secrets, take at least the following steps: 1. **Enable Encryption at Rest for Secrets.**" — kubernetes.io [4]

So a vanilla Secret is *better than a literal in the manifest* (it's a separate, RBAC-gateable
object, injectable without the value appearing in the Deployment), but it is **not** a vault. This is
exactly the gap the epic closes later: **Sealed Secrets / External Secrets Operator (ESO)** in Band D
(GitOps, story 13) so no plaintext secret ever lives in Git. For story 02, a plain Secret is the
right learning step — just say out loud *why it isn't enough for production*.

- Read: Secrets — https://kubernetes.io/docs/concepts/configuration/secret/ [4]

You should be able to answer:
- Decode this in your head: a Secret with `data: { password: cG9zdGdyZXM= }` holds what value, and what does that prove about base64-vs-encryption?
- Name two things a plain Secret *does* improve over an inline `env: value:` literal, and the one big thing it does **not** (and which epic story fixes that).

### env injection (`envFrom` + `configMapKeyRef`/`secretKeyRef` vs inline `env`)

**What it is.** Three ways to get config into the container's process environment, in increasing
decoupling:

1. **Inline `env: value:`** — a literal in the Deployment. Simplest, but the value lives in the manifest. Fine for trivial non-secret flags.

> "`env` allows you to set environment variables for a container, specifying a value directly for each variable that you name." — kubernetes.io [6]

2. **`valueFrom` → `configMapKeyRef` / `secretKeyRef`** — pull *one specific key* from a ConfigMap or Secret into a *named* env var (lets you rename, and keeps the value out of the Deployment):

> "Assign the `backend-username` value defined in the Secret to the `SECRET_USERNAME` environment variable in the Pod specification." — kubernetes.io [5]
> ```yaml
> env:
> - name: SECRET_USERNAME
>   valueFrom:
>     secretKeyRef:
>       name: backend-user
>       key: backend-username
> ```
> — kubernetes.io [5]

3. **`envFrom`** — bulk-inject *every* key as an env var of the same name:

> "`envFrom` allows you to set environment variables for a container by referencing either a ConfigMap or a Secret. When you use `envFrom`, all the key-value pairs in the referenced ConfigMap or Secret are set as environment variables for the container." — kubernetes.io [6]

For Lighthouse the clean shape is: `envFrom` the **ConfigMap** (it holds `Database__Provider` and the
non-secret config, keyed exactly as the app expects), and use a **`secretKeyRef`** to map the
Postgres password into a single env var — then assemble the connection string. The mapping nuance
that bites people: ASP.NET reads nested config keys with the **`__` (double-underscore)** separator
(`Database__Provider` → `Database:Provider`), so the *key names inside the ConfigMap/Secret must be
exactly* `Database__Provider` / `Database__ConnectionString` for `envFrom` to land them on the right
config nodes (see §3).

- Read: Distribute Credentials Securely (Secrets → env) — https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/ [5]
- Read: Define Environment Variables for a Container — https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/ [6]

You should be able to answer:
- When do you reach for `envFrom` vs `secretKeyRef` vs an inline `env: value:`? Give the one-line rule for each.
- Lighthouse expects config key `Database:ConnectionString`. What must the *env var name* be (and why the double underscore), whether you inject it via `envFrom` or `secretKeyRef`?

## 3. Repo-grounded facts (flip the REAL Lighthouse to Postgres — from the source, not a guess)

All cited from this repo at HEAD — verify before you copy. **This is the crux of the story: get the
env vars right from the source.**

- **THE SWITCH — one config section, two keys.** At runtime Lighthouse binds the config section
  `Database` into a `DatabaseConfiguration` POCO and branches on `Provider`:
  - `DatabaseConfigurator.cs:15-16` binds it: `builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection("Database"))`.
  - `DatabaseConfigurator.cs:24-28` is the branch: `switch (dbConfig.Provider.ToLower()) { case "postgresql": case "postgres": options.UseNpgsql(dbConfig.ConnectionString, npgsql => { … npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres"); … }); … case "sqlite": … options.UseSqlite(connection, …); … default: throw new NotSupportedException(...) }`.
  - The POCO (`DatabaseConfiguration.cs:3-8`): `Provider` (default `"Sqlite"`) and `ConnectionString` (default `"Data Source=LighthouseAppContext.db"`).
  - The shipped default (`appsettings.json:39-42`): `"Database": { "Provider": "sqlite", "ConnectionString": "Data Source=LighthouseAppContext.db" }` — so out of the box you get SQLite (story 01's behaviour).
- **THE EXACT ENV VARS TO FLIP TO POSTGRES.** ASP.NET maps env-var `__` to config `:`, so the two
  keys `Database:Provider` / `Database:ConnectionString` are set from the environment as:
  - **`Database__Provider=postgres`**  (accepted values are `postgres` or `postgresql`, case-insensitive — `DatabaseConfigurator.cs:24-27`; anything else throws `NotSupportedException`, `:79-80`).
  - **`Database__ConnectionString=Host=<postgres-service>;Database=lighthouse;Username=postgres;Password=<pw>`** (Npgsql connection-string form).
  - **Ground truth — the official compose example does exactly this** (`examples/postgres/docker-compose.yml:27-29`):
    ```yaml
    environment:
      Database__Provider: postgres
      Database__ConnectionString: "Host=postgres;Database=lighthouse;Username=postgres;Password=postgres"
    ```
    On k8s the only change is `Host=postgres` → the **Postgres Service DNS name** (e.g.
    `Host=lighthouse-postgres`). Same two env vars, full stop.
  - The `Create-Migration.ps1` script corroborates the same keys for both providers
    (`:32,34` sqlite; `:53-54` postgres), so these are the canonical env names.
- **Migrations run automatically, against the right assembly.** On boot Lighthouse calls
  `context.Database.Migrate()` (`DatabaseConfigurator.cs:85-92`, invoked from
  `Program.cs:971` `ApplyMigrations`). With `Provider=postgres` the Npgsql branch pins
  `MigrationsAssembly("Lighthouse.Migrations.Postgres")` (`:32`) — that project is built into the
  image (`Dockerfile:17-19`). **So you do not run migrations by hand**: point Lighthouse at an empty
  Postgres DB and it creates its own schema on first start. (If startup hangs/crashes, that's the
  signal the connection string or password is wrong — see §4.)
- **Image, port, data dir** (unchanged from story 01, re-confirmed): image
  `ghcr.io/letpeoplework/lighthouse` (`README.md:52`); container listens on **80**/**443**
  (`Dockerfile:4,66-67`); `LIGHTHOUSE_DOCKER=true` baked in (`Dockerfile:68`). **Pin a real release
  tag, not `:latest`.** Latest at time of writing: **`v26.6.7.1`**
  (`gh release view --repo LetPeopleWork/Lighthouse --json tagName -q .tagName`). Note: once you move
  to Postgres, the SQLite `/app/data` PVC from a naive run is **no longer needed for app data** — the
  durable storage now belongs to the *Postgres* pod, not the Lighthouse pod. (The Lighthouse pod is
  back to being stateless — which is the whole point and what makes scaling in Band B possible.)
- **Postgres image:** the compose example uses `postgres:17.2-alpine`
  (`examples/postgres/docker-compose.yml:5`). For the hands-on below pin a concrete tag — either
  match that (`postgres:17.2-alpine`) or the round `postgres:16`. **Never `postgres:latest`.**

## 4. Debug reflex (carry this through the story)

Carry forward story 01's rule, then add the two new failure shapes this story introduces:

- **describe→Events before the container runs; logs after** (story 01). `kubectl describe pod` →
  Events for *pre-run* problems (ImagePull, scheduling, **failed mounts**); `kubectl logs` only once
  the container is up.
- **PVC stuck `Pending`** → `kubectl describe pvc <name>` and `kubectl get storageclass`. A `Pending`
  PVC means nothing satisfied the claim: usually a `storageClassName` that doesn't exist, or (on a
  cluster without a default provisioner) no class at all. On k3s, confirm `local-path` is the
  `(default)` StorageClass; a pod that references the PVC will sit in `Pending` (its Events show
  *"waiting for first consumer"* or *"FailedScheduling"*) until the claim binds.
- **Postgres auth / connection failures** → the Lighthouse pod will log an Npgsql connection error
  and `Database.Migrate()` will fail on boot. Check, in order: (1) is the Postgres pod `Running` and
  its PVC `Bound`? (2) does the **Secret value actually injected** match what Postgres was initialized
  with — `kubectl exec` into the Lighthouse pod and `echo $Database__ConnectionString` / check the
  password env var, and remember a Secret only changes a *running* pod if you restart it; (3) is the
  `Host=` in the connection string the **Service name**, resolvable by cluster DNS? Mnemonic: **PVC
  Pending → describe pvc + storageclass; app won't start → logs for Npgsql, then verify the injected
  Secret.**

## 5. Hands-on — copy/paste manifests

Try each block from memory first; these are the backstop. Replace `<TAG>` with a real release tag
(§3, e.g. `v26.6.7.1`). Work in `~/learn-k8s/story-02/`. The Postgres Service is named
`lighthouse-postgres`, which is the DNS name the connection string targets.

### 5.1 Postgres: PVC + Deployment + ClusterIP Service

```yaml
# postgres.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-data
spec:
  accessModes:
    - ReadWriteOnce          # one node mounts it read/write — fine for single-node k3s
  resources:
    requests:
      storage: 2Gi
  # No storageClassName → k3s default 'local-path' dynamically provisions a node-local PV.
  # (Verify: kubectl get storageclass  → local-path should be (default).)
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lighthouse-postgres
  labels:
    app: lighthouse-postgres
spec:
  replicas: 1
  selector:
    matchLabels:
      app: lighthouse-postgres
  template:
    metadata:
      labels:
        app: lighthouse-postgres
    spec:
      containers:
        - name: postgres
          image: postgres:16            # pinned, never :latest
          ports:
            - containerPort: 5432
          env:
            - name: POSTGRES_DB
              value: lighthouse
            - name: POSTGRES_USER
              value: postgres
            - name: POSTGRES_PASSWORD     # sourced from the Secret (5.2), NOT a literal
              valueFrom:
                secretKeyRef:
                  name: lighthouse-db
                  key: postgres-password
            - name: PGDATA
              value: /var/lib/postgresql/data/pgdata   # subdir avoids "lost+found" init quirk
          volumeMounts:
            - name: data
              mountPath: /var/lib/postgresql/data
      volumes:
        - name: data
          persistentVolumeClaim:
            claimName: postgres-data       # ← the PVC above; this is what makes data durable
---
apiVersion: v1
kind: Service
metadata:
  name: lighthouse-postgres            # ← this name is the DNS host in the connection string
spec:
  type: ClusterIP                      # internal only — the DB is never exposed to the host
  selector:
    app: lighthouse-postgres
  ports:
    - port: 5432
      targetPort: 5432
```

### 5.2 The Secret (DB password) and the ConfigMap (non-secret config)

```yaml
# db-secret.yaml  — base64 is NOT encryption (§2 Secret caveat); plain Secret is a learning step,
# Sealed Secrets / ESO come in Band D (story 13).
apiVersion: v1
kind: Secret
metadata:
  name: lighthouse-db
type: Opaque
stringData:                       # stringData lets you write the raw value; k8s base64s it for you
  postgres-password: "supersecret-change-me"
```

```yaml
# app-config.yaml — non-secret config, keyed EXACTLY as the app's config nodes (Database__*).
apiVersion: v1
kind: ConfigMap
metadata:
  name: lighthouse-config
data:
  Database__Provider: "postgres"           # the switch (§3): DatabaseConfigurator picks UseNpgsql
  # Non-secret parts of the connection. Password is injected separately (secretKeyRef) and the
  # full connection string is assembled in the Deployment env below.
  POSTGRES_HOST: "lighthouse-postgres"     # = the Postgres Service DNS name
  POSTGRES_DB: "lighthouse"
  POSTGRES_USER: "postgres"
```

### 5.3 Lighthouse Deployment — consume ConfigMap + Secret, point at the Postgres Service

```yaml
# lighthouse.yaml
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
      app: lighthouse
  template:
    metadata:
      labels:
        app: lighthouse
    spec:
      containers:
        - name: lighthouse
          image: ghcr.io/letpeoplework/lighthouse:<TAG>   # pin a real release tag, e.g. v26.6.7.1
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: 80
          envFrom:
            - configMapRef:
                name: lighthouse-config        # brings in Database__Provider (the switch) + POSTGRES_*
          env:
            - name: POSTGRES_PASSWORD          # one key pulled from the Secret
              valueFrom:
                secretKeyRef:
                  name: lighthouse-db
                  key: postgres-password
            # Assemble the Npgsql connection string (§3 exact form). Env var name uses '__' so
            # ASP.NET maps it to config node Database:ConnectionString.
            - name: Database__ConnectionString
              value: "Host=$(POSTGRES_HOST);Database=$(POSTGRES_DB);Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)"
          # No PVC on the Lighthouse pod: with Postgres holding state, this pod is stateless again.
---
apiVersion: v1
kind: Service
metadata:
  name: lighthouse
spec:
  type: ClusterIP
  selector:
    app: lighthouse
  ports:
    - port: 80
      targetPort: 80
---
# temporary viewing hatch (delete after) — same pattern as story 01
apiVersion: v1
kind: Service
metadata:
  name: lighthouse-view
spec:
  type: NodePort
  selector:
    app: lighthouse
  ports:
    - port: 80
      targetPort: 80
      nodePort: 30080
```

> Note: k8s expands `$(VAR)` in an `env value:` only from env vars defined **earlier in the same
> container's `env`/`envFrom`**. `POSTGRES_HOST/DB/USER` come from the `envFrom` ConfigMap and
> `POSTGRES_PASSWORD` from the `secretKeyRef` above, so the `$(...)` interpolation resolves. If your
> cluster ever leaves a literal `$(POSTGRES_HOST)` in the string, that ordering is why — fall back to
> putting the whole `Database__ConnectionString` in the ConfigMap (sans password) and a second env
> with the password, or assemble it fully in the Secret.

### 5.4 Apply, watch the binding, confirm Postgres is the backend

```bash
kubectl apply -f db-secret.yaml -f app-config.yaml -f postgres.yaml
kubectl get pvc                              # postgres-data: watch STATUS go Pending → Bound
kubectl get storageclass                     # confirm local-path is (default) on k3s
kubectl describe pvc postgres-data           # if stuck Pending: read Events (no class? no provisioner?)
kubectl rollout status deploy/lighthouse-postgres
kubectl apply -f lighthouse.yaml
kubectl rollout status deploy/lighthouse
kubectl logs -l app=lighthouse | grep -i -E "database|migrat|npgsql"   # see it migrate the Postgres schema
# The startup banner logs the provider (Program.cs:1085 "💾 Database"): should read postgres, not sqlite.
curl -s http://localhost:30080/ | head       # single-node k3s: node == host
```

### 5.5 The payoff experiment — delete the Postgres pod, watch the data SURVIVE

```bash
# 1. In the browser (http://localhost:30080) create some state — a team or project.
# 2. Delete the POSTGRES pod (the stateful one), not the app pod:
kubectl get pods -l app=lighthouse-postgres -w &
kubectl delete pod -l app=lighthouse-postgres        # the Deployment recreates it...
# 3. The new Postgres pod re-mounts the SAME PVC (postgres-data) — same data on disk.
kubectl rollout status deploy/lighthouse-postgres
kill %1
# 4. Reload the browser → your team/project is STILL THERE.
# Why: the database lives on the PVC (a node-local volume via local-path), whose lifecycle is
# INDEPENDENT of the pod [1]. Contrast story 01: SQLite lived in the pod's writable layer and died
# with the pod. Same delete, opposite outcome — because storage is now decoupled from compute.
```

### 5.6 Tear down

```bash
kubectl delete -f lighthouse.yaml -f postgres.yaml -f app-config.yaml -f db-secret.yaml
kubectl get pvc                              # postgres-data may linger (default reclaim) — delete explicitly:
kubectl delete pvc postgres-data             # THIS is what actually destroys the data (see §7 spike)
kubectl get deploy,rs,svc,pods,pvc,secret,configmap
```

## 6. Self-check (maps to the exit criterion)

Exit criterion: *flip Lighthouse to Postgres backed by a PVC, with config/secrets externalized, and
explain why the data now survives a pod delete.* Unaided, you should be able to:

- [ ] State the **exact two env vars** that switch Lighthouse to Postgres (`Database__Provider=postgres`, `Database__ConnectionString=Host=…;Database=…;Username=…;Password=…`) and cite where in the repo the switch lives (`DatabaseConfigurator.cs:24-28`, default in `appsettings.json:39-42`).
- [ ] Explain why the env var name uses `__` (ASP.NET → `Database:ConnectionString`).
- [ ] Write a PVC + Postgres Deployment from scratch and explain how, on k3s, the default `local-path` StorageClass dynamically provisions the PV that binds the claim.
- [ ] Explain the PV/PVC binding model (one-to-one, exclusive, control-loop) and what "lifecycle independent of any individual Pod" means for the data-loss demo.
- [ ] Put non-secret config in a ConfigMap and the password in a Secret, and inject them via `envFrom` + `secretKeyRef` — and explain why the password does *not* go in the ConfigMap.
- [ ] State the honest Secret caveat: base64 ≠ encryption, Secrets are unencrypted in etcd by default, and name the epic story (13, Sealed Secrets/ESO) that closes that gap.
- [ ] Confirm you do **not** run migrations by hand — Lighthouse runs `Database.Migrate()` on boot against `Lighthouse.Migrations.Postgres` (`DatabaseConfigurator.cs:85-92`, `:32`).
- [ ] Demonstrate and explain the payoff: delete the Postgres pod, data survives — and contrast, in your own words, with story 01's loss.
- [ ] Diagnose a `Pending` PVC (`describe pvc` + `get storageclass`) and a Postgres auth failure (logs → injected-Secret check).

If any box needs a doc to complete, you're not through the gate yet.

## 7. For your spike (nw-spike)

Pick one throwaway experiment that *tests the persistence lesson at its edges*:

> **Delete the Postgres POD vs delete the Postgres PVC — what's the actual difference?** First create
> state, then `kubectl delete pod -l app=lighthouse-postgres` and reload (data survives — you saw
> this in 5.5). Now `kubectl delete pvc postgres-data` (you'll likely need to delete the Deployment
> first so nothing holds the volume), recreate everything, reload — **data gone.** Form a hypothesis
> *before* you run it: which delete touches storage and which touches only compute? Bonus: while the
> Postgres Deployment is scaled to 0 (or the pod is mid-restart), `kubectl get pvc` — is the PVC
> still `Bound` with no pod using it? What does that tell you about the PVC's lifecycle vs the pod's
> [1]? Second bonus: make a PVC request a `storageClassName: does-not-exist` and watch it sit
> `Pending` forever — then `kubectl describe pvc` and read exactly what the binder says it's waiting
> for.

Investigate, don't look it up: delete pod, observe; delete pvc, observe; the difference *is* the
lesson. The "PVC Bound while no pod uses it" observation is the experiential proof of "lifecycle
independent of any individual Pod."

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Persistent Volumes (PV/PVC, binding, dynamic provisioning) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 2 | Storage Classes (StorageClass, default class) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 3 | ConfigMap | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 4 | Secret (base64≠encryption caveat) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 5 | Distribute Credentials Securely (secretKeyRef) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quote + example confirmed) |
| 6 | Define Environment Variables (env / envFrom) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 7 | Lighthouse repo (DatabaseConfigurator.cs, DatabaseConfiguration.cs, appsettings.json, docker-compose, Dockerfile, Create-Migration.ps1, README) | this repo | Primary | First-party source | 2026-06-07 | Y (line-cited) |

All k8s sources are primary, first-party project documentation (High tier). Repo facts are line-cited
from this repository's own source files — the strongest possible authority for what the image and the
provider switch actually do.

## Knowledge Gaps

- **`configMapKeyRef` exact verbatim quote not captured.** The define-env-var page [6] confirms `env`
  and `envFrom` verbatim but routes `configMapKeyRef` detail to the configure-pod-configmap task
  page; the §2 `configMapKeyRef` usage is described by analogy to the verbatim `secretKeyRef` example
  from [5] (identical shape, ConfigMap instead of Secret). Confidence High by symmetry; if you want
  the verbatim line, read https://kubernetes.io/docs/tasks/configure-pod-container/configure-pod-configmap/.
- **`$(VAR)` interpolation ordering in `env value:`.** The §5.3 note relies on k8s expanding
  `$(VAR)` from earlier env entries in the same container. This is documented k8s behaviour but not
  re-fetched here; the §5.3 note gives a fallback (assemble the whole connection string in the
  ConfigMap, password aside) if your cluster behaves differently. Verify on first run via
  `kubectl exec … echo $Database__ConnectionString`.
- **Exact current release tag rots.** `v26.6.7.1` was latest at write time; pin whatever
  `gh release view --repo LetPeopleWork/Lighthouse --json tagName -q .tagName` returns at build time.
- **Postgres image tag is a choice.** The compose example pins `postgres:17.2-alpine`; §5 uses
  `postgres:16` for a round number. Either is fine for learning — just pin *a* concrete tag, never
  `:latest`. Matching the compose example most closely tracks what LPW tests against.

## Full Citations

[1] The Kubernetes Authors. "Persistent Volumes". kubernetes.io. https://kubernetes.io/docs/concepts/storage/persistent-volumes/. Accessed 2026-06-07.
[2] The Kubernetes Authors. "Storage Classes". kubernetes.io. https://kubernetes.io/docs/concepts/storage/storage-classes/. Accessed 2026-06-07.
[3] The Kubernetes Authors. "ConfigMaps". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/configmap/. Accessed 2026-06-07.
[4] The Kubernetes Authors. "Secrets". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/secret/. Accessed 2026-06-07.
[5] The Kubernetes Authors. "Distribute Credentials Securely Using Secrets". kubernetes.io. https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/. Accessed 2026-06-07.
[6] The Kubernetes Authors. "Define Environment Variables for a Container". kubernetes.io. https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/. Accessed 2026-06-07.
[7] LetPeopleWork. Lighthouse repository — `Lighthouse.Backend/Lighthouse.Backend/Data/DatabaseConfigurator.cs` (lines 15-16, 24-28, 32, 47-78, 79-80, 85-92), `Data/DatabaseConfiguration.cs` (lines 3-8), `Lighthouse.Backend/appsettings.json` (lines 39-42), `examples/postgres/docker-compose.yml` (lines 5, 27-29), `Dockerfile` (lines 4, 17-19, 66-68), `Lighthouse.Backend/Create-Migration.ps1` (lines 32, 34, 53-54), `Program.cs` (lines 971, 1085), `README.md` (line 52). Accessed 2026-06-07.
