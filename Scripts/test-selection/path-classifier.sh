#!/usr/bin/env bash
# Shared path classifier used by `.github/workflows/ci_changes.yml` and
# `Scripts/test-selection/dev-test.sh` to decide which connector integration
# suites, and whether the backend suite, a change touches. Every classify_*
# function takes a newline-separated list of changed paths (as
# `git diff --name-only` would emit) and echoes "true" or "false".

# The root Dockerfile counts as backend: the backend's SonarCloud analysis is what scans it.
BACKEND_REGEX='^(Lighthouse.Backend(/|.Tests/)|Scripts/test-timings/|\.github/actions/|Dockerfile$)'
# The parent-source code decides what the Jira connector reads, so a change to it needs the live Jira suite.
# Its tests and busier neighbours (WorkItemBase, WorkTrackingSystemOptionsOwner) stay out: each run spends live
# Jira quota, and those change far more often than what the connector reads does.
JIRA_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Modules/TrackerIntegration/Adapters/Connectors/Jira/|^Lighthouse\.Backend/Lighthouse\.Backend/Modules/TrackerIntegration/Domain/ParentSourceSelector\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(ParentSource[^/]*|ParentReadings|ParentChoice|ParentResolution)\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/Parents/|^Lighthouse\.Backend/Lighthouse\.Backend/Modules/WorkItems/Domain/FetchFingerprint\.cs$'
ADO_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Modules/TrackerIntegration/Adapters/Connectors/AzureDevOps/'
LINEAR_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Modules/TrackerIntegration/Adapters/Connectors/Linear/'
SERVICENOW_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Modules/TrackerIntegration/Adapters/Connectors/ServiceNow/'
GITHUB_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/(Services/(Implementation|Interfaces)|Platform/Releases)/[^/]*([Gg]it[Hh]ubService|LighthouseReleaseService)[^/]*\.cs$'

# A change in any of these locations must force every integration category to
# run (a regression here can break any connector):
#   - The connectors' shared subfolders (Adapters/Connectors/Auth/, Adapters/Connectors/OAuth/) of Tracker
#     integration, under production OR tests
#   - Tracker integration's Application, Contracts and Domain (factories, schema, keys, the connector interfaces) and
#     its OAuth adapters
#   - Connector-shared models (Models/WorkTrackingSystemConnection*, Models/OAuth/)
#   - Models/WorkTrackingSystems.cs, the enum every connector switches on
#   - Program.cs (DI graph for every connector)
#   - Solution + csproj files
SHARED_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Modules/TrackerIntegration/Adapters/Connectors/(Auth|OAuth)/|^Lighthouse\.Backend/Lighthouse\.Backend/Modules/TrackerIntegration/(Application|Contracts|Domain)/|^Lighthouse\.Backend/Lighthouse\.Backend/Modules/TrackerIntegration/Adapters/OAuth/|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(WorkTrackingSystemConnection|OAuth/)|^Lighthouse\.Backend/Lighthouse\.Backend/Models/WorkTrackingSystems\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Program\.cs$|^Lighthouse\.Backend/Lighthouse\.sln$|^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/[^/]*\.csproj$'

classify_backend() { printf '%s\n' "$1" | grep -Eq "$BACKEND_REGEX" && echo true || echo false; }
classify_jira()   { printf '%s\n' "$1" | grep -Eq "$JIRA_REGEX"   && echo true || echo false; }
classify_ado()    { printf '%s\n' "$1" | grep -Eq "$ADO_REGEX"    && echo true || echo false; }
classify_linear() { printf '%s\n' "$1" | grep -Eq "$LINEAR_REGEX" && echo true || echo false; }
classify_servicenow() { printf '%s\n' "$1" | grep -Eq "$SERVICENOW_REGEX" && echo true || echo false; }
classify_github() { printf '%s\n' "$1" | grep -Eq "$GITHUB_REGEX" && echo true || echo false; }
classify_shared() { printf '%s\n' "$1" | grep -Eq "$SHARED_REGEX" && echo true || echo false; }
