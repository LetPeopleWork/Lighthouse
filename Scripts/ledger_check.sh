#!/usr/bin/env bash
# PreToolUse hook wrapper: cheap prefilter so only `git commit` commands pay the
# Python startup. The tool-call JSON arrives on stdin; forward it to the checker
# only when it is a commit, otherwise allow immediately.
set -euo pipefail

input=$(cat)
case "$input" in
*'git commit'*) ;;
*)
	exit 0
	;;
esac

root="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
printf '%s' "$input" | python3 "$root/Scripts/ledger_check.py"
