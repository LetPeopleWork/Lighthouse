#!/usr/bin/env bash
# Kubernetes cannot set an environment variable from an init container, so the upgrade step leaves the
# data directory it chose in a file. The image's own entrypoint then starts Postgres on it unchanged.
set -euo pipefail

PGDATA="$(cat /decision/pgdata)"
export PGDATA
exec docker-entrypoint.sh postgres
