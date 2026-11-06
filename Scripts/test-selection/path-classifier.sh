#!/usr/bin/env bash
# Shared path classifier used by `.github/workflows/ci_changes.yml` and
# `Scripts/test-selection/dev-test.sh` to decide which connector integration
# suites, and whether the backend suite, a change touches. Every classify_*
# function takes a newline-separated list of changed paths (as
# `git diff --name-only` would emit) and echoes "true" or "false".

BACKEND_REGEX='^(Lighthouse.Backend(/|.Tests/)|Scripts/test-timings/|\.github/actions/)'
# The parent-source code decides what the Jira connector reads, so a change to it needs the live Jira suite.
# Its tests and busier neighbours (WorkItemBase, WorkTrackingSystemOptionsOwner) stay out: each run spends live
# Jira quota, and those change far more often than what the connector reads does.
JIRA_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Jira/|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(ParentSource[^/]*|ParentReadings|ParentChoice|ParentResolution)\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/Parents/|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/WorkItems/FetchFingerprint\.cs$'
ADO_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/AzureDevOps/'
LINEAR_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Linear/'
SERVICENOW_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/ServiceNow/'
GITHUB_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/(Implementation|Interfaces)/[^/]*([Gg]it[Hh]ubService|LighthouseReleaseService)[^/]*\.cs$'

# A change in any of these locations must force every integration category to
# run (a regression here can break any connector):
#   - Connector base subfolders (Auth/, OAuth/) under production OR tests
#   - Top-level WorkTrackingConnectors/*.cs in production (schema, keys)
#   - Connector-shared interfaces (Services/Interfaces/WorkTrackingConnectors/I*.cs)
#   - Connector-shared models (Models/WorkTrackingSystemConnection*, Models/OAuth/)
#   - Models/WorkTrackingSystems.cs, the enum every connector switches on
#   - Program.cs (DI graph for every connector)
#   - Solution + csproj files
SHARED_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/(Auth|OAuth)/|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/WorkTrackingConnectors/[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Interfaces/WorkTrackingConnectors/I[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(WorkTrackingSystemConnection|OAuth/)|^Lighthouse\.Backend/Lighthouse\.Backend/Models/WorkTrackingSystems\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Program\.cs$|^Lighthouse\.Backend/Lighthouse\.sln$|^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/[^/]*\.csproj$'

classify_backend() { printf '%s\n' "$1" | grep -Eq "$BACKEND_REGEX" && echo true || echo false; }
classify_jira()   { printf '%s\n' "$1" | grep -Eq "$JIRA_REGEX"   && echo true || echo false; }
classify_ado()    { printf '%s\n' "$1" | grep -Eq "$ADO_REGEX"    && echo true || echo false; }
classify_linear() { printf '%s\n' "$1" | grep -Eq "$LINEAR_REGEX" && echo true || echo false; }
classify_servicenow() { printf '%s\n' "$1" | grep -Eq "$SERVICENOW_REGEX" && echo true || echo false; }
classify_github() { printf '%s\n' "$1" | grep -Eq "$GITHUB_REGEX" && echo true || echo false; }
classify_shared() { printf '%s\n' "$1" | grep -Eq "$SHARED_REGEX" && echo true || echo false; }
