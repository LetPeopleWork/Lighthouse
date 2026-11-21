#!/usr/bin/env bash
# Checks that the ci-auth-suites-show examine command shows how the live authentication, RBAC and proxy-auth
# runs did in CI for a revision: the Verify Authentication job's result and one line for each of its three
# Playwright steps, a failed step as failed and a step the run does not have as missing.
# It builds the examine commands, then runs the command with a stand-in gh first on PATH that replays recorded
# run lists and job lists, so no network is used. The job and step names are the ones gh lists for a real run
# (run 44934303124 of "Build And Deploy Lighthouse"); the job lists below keep the jobs and steps that matter
# and drop the rest. Revisions are real first-parent commits of this repository.
# Usage: bash Scripts/examine/test-ci-auth-suites-show.sh
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"
command="$repo/Scripts/examine/bin/ci-auth-suites-show"
scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
failures=0
checks=0
output=""
status=0
recorded=""

auth_step="Run Auth Playwright tests"
rbac_step="Run RBAC Playwright tests"
proxy_step="Run proxied-auth Playwright tests"

# --- steps -------------------------------------------------------------------------------------------------

given_the_examine_commands_are_built() {
  bash "$repo/Scripts/examine/build-examine-commands" >"$scratch/build.log" 2>&1 \
    || { cat "$scratch/build.log"; echo "FAIL: building the examine commands failed, so nothing below can be trusted"; exit 1; }
}

first_parent() { git -C "$repo" rev-list --first-parent -n 1 "HEAD~$1"; }

# A stand-in gh that answers only what the real one would for the recorded runs. "gh run list" answers runs of
# the "Build And Deploy Lighthouse" workflow for the commit asked about (an empty list for any other commit or
# workflow); "gh run view RUN --json jobs" answers that run's recorded jobs. Both apply --jq/-q like gh does.
# Every call is written to calls.log; anything else is refused loudly.
a_recorded_gh() {
  recorded="$scratch/$1"
  mkdir -p "$recorded/bin" "$recorded/runs" "$recorded/jobs"
  : > "$recorded/calls.log"
  cat > "$recorded/bin/gh" <<EOF
#!/usr/bin/env bash
recorded="$recorded"
EOF
  cat >> "$recorded/bin/gh" <<'EOF'
printf '%s\n' "$*" >> "$recorded/calls.log"
verb="${1:-} ${2:-}"; shift 2 || true
run=""; commit=""; workflow=""; query=""
while [ "$#" -gt 0 ]; do
  case "$1" in
    -c|--commit) commit="$2"; shift 2 ;;
    -w|--workflow) workflow="$2"; shift 2 ;;
    -q|--jq) query="$2"; shift 2 ;;
    -R|--repo|--json|-L|--limit|-b|--branch|-s|--status) shift 2 ;;
    -*) shift ;;
    *) run="$1"; shift ;;
  esac
done
answer() { if [ -n "$query" ]; then jq -r "$query" "$1" | tr -d '\r'; else tr -d '\r' < "$1"; fi; }
case "$verb" in
  "run list")
    if [ "$workflow" = "Build And Deploy Lighthouse" ] && [ -n "$commit" ] && [ -f "$recorded/runs/$commit.json" ]; then
      answer "$recorded/runs/$commit.json"
    else
      echo '[]' > "$recorded/empty.json"; answer "$recorded/empty.json"
    fi ;;
  "run view")
    [ -f "$recorded/jobs/$run.json" ] || { echo "recorded gh: no run $run" >&2; exit 1; }
    answer "$recorded/jobs/$run.json" ;;
  *) echo "recorded gh: no recording for: gh $verb $*" >&2; exit 3 ;;
esac
EOF
  chmod +x "$recorded/bin/gh"
}

a_run_on_commit() {
  local commit=$1 run=$2 state=$3
  printf '[{"databaseId": %s, "status": "%s", "conclusion": ""}]\n' "$run" "$state" > "$recorded/runs/$commit.json"
}

# One step as gh lists it.
step() { printf '{"name": "%s", "status": "completed", "conclusion": "%s", "number": %s}' "$1" "$2" "$3"; }

# The jobs of one run as gh lists them: the backend job (given its own result, so reading it shows up) and the
# Verify Authentication job with its result and the steps given.
jobs_of_run() {
  local run=$1 backend=$2 auth_job=$3; shift 3
  local steps
  steps="$(IFS=,; echo "$*")"
  cat > "$recorded/jobs/$run.json" <<EOF
{"jobs": [
  {"name": "Verify Backend / backend", "status": "completed", "conclusion": "$backend", "steps": [
    $(step "Set up job" success 1), $(step "Complete job" success 2)]},
  {"name": "Verify Authentication / verifyauth", "status": "completed", "conclusion": "$auth_job", "steps": [$steps]}
]}
EOF
}

