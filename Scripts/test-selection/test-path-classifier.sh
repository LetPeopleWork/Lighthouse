#!/usr/bin/env bash
# Test the path-classifier.sh shared library used by both ci_changes.yml and
# dev-test.sh. Each synthetic diff list is fed to the classifier; expected
# outputs are asserted with set -e + explicit failure messages.

set -u
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./path-classifier.sh
. "$script_dir/path-classifier.sh"

pass=0
fail=0

assert() {
  local desc=$1
  local actual=$2
  local expected=$3
  if [ "$actual" = "$expected" ]; then
    pass=$((pass + 1))
  else
    fail=$((fail + 1))
    echo "FAIL: $desc"
    echo "  expected: $expected"
    echo "  actual:   $actual"
  fi
}

# Scenario 1: only Jira connector code changed
diff_jira_only=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/Jira/JiraWorkTrackingConnector.cs\nLighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration/Adapters/Connectors/Jira/JiraWriteBackTest.cs'
assert "jira-only: jira=true"            "$(classify_jira "$diff_jira_only")"     "true"
assert "jira-only: ado=false"            "$(classify_ado "$diff_jira_only")"      "false"
assert "jira-only: linear=false"         "$(classify_linear "$diff_jira_only")"   "false"
assert "jira-only: shared=false"         "$(classify_shared "$diff_jira_only")"   "false"

# Scenario 2: only ADO connector code changed
diff_ado_only=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs'
assert "ado-only: ado=true"              "$(classify_ado "$diff_ado_only")"       "true"
assert "ado-only: jira=false"            "$(classify_jira "$diff_ado_only")"      "false"
assert "ado-only: linear=false"          "$(classify_linear "$diff_ado_only")"    "false"
assert "ado-only: shared=false"          "$(classify_shared "$diff_ado_only")"    "false"

# Scenario 3: only Linear connector test changed
diff_linear_only=$'Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration/Adapters/Connectors/Linear/LinearWorkTrackingConnectorTest.cs'
assert "linear-only: linear=true"        "$(classify_linear "$diff_linear_only")" "true"
assert "linear-only: ado=false"          "$(classify_ado "$diff_linear_only")"    "false"
assert "linear-only: jira=false"         "$(classify_jira "$diff_linear_only")"   "false"
assert "linear-only: shared=false"       "$(classify_shared "$diff_linear_only")" "false"

# Scenario 4: shared connector base (OAuth subfolder)
diff_shared_oauth=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/OAuth/OAuthSchemaExtensions.cs'
assert "shared-oauth: shared=true"       "$(classify_shared "$diff_shared_oauth")" "true"

# Scenario 5: shared connector Auth subfolder
diff_shared_auth=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/Auth/WorkTrackingAuthStrategyFactory.cs'
assert "shared-auth: shared=true"        "$(classify_shared "$diff_shared_auth")"  "true"

# Scenario 6: shared connector top-level (AuthenticationMethodSchema.cs)
diff_shared_top=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/AuthenticationMethodSchema.cs'
assert "shared-top: shared=true"         "$(classify_shared "$diff_shared_top")"   "true"

# Scenario 7: shared interfaces
diff_shared_iface=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Application/IWorkTrackingConnector.cs'
assert "shared-iface: shared=true"       "$(classify_shared "$diff_shared_iface")" "true"

# Scenario 8: shared models
diff_shared_model=$'Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemConnection.cs'
assert "shared-model: shared=true"       "$(classify_shared "$diff_shared_model")" "true"

# Scenario 8b: the connector enum counts as shared
diff_shared_enum=$'Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystems.cs'
assert "shared-enum: shared=true"        "$(classify_shared "$diff_shared_enum")"  "true"

# Scenario 9: Program.cs counts as shared (DI for all connectors)
diff_program=$'Lighthouse.Backend/Lighthouse.Backend/Program.cs'
assert "program: shared=true"            "$(classify_shared "$diff_program")"      "true"

# Scenario 10: csproj counts as shared
diff_csproj=$'Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj'
assert "csproj: shared=true"             "$(classify_shared "$diff_csproj")"       "true"

# Scenario 11: only docs / FE / unrelated -> all false
diff_docs_only=$'docs/feature/test-speed-improvements/feature-delta.md\nLighthouse.Frontend/src/main.tsx'
assert "docs-only: jira=false"           "$(classify_jira "$diff_docs_only")"      "false"
assert "docs-only: ado=false"            "$(classify_ado "$diff_docs_only")"       "false"
assert "docs-only: linear=false"         "$(classify_linear "$diff_docs_only")"    "false"
assert "docs-only: shared=false"         "$(classify_shared "$diff_docs_only")"    "false"

