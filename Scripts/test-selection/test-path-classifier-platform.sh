#!/usr/bin/env bash
# The release code (the GitHub service and the Lighthouse release service, their interfaces and their tests)
# lives under Platform/Releases. A change there must still run the live GitHub suite in CI, or a release
# regression reaches main without the suite that talks to GitHub ever running. The rest of Platform/Releases
# and every other Platform area must not spend that suite, and must still run the backend suite.

set -u
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./path-classifier.sh
. "$script_dir/path-classifier.sh"

pass=0
fail=0

production=Lighthouse.Backend/Lighthouse.Backend/Platform
tests=Lighthouse.Backend/Lighthouse.Backend.Tests/Platform

only_backend_and_github="backend=true github=true jira=false ado=false linear=false servicenow=false shared=false"
only_backend="backend=true github=false jira=false ado=false linear=false servicenow=false shared=false"

suites_selected_for() {
  local changed=$1
  echo "backend=$(classify_backend "$changed") github=$(classify_github "$changed") jira=$(classify_jira "$changed") ado=$(classify_ado "$changed") linear=$(classify_linear "$changed") servicenow=$(classify_servicenow "$changed") shared=$(classify_shared "$changed")"
}

then_a_change_to_it_alone_selects() {
  local changed=$1
  local expected=$2
  local why=$3
  local actual
  actual="$(suites_selected_for "$changed")"
  if [ "$actual" = "$expected" ]; then
    pass=$((pass + 1))
  else
    fail=$((fail + 1))
    echo "FAIL: a change to $changed alone selects the wrong CI suites"
    echo "  expected: $expected"
    echo "  actual:   $actual"
    echo "  why:      $why"
    echo "  how:      make GITHUB_REGEX in path-classifier.sh select exactly the GitHubService and LighthouseReleaseService"
    echo "            files under Platform/Releases of the production and test projects, and leave every other pattern as it is"
  fi
}

release_code_needs_github="the release code talks to GitHub, so its change must run the live GitHub suite (and the backend suite)"
for release_path in \
  "$production/Releases/GitHubService.cs" \
  "$production/Releases/IGitHubService.cs" \
  "$production/Releases/LighthouseReleaseService.cs" \
  "$production/Releases/ILighthouseReleaseService.cs" \
  "$tests/Releases/GitHubServiceTest.cs" \
  "$tests/Releases/GithubServiceTest.cs" \
  "$tests/Releases/LighthouseReleaseServiceTest.cs" \
  "$tests/Releases/LighthouseReleaseServiceIntegrationTest.cs"
do
  then_a_change_to_it_alone_selects "$release_path" "$only_backend_and_github" "$release_code_needs_github"
done

other_platform_code_spares_github="this code does not talk to GitHub; each live GitHub run spends quota, so it runs the backend suite only"
for other_platform_path in \
  "$production/Releases/AssemblyService.cs" \
  "$production/Encryption/CryptoService.cs"
do
  then_a_change_to_it_alone_selects "$other_platform_path" "$only_backend" "$other_platform_code_spares_github"
done

echo
echo "passed: $pass"
echo "failed: $fail"
[ $fail -eq 0 ] || exit 1
