#!/usr/bin/env bash
# Helper to resolve action tag -> commit SHA for external actions used in workflows.
# NOTE: This script queries GitHub. Run locally with network access.

set -euo pipefail

ACTIONS=(
  "actions/checkout@v4"
  "actions/setup-node@v4"
  "pnpm/action-setup@v4"
  "actions/setup-dotnet@v3"
  "actions/setup-java@v3"
  "actions/cache@v4"
  "docker/login-action@v2"
  "docker/setup-buildx-action@v2"
  "sigstore/cosign-installer@v3.5.0"
  "actions/download-artifact@v4"
  "actions/upload-artifact@v4"
  "ncipollo/release-action@v1"
  "SonarSource/sonarqube-scan-action@v7"
  "vimtor/action-zip@v1.2"
  "dawidd6/action-download-artifact@v6"
  "peter-evans/create-pull-request@v6"
  "dependabot/fetch-metadata@v2"
  "geekyeggo/delete-artifact@v5"
)

APPLY=false
if [ "${1-}" = "--apply" ]; then
  APPLY=true
fi

declare -A RESOLVED

for act in "${ACTIONS[@]}"; do
  repo=${act%@*}
  tag=${act#*@}
  echo "Resolving $repo tag $tag..."
  sha=$(git ls-remote https://github.com/$repo.git refs/tags/$tag 2>/dev/null | awk '{print $1}' || true)
  if [ -z "$sha" ]; then
    echo "  Tag not found as tag ref; trying branch or tag by name..."
    sha=$(git ls-remote https://github.com/$repo.git $tag 2>/dev/null | awk '{print $1}' || true)
  fi
  if [ -z "$sha" ]; then
    echo "  Couldn't resolve SHA for $act. Please check repo/tag and try again."
  else
    echo "  $act -> $sha"
    RESOLVED["$act"]=$sha
    if [ "$APPLY" = true ]; then
      # Replace occurrences in workflows and actions
      pattern="${repo}@${tag}"
      replacement="${repo}@${sha}"
      echo "  Replacing occurrences of $pattern -> $replacement in workflows..."
      grep -R --line-number --with-filename -e "$pattern" .github || true
      grep -R --line-number --with-filename -e "$pattern" .github | cut -d: -f1 | sort -u | while read -r file; do
        sed -i.bak "s|${pattern}|${replacement}|g" "$file"
        echo "    Updated $file (backup at ${file}.bak)"
      done
    fi
  fi
done

if [ "$APPLY" = true ]; then
  echo "\nAll done. Please review .github/* files, run tests, and commit changes."
else
  echo "\nRun this script with --apply to perform replacements in workflow files once you're satisfied with the mapping."
fi

echo "\nNotes:"
echo "- Pinning actions to SHAs makes runs reproducible. Dependabot will create PRs to keep them up to date." 
echo "- Always review automated replacements before committing."
