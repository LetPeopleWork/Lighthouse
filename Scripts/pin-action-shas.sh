#!/usr/bin/env bash
# Helper to resolve action tag -> commit SHA for external actions used in workflows.
# This version automatically discovers external actions referenced with `uses:` in
# `.github/workflows/*.yml` and resolves non-SHA refs (tags/branches) to commit SHAs.
# NOTE: This script queries GitHub. Run locally with network access.

set -euo pipefail

USAGE="Usage: $0 [--apply] [--verbose]"

APPLY=false
VERBOSE=false

while [ "$#" -gt 0 ]; do
  case "$1" in
    --apply) APPLY=true; shift ;;
    --verbose) VERBOSE=true; shift ;;
    --help|-h) echo "$USAGE"; exit 0; ;;
    *) echo "Unknown arg: $1"; echo "$USAGE"; exit 2 ;;
  esac
done

# Find workflow files
mapfile -t WORKFLOW_FILES < <(find .github/workflows -type f \( -name '*.yml' -o -name '*.yaml' \) 2>/dev/null || true)
if [ ${#WORKFLOW_FILES[@]} -eq 0 ]; then
  echo "No workflow files found under .github/workflows. Exiting."
  exit 0
fi

# Extract uses: entries (handles quoted and unquoted values) and filter out local and docker uses
USAGES_RAW=$(grep -h -E "uses:\s*" "${WORKFLOW_FILES[@]}" || true)
mapfile -t USAGE_VALUES < <(printf '%s\n' "$USAGES_RAW" | sed -n "s/.*uses:[[:space:]]*['\"]\?\([^'\"[:space:]]\+\)['\"]\?.*/\1/p" | sed 's/#.*//' | grep -Ev '^\./|^docker://' | sort -u || true)

if [ ${#USAGE_VALUES[@]} -eq 0 ]; then
  echo "No external actions found in workflows. Nothing to do."
  exit 0
fi

declare -A RESOLVED_MAP

# Helper: resolve a ref -> sha for a repo (owner/repo)
resolve_ref() {
  local owner_repo="$1"
  local ref="$2"
  local sha=""

  # If ref already looks like a 40-char SHA, return it
  if printf '%s' "$ref" | grep -Eq '^[0-9a-f]{40}$'; then
    echo "$ref"
    return 0
  fi

  # Try tag refs
  sha=$(git ls-remote "https://github.com/${owner_repo}.git" "refs/tags/${ref}" 2>/dev/null | awk '{print $1}' | head -n1 || true)
  if [ -n "$sha" ]; then
    echo "$sha"
    return 0
  fi

  # Try branch or direct ref
  sha=$(git ls-remote "https://github.com/${owner_repo}.git" "${ref}" 2>/dev/null | awk '{print $1}' | head -n1 || true)
  if [ -n "$sha" ]; then
    echo "$sha"
    return 0
  fi

  # Try heads
  sha=$(git ls-remote "https://github.com/${owner_repo}.git" "refs/heads/${ref}" 2>/dev/null | awk '{print $1}' | head -n1 || true)
  if [ -n "$sha" ]; then
    echo "$sha"
    return 0
  fi

  # nothing found
  return 1
}

# Iterate over discovered usages
for u in "${USAGE_VALUES[@]}"; do
  # skip entries without an @ (shouldn't happen but be defensive)
  if ! printf '%s' "$u" | grep -q '@'; then
    [ "$VERBOSE" = true ] && echo "Skipping usage without @: $u"
    continue
  fi

  name=${u%@*}    # owner/repo[/subpath]
  ref=${u#*@}     # tag/branch/SHA

  # Determine owner/repo (first two path segments)
  owner_repo=$(printf '%s' "$name" | awk -F'/' '{print $1"/"$2}')
  if [ -z "$owner_repo" ]; then
    echo "Could not determine owner/repo for usage: $u" >&2
    continue
  fi

  # Skip if ref already a full SHA
  if printf '%s' "$ref" | grep -Eq '^[0-9a-f]{40}$'; then
    [ "$VERBOSE" = true ] && echo "Already pinned: $u"
    continue
  fi

  sha=$(resolve_ref "$owner_repo" "$ref" || true)
  if [ -z "$sha" ]; then
    echo "Warning: Could not resolve $owner_repo@$ref (usage: $u)" >&2
  else
    RESOLVED_MAP["$u"]=$sha
    [ "$VERBOSE" = true ] && echo "Resolved $u -> $sha"
  fi
done

# Report
if [ ${#RESOLVED_MAP[@]} -eq 0 ]; then
  echo "No refs resolved to SHAs. Exiting."
  exit 0
fi

echo "Planned replacements (dry-run):"
for k in "${!RESOLVED_MAP[@]}"; do
  printf '  %s -> %s\n' "$k" "${RESOLVED_MAP[$k]}"
done

if [ "$APPLY" != true ]; then
  echo "\nRun with --apply to perform replacements in workflow files."
  exit 0
fi

# Apply replacements
echo "Applying replacements..."
updated_files_count=0
for old in "${!RESOLVED_MAP[@]}"; do
  sha=${RESOLVED_MAP[$old]}
  name=${old%@*}
  new="${name}@${sha}"

  # Find files containing the exact old string (fixed string search)
  mapfile -t files_to_update < <(grep -R --line-number --with-filename -F -e "$old" .github 2>/dev/null | cut -d: -f1 | sort -u || true)
  for file in "${files_to_update[@]}"; do
    if [ -z "$file" ]; then
      continue
    fi

    # Use perl if available for safe literal replacement; fall back to sed with escaped pattern
    if command -v perl >/dev/null 2>&1; then
      perl -0777 -pe "s/\Q$old\E/$new/g" "$file" > "${file}.tmp" && mv "${file}.tmp" "$file"
      echo "  Updated $file"
      updated_files_count=$((updated_files_count+1))
    else
      # Escape | and & for sed
      esc_old=$(printf '%s' "$old" | sed -e 's/[\/&|]/\\&/g')
      esc_new=$(printf '%s' "$new" | sed -e 's/[\/&|]/\\&/g')
      sed -i "s|$esc_old|$esc_new|g" "$file"
      echo "  Updated $file"
      updated_files_count=$((updated_files_count+1))
    fi
  done
done

if [ $updated_files_count -gt 0 ]; then
  echo "\nDone. Updated $updated_files_count files. Please review and commit changes."
else
  echo "\nNo files updated. It may be that usages are already pinned or not present in .github files."
fi

exit 0
