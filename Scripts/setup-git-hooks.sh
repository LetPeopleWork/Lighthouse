#!/usr/bin/env bash
# Points git at the repo's committed hooks (.githooks). Run once per clone.
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
git -C "$repo_root" config core.hooksPath .githooks
chmod +x "$repo_root/.githooks/pre-push"

echo "core.hooksPath -> .githooks (pre-push SonarQube scan active)."
if ! command -v sonar >/dev/null 2>&1 && [ ! -x "$HOME/.local/share/sonarqube-cli/bin/sonar" ]; then
  echo "Note: SonarQube CLI not detected. Install it so the hook can run: https://cli.sonarqube.com" >&2
fi
