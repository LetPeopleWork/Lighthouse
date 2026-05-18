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
diff_jira_only=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs\nLighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWriteBackTest.cs'
assert "jira-only: jira=true"            "$(classify_jira "$diff_jira_only")"     "true"
assert "jira-only: ado=false"            "$(classify_ado "$diff_jira_only")"      "false"
assert "jira-only: linear=false"         "$(classify_linear "$diff_jira_only")"   "false"
assert "jira-only: shared=false"         "$(classify_shared "$diff_jira_only")"   "false"

# Scenario 2: only ADO connector code changed
diff_ado_only=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs'
assert "ado-only: ado=true"              "$(classify_ado "$diff_ado_only")"       "true"
assert "ado-only: jira=false"            "$(classify_jira "$diff_ado_only")"      "false"
assert "ado-only: linear=false"          "$(classify_linear "$diff_ado_only")"    "false"
assert "ado-only: shared=false"          "$(classify_shared "$diff_ado_only")"    "false"

# Scenario 3: only Linear connector test changed
diff_linear_only=$'Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorTest.cs'
assert "linear-only: linear=true"        "$(classify_linear "$diff_linear_only")" "true"
assert "linear-only: ado=false"          "$(classify_ado "$diff_linear_only")"    "false"
assert "linear-only: jira=false"         "$(classify_jira "$diff_linear_only")"   "false"
assert "linear-only: shared=false"       "$(classify_shared "$diff_linear_only")" "false"

# Scenario 4: shared connector base (OAuth subfolder)
diff_shared_oauth=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/OAuth/OAuthSchemaExtensions.cs'
assert "shared-oauth: shared=true"       "$(classify_shared "$diff_shared_oauth")" "true"

# Scenario 5: shared connector Auth subfolder
diff_shared_auth=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Auth/WorkTrackingAuthStrategyFactory.cs'
assert "shared-auth: shared=true"        "$(classify_shared "$diff_shared_auth")"  "true"

# Scenario 6: shared connector top-level (AuthenticationMethodSchema.cs)
diff_shared_top=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs'
assert "shared-top: shared=true"         "$(classify_shared "$diff_shared_top")"   "true"

# Scenario 7: shared interfaces
diff_shared_iface=$'Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs'
assert "shared-iface: shared=true"       "$(classify_shared "$diff_shared_iface")" "true"

# Scenario 8: shared models
diff_shared_model=$'Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemConnection.cs'
assert "shared-model: shared=true"       "$(classify_shared "$diff_shared_model")" "true"

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
diff_mixed=$'Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs\nLighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/OAuth/OAuthSchemaExtensions.cs'
assert "mixed: jira=true"                "$(classify_jira "$diff_mixed")"          "true"
assert "mixed: shared=true"              "$(classify_shared "$diff_mixed")"        "true"

echo
echo "passed: $pass"
echo "failed: $fail"
[ $fail -eq 0 ] || exit 1
