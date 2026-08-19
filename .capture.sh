#!/bin/sh
for i in $(seq 1 120); do
  body=$(curl -s http://localhost:5199/api/v1/features)
  case "$body" in
    "[]"|"") sleep 1;;
    *) printf '%s' "$body" > "$HOME/lighthouse-e2e-local/features-capture.json"; exit 0;;
  esac
done
