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
  #   EVENT_NAME, JIRA_CONNECTOR, ADO_CONNECTOR, LINEAR_CONNECTOR, CONNECTOR_SHARED
  local parts=("Category!=Integration")
  local force_full="false"
  if [ "${EVENT_NAME:-}" != "pull_request" ]; then
    force_full="true"
  fi
  if [ "${CONNECTOR_SHARED:-false}" == "true" ]; then
    force_full="true"
  fi
  if [ "$force_full" == "true" ]; then
    parts+=("Category=Integration")
  else
    [ "${JIRA_CONNECTOR:-false}"   == "true" ] && parts+=("Category=JiraIntegration")
    [ "${ADO_CONNECTOR:-false}"    == "true" ] && parts+=("Category=AdoIntegration")
    [ "${LINEAR_CONNECTOR:-false}" == "true" ] && parts+=("Category=LinearIntegration")
    parts+=("Category=GithubIntegration")
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

# Scenario 1: PR touches nothing relevant -> unit + Github only
EVENT_NAME=pull_request JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "PR + nothing relevant" "Category!=Integration|Category=GithubIntegration"

# Scenario 2: PR touches Jira only
EVENT_NAME=pull_request JIRA_CONNECTOR=true  ADO_CONNECTOR=false LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "PR + Jira only" "Category!=Integration|Category=JiraIntegration|Category=GithubIntegration"

# Scenario 3: PR touches ADO only
EVENT_NAME=pull_request JIRA_CONNECTOR=false ADO_CONNECTOR=true  LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "PR + ADO only" "Category!=Integration|Category=AdoIntegration|Category=GithubIntegration"

# Scenario 4: PR touches Linear only
EVENT_NAME=pull_request JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=true  CONNECTOR_SHARED=false
run_case "PR + Linear only" "Category!=Integration|Category=LinearIntegration|Category=GithubIntegration"

# Scenario 5: PR touches Jira + ADO
EVENT_NAME=pull_request JIRA_CONNECTOR=true  ADO_CONNECTOR=true  LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "PR + Jira + ADO" "Category!=Integration|Category=JiraIntegration|Category=AdoIntegration|Category=GithubIntegration"

# Scenario 6: PR + shared base touched -> forced full
EVENT_NAME=pull_request JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false CONNECTOR_SHARED=true
run_case "PR + shared -> forced full" "Category!=Integration|Category=Integration"

# Scenario 7: push event (main / release tag) -> forced full regardless of inputs
EVENT_NAME=push JIRA_CONNECTOR=false ADO_CONNECTOR=false LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "push event -> forced full" "Category!=Integration|Category=Integration"

# Scenario 8: workflow_dispatch -> forced full
EVENT_NAME=workflow_dispatch JIRA_CONNECTOR=true ADO_CONNECTOR=false LINEAR_CONNECTOR=false CONNECTOR_SHARED=false
run_case "workflow_dispatch -> forced full" "Category!=Integration|Category=Integration"

echo
echo "passed: $pass"
echo "failed: $fail"
[ $fail -eq 0 ] || exit 1
