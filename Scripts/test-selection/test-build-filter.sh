#!/usr/bin/env bash
# Simulates the filter-building bash block from .github/workflows/ci_backend.yml
# (step: "Compute Test Backend filter") and asserts the emitted filter string
# is correct for representative trigger scenarios.
#
# Keep this script in sync with ci_backend.yml's Compute Test Backend filter
# step; if you change the logic in one place, change it here too.

set -u

build_filter() {
  # Inputs (env vars):
  #   JIRA_CONNECTOR, ADO_CONNECTOR, LINEAR_CONNECTOR, SERVICENOW_CONNECTOR, GITHUB_CONNECTOR, CONNECTOR_SHARED
  # Note: this project pushes directly to main; there is no PR-vs-push split.
  # Every trigger respects the per-connector inputs. Only CONNECTOR_SHARED=true
  # forces a full run (because shared paths can affect every connector).
  local parts=("Category!=Integration|(Category!=JiraIntegration&Category!=AdoIntegration&Category!=LinearIntegration&Category!=ServiceNowIntegration&Category!=GithubIntegration)")
  local force_full="false"
  if [ "${CONNECTOR_SHARED:-false}" == "true" ]; then
    force_full="true"
  fi
  if [ "$force_full" == "true" ]; then
    parts+=("(Category=Integration&Category!=UsageDataCanary)")
  else
    [ "${JIRA_CONNECTOR:-false}"   == "true" ] && parts+=("Category=JiraIntegration")
    [ "${ADO_CONNECTOR:-false}"    == "true" ] && parts+=("Category=AdoIntegration")
    [ "${LINEAR_CONNECTOR:-false}" == "true" ] && parts+=("Category=LinearIntegration")
    [ "${SERVICENOW_CONNECTOR:-false}" == "true" ] && parts+=("Category=ServiceNowIntegration")
    [ "${GITHUB_CONNECTOR:-false}" == "true" ] && parts+=("Category=GithubIntegration")
  fi
  ( IFS='|'; echo "${parts[*]}" )
}

pass=0
fail=0
assert() {
  local desc=$1 actual=$2 expected=$3
  if [ "$actual" = "$expected" ]; then pass=$((pass + 1))
  else
    fail=$((fail + 1))
    echo "FAIL: $desc"
    echo "  expected: $expected"
    echo "  actual:   $actual"
  fi
}

run_case() {
  local desc=$1 expected=$2
  local actual
  actual=$(build_filter)
  assert "$desc" "$actual" "$expected"
}

always_on='Category!=Integration|(Category!=JiraIntegration&Category!=AdoIntegration&Category!=LinearIntegration&Category!=ServiceNowIntegration&Category!=GithubIntegration)'
guarded_integration='(Category=Integration&Category!=UsageDataCanary)'

# Scenario 1: nothing relevant changed -> the always-on clause alone (no live suite runs)
JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "nothing relevant" "$always_on"

# Scenario 2: Jira only
JIRA_CONNECTOR=true  ADO_CONNECTOR=false LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "Jira only" "$always_on|Category=JiraIntegration"

# Scenario 3: ADO only
JIRA_CONNECTOR=false ADO_CONNECTOR=true  LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "ADO only" "$always_on|Category=AdoIntegration"

# Scenario 4: Linear only
JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=true  SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "Linear only" "$always_on|Category=LinearIntegration"

# Scenario 5: Jira + ADO
JIRA_CONNECTOR=true  ADO_CONNECTOR=true  LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "Jira + ADO" "$always_on|Category=JiraIntegration|Category=AdoIntegration"

# Scenario 6: shared base touched -> forced full
JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=false CONNECTOR_SHARED=true
run_case "shared -> forced full" "$always_on|$guarded_integration"

# Scenario 7: shared touched + per-connector inputs ignored when shared is true
JIRA_CONNECTOR=true  ADO_CONNECTOR=true  LINEAR_CONNECTOR=true  SERVICENOW_CONNECTOR=true  GITHUB_CONNECTOR=true  CONNECTOR_SHARED=true
run_case "shared overrides per-connector" "$always_on|$guarded_integration"

# Scenario 8: all connectors touched, no shared -> all five sub-categories
JIRA_CONNECTOR=true  ADO_CONNECTOR=true  LINEAR_CONNECTOR=true  SERVICENOW_CONNECTOR=true GITHUB_CONNECTOR=true  CONNECTOR_SHARED=false
run_case "all connectors" "$always_on|Category=JiraIntegration|Category=AdoIntegration|Category=LinearIntegration|Category=ServiceNowIntegration|Category=GithubIntegration"

# Scenario 9: GitHub only -> the always-on clause + GitHub suite (the path-scoped flake fix)
JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=false GITHUB_CONNECTOR=true  CONNECTOR_SHARED=false
run_case "GitHub only" "$always_on|Category=GithubIntegration"

# Scenario 10: ServiceNow only -> the always-on clause + ServiceNow suite
JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false SERVICENOW_CONNECTOR=true  GITHUB_CONNECTOR=false CONNECTOR_SHARED=false
run_case "ServiceNow only" "$always_on|Category=ServiceNowIntegration"

echo
echo "passed: $pass"
echo "failed: $fail"
[ $fail -eq 0 ] || exit 1