given_a_finished_run_whose_rbac_step_failed() {
  a_recorded_gh rbac-failed
  a_run_on_commit "$(first_parent 0)" 44900000001 completed
  jobs_of_run 44900000001 success failure \
    "$(step "Set up job" success 1)" \
    "$(step "Start Lighthouse for Auth tests" success 19)" \
    "$(step "$auth_step" success 21)" \
    "$(step "Start Lighthouse for RBAC tests" success 24)" \
    "$(step "$rbac_step" failure 26)" \
    "$(step "Start Lighthouse behind reverse proxy (proxied-auth tests)" success 29)" \
    "$(step "$proxy_step" success 31)" \
    "$(step "Complete job" success 40)"
}

# The revision asked about has a run still in progress; the run one first-parent commit earlier is parked at the
# release approval, which counts as finished, and all three steps passed in it.
given_a_finished_run_one_commit_back_where_every_step_passed() {
  a_recorded_gh all-passed
  a_run_on_commit "$(first_parent 0)" 44900000002 in_progress
  a_run_on_commit "$(first_parent 1)" 44934303124 waiting
  jobs_of_run 44900000002 success failure \
    "$(step "$auth_step" failure 21)" "$(step "$rbac_step" failure 26)" "$(step "$proxy_step" failure 31)"
  jobs_of_run 44934303124 skipped success \
    "$(step "Set up job" success 1)" \
    "$(step "$auth_step" success 21)" \
    "$(step "$rbac_step" success 26)" \
    "$(step "$proxy_step" success 31)" \
    "$(step "Complete job" success 40)"
}

# A run from before the proxy-auth step existed: the job passed, and the step is simply not in it.
given_a_finished_run_without_the_proxy_auth_step() {
  a_recorded_gh proxy-step-absent
  a_run_on_commit "$(first_parent 0)" 44900000003 completed
  jobs_of_run 44900000003 success success \
    "$(step "Set up job" success 1)" \
    "$(step "$auth_step" success 21)" \
    "$(step "$rbac_step" success 26)" \
    "$(step "Complete job" success 30)"
}

# Runs exist only for commits far newer than the revision asked about, so none is within its lookup window.
given_no_finished_run_within_reach_of_the_revision() {
  a_recorded_gh no-run
  a_run_on_commit "$(first_parent 0)" 44900000004 completed
  jobs_of_run 44900000004 success success \
    "$(step "$auth_step" success 21)" "$(step "$rbac_step" success 26)" "$(step "$proxy_step" success 31)"
}

when_the_examiner_shows_the_auth_suites_of() {
  local revision=$1
  if [ ! -x "$command" ]; then
    output="the command $command does not exist after building the examine commands"
    status=127
    return
  fi
  output="$(PATH="$recorded/bin:$PATH" timeout 120 "$command" "$revision" 2>&1)"
  status=$?
}

# --- assertions (each states what failed, why it matters and what to make true) ------------------------------

check() {
  local holds=$1 what=$2 why=$3 how=$4
  checks=$((checks + 1))
  if [ "$holds" = yes ]; then
    echo "ok: $what"
  else
    failures=$((failures + 1))
    echo "FAIL: $what"
    echo "  why: $why"
    echo "  how: $how"
    echo "  exit: $status; output (last lines):"
    printf '%s\n' "$output" | tail -12 | sed 's/^/    /'
  fi
}

holds() { if "$@"; then echo yes; else echo no; fi; }
output_has() { printf '%s\n' "$output" | grep -qF -- "$1"; }
output_lacks() { ! output_has "$1"; }
exits_with() { [ "$status" = "$1" ]; }

# Every line naming the subject carries the expected conclusion and no other one, and at least one line names it.
conclusions="success failure missing skipped cancelled"
word() { printf '(^|[^[:alnum:]])%s([^[:alnum:]]|$)' "$1"; }
shown_as() {
  local subject=$1 expected=$2 lines other
  lines="$(printf '%s\n' "$output" | grep -F -- "$subject")" || return 1
  [ "$(printf '%s\n' "$lines" | grep -ciE -- "$(word "$expected")")" = "$(printf '%s\n' "$lines" | wc -l | tr -d ' ')" ] \
    || return 1
  for other in $conclusions; do
    [ "$other" = "$expected" ] && continue
    if printf '%s\n' "$lines" | grep -qiE -- "$(word "$other")"; then return 1; fi
  done
}

job_shows() {
  local expected=$1 why=$2
  check "$(holds shown_as "Verify Authentication" "$expected")" \
    "the Verify Authentication job is shown as $expected" \
    "$why" \
    "print a line naming the Verify Authentication job with the conclusion gh records for it, read from the job named 'Verify Authentication / verifyauth', not the backend job"
}

