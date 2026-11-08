#!/usr/bin/env bash
# Capture every HTTP response of the Flow metrics read surface for one git revision, so that a
# behaviour-free move can be checked by comparing two captures. See README.md in this folder for
# how to use it and the rule that base and candidate are captured back to back on the same day.
#
# Usage: capture-metrics.sh <git-revision> <output-directory>
#
# Environment (all optional):
#   CAPTURE_WORK_ROOT   where the throwaway worktree, database copy and key ring go
#                       (default: $HOME/.cache/nwdes/story-6358)
#   CAPTURE_DB_DIR      folder holding LighthouseAppContext.db (+ -wal/-shm) to copy
#                       (default: <main checkout>/Lighthouse.Backend/Lighthouse.Backend/DB_Backup)
#   CAPTURE_MAX_ROUNDS  how many request rounds may be needed before two consecutive rounds agree
#                       (default: 10)
#   CAPTURE_KEEP_WORK   1 keeps the work folder (database copy with live, encrypted credentials!)
#                       for debugging; default 0 deletes it.

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "usage: $0 <git-revision> <output-directory>" >&2
    exit 2
fi

REVISION_ARG=$1
OUT_DIR=$2

for tool in git dotnet curl jq sqlite3 python3 sha256sum; do
    command -v "$tool" >/dev/null || { echo "missing tool: $tool" >&2; exit 2; }
done

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)
REVISION=$(git -C "$REPO_ROOT" rev-parse --verify "${REVISION_ARG}^{commit}")
SHORT_SHA=$(git -C "$REPO_ROOT" rev-parse --short "$REVISION")

WORK_ROOT=${CAPTURE_WORK_ROOT:-$HOME/.cache/nwdes/story-6358}
# The physical path: if any part of it is a symlink (here $HOME/.cache is one), the compiler
# sees file paths that no longer match the .editorconfig sections, the nullable suppressions
# stop applying, and the build fails with hundreds of CS8618 errors.
mkdir -p "$WORK_ROOT"
WORK_ROOT=$(cd "$WORK_ROOT" && pwd -P)
DB_SOURCE_DIR=${CAPTURE_DB_DIR:-$REPO_ROOT/Lighthouse.Backend/Lighthouse.Backend/DB_Backup}
MAX_ROUNDS=${CAPTURE_MAX_ROUNDS:-10}
KEEP_WORK=${CAPTURE_KEEP_WORK:-0}

# The fixed window every dated request uses. The backup's history ends on 2026-10-17 (the last
# refresh of every team and portfolio), so a window ending there covers real data and none of
# its days lie in the future, whatever today is.
WINDOW_START=2026-07-20
WINDOW_END=2026-10-17
PAST_DAY=2026-10-01
# A day after today sends blockedItemsAtDate down its "current" branch.
FUTURE_DAY=2099-01-01

