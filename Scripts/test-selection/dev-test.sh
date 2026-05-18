#!/usr/bin/env bash
# Local equivalent of the CI backend test-selection logic from slice-03b / CS-H.
# Detects which connector folders the current branch's diff touches, builds
# the matching `dotnet test --filter` string, and runs the backend test
# project. Mirrors `.github/workflows/ci_backend.yml`'s Compute Test Backend
# filter step plus the per-connector outputs from `ci_changes.yml`.
#
# Usage:
#   Scripts/test-selection/dev-test.sh             # PR-like: only run what your diff touches
#   Scripts/test-selection/dev-test.sh --full      # full suite (every integration category)
#   Scripts/test-selection/dev-test.sh --unit-only # skip every integration category
#   Scripts/test-selection/dev-test.sh --dry-run   # print the resolved filter and exit
#   Scripts/test-selection/dev-test.sh --help

set -eu

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./path-classifier.sh
. "$script_dir/path-classifier.sh"

mode="auto"
dry_run="false"

while [ $# -gt 0 ]; do
  case "$1" in
    --full)      mode="full";      shift ;;
    --unit-only) mode="unit-only"; shift ;;
    --dry-run)   dry_run="true";   shift ;;
    -h|--help)
      sed -n '2,/^set -eu/{/^set -eu/d; s/^# \?//; p; }' "$0"
      exit 0 ;;
    *)
      echo "unknown flag: $1 (use --help)" >&2
      exit 2 ;;
  esac
done

# Resolve the diff base. Prefer origin/main when available so local branch
# work mirrors what CI would see for the PR.
diff_base="origin/main"
if ! git rev-parse --verify --quiet "$diff_base" > /dev/null 2>&1; then
  diff_base="HEAD^"
fi

diff_list="$(git diff --name-only "$diff_base"...HEAD 2>/dev/null || true)"

build_filter() {
  local parts=("Category!=Integration")
  case "$mode" in
    unit-only)
      ( IFS='|'; echo "${parts[*]}" )
      return ;;
    full)
      parts+=("Category=Integration")
      ( IFS='|'; echo "${parts[*]}" )
      return ;;
  esac

  # auto mode: classify the diff and include each touched connector's tag
  local shared
  shared=$(classify_shared "$diff_list")
  if [ "$shared" = "true" ]; then
    parts+=("Category=Integration")
  else
    [ "$(classify_jira   "$diff_list")" = "true" ] && parts+=("Category=JiraIntegration")
    [ "$(classify_ado    "$diff_list")" = "true" ] && parts+=("Category=AdoIntegration")
    [ "$(classify_linear "$diff_list")" = "true" ] && parts+=("Category=LinearIntegration")
    # GitHub-side integration tests are tiny; always include.
    parts+=("Category=GithubIntegration")
  fi
  ( IFS='|'; echo "${parts[*]}" )
}

filter="$(build_filter)"
echo "dev-test mode=$mode diff_base=$diff_base"
echo "  changed paths:"
echo "$diff_list" | sed 's/^/    /' | head -20
echo "  resolved filter: $filter"
if [ "$dry_run" = "true" ]; then
  exit 0
fi

repo_root="$(git rev-parse --show-toplevel)"
exec dotnet test "$repo_root/Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj" --filter "$filter"
