#!/usr/bin/env bash
# Starts a scratch worktree's backend on a free port with an empty SQLite file, saves its OpenAPI
# document, and stops it again.
# usage: run-and-fetch-doc.sh <worktree-dir> <port> <out.json>
set -uo pipefail
wt="$1"; port="$2"; out="$3"
state="$wt/.proto-state"; mkdir -p "$state"
cd "$wt/Lighthouse.Backend/Lighthouse.Backend" || exit 1
bin=$(ls -d bin/Debug/net*/linux-x64/Lighthouse 2>/dev/null | head -1)
[ -z "$bin" ] && bin=$(ls -d bin/Debug/net*/Lighthouse | head -1)
ASPNETCORE_ENVIRONMENT=Development "./$bin" --contentRoot "$PWD" \
  --Kestrel:Endpoints:Http:Url=http://localhost:$port \
  --Kestrel:Endpoints:Https:Url=https://localhost:$((port + 1000)) \
  --Database:Provider=sqlite "--Database:ConnectionString=Data Source=$state/lh.db" \
  --Encryption:KeyStorePath=$state/keys --Serilog:WriteTo:0:Args:path=$state/logs/log-.txt \
  > "$state/stdout.log" 2>&1 &
pid=$!
for _ in $(seq 1 90); do
  if curl -sf "http://localhost:$port/swagger/v1/swagger.json" -o "$out"; then
    echo "saved $out from $bin"
    kill "$pid"; wait "$pid" 2>/dev/null
    exit 0
  fi
  sleep 2
done
echo "no document; last log lines:"; tail -20 "$state/stdout.log"
kill "$pid"
exit 1