# Scenario 12: mixed Jira + shared -> jira true, shared also true (shared wins in caller)
diff_mixed=$'Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/Jira/JiraWorkTrackingConnector.cs\nLighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Adapters/Connectors/OAuth/OAuthSchemaExtensions.cs'
assert "mixed: jira=true"                "$(classify_jira "$diff_mixed")"          "true"
assert "mixed: shared=true"              "$(classify_shared "$diff_mixed")"        "true"

# Scenario 13: only GitHub service / its integration test changed
diff_github_only=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/GitHubService.cs\nLighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/GithubServiceTest.cs'
assert "github-only: github=true"        "$(classify_github "$diff_github_only")"  "true"
assert "github-only: jira=false"         "$(classify_jira "$diff_github_only")"    "false"
assert "github-only: shared=false"       "$(classify_shared "$diff_github_only")"  "false"

# Scenario 14: LighthouseReleaseService (depends on GitHub) -> github=true
diff_release=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/LighthouseReleaseService.cs'
assert "release: github=true"            "$(classify_github "$diff_release")"      "true"

# Scenario 15: connectors / docs do NOT trigger the GitHub suite
assert "jira-only: github=false"         "$(classify_github "$diff_jira_only")"    "false"
assert "docs-only: github=false"         "$(classify_github "$diff_docs_only")"    "false"

# Scenario 16: the backend suite runs for backend sources, backend tests and test-timing scripts
assert "backend: backend source=true"    "$(classify_backend "$diff_shared_enum")"  "true"
assert "backend: backend tests=true"     "$(classify_backend "$diff_linear_only")"  "true"
assert "backend: test-timings=true"      "$(classify_backend $'Scripts/test-timings/merge.sh')" "true"
assert "backend: docs+frontend=false"    "$(classify_backend "$diff_docs_only")"    "false"
assert "backend: .github/actions=true"   "$(classify_backend $'.github/actions/publish/action.yml')" "true"
assert "backend: root Dockerfile=true"   "$(classify_backend $'Dockerfile')" "true"
assert "backend: other Dockerfile=false" "$(classify_backend $'examples/keycloak/Dockerfile')" "false"

# Scenario 17: a change only under .github/actions/ runs the backend suite but no connector suite
diff_actions_only=$'.github/actions/build-backend/action.yml'
assert "actions-only: backend=true"        "$(classify_backend "$diff_actions_only")"    "true"
assert "actions-only: jira=false"          "$(classify_jira "$diff_actions_only")"       "false"
assert "actions-only: ado=false"           "$(classify_ado "$diff_actions_only")"        "false"
assert "actions-only: linear=false"        "$(classify_linear "$diff_actions_only")"     "false"
assert "actions-only: servicenow=false"    "$(classify_servicenow "$diff_actions_only")" "false"
assert "actions-only: github connector=false" "$(classify_github "$diff_actions_only")"     "false"
assert "actions-only: shared=false"        "$(classify_shared "$diff_actions_only")"     "false"
assert "frontend manifest: backend=false"  "$(classify_backend $'Lighthouse.Frontend/package.json')" "false"
assert "docs readme: backend=false"        "$(classify_backend $'docs/readme.md')"       "false"

# Scenario 18: the parent-source code decides what the Jira connector reads, so it runs the live Jira suite
for parent_source_path in \
  Lighthouse.Backend/Lighthouse.Backend/Models/ParentSources.cs \
  Lighthouse.Backend/Lighthouse.Backend/Models/ParentSourceEntry.cs \
  Lighthouse.Backend/Lighthouse.Backend/Models/ParentReadings.cs \
  Lighthouse.Backend/Lighthouse.Backend/Models/ParentChoice.cs \
  Lighthouse.Backend/Lighthouse.Backend/Models/ParentResolution.cs \
  Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/Domain/FetchFingerprint.cs
do
  assert "parent sources: jira=true for $parent_source_path"    "$(classify_jira "$parent_source_path")"   "true"
  assert "parent sources: ado=false for $parent_source_path"    "$(classify_ado "$parent_source_path")"    "false"
  assert "parent sources: shared=false for $parent_source_path" "$(classify_shared "$parent_source_path")" "false"
done

# The selector now lives in the Tracker integration Domain, which every live suite shares, so it also selects them.
parent_source_selector=Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration/Domain/ParentSourceSelector.cs
assert "parent sources: jira=true for $parent_source_selector" "$(classify_jira "$parent_source_selector")" "true"
assert "parent sources: ado=false for $parent_source_selector" "$(classify_ado "$parent_source_selector")" "false"

# Scenario 19: files that change often stay out, because every live run spends Jira quota
assert "work item base: jira=false"      "$(classify_jira $'Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemBase.cs')" "false"
assert "options owner: jira=false"       "$(classify_jira $'Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs')" "false"
assert "other work item service: jira=false" "$(classify_jira $'Lighthouse.Backend/Lighthouse.Backend/Modules/WorkItems/Application/WorkItemService.cs')" "false"
assert "parent source tests: jira=false" "$(classify_jira $'Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration/Domain/ParentSourceSelectorTest.cs')" "false"

