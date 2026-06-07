> nw-research reading guide for story #5190 — read the concepts, try it yourself first, then the copy/paste commands in §7 are there when you want them.

# Story 00 — Local Cluster & kubectl Basics (Reading Guide)

**Date**: 2026-06-07 | **Step**: nw-research (instructor, not implementer) | **Sources**: 5 primary, all High-tier official docs
**Doc currency**: k3s docs last updated 2026-06-03 (nav references k8s v1.36.x); k8s concept pages evergreen-per-version.

> **🗂 Workspace: SCRATCH — not the repo.** Everything here (the nginx Pod + Service) is throwaway
> learning scaffolding. Keep it in a personal scratch dir, e.g. `~/learn-k8s/story-00/`. Nothing
> from this story goes into the Lighthouse repo — real manifests first land in `chart/` at story 09.

## 1. Orientation

Story 00 is the foundation slice of the l8e Kubernetes Learning epic: get a real cluster running on your own machine and become fluent with `kubectl` against it. Everything later (workloads, networking, config, ops) assumes this muscle memory. "Done" feels like reaching for `kubectl get`, `describe`, and `delete` without thinking — you can stand up an nginx Pod, prove it serves traffic through a Service, and tear it all down from a blank prompt. The gate is fluency, not having a YAML file you copied.

## 2. Concepts

### kubectl + kubeconfig + contexts

**What it is.** `kubectl` is the CLI that talks to the cluster's API server. It finds *which* cluster, *which* user, and *which* namespace from a **kubeconfig** file. A **context** bundles those three (cluster + user + namespace) under one name so you can switch targets with one command.

> "The `kubectl` command-line tool uses kubeconfig files to find the information it needs to choose a cluster and communicate with the API server of a cluster." — kubernetes.io [1]
> "Each context has three parameters: cluster, namespace, and user." — kubernetes.io [1]

Default lookup is `$HOME/.kube/config`. If the `KUBECONFIG` env var is set, kubectl uses the *merged* result of the files it lists; otherwise it falls back to the single default file [1]. This matters because k3s writes its kubeconfig somewhere else (see §3).

- Read: Organizing Cluster Access Using kubeconfig Files — https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig/ [1]
- Read: kubectl reference (overview + syntax) — https://kubernetes.io/docs/reference/kubectl/ [5]

You should be able to answer:
- Where does kubectl look for config if `KUBECONFIG` is unset, and what changes when it is set?
- What three things does a context name resolve to, and which command switches the active context?

### Pod

**What it is.** The smallest deployable unit — one or more co-located, co-scheduled containers sharing network and storage.

> "Pods are the smallest deployable units of computing that you can create and manage in Kubernetes." — kubernetes.io [2]

Critically, a bare Pod is **ephemeral and not self-healing**. Delete it, evict it, or lose its node, and it's just gone — nothing brings it back.

> "Pods are designed as relatively ephemeral, disposable entities... The Pod remains on that node until the Pod finishes execution, the Pod object is deleted, the Pod is evicted for lack of resources, or the node fails." — kubernetes.io [2]
> "Usually you don't need to create Pods directly, even singleton Pods. Instead, create them using workload resources such as Deployment or Job." — kubernetes.io [2]

Story 00 deliberately has you create a *bare* Pod so you feel its fragility — that lesson sets up your spike (§6) and the next story.

- Read: Pods — https://kubernetes.io/docs/concepts/workloads/pods/ [2]

You should be able to answer:
- Name the four events that end a Pod's life on its node.
- Why does the doc say you'll "rarely create individual Pods directly," and what creates/heals them instead?

### Service (ClusterIP vs NodePort)

**What it is.** A stable network front for a *set* of Pods, selected by label. It exists because Pod IPs come and go.

> "A Service is a method for exposing a network application that is running as one or more Pods in your cluster." — kubernetes.io [3]
> "Pods are ephemeral resources... the set of Pods running in one moment in time could be different from the set of Pods running that application a moment later." — kubernetes.io [3]

Two types you must distinguish:
- **ClusterIP** (default): a virtual IP reachable *only inside* the cluster. "Kubernetes assigns this Service an IP address (the cluster IP)" [3]. Good for Pod-to-Pod; useless for curling from your host.
- **NodePort**: opens a static port on *every node's* IP so external clients reach the Service at `<NodeIP>:<NodePort>`. The default allocation range is **30000–32767** [3]. This is the type you need to curl from the host in story 00.

- Read: Service — https://kubernetes.io/docs/concepts/services-networking/service/ [3]
- Read: the `type: NodePort` section specifically on that page [3]