if [[ "$WORK_ROOT" == "$REPO_ROOT" || "$WORK_ROOT" == "$REPO_ROOT"/* ]]; then
    echo "CAPTURE_WORK_ROOT ($WORK_ROOT) must lie outside the repository ($REPO_ROOT)" >&2
    exit 2
fi

if [[ ! -f "$DB_SOURCE_DIR/LighthouseAppContext.db" ]]; then
    echo "no database at $DB_SOURCE_DIR/LighthouseAppContext.db" >&2
    exit 2
fi

if [[ -e "$OUT_DIR" && -n "$(ls -A "$OUT_DIR" 2>/dev/null)" ]]; then
    echo "output directory $OUT_DIR exists and is not empty" >&2
    exit 2
fi

mkdir -p "$WORK_ROOT"
WORK=$(mktemp -d "$WORK_ROOT/run-$SHORT_SHA-XXXXXX")
SRC="$WORK/src"
DB_DIR="$WORK/db"
KEY_DIR="$WORK/keys"
LOG_DIR="$WORK/logs"
ROUNDS_DIR="$WORK/rounds"
DB_FILE="$DB_DIR/LighthouseAppContext.db"
SERVER_PID=""

log() { printf '[capture %s] %s\n' "$(date +%H:%M:%S)" "$*" >&2; }

stop_server() {
    if [[ -n "$SERVER_PID" ]] && kill -0 "$SERVER_PID" 2>/dev/null; then
        log "stopping server (pid $SERVER_PID)"
        kill -TERM "$SERVER_PID" 2>/dev/null || true
        for _ in $(seq 1 60); do
            kill -0 "$SERVER_PID" 2>/dev/null || break
            sleep 1
        done
        kill -KILL "$SERVER_PID" 2>/dev/null || true
        wait "$SERVER_PID" 2>/dev/null || true
    fi
    SERVER_PID=""
}

cleanup() {
    local status=$?
    stop_server
    if git -C "$REPO_ROOT" worktree list --porcelain | grep -qxF "worktree $SRC"; then
        git -C "$REPO_ROOT" worktree remove --force "$SRC" || true
    fi
    git -C "$REPO_ROOT" worktree prune || true
    if [[ "$KEEP_WORK" == "1" ]]; then
        log "kept work folder $WORK (holds a database copy with encrypted credentials: delete it when done)"
    else
        rm -rf "$WORK"
    fi
    exit "$status"
}
trap cleanup EXIT INT TERM

# ---------------------------------------------------------------------------------------------
# 1. Build the revision in a throwaway worktree outside the repository
# ---------------------------------------------------------------------------------------------
log "worktree for $SHORT_SHA at $SRC"
git -C "$REPO_ROOT" worktree add --detach --quiet "$SRC" "$REVISION"

BACKEND="$SRC/Lighthouse.Backend"
PROJECT="$BACKEND/Lighthouse.Backend"

# The backend references the migration assemblies by path, so they are built first.
log "building (migrations, then backend)"
build() {
    local project=$1; shift
    if ! dotnet build "$BACKEND/$project" -c Debug -nologo -v quiet "$@" > "$WORK/build-$project.log" 2>&1; then
        tail -n 40 "$WORK/build-$project.log" >&2
        echo "build of $project failed" >&2
        exit 1
    fi
}
build Lighthouse.Migrations.Sqlite
build Lighthouse.Migrations.Postgres
for migrations in Sqlite Postgres; do
    [[ -f "$BACKEND/Migrations/Lighthouse.Migrations.$migrations.dll" ]] \
        || { echo "Lighthouse.Migrations.$migrations.dll was not copied to $BACKEND/Migrations" >&2; exit 1; }
done
# The migration builds already compiled the backend, before their DLLs existed, so an
# incremental build would keep a deps.json without them and the app could not migrate.
build Lighthouse.Backend --no-incremental

APP_DLL=$(find "$PROJECT/bin/Debug" -name Lighthouse.dll -path '*net10.0*' | head -n 1)
[[ -n "$APP_DLL" ]] || { echo "Lighthouse.dll not found under $PROJECT/bin/Debug" >&2; exit 1; }
[[ -f "$(dirname "$APP_DLL")/Lighthouse.Migrations.Sqlite.dll" ]] \
    || { echo "Lighthouse.Migrations.Sqlite.dll missing beside $APP_DLL" >&2; exit 1; }

# ---------------------------------------------------------------------------------------------
# 2. A fresh copy of the backup, made safe to run offline
# ---------------------------------------------------------------------------------------------
mkdir -p "$DB_DIR" "$KEY_DIR" "$LOG_DIR" "$ROUNDS_DIR"
for suffix in "" -wal -shm; do
    if [[ -f "$DB_SOURCE_DIR/LighthouseAppContext.db$suffix" ]]; then
        cp "$DB_SOURCE_DIR/LighthouseAppContext.db$suffix" "$DB_DIR/"
    fi
done
DB_SOURCE_HASH=$( (cd "$DB_SOURCE_DIR" && sha256sum LighthouseAppContext.db*) )

# Periodic refresh: the team and portfolio updaters wait StartDelay minutes before their first
# pass and Interval minutes between passes; connection health looks again after twice the
# longer Interval. 40000 minutes (about 28 days) keeps all three asleep for the whole run and
# stays below the ~49.7-day ceiling Task.Delay accepts. Tracker URLs point at a closed local
# port, so even a refresh or health probe that did run could not reach a real tracker.
sqlite3 "$DB_FILE" <<'SQL'
.bail on
UPDATE AppSettings SET Value = '40000'
 WHERE Key IN ('PeriodicRefresh:Throughput:StartDelay', 'PeriodicRefresh:Throughput:Interval',
               'PeriodicRefresh:Features:StartDelay', 'PeriodicRefresh:Features:Interval',
               'PeriodicRefresh:Forecasts:StartDelay', 'PeriodicRefresh:Forecasts:Interval');
UPDATE WorkTrackingSystemConnectionOption SET Value = 'http://127.0.0.1:9'
 WHERE IsSecret = 0 AND Key LIKE '%Url';
SQL

PORT=$(python3 -I -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1])')
PORT2=$(python3 -I -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1])')
BASE="http://127.0.0.1:$PORT"

# ---------------------------------------------------------------------------------------------
# 3. Start the backend
# ---------------------------------------------------------------------------------------------
# - Kestrel endpoint keys, because appsettings.Development.json pins 5169/7246 and
#   ASPNETCORE_URLS is ignored. The "Https" endpoint is bound as plain http on a second free
#   port so no certificate is needed and 7246 is never taken.
# - A key ring of its own outside the repository: the credentials in the backup were encrypted
#   under another ring, so they read as unreadable (StartEvenIfNothingStoredCanBeRead lets the
#   instance start anyway) and no connector can authenticate.
# - Every outbound HTTP call goes to a closed local proxy port.
# - A pinned time zone, so "today" is the same calendar day for every run on the same date.
log "starting backend on $BASE"
(
    cd "$PROJECT"
    export ASPNETCORE_ENVIRONMENT=Development
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export TZ=UTC
    export Lighthouse__TimeZone=UTC
    export Kestrel__Endpoints__Http__Url="http://127.0.0.1:$PORT"
    export Kestrel__Endpoints__Https__Url="http://127.0.0.1:$PORT2"
    export Database__Provider=sqlite
    export Database__ConnectionString="Data Source=$DB_FILE"
    export Encryption__KeyStorePath="$KEY_DIR"
    export Encryption__StartEvenIfNothingStoredCanBeRead=true
    export Serilog__WriteTo__0__Args__path="$LOG_DIR/log-.txt"
    export HTTP_PROXY=http://127.0.0.1:9 HTTPS_PROXY=http://127.0.0.1:9 ALL_PROXY=http://127.0.0.1:9
    export http_proxy=http://127.0.0.1:9 https_proxy=http://127.0.0.1:9 all_proxy=http://127.0.0.1:9
    export NO_PROXY=127.0.0.1,localhost no_proxy=127.0.0.1,localhost
    exec dotnet "$APP_DLL"
) > "$LOG_DIR/stdout.txt" 2>&1 &
SERVER_PID=$!

CURL=(curl --noproxy '*' -sS --max-time 120)

for i in $(seq 1 180); do
    if ! kill -0 "$SERVER_PID" 2>/dev/null; then
        tail -n 60 "$LOG_DIR/stdout.txt" >&2
        echo "server exited during start-up" >&2
        exit 1
    fi
    if [[ "$("${CURL[@]}" -o /dev/null -w '%{http_code}' "$BASE/health/ready" 2>/dev/null || true)" == "200" ]]; then
        break
    fi
    if [[ $i -eq 180 ]]; then
        tail -n 60 "$LOG_DIR/stdout.txt" >&2
        echo "server not ready after 180 s" >&2
        exit 1
    fi
    sleep 1
done
log "server ready"

# ---------------------------------------------------------------------------------------------
# 4. The request list (fixed once, then replayed every round)
# ---------------------------------------------------------------------------------------------
json_get() { "${CURL[@]}" -f "$BASE$1"; }

TEAM_IDS=$(json_get /api/v1/teams | jq -r '.[].id' | sort -n)
PORTFOLIO_IDS=$(json_get /api/v1/portfolios | jq -r '.[].id' | sort -n)

Q="startDate=$WINDOW_START&endDate=$WINDOW_END"
REQUESTS="$WORK/requests.tsv"
: > "$REQUESTS"
add() { printf '%s\t%s\n' "$1" "$2" >> "$REQUESTS"; }

# Endpoints both owner kinds share (the team routes live on TeamMetricsController and
# TeamFeatureSizeMetricsController, the portfolio routes on PortfolioMetricsController).
add_shared_owner_requests() {
    local prefix=$1 base=$2
    local endpoint
    for endpoint in throughput arrivals wipOverTime cycleTimePercentiles workItemAgePercentiles \
        ageInStatePercentiles cumulativeStateTime cumulativeStateTime/candidates cycleTimeData \
        multiitemforecastpredictabilityscore throughputInfo arrivalsInfo wipOverviewInfo \
        flowEfficiencyInfo totalWorkItemAgeInfo predictabilityScoreInfo cycleTimePercentilesInfo \
        throughput/pbc arrivals/pbc wipOverTime/pbc totalWorkItemAge/pbc cycleTime/pbc \
        estimationVsCycleTime blockedCountHistory allFeaturesForSizeChart sizePercentiles \
        featureSizePercentilesInfo; do
        add "$prefix/${endpoint//\//_}" "$base/$endpoint?$Q"
    done
    add "$prefix/wip" "$base/wip?asOfDate=$WINDOW_END"
    add "$prefix/totalWorkItemAge" "$base/totalWorkItemAge?asOfDate=$WINDOW_END"
    add "$prefix/blockedItemsAtDate__past" "$base/blockedItemsAtDate?date=$PAST_DAY"
    add "$prefix/blockedItemsAtDate__current" "$base/blockedItemsAtDate?date=$FUTURE_DAY"

    local horizon
    for horizon in 30 60 90; do
        add "$prefix/percentiles-over-time__CycleTime_h$horizon" "$base/percentiles-over-time?metricType=CycleTime&horizon=$horizon&$Q"
        add "$prefix/percentiles-over-time__CycleTime_h${horizon}_all" "$base/percentiles-over-time?metricType=CycleTime&horizon=$horizon"
    done
    add "$prefix/percentiles-over-time__WorkItemAge" "$base/percentiles-over-time?metricType=WorkItemAge&$Q"
    add "$prefix/percentiles-over-time__WorkItemAge_all" "$base/percentiles-over-time?metricType=WorkItemAge"

    local type
    for type in Throughput WorkItemAge Wip CycleTime Arrivals FeatureSize; do
        add "$prefix/process-behavior-over-time__$type" "$base/process-behavior-over-time?type=$type&$Q"
        add "$prefix/process-behavior-over-time__${type}_all" "$base/process-behavior-over-time?type=$type"
    done

    # One drill-down per state the cumulative-state-time chart shows for this owner.
    local state
    while IFS= read -r state; do
        [[ -n "$state" ]] || continue
        add "$prefix/cumulativeStateTime_items__$(printf '%s' "$state" | tr -c 'A-Za-z0-9' '_')" \
            "$base/cumulativeStateTime/items?$Q&state=$(jq -rn --arg s "$state" '$s|@uri')"
    done < <(json_get "$base/cumulativeStateTime?$Q" | jq -r '.states[].state')
}

add_reports() {
    local prefix=$1 base=$2
    add "$prefix/reports" "$base/reports"
    add "$prefix/reports_templates" "$base/reports/templates"
    local report
    for report in $(json_get "$base/reports" | jq -r '.. | objects | select(has("id") and has("name")) | .id' | sort -nu); do
        add "$prefix/report__$report" "$base/reports/$report"
    done
}

for team in $TEAM_IDS; do
    base="/api/v1/teams/$team/metrics"
    add_shared_owner_requests "team-$team" "$base"
    add "team-$team/throughput__filtered" "$base/throughput?$Q&view=filtered"
    add "team-$team/throughput_pbc__filtered" "$base/throughput/pbc?$Q&view=filtered"
    add "team-$team/multiitemforecastpredictabilityscore__filtered" "$base/multiitemforecastpredictabilityscore?$Q&view=filtered"
    add "team-$team/featuresInProgress" "$base/featuresInProgress?asOfDate=$WINDOW_END"
    add "team-$team/featuresWorkedOnInfo" "$base/featuresWorkedOnInfo?$Q"
    add "team-$team/forecastInputCandidates" "$base/forecastInputCandidates"
    add "team-$team/sleRisk" "$base/sleRisk"
    # Consumers of the metrics services outside the metrics controllers.
    add "team-$team/feature-list" "/api/v1/teams/$team/feature-list"
    add "team-$team/refinement" "/api/v1/teams/$team/refinement"
    add_reports "team-$team" "/api/v1/teams/$team"
done

for portfolio in $PORTFOLIO_IDS; do
    base="/api/v1/portfolios/$portfolio/metrics"
    add_shared_owner_requests "portfolio-$portfolio" "$base"
    add "portfolio-$portfolio/started" "$base/started?$Q"
    add "portfolio-$portfolio/featureSize_pbc" "$base/featureSize/pbc?$Q"
    add "portfolio-$portfolio/featureSizeEstimation" "$base/featureSizeEstimation?$Q"
    add "portfolio-$portfolio/feature-list" "/api/v1/portfolios/$portfolio/feature-list"
    add_reports "portfolio-$portfolio" "/api/v1/portfolios/$portfolio"
done
add "global/reports" "/api/v1/reports"

REQUEST_COUNT=$(wc -l < "$REQUESTS")
log "$REQUEST_COUNT requests per round (teams: $(echo $TEAM_IDS), portfolios: $(echo $PORTFOLIO_IDS))"

# ---------------------------------------------------------------------------------------------
# 5. Rounds until the read surface stops changing
# ---------------------------------------------------------------------------------------------
# Opening an over-time chart over a dated window asks the hosted OverTimeHistoryFiller to work
# out the days the window is missing, and it writes them in the background in passes of at most
# 90 days and 10 seconds. So the first answers depend on how far the filler got, which depends
# on machine speed. The capture therefore replays the whole request list until two consecutive
# rounds are identical, waiting for the filler to fall idle after each round, and keeps the
# settled answers. The days the filler wrote are part of what is compared (db/ below), because
# the reconstruction code is part of what moves.

snapshot_counts() {
    sqlite3 -cmd '.timeout 10000' "$DB_FILE" \
        'SELECT (SELECT count(*) FROM PercentilesOverTimeSnapshots), (SELECT count(*) FROM ProcessBehaviorSnapshots), (SELECT count(*) FROM BlockedCountSnapshots);'
}

wait_for_filler_idle() {
    # Idle = the snapshot tables did not change over 20 s (longer than one 10-second pass).
    local previous current stable=0
    previous=$(snapshot_counts)
    for _ in $(seq 1 240); do
        sleep 5
        current=$(snapshot_counts)
        if [[ "$current" == "$previous" ]]; then
            stable=$((stable + 1))
            [[ $stable -ge 4 ]] && return 0
        else
            stable=0
            previous=$current
        fi
    done
    echo "filler did not fall idle within 20 minutes" >&2
    return 1
}

capture_one() {
    local name=$1 path=$2 dir=$3
    local file="$dir/$name.json" body="$dir/.body" code
    mkdir -p "$(dirname "$file")"
    code=$("${CURL[@]}" -o "$body" -w '%{http_code}' "$BASE$path") || code=000
    if [[ ! -s "$body" ]]; then
        jq -nS --arg path "$path" --argjson status "$code" '{status: $status, request: $path, body: null}' > "$file"
    elif jq -e . "$body" > /dev/null 2>&1; then
        jq -nS --arg path "$path" --argjson status "$code" --slurpfile b "$body" '{status: $status, request: $path, body: $b[0]}' > "$file"
    else
        jq -nS --arg path "$path" --argjson status "$code" --rawfile b "$body" '{status: $status, request: $path, bodyText: $b}' > "$file"
    fi
    rm -f "$body"
    normalise_monte_carlo "$name" "$file" "$dir.raw-monte-carlo/$name.json"
}

# The predictability score is a Monte Carlo "how many" forecast over the window's throughput,
# drawn from an unseeded random source (DrawStreamFactory / Random.Shared), so its trial
# histogram, percentiles, score and the score's comparison sentence differ from run to run
# while the code is unchanged. Inside one run the metrics cache holds a result for a while,
# which is why rounds can agree. Those values are replaced by a marker; everything else in the
# response (status, shape, labels, the number of trials) is still compared. The raw bodies are
# kept in raw-monte-carlo/, which compare.sh does not compare.
normalise_monte_carlo() {
    local name=$1 file=$2 raw=$3 marker='<monte-carlo: not comparable between runs>'
    case "$name" in
        */multiitemforecastpredictabilityscore|*/multiitemforecastpredictabilityscore__*)
            mkdir -p "$(dirname "$raw")" && cp "$file" "$raw"
            jq -S --arg m "$marker" '
                if .status == 200 then
                    .body.forecastResults = {trials: ([.body.forecastResults[]] | add)}
                    | .body.percentiles |= map(.value = $m)
                    | .body.predictabilityScore = $m
                else . end' "$file" > "$file.tmp" && mv "$file.tmp" "$file"
            ;;
        */predictabilityScoreInfo)
            mkdir -p "$(dirname "$raw")" && cp "$file" "$raw"
            jq -S --arg m "$marker" '
                if .status == 200 then
                    .body.score = $m
                    | if .body.comparison != null then
                          .body.comparison |= (.currentValue = $m | .previousValue = $m | .percentageDelta = $m | .direction = $m)
                      else . end
                else . end' "$file" > "$file.tmp" && mv "$file.tmp" "$file"
            ;;
    esac
}

