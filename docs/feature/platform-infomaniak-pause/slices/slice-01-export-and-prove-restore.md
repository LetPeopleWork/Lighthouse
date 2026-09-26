# Slice 01: Export everything, prove it restores

**Goal:** Tenant Zero's data and every platform secret exist outside Infomaniak, and a local restore
from them has worked.

**Stories:** US-1 (see `../feature-delta.md`).

## IN
- Unseal OpenBao if it is sealed.
- List every `ExternalSecret` across the cluster, which gives the full list of OpenBao KV paths to
  export.
- `bao kv get` each path into the password manager, together with the unseal keys and root token.
- `pg_dump` the lpw database (CNPG primary) to local storage after the last write.
- Restore the dump into a local Postgres and start a local Lighthouse with the exported encryption key.
- Check that Portfolios and Teams are present, and that a stored connection gets past authentication.

## OUT
- Anything destructive (that is slice 02).
- Exporting Prometheus/Grafana history. It is monitoring data, not needed for a respin.
- Automating the export as a script. It runs once, by hand.

## Learning hypothesis
Disproves "the lpw dump plus the exported OpenBao secrets are enough to rebuild Tenant Zero" if the local
restore cannot decrypt a stored credential, or if an `ExternalSecret` path was not exported. Confirms
that slice 02 can go ahead.

## Acceptance criteria
AC-1.1 … AC-1.4 in the feature-delta.

## Dependencies
Cluster reachable, operator credentials in hand (feature-delta Pre-requisites).

## Effort
About 2–3 h. Reference class: the slice-10 restore rehearsal, run against a local target this time.
