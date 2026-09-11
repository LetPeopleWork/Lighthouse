#!/usr/bin/env bash
#
# Fails if any public-facing file still describes Lighthouse itself as open
# source or MIT licensed.
#
# Lighthouse is source available, not open source. Saying otherwise on a
# surface a prospect reads — the README, the docs site, a string in the running
# app — contradicts the LICENSE file, and the contradiction is what a reader
# notices first. This check is the repeatable answer to "did we get them all?".
#
# It does NOT flag true statements about other people's software (Postgres and
# Keycloak really are open source), the licence documents themselves (they must
# state the MIT history), or third-party dependency inventories. Those are
# listed one by one below with a reason each, because a check whose exclusions
# are unexplained gets widened by the next person who hits a false positive.
#
# Usage:  Scripts/check_license_claims.sh [--verbose]
# Exit:   0 clean, 1 offending claims found

set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.." || exit 2

VERBOSE=0
[[ "${1:-}" == "--verbose" ]] && VERBOSE=1

PATTERN='open[ -]source|\bMIT\b'

# Paths never scanned.
#
# docs/feature, docs/product and docs/architecture are wave workspaces and
# design records. They are not published — docs/_config.yml excludes all three
# from the Jekyll build — so they are internal notes, and they discuss the
# licence history on purpose.
#
# docs/_site is build output. tools/codesign holds a CI runner's own checkout.
# This script skips itself: it is full of the very phrases it hunts for.
skip_path() {
  case "$1" in
    docs/feature/*|docs/product/*|docs/architecture/*) return 0 ;;
    docs/_site/*|tools/codesign/*)                     return 0 ;;
    Scripts/check_license_claims.sh)                   return 0 ;;
    *) return 1 ;;
  esac
}

# Permitted matches: "<path glob>|<regex the matched line must satisfy>|<why>".
# A hit is allowed only when BOTH its path and its text match an entry.
ALLOW=(
  "LICENSE|MIT|the licence itself records which releases remain under MIT"
  "LICENSE|not open source|the licence says plainly that it is not open source; that is the point"
  "NOTICE|MIT|the notice preserves the MIT text for releases that predate the change"
  "NOTICE|third-party open-source components|describes bundled dependencies, not Lighthouse"
  "README.md|Releases up to and including v26.9.9.9 remain MIT|states the historical grant, which readers need"
  "docs/LICENSE|MIT|the just-the-docs Jekyll theme's own MIT licence, (c) 2022 just-the-docs, not ours"
  # The licensing page is the one page that has to discuss both terms to explain
  # the change. Allowed phrase by phrase rather than file-wide, so that writing
  # "Lighthouse is open source" on that page still fails.
  "docs/licensing/licensing.md|It is not open source under|denies the label; that is the sentence's whole job"
  "docs/licensing/licensing.md|published under the MIT License from 2025 until|states the historical grant a reader needs"
  "docs/licensing/licensing.md|The MIT License permitted both|explains why the licence changed"
  "docs/Installation/configuration.md|Postgres is an open-source|true statement about Postgres, not about Lighthouse"
  "docs/Installation/authentication.md|Keycloak.*open-source|true statement about Keycloak, not about Lighthouse"
  "docs/releasenotes/releasenotes.md|Open-Source Software \(OSS\) Attribution|names the section listing third-party components we bundle"
  "docs/releasenotes/releasenotes.md|open-source components bundled|describes third-party components, not Lighthouse"
  "*/sbom/*.cdx.json|MIT|generated inventory of third-party dependency licences"
  "*/sbom/*.cdx.json|open.source|same, generated"
  "*StateMappingsEditor.test.tsx|Open source dropdown|opens a dropdown listing work-tracking sources; nothing to do with licensing"
  "*ThirdPartyPackagesSection.test.tsx|MIT|asserts the third-party licence list renders"
)

allowed() {
  local path="$1" text="$2" entry glob rx
  for entry in "${ALLOW[@]}"; do
    glob="${entry%%|*}"
    rx="${entry#*|}"; rx="${rx%%|*}"
    # shellcheck disable=SC2053  # glob matching is the point
    if [[ "$path" == $glob ]] && [[ "$text" =~ $rx ]]; then
      return 0
    fi
  done
  return 1
}

fail=0
allowed_count=0

while IFS= read -r -d '' file; do
  skip_path "$file" && continue
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    lineno="${hit%%:*}"
    text="${hit#*:}"
    if allowed "$file" "$text"; then
      allowed_count=$((allowed_count + 1))
      [[ $VERBOSE -eq 1 ]] && printf '  allowed  %s:%s\n' "$file" "$lineno"
      continue
    fi
    if [[ $fail -eq 0 ]]; then
      echo "Lighthouse is described as open source or MIT on these surfaces:"
      echo
    fi
    fail=1
    printf '  %s:%s\n      %s\n' "$file" "$lineno" "$(echo "$text" | sed 's/^[[:space:]]*//' | cut -c1-120)"
  done < <(grep -nIiE "$PATTERN" -- "$file" 2>/dev/null)
done < <(git ls-files -z)

echo
if [[ $fail -eq 1 ]]; then
  echo "FAIL — each line above must say source available, or name the other"
  echo "       software it is actually describing."
  echo
  echo "If a hit is a true statement about something other than Lighthouse,"
  echo "add it to ALLOW in this script with the reason, rather than widening"
  echo "the pattern."
  exit 1
fi

printf 'PASS — no surface describes Lighthouse as open source or MIT licensed.\n'
printf '       %d known-good matches skipped (run with --verbose to list them).\n' "$allowed_count"
