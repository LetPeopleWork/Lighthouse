#!/usr/bin/env bash
# Generates zod schemas, TS types, axios functions and MSW mocks from one OpenAPI document with orval.
# usage: generate.sh <openapi.json> <out-dir>   (run inside a folder with orval installed)
set -euo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
start=$(date +%s.%N)
INPUT="$1" OUT="$2" npx orval --config "$here/orval.config.mjs"
end=$(date +%s.%N)
python3 -I - "$start" "$end" <<'EOF'
import sys
print(f"generation took {float(sys.argv[2]) - float(sys.argv[1]):.1f} s")
EOF
