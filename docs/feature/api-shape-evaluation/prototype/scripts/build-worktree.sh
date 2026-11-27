#!/usr/bin/env bash
# Builds the backend of a scratch git worktree (migrations first, as the backend references their DLLs).
# usage: build-worktree.sh <worktree-dir>
set -uo pipefail
wt="$1"
cd "$wt/Lighthouse.Backend" || exit 1
dotnet build Lighthouse.Migrations.Sqlite -v q -nologo || exit 2
dotnet build Lighthouse.Migrations.Postgres -v q -nologo || exit 3
dotnet build Lighthouse.Backend -v q -nologo || {
  find Lighthouse.Backend/bin -name Lighthouse.deps.json -delete
  dotnet build Lighthouse.Backend -v q -nologo || exit 4
}
echo "BUILD OK $wt"
