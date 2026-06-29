# ADR-088: Substrate Abstraction Boundary — the OpenTofu Module Outputs a *Conformant-Cluster Contract*, Not a Provider API; the Primary Adapter Is Infomaniak Managed Kubernetes (Swiss, via the OpenTofu Connector), with k3s-on-Compute as the Portable Fallback Adapter (Hetzner EU / Any OpenStack)

**Status**: **ACCEPTED** (2026-06-29, Benjamin). **O-1 RESOLVED 2026-06-29** by live web verification (see Finding) — the prior "managed-k8s unconfirmed" caveat is retired. Primary = Infomaniak managed k8s (Swiss); fallback = k3s-on-compute (Hetzner EU / any OpenStack).
**Date**: 2026-06-29 (revised same day after O-1 web verification)
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5320 substrate, #5202 routing) — converges cross-cutting decision **CC-4** and resolves Luna's open question on the Infomaniak Kubernetes mode (O-1)
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: Honours D0b (vendor-neutral, OpenTofu multi-provider). Everything above this boundary (ADR-086 GitOps, ADR-087 secrets, ADR-091 DB, ADR-090 observability) consumes only the conformant-cluster contract, never a provider API or a specific cluster bring-up mechanism. Hetzner (EU) and AWS-EKS parity (deferred, slice-12) land *behind this same boundary*.

---

## Context

CC-4 asks where the provider-neutral surface ends and the provider-specific surface begins, and what the platform requires of "any conformant cluster". Luna left one open question (O-1): **does Infomaniak Public Cloud offer a managed-Kubernetes service, or must the module stand up Kubernetes on raw OpenStack compute?**

**Finding (O-1 RESOLVED 2026-06-29 via live web verification — supersedes the 2026-01 cutoff assumption):** In **January 2026 Infomaniak launched a first-class managed Kubernetes service** in its sovereign Swiss Public Cloud. Confirmed properties:
- Infomaniak **manages the control plane** — creation, high availability, security, and updates on rolling-update principles.
- Provisionable via the console, the **API**, and a **Terraform / OpenTofu connector** — so it fits the OpenTofu substrate module directly.
- A **free shared control-plane plan**: shared control plane, one API-server replica, a shared datastore, **up to 10 nodes, no SLA** — explicitly positioned for testing/familiarisation. A **dedicated control plane** is €0.03604/hour.
- **CHF 300 free credit, valid 3 months**, usable across Public Cloud including Kubernetes.
- Entirely designed, operated and controlled **in Switzerland** (data sovereignty; no extra-European dependency), alongside Octavia LoadBalancer, Cinder block storage, and S3-compatible object storage.

This **retires the prior planning assumption** (stand k8s up ourselves). The managed offering is now the **primary** bring-up. The self-stood k3s mechanism is **retained** — not as the Infomaniak default, but as the **portable fallback adapter** for providers without a managed k8s product (notably **Hetzner**, the chosen EU alternative: Hetzner Cloud exposes VMs/LB/volumes but no first-class managed k8s, so k3s-on-compute is the right fit there) and as the proof that the platform is not coupled to any one provider's managed offering.

**Provider strategy (operator decision, 2026-06-29):** start on **Infomaniak managed k8s** — Swiss data residency + a free tier to test on — and keep **Hetzner** as the **EU** fallback. Both sit behind the contract below.

## Decision

### 1. The boundary is a *contract*, not a provider surface or a bring-up mechanism

The OpenTofu substrate module exposes a small **conformant-cluster contract** as its outputs — and *nothing provider- or bring-up-specific leaks above it*:

| Contract requirement (what the platform layer needs) | Infomaniak managed k8s (primary) | k3s-on-compute fallback (Hetzner / any OpenStack) |
|---|---|---|
| A reachable Kubernetes API (a kubeconfig output) | provider-managed control plane | k3s server on a control-plane VM |
| **CNI with NetworkPolicy enforcement** (CC-1 isolation depends on it) | provider CNI — **probe-verified** (see Earned-Trust probe) | k3s default Flannel **replaced by Calico** (Flannel does not enforce NetworkPolicy) |
| An **ingress controller** | ingress-nginx (installed by ArgoCD, not the substrate); substrate only guarantees a `LoadBalancer` Service works | same |
| A **default `StorageClass`** (RWO) | provider CSI default | Cinder CSI (OpenStack) / hcloud-csi (Hetzner) set default |
| A **`Service type=LoadBalancer`** that gets an external IP | provider LB integration | Octavia (OpenStack) / hcloud LB via cloud-controller-manager |
| Outbound egress + DNS resolution | provider network | router + floating IP |

The platform layer (ADR-086 onward) is written against *this table*, never against a provider API or whether the control plane is managed. That is the vendor-neutrality guarantee: any new provider (Hetzner now, EKS at slice-12) must satisfy the same six rows and the platform is unchanged.

### 2. Primary bring-up = Infomaniak managed Kubernetes via the OpenTofu connector

The module's **primary adapter** calls Infomaniak's Terraform/OpenTofu Kubernetes connector to provision a managed cluster (free shared control plane for dev/test; dedicated control plane for production), plus the network and an N-node worker pool. Output = a kubeconfig + the conformant-cluster contract above. The control-plane lifecycle (HA, security, version updates) is the provider's responsibility — the lowest operational surface for a small operator, and the reason this is primary. `tofu destroy` removes every created resource (US-01 teardown AC).

### 3. Fallback bring-up = k3s on compute via cloud-init (Hetzner EU / any OpenStack)

