# Slice 07 — automated-provisioning

- **ADO story**: #5207 (Tenant provisioning automation)
- **job_id**: `job-saas-operator-onboard-tenant`
- **Band**: Productization payoff

## Learning hypothesis

> A **single declarative tenant record** (an ApplicationSet generator entry / tenant CR / values
> record — form named for DESIGN) expands into a complete isolated tenant — namespace, Postgres,
> store-sourced secrets, subdomain route, app — reconciled by ArgoCD, and removing the record tears
> it all down. If true, onboarding a customer is a minutes-long, reviewable, one-action commit. This
> is the SaaS payoff.

## Elevator Pitch

- **Before**: Onboarding a tenant means hand-copying the slice-06 artifacts and parameterising them.
- **After**: Benjamin adds one tenant record `{name: riverbank, subdomain: riverbank, plan: standard}` and merges → sees `argocd app list` show Riverbank's whole stack Synced/Healthy and `https://riverbank.lighthouse.letpeople.work` serving — in minutes.
- **Decision enabled**: "Can we onboard a customer with one commit?" — yes, isolated and reviewable.

## In / Out

- **IN**: A tenant generator that fans one record into the full isolated tenant; uniqueness validation on tenant id/subdomain/DB at PR time; one-action de-provision (record removal → full prune).
- **OUT**: A self-service signup UI (operator commits the record); billing/plans beyond a sizing parameter.

## Dogfood moment

Re-express Tenant Zero and Acme as generator records — they were hand-built; now they are produced by the same automation every customer uses (no special-casing production).

## Thin end-to-end path

Define the tenant generator → add one tenant record → merge → ArgoCD provisions ns+DB+secret+route+app → tenant serves → remove the record → everything is pruned (no orphans).

## Done = observable

- One committed tenant record yields a fully isolated, reachable tenant in minutes.
- Removing the record prunes the namespace, DB, secret, route and DNS with no orphaned resources.
- Tenant Zero and Acme are now produced by the generator, not bespoke.

## Depends on

- slice-06 (the hand-proven pattern to template).