run_round() {
    local dir=$1 name path
    mkdir -p "$dir"
    while IFS=$'\t' read -r name path; do
        capture_one "$name" "$path" "$dir"
    done < "$REQUESTS"
}

wait_for_filler_idle
round=1
settled=""
while [[ $round -le $MAX_ROUNDS ]]; do
    log "round $round"
    run_round "$ROUNDS_DIR/$round"
    wait_for_filler_idle
    if [[ $round -gt 1 ]] && diff -r -q "$ROUNDS_DIR/$((round - 1))" "$ROUNDS_DIR/$round" > /dev/null; then
        settled=$round
        break
    fi
    if [[ $round -gt 1 ]]; then
        log "round $round differs from round $((round - 1)) in $(diff -r -q "$ROUNDS_DIR/$((round - 1))" "$ROUNDS_DIR/$round" | wc -l) files"
    fi
    round=$((round + 1))
done

if [[ -z "$settled" ]]; then
    echo "responses still changed after $MAX_ROUNDS rounds; raise CAPTURE_MAX_ROUNDS or inspect $ROUNDS_DIR (set CAPTURE_KEEP_WORK=1)" >&2
    exit 1
fi
log "settled after $settled rounds"

# ---------------------------------------------------------------------------------------------
# 6. Write the output
# ---------------------------------------------------------------------------------------------
mkdir -p "$OUT_DIR/db"
cp -r "$ROUNDS_DIR/$settled" "$OUT_DIR/requests"
cp -r "$ROUNDS_DIR/$settled.raw-monte-carlo" "$OUT_DIR/raw-monte-carlo"