For a provider with no managed k8s (Hetzner) or for full portability proof, a **second adapter** provisions a network + control-plane VM + worker pool + `cloud-init` that installs **k3s** (server + token-joined agents), **Flannel disabled and Calico applied** (NetworkPolicy enforcement for CC-1), the provider **cloud-controller-manager** (LoadBalancer + node lifecycle) and the **CSI** driver (default StorageClass). Output = the *same* kubeconfig + contract. **Why k3s** over CAPO/kubeadm for the fallback: a single conformant binary, reproducible from cloud-init, lowest op-surface at the ≥20-tenant density target, no bootstrap management cluster. CAPO is the recorded scale-out path (declarative node pools, rolling node upgrades) behind the *same* contract.

This is the "one module, provider-selected bring-up, no fork" discipline (mirrors ADR-080's DB modes): the bring-up mechanism is an adapter; the boundary does not move.

| Quality attribute | Weight | (A) Infomaniak managed k8s ✅ PRIMARY | (B) k3s on compute (Hetzner/OpenStack) ✅ FALLBACK | (C) Cluster API (CAPO) |
|---|---|---|---|---|
| Available on Infomaniak today | Highest | **Yes — GA Jan 2026, free tier** | Yes (on compute) | Yes (on compute) |
| Reproducible from `tofu apply` (US-01) | Highest | **Yes — OpenTofu connector** | cloud-init, fully declarative | needs a mgmt cluster first |
| Operational surface for a small team | High | **Lowest — provider runs the control plane** | Low (single binary, we own lifecycle) | Higher (mgmt cluster + CAPO controllers) |
| Data residency | High | **🇨🇭 Swiss (sovereign)** | 🇪🇺 EU on Hetzner; provider-dependent | provider-dependent |
| Vendor-neutral / portable | High | Couples to Infomaniak's offering (mitigated by the contract + the fallback adapter) | **Runs on any OpenStack/VM/Hetzner** | Portable across CAPI providers |
| Cost to start | High | **Free shared control plane + CHF 300 credit** | Compute cost only | Compute + mgmt overhead |
| Node-pool rolling upgrades at scale | Medium | **Native (managed)** | Manual / scripted | **Native (MachineDeployment)** |

## Consequences

- **Positive**: starting on Infomaniak managed k8s gives Swiss data sovereignty, a free tier to dogfood Tenant Zero on, and the lowest control-plane op-surface (no k8s lifecycle to own on the primary path). The contract abstraction means Hetzner (EU) and EKS (slice-12) are "satisfy the six rows", not "rewrite the platform". The retained k3s fallback proves — and preserves — portability, so adopting managed k8s does not lock the platform in.
- **Negative / cost**: the primary path couples cluster bring-up to Infomaniak's connector + offering (free-tier has no SLA — fine for test, move to a dedicated control plane for production fleet criticality); the fallback path (Hetzner) means we own the k3s/Calico/CCM/CSI lifecycle there. Two adapters to maintain, but both reduce to one contract.
- **Standalone gate**: untouched — the substrate is hosted-platform-only; the standalone product and the chart make no substrate assumption beyond "a conformant cluster", which is exactly this contract.

### Earned-Trust probe (CC-4 honesty — the substrate must prove it is conformant, managed or not)

A `substrate.probe` runs after `tofu apply` and before ArgoCD bootstrap (slice-01→02 gate). It asserts each contract row *empirically* in the real cluster, **regardless of whether the control plane is managed or self-stood** — a managed CNI can still fail to enforce NetworkPolicy, so the probe is not skipped on the managed path: (1) apply a deny-all `NetworkPolicy` + a test pod and assert cross-namespace traffic is **actually dropped** (catches a CNI that ignores NetworkPolicy); (2) create a `Service type=LoadBalancer` and assert an external IP is assigned within a timeout (catches missing LB integration); (3) create a PVC on the default `StorageClass` and assert it **binds** (catches a missing/default-less CSI). Any failure emits `health.startup.refused{component=substrate, lie=<no-networkpolicy|no-loadbalancer|no-default-storageclass>, suggested=<install Calico|enable LB CCM|set default StorageClass>}` and the substrate is not handed to the platform layer. (Self-application: re-runs after each managed-k8s version bump and each k3s/Calico/CCM/CSI bump on the fallback.)

## Alternatives considered

1. **Self-stood k3s as the *primary* (the prior planning assumption)** — superseded once O-1 confirmed Infomaniak managed k8s exists with a free tier + OpenTofu connector. k3s-on-compute is retained as the **fallback adapter** (Hetzner EU / any OpenStack), not discarded.
2. **kubeadm via cloud-init (fallback)** — rejected for the fallback in favour of k3s: more orchestration and bespoke join/upgrade scripting for no benefit at this scale.
3. **Cluster API Provider OpenStack (CAPO)** — deferred to scale-out: superior declarative node-pool lifecycle, but needs a bootstrap management cluster (chicken-and-egg) and more controllers; revisit when node-pool churn justifies it. Lives behind the same contract.
4. **Hetzner as the primary** — rejected as primary, kept as the **EU fallback**: no first-class managed k8s (would force the k3s-on-compute path) and EU- rather than Swiss-sovereign. Sensible second provider precisely because it exercises the fallback adapter and the contract portability.
5. **Bake ingress-nginx / cert-manager into the substrate module** — rejected: those are platform components owned by ArgoCD (ADR-086 `platform/`), not substrate; the substrate guarantees only that a `LoadBalancer` Service works. Keeps the boundary clean.
