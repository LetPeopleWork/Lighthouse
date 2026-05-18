#!/usr/bin/env bash
# Shared path classifier used by `.github/workflows/ci_changes.yml` and
# `Scripts/test-selection/dev-test.sh` to decide which connector integration
# suites a change touches. Every classify_* function takes a newline-separated
# list of changed paths (as `git diff --name-only` would emit) and echoes
# "true" or "false".

JIRA_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Jira/'
ADO_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/AzureDevOps/'
LINEAR_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/Linear/'

# A change in any of these locations must force every integration category to
# run (a regression here can break any connector):
#   - Connector base subfolders (Auth/, OAuth/) under production OR tests
#   - Top-level WorkTrackingConnectors/*.cs in production (schema, keys, enum)
#   - Connector-shared interfaces (Services/Interfaces/WorkTrackingConnectors/I*.cs)
#   - Connector-shared models (Models/WorkTrackingSystemConnection*, Models/OAuth/)
#   - Program.cs (DI graph for every connector)
#   - Solution + csproj files
SHARED_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/Services/Implementation/WorkTrackingConnectors/(Auth|OAuth)/|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Implementation/WorkTrackingConnectors/[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Services/Interfaces/WorkTrackingConnectors/I[^/]+\.cs$|^Lighthouse\.Backend/Lighthouse\.Backend/Models/(WorkTrackingSystemConnection|OAuth/)|^Lighthouse\.Backend/Lighthouse\.Backend/Program\.cs$|^Lighthouse\.Backend/Lighthouse\.sln$|^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/[^/]*\.csproj$'

classify_jira()   { printf '%s\n' "$1" | grep -Eq "$JIRA_REGEX"   && echo true || echo false; }
classify_ado()    { printf '%s\n' "$1" | grep -Eq "$ADO_REGEX"    && echo true || echo false; }
classify_linear() { printf '%s\n' "$1" | grep -Eq "$LINEAR_REGEX" && echo true || echo false; }
classify_shared() { printf '%s\n' "$1" | grep -Eq "$SHARED_REGEX" && echo true || echo false; }