# The snapshot rows the run ended with, without the surrogate Id (its values follow write order).
dump_table() {
    local table=$1 columns
    columns=$(sqlite3 "$DB_FILE" "SELECT group_concat('\"' || name || '\"', ',') FROM pragma_table_info('$table') WHERE name <> 'Id';")
    sqlite3 -header -separator $'\t' "$DB_FILE" "SELECT $columns FROM \"$table\" ORDER BY $columns;" > "$OUT_DIR/db/$table.tsv"
}
for table in PercentilesOverTimeSnapshots ProcessBehaviorSnapshots BlockedCountSnapshots; do
    dump_table "$table"
done

{
    echo "revision: $REVISION"
    echo "captured_at_utc: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo "today_utc: $(date -u +%Y-%m-%d)"
    echo "window: $WINDOW_START..$WINDOW_END"
    echo "requests_per_round: $REQUEST_COUNT"
    echo "rounds_until_settled: $settled"
    echo "teams: $(echo $TEAM_IDS)"
    echo "portfolios: $(echo $PORTFOLIO_IDS)"
    echo "database_source:"
    echo "$DB_SOURCE_HASH" | sed 's/^/  /'
} > "$OUT_DIR/_manifest.txt"

stop_server
log "done: $OUT_DIR ($REQUEST_COUNT requests, settled after $settled rounds)"