You should be able to answer:
- Why can't you curl a ClusterIP Service from your host, but you can curl a NodePort one?
- What is the default NodePort range, and how does a Service link to the right Pods (what's the matching mechanism)?

### k3s specifics

**What it is.** k3s is a lightweight, single-binary Kubernetes distribution. One install command brings up a working single-node cluster:

> `curl -sfL https://get.k3s.io | sh -` — docs.k3s.io [4]

It is **single-node by default** (one server acts as control plane *and* worker) and bundles batteries most distros leave out: **CoreDNS, Traefik (Ingress), a Network Policy Controller, and ServiceLB (Klipper)** [4a]. See §3 for the gotchas these create.

- Read: k3s Quick-Start — https://docs.k3s.io/quick-start [4]
- Read: k3s Networking Services — https://docs.k3s.io/networking/networking-services [4a]

You should be able to answer:
- What single command installs k3s, and what node topology do you get out of the box?
- Name two components k3s bundles that a vanilla cluster would not.

## 3. k3s gotchas (cited)

- **Kubeconfig lives at `/etc/rancher/k3s/k3s.yaml`, not `~/.kube/config`.** "A kubeconfig file will be written to `/etc/rancher/k3s/k3s.yaml`." [4]. Plain `kubectl` won't find it via the default path — you must point `KUBECONFIG` at it (or merge/copy it). Recall from §2 that setting `KUBECONFIG` changes which file kubectl reads [1]. Investigate *why* that file is root-owned and what that implies for running kubectl as your user.
- **Traefik is installed by default.** "Traefik is deployed by default when starting the server"; remove with `--disable=traefik` [4a]. It occupies ports 80/443 via a LoadBalancer Service — relevant if you later wonder what's already bound.
- **ServiceLB (Klipper) is installed by default.** It provides LoadBalancer Services without a cloud provider by creating a DaemonSet in `kube-system`; disable with `--disable=servicelb` [4a]. This is why `type: LoadBalancer` "just works" on k3s when it wouldn't on a bare kubeadm cluster — note that for later, but story 00 only needs NodePort.
- **Single-node default.** The one machine is both control plane and workload host; there's no separate worker to schedule onto. Keep this in mind when reasoning about where your Pod actually runs.

## 4. The six kubectl verbs in one mental model

Don't memorize flags — internalize the loop. Two verbs *change* cluster state; four *observe or reach into* it.

- **apply** — declare desired state ("make the cluster match this manifest"). Idempotent: re-running converges, it doesn't duplicate.
- **delete** — remove an object you declared.
- **get** — list objects and their high-level status ("what exists, is it Ready?").
- **describe** — zoom into ONE object: its spec, status, and the Events timeline (your first stop when something's wrong).
- **logs** — read what a container *wrote to stdout/stderr* (the app's own voice).
- **exec** — open a shell/command *inside* a running container (look around from the inside).

Mental model: **apply/delete are the write side; get→describe is the read funnel (wide → narrow); logs/exec are the two ways into a container (its output vs. its interior).** When debugging, the reflex is `get` (what's there) → `describe` (why is it unhappy — read Events) → `logs`/`exec` (what's the app doing). Reference: kubectl docs [5].

## 5. Self-check (maps to the exit criterion)

Exit criterion: *"Can deploy, inspect, and delete a pod from scratch without looking up commands."* Unaided, you should be able to:

- [ ] Install k3s and confirm the node is `Ready` — without rereading the quick-start.
- [ ] Make `kubectl` talk to the cluster by resolving the kubeconfig location yourself (explain what you set and why).
- [ ] List cluster nodes and Pods and read their status at a glance.
- [ ] Create an nginx Pod from a manifest *you wrote* (not copied) and confirm it's Running.
- [ ] Use `describe` to find a Pod's Events, `logs` to see nginx output, and `exec` to get a shell inside it.
- [ ] Expose the Pod with a NodePort Service you wrote, then curl it from the host and get nginx's welcome page.
- [ ] Delete the Pod and the Service and confirm they're gone.
- [ ] Explain ClusterIP vs NodePort, and why a bare Pod doesn't come back after deletion — in your own words.

If any box needs a doc to complete, you're not through the gate yet.

## 6. For your spike (nw-spike)

Run one small throwaway experiment to *feel* the lesson behind §2's "Pods are not self-healing":

> **Delete your running bare Pod with `kubectl delete pod` while watching `kubectl get pods -w`. What happens? Now: does anything recreate it — and if not, what would have to own the Pod for it to come back?**

Investigate, don't look up the answer: create the Pod, delete it, observe. Then form a hypothesis about what *kind* of object would auto-replace it, and what evidence in `kubectl get`/`describe` output would tell you a controller (not you) is now managing the Pod. This sets up the next story without you needing the answer handed to you.

## 7. Hands-on — copy/paste commands

Try each block from memory first if you can; these are here as a backstop and reference. `nginx:1.27`
is pinned deliberately — avoid `:latest`.

### 7.1 Install k3s and point kubectl at it

```bash
curl -sfL https://get.k3s.io | sh -

# k3s writes a root-owned kubeconfig to /etc/rancher/k3s/k3s.yaml. Make it usable as your user:
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown "$USER" ~/.kube/config
chmod 600 ~/.kube/config

# (alternative, per-shell instead of copying:  export KUBECONFIG=/etc/rancher/k3s/k3s.yaml)

kubectl get nodes        # node should reach STATUS=Ready within ~30s
kubectl get pods -A      # see the bundled CoreDNS / Traefik / ServiceLB pods
```

### 7.2 The six verbs, on a real Pod

Write this yourself first; reach for the file when you want it.

```yaml
# nginx-pod.yaml
apiVersion: v1
kind: Pod
metadata:
  name: hello-nginx
  labels:
    app: hello-nginx
spec:
  containers:
    - name: nginx
      image: nginx:1.27
      ports:
        - containerPort: 80
```

```bash
kubectl apply -f nginx-pod.yaml
kubectl get pods                       # wide view: is it Running?
kubectl get pod hello-nginx -o wide    # which node / Pod IP
kubectl describe pod hello-nginx       # spec + status + Events (your debug first-stop)
kubectl logs hello-nginx               # nginx's stdout/stderr
kubectl exec -it hello-nginx -- cat /usr/share/nginx/html/index.html   # look inside
kubectl exec -it hello-nginx -- sh     # ...or open a shell, then `exit`
```

### 7.3 Expose it with a NodePort and curl from the host

```yaml
# nginx-svc.yaml
apiVersion: v1
kind: Service
metadata:
  name: hello-nginx
spec:
  type: NodePort
  selector:
    app: hello-nginx       # must match the Pod's labels — this is the link
  ports:
    - port: 80             # the Service's cluster-internal port
      targetPort: 80       # the container port it forwards to
      nodePort: 30080      # the static node port (must be in 30000–32767)
```

```bash
kubectl apply -f nginx-svc.yaml
kubectl get svc hello-nginx            # see TYPE=NodePort and the 80:30080/TCP mapping
curl http://localhost:30080            # single-node k3s: the node is your host → nginx welcome page
```

### 7.4 Tear it down

```bash
kubectl delete -f nginx-svc.yaml
kubectl delete -f nginx-pod.yaml
# or by name:  kubectl delete pod hello-nginx && kubectl delete svc hello-nginx
kubectl get pods,svc                   # confirm both are gone
```

### 7.5 The spike (§6) — observe, then hypothesize

```bash
kubectl apply -f nginx-pod.yaml
kubectl get pods -w &                  # watch in the background
kubectl delete pod hello-nginx         # what does the watch show? does anything recreate it?
# stop the watch:  kill %1
```

You'll see the Pod terminate and *stay* gone — nothing owns it. Form your hypothesis about what
*kind* of object would bring it back before you read story 01.

## Source Analysis

| # | Source | Domain | Reputation | Type | Accessed | Verified |
|---|--------|--------|------------|------|----------|----------|
| 1 | Organizing Cluster Access (kubeconfig) | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 2 | Pods | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 3 | Service | kubernetes.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 4/4a | Quick-Start / Networking Services | docs.k3s.io | High (1.0) | Official | 2026-06-07 | Y (quotes confirmed) |
| 5 | kubectl reference | kubernetes.io | High (1.0) | Official | 2026-06-07 | Referenced (verb semantics standard) |

All sources are primary, first-party project documentation (High tier per source-verification). No secondary sources required — the official docs fully cover story-00 scope.

## Knowledge Gaps

- **kubectl reference page [5] not deep-fetched.** §4's verb semantics and §7's commands are corroborated by the concept pages and standard usage; read [5] for the full flag set.
- **k3s `KUBECONFIG` wiring not spelled out by the quick-start page** [4] (it states the file path but not the env-var step). §3 explains why and §7.1 gives both the copy-to-`~/.kube/config` and the `export KUBECONFIG` recipes.

## Full Citations

[1] The Kubernetes Authors. "Organizing Cluster Access Using kubeconfig Files". kubernetes.io. https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig/. Accessed 2026-06-07.
[2] The Kubernetes Authors. "Pods". kubernetes.io. https://kubernetes.io/docs/concepts/workloads/pods/. Accessed 2026-06-07.
[3] The Kubernetes Authors. "Service". kubernetes.io. https://kubernetes.io/docs/concepts/services-networking/service/. Accessed 2026-06-07.
[4] SUSE / k3s Authors. "Quick-Start Guide". docs.k3s.io. https://docs.k3s.io/quick-start. Last updated 2026-06-03. Accessed 2026-06-07.
[4a] SUSE / k3s Authors. "Networking Services". docs.k3s.io. https://docs.k3s.io/networking/networking-services. Accessed 2026-06-07.
[5] The Kubernetes Authors. "kubectl Reference". kubernetes.io. https://kubernetes.io/docs/reference/kubectl/. Accessed 2026-06-07.