# ---------------------------------------------------------------------------------------------------------------------
# Tracker integration on its module path. The connectors and their shared code live in
# Modules/TrackerIntegration; a change there must select the live suites it can break,
# or a connector change would reach main without its live suite ever running.
module=Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration
module_tests=Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/TrackerIntegration

live_suite_flags() {
  echo "jira=$(classify_jira "$1") ado=$(classify_ado "$1") linear=$(classify_linear "$1") servicenow=$(classify_servicenow "$1") shared=$(classify_shared "$1")"
}

# Scenario 20: a connector's own code or test on the module path selects that tracker's live suite, and only it
for tracker_case in Jira:jira AzureDevOps:ado Linear:linear ServiceNow:servicenow; do
  tracker=${tracker_case%%:*}
  suite=${tracker_case##*:}
  expected="jira=false ado=false linear=false servicenow=false shared=false"
  expected=${expected/$suite=false/$suite=true}
  assert "module connector code: only the $tracker live suite runs" \
    "$(live_suite_flags "$module/Adapters/Connectors/$tracker/${tracker}Connector.cs")" "$expected"
  assert "module connector test: only the $tracker live suite runs" \
    "$(live_suite_flags "$module_tests/Adapters/Connectors/$tracker/${tracker}ConnectorTest.cs")" "$expected"
done

# Scenario 21: the parent-source selector decides what the Jira connector reads, on the module path too
assert "module parent-source selector: jira=true" "$(classify_jira "$module/Domain/ParentSourceSelector.cs")" "true"

# Scenario 22: code every connector shares, on the module path, runs every live suite
for shared_path in \
  "$module/Adapters/Connectors/Auth/WorkTrackingAuthStrategyFactory.cs" \
  "$module/Adapters/Connectors/OAuth/OAuthSchemaExtensions.cs" \
  "$module_tests/Adapters/Connectors/Auth/WorkTrackingAuthStrategyFactoryTest.cs" \
  "$module_tests/Adapters/Connectors/OAuth/OAuthSchemaExtensionsTest.cs" \
  "$module/Application/WorkTrackingConnectorFactory.cs" \
  "$module/Application/OAuth/OAuthService.cs" \
  "$module/Domain/AuthenticationMethodSchema.cs" \
  "$module/Adapters/OAuth/OAuthCallbackStore.cs"
do
  assert "module shared code: shared=true for $shared_path" "$(classify_shared "$shared_path")" "true"
done

# Scenario 23: module code no live connector depends on selects no live suite
for quiet_path in \
  "$module/Adapters/Connectors/Csv/CsvWorkTrackingConnector.cs" \
  "$module/Adapters/Background/ConnectionHealthBackgroundService.cs"
do
  assert "module code outside the live connectors: no live suite for $quiet_path" \
    "$(live_suite_flags "$quiet_path")" "jira=false ado=false linear=false servicenow=false shared=false"
done

# Scenario 24: the examine command connector-throws-count finds the connector folders where they live.
# It is generated with the checkout's path built in, so each case builds it inside a small made-up checkout
# whose CSV, Linear and ServiceNow folders each hold one connector method that throws NotSupportedException
# and one commented-out throw that must not count.
examine_dir="$(cd "$script_dir/../examine" && pwd)"

throws_count_in_checkout_with_connectors_under() {
  local connectors_folder=$1
  local checkout
  checkout="$(mktemp -d)"
  mkdir -p "$checkout/Scripts/examine"
  find "$examine_dir" -maxdepth 1 -type f -exec cp {} "$checkout/Scripts/examine/" \;
  for folder in Csv Linear ServiceNow; do
    mkdir -p "$checkout/$connectors_folder/$folder"
    printf '%s\n' "class ${folder}Connector {" \
      "  void Write() => throw new NotSupportedException(\"cannot write\");" \
      "  // void Old() => throw new NotSupportedException();" \
      "}" > "$checkout/$connectors_folder/$folder/${folder}Connector.cs"
  done
  bash "$checkout/Scripts/examine/build-examine-commands" > /dev/null 2>&1
  local output exit_code=0
  output="$(bash "$checkout/Scripts/examine/bin/connector-throws-count" 2>&1)" || exit_code=$?
  rm -rf "$checkout"
  printf '%s\nexit: %s' "$output" "$exit_code"
}

expected_throws_count=$'Csv: 1\nLinear: 1\nServiceNow: 1\ntotal: 3\nexit: 0'
assert "connector-throws-count: counts the connectors on the module path" \
  "$(throws_count_in_checkout_with_connectors_under "$module/Adapters/Connectors")" "$expected_throws_count"

echo
echo "passed: $pass"
echo "failed: $fail"
[ $fail -eq 0 ] || exit 1