step_shows() {
  local name=$1 expected=$2 why=$3
  check "$(holds shown_as "$name" "$expected")" \
    "the step '$name' is shown as $expected" \
    "$why" \
    "print one line naming the step '$name' with the conclusion gh records for that very step in the Verify Authentication job ('missing' when the job has no such step)"
}

then_the_run_is_named() {
  local run=$1 not_run=${2:-}
  check "$(holds output_has "$run")" \
    "the output names CI run $run" \
    "the examiner must see which run the results come from, to open it on GitHub" \
    "print the id of the newest finished Build And Deploy Lighthouse run at or before the revision"
  if [ -n "$not_run" ]; then
    check "$(holds output_lacks "$not_run")" \
      "the output does not show run $not_run, which is still in progress" \
      "a run still in progress has no final step results; showing it would report a result that may still change" \
      "skip runs that are neither completed nor waiting, and try earlier first-parent commits, as ci-live-suites-show does"
  fi
}

then_the_failed_rbac_step_and_the_job_show_failure() {
  then_the_run_is_named 44900000001
  job_shows failure "the job failed because the RBAC step failed; a green job line would hide a broken RBAC run"
  step_shows "$rbac_step" failure "a failed RBAC run must be visible as failed at EXAMINE, or a move that broke RBAC passes"
  step_shows "$auth_step" success "the authentication step passed in this run; reporting the job's failure on it blames the wrong suite"
  step_shows "$proxy_step" success "the proxy-auth step passed in this run; reporting the job's failure on it blames the wrong suite"
}

then_every_line_shows_success() {
  then_the_run_is_named 44934303124 44900000002
  job_shows success "every authentication suite passed in this run"
  step_shows "$auth_step" success "the authentication step passed in this run"
  step_shows "$rbac_step" success "the RBAC step passed in this run"
  step_shows "$proxy_step" success "the proxy-auth step passed in this run"
}

then_the_absent_step_shows_missing() {
  then_the_run_is_named 44900000003
  step_shows "$proxy_step" missing "a step the run never ran proved nothing; showing it as passed would be a false green"
  step_shows "$auth_step" success "the authentication step passed in this run"
  step_shows "$rbac_step" success "the RBAC step passed in this run"
}

then_no_run_found_exits_1() {
  check "$(holds exits_with 1)" \
    "a revision with no finished CI run within its lookup window exits 1" \
    "with no run there are no results; exiting 0 would read as passing suites" \
    "exit 1 when no finished Build And Deploy Lighthouse run is found within 40 first-parent commits"
  check "$(holds output_lacks "$auth_step")" \
    "a revision with no finished CI run shows no step results" \
    "step lines without a run would be invented results" \
    "print only that no finished CI run was found"
}

is_admitted() {
  [ "$(jq -r '.commands[] | select(.name == "ci-auth-suites-show") | .path' "$repo/.nwave/examine.json" 2>/dev/null)" \
    = "Scripts/examine/bin/ci-auth-suites-show" ]
}

then_the_command_is_an_admitted_examine_probe() {
  output="$(jq -c '.commands[] | select(.name == "ci-auth-suites-show")' "$repo/.nwave/examine.json" 2>&1)"
  status=$?
  check "$(holds is_admitted)" \
    "the examine commands list admits ci-auth-suites-show at Scripts/examine/bin/ci-auth-suites-show" \
    "the examiner may run only the commands that list admits" \
    "add {\"name\": \"ci-auth-suites-show\", \"path\": \"Scripts/examine/bin/ci-auth-suites-show\"} to .nwave/examine.json"
}

# --- scenarios ---------------------------------------------------------------------------------------------

given_the_examine_commands_are_built

echo "== a finished run whose RBAC step failed"
given_a_finished_run_whose_rbac_step_failed
when_the_examiner_shows_the_auth_suites_of "$(first_parent 0)"
then_the_failed_rbac_step_and_the_job_show_failure

echo "== a finished run one commit back where every step passed"
given_a_finished_run_one_commit_back_where_every_step_passed
when_the_examiner_shows_the_auth_suites_of "$(first_parent 0)"
then_every_line_shows_success

echo "== a finished run without the proxy-auth step"
given_a_finished_run_without_the_proxy_auth_step
when_the_examiner_shows_the_auth_suites_of "$(first_parent 0)"
then_the_absent_step_shows_missing

echo "== no finished run within reach of the revision"
given_no_finished_run_within_reach_of_the_revision
when_the_examiner_shows_the_auth_suites_of "$(first_parent 50)"
then_no_run_found_exits_1

echo "== the command is an admitted examine probe"
then_the_command_is_an_admitted_examine_probe

if [ "$failures" -eq 0 ]; then
  echo "RESULT: all $checks checks passed"
  exit 0
fi
echo "RESULT: $failures of $checks checks failed"
exit 1
