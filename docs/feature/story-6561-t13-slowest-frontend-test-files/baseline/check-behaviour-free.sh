#!/usr/bin/env bash
# Usage (from the repository root):
#   check-behaviour-free.sh <before-commit> <slice-X-moved-assertions.md> [--allow-extraction]
# Exits 1 when a test was weakened, when an `expect(` was removed without a row in the moved-assertions file, or when
# a production file changed beyond what the design allows. See check_behaviour_free.py for the rules.
set -eu
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
before="${1:?before-commit required}"
moved="${2:?moved-assertions file required}"
shift 2
exec python3 -I "$here/check_behaviour_free.py" --before "$before" --moved "$moved" "$@"
