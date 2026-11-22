#!/usr/bin/env bash
# Checks that the platform answers examine command tells a changed answer from an unchanged one, and that it
# leaves out the values that change on every run without hiding the ones that must not change.
# It builds the examine commands, then runs the command's compare stage on capture folders written here: two that
# differ in one answer (the log level), two that match, two that differ only in values that change on every run
# (the process id, the install time and when a problem was recorded), two where a stable value beside those
# changed, and two empty ones. Capturing on real revisions takes minutes, so this check feeds the compare stage
# directly; the examiner runs the full command. It also checks that the command's usage names every answer it
# captures, that both the command and its selftest are admitted examine probes, and that the selftest runs this check.
# Usage: bash Scripts/examine/test-platform-answers-compare.sh
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"
command="$repo/Scripts/examine/bin/platform-answers-compare"
selftest="$repo/Scripts/examine/bin/platform-answers-selftest"
scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
failures=0
checks=0
output=""
status=0

# --- steps -------------------------------------------------------------------------------------------------

given_the_examine_commands_are_built() {
  bash "$repo/Scripts/examine/build-examine-commands" >"$scratch/build.log" 2>&1 \
    || { cat "$scratch/build.log"; echo "FAIL: building the examine commands failed, so nothing below can be trusted"; exit 1; }
}

# One captured answer, in the shape the capture writes: the status, the request and the body.
an_answer() {
  local folder=$1 name=$2 request=$3 body=$4
  mkdir -p "$(dirname "$folder/$name.json")"
  printf '{\n  "status": 200,\n  "request": "%s",\n  "body": %s\n}\n' "$request" "$body" > "$folder/$name.json"
}

# What one run of the backend says about itself. The process id and the install time are new on every run.
the_system_info() {
  local folder=$1 process_id=$2 installed_at=$3 database=$4
  an_answer "$folder" system-info "/api/latest/systeminfo" \
    "{\"os\": \"Linux\", \"runtime\": \".NET 10.0.0\", \"architecture\": \"X64\", \"processId\": $process_id, \"databaseProvider\": \"$database\", \"logPath\": \"logs\", \"authenticationEnabled\": false, \"authorizationEnabled\": false, \"emergencyAdminSubjects\": [], \"baseUrl\": \"http://localhost\", \"installTimestamp\": \"$installed_at\"}"
}

the_log_level() {
  local folder=$1 level=$2
  an_answer "$folder" log-level "/api/latest/logs/level" "\"$level\""
}

# The problems the log recorded; when each was recorded is a clock value, its words are not.
the_log_problems() {
  local folder=$1 recorded_at=$2 message=$3
  an_answer "$folder" log-problems "/api/latest/logs/problems" \
    "[{\"recordedAt\": \"$recorded_at\", \"level\": \"Warning\", \"source\": \"Startup\", \"message\": \"$message\", \"exceptionType\": null}]"
}

# The twelve platform answers of one revision, read with authentication off and without calling GitHub: the
# system info, the current version, whether it can update itself and how it was distributed, the encryption state
# and the secrets check, the database-management status, the usage-data state, the log level, the supported log
# levels and the recorded problems, and the update status.
a_capture_of_the_platform_answers() {
  local folder=$1
  the_system_info "$folder" 4242 "2026-11-22T08:00:00Z" sqlite
  an_answer "$folder" version-current "/api/latest/version/current" '"v26.11.22.1"'
  an_answer "$folder" version-update-supported "/api/latest/version/updateSupported" 'false'
  an_answer "$folder" version-distribution "/api/latest/version/distribution" \
    '{"variantId": "standalone", "updateOwner": "lighthouse"}'
  an_answer "$folder" encryption-state "/api/latest/encryption" \
    '{"custody": "minted", "canMint": true, "secretsUnderPublishedKey": 0, "readableSecretsNotOnTheActiveKey": 0}'
  an_answer "$folder" encryption-secrets "/api/latest/encryption/secrets" \
    '{"connections": []}'
  an_answer "$folder" database-management-status "/api/latest/database-management/status" \
    '{"provider": "sqlite", "isOperationBlocked": false, "blockedReason": null, "isToolingAvailable": true, "toolingGuidanceMessage": null, "toolingGuidanceUrl": null, "activeOperation": null}'
  an_answer "$folder" usage-data-state "/api/latest/usagedata/state" \
    '{"sending": false, "decision": null, "mayAsk": true, "reAskAfterDays": 90, "administratorDisabled": false, "acceptedSources": []}'
  the_log_level "$folder" Information
  an_answer "$folder" log-levels-supported "/api/latest/logs/level/supported" \
    '["Verbose", "Debug", "Information", "Warning", "Error"]'
  the_log_problems "$folder" "2026-11-22T08:00:05Z" "No connection could be refreshed yet"
  an_answer "$folder" update-status "/api/latest/update/status" \
    '{"hasActiveUpdates": false, "activeCount": 0}'
}

two_identical_captures() {
  a_capture_of_the_platform_answers "$scratch/identical-base"
  # A copy, not the same folder, so a compare that only looks at the folder names cannot pass.
  cp -R "$scratch/identical-base" "$scratch/identical-candidate"
}

two_captures_where_the_log_level_changed() {
  a_capture_of_the_platform_answers "$scratch/changed-base"
  a_capture_of_the_platform_answers "$scratch/changed-candidate"
  the_log_level "$scratch/changed-candidate" Debug
}

two_captures_from_different_runs_with_the_same_answers() {
  a_capture_of_the_platform_answers "$scratch/other-run-base"
  a_capture_of_the_platform_answers "$scratch/other-run-candidate"
  the_system_info "$scratch/other-run-candidate" 9917 "2026-11-22T09:30:00Z" sqlite
  the_log_problems "$scratch/other-run-candidate" "2026-11-22T09:30:07Z" "No connection could be refreshed yet"
}

two_captures_from_different_runs_where_stable_answers_beside_them_changed() {
  a_capture_of_the_platform_answers "$scratch/stable-base"
  a_capture_of_the_platform_answers "$scratch/stable-candidate"
  the_system_info "$scratch/stable-candidate" 9917 "2026-11-22T09:30:00Z" postgres
  the_log_problems "$scratch/stable-candidate" "2026-11-22T09:30:07Z" "The database could not be reached"
}

two_empty_captures() {
  mkdir -p "$scratch/empty-base" "$scratch/empty-candidate"
}

when_the_compare_stage_compares() {
  local base=$1 candidate=$2
  if [ ! -x "$command" ]; then
    output="the command $command does not exist after building the examine commands"
    status=127
    return
  fi
  output="$("$command" --compare-captures "$base" "$candidate" 2>&1)"
  status=$?
}

# The usage of an examine command is the comment block at its top: it is all the examiner reads before running it.
when_the_examiner_reads_the_command_usage() {
  if [ ! -f "$command" ]; then
    output="the command $command does not exist after building the examine commands"
    status=127
    return
  fi
  output="$(sed -n '2,/^[^#]/p' "$command" | grep '^#')"
  status=0
}

when_the_examiner_runs_the_selftest() {
  if [ ! -x "$selftest" ]; then
    output="the command $selftest does not exist after building the examine commands"
    status=127
    return
  fi
  # The selftest runs this very script; the marker stops that inner run from starting the selftest again.
  output="$(PLATFORM_ANSWERS_SELFTEST_INNER_RUN=1 timeout 300 "$selftest" 2>&1)"
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
output_has() { printf '%s\n' "$output" | grep -qE "$1"; }
output_has_any_case() { printf '%s\n' "$output" | grep -qiE "$1"; }
exits_with() { [ "$status" = "$1" ]; }
nothing_called_identical() { ! output_has 'VERDICT: IDENTICAL'; }
exits_with_failure() { [ "$status" != 0 ]; }

then_the_changed_answer_is_reported_different() {
  check "$(holds exits_with 1)" \
    "a capture pair with one changed platform answer exits 1" \
    "the examiner reads the exit to tell a changed platform answer from an unchanged one" \
    "let platform-answers-compare --compare-captures BASE CANDIDATE exit 1 when any answer differs"
  check "$(holds output_has '^VERDICT: DIFFERENT')" \
    "a capture pair with one changed platform answer ends on a DIFFERENT verdict" \
    "a move that changes what the platform tells an administrator must not pass as behaviour-free" \
    "print a last line starting VERDICT: DIFFERENT when any answer differs"
  check "$(holds output_has '^log-level: 1 compared, 1 different')" \
    "the report says the log level is the answer that differs" \
    "the reader must see which kind of answer changed, out of how many compared" \
    "print one line per kind of answer: '<kind>: <n> compared, <m> different'"
  check "$(holds output_has '^system-info: 1 compared, 0 different')" \
    "the report says the unchanged system info was compared and matches" \
    "a report that marks every answer different cannot point at the one that changed" \
    "compare each answer on its own and count only the ones whose body differs"
  check "$(holds output_has 'log-level\.json')" \
    "the differences name the changed answer's file, log-level" \
    "without the answer's name nobody can find what changed" \
    "list the differing answers by their place in the capture folder"
}

then_the_identical_answers_are_reported_identical() {
  check "$(holds exits_with 0)" \
    "a capture pair with matching platform answers exits 0" \
    "a command that always says DIFFERENT cannot show a move left the platform answers alone" \
    "let platform-answers-compare --compare-captures BASE CANDIDATE exit 0 when every answer matches"
  check "$(holds output_has '^VERDICT: IDENTICAL, all 12 ')" \
    "a capture pair with matching answers ends on IDENTICAL over all 12 captured answers" \
    "IDENTICAL is only worth something when every captured answer was compared" \
    "print VERDICT: IDENTICAL, all <n> ... with n the number of answers compared"
}

then_the_values_of_the_run_are_not_compared() {
  check "$(holds exits_with 0)" \
    "a capture pair that differs only in the process id, the install time and when a problem was recorded exits 0" \
    "those values are new on every run, so two runs of an unchanged revision would otherwise never match" \
    "blank processId, installTimestamp and recordedAt at any depth in both captures before --compare-captures compares them"
  check "$(holds output_has '^VERDICT: IDENTICAL, all 12 ')" \
    "a capture pair that differs only in values of the run ends on IDENTICAL over all 12 captured answers" \
    "leaving out a volatile value must not drop the answer that carries it from the comparison" \
    "blank only the masked values; keep every answer in the count"
}

then_the_stable_answers_beside_them_are_still_compared() {
  check "$(holds exits_with 1)" \
    "a capture pair where the database provider and a problem's message changed, beside values of the run, exits 1" \
    "a mask that blanks a whole answer, or a stable value next to a volatile one, hides a real change" \
    "blank only the values of the masked keys, never their siblings or the answer around them"
  check "$(holds output_has '^system-info: 1 compared, 1 different')" \
    "the report says the system info differs when its database provider changed" \
    "the database provider is a stable answer: a move that changes it changes what the instance runs on" \
    "do not mask databaseProvider or the system info answer as a whole"
  check "$(holds output_has '^log-problems: 1 compared, 1 different')" \
    "the report says the recorded problems differ when a problem's message changed" \
    "what went wrong is a stable answer; only when it was recorded changes from run to run" \
    "mask recordedAt only, not the problem entry it sits in"
}

then_nothing_compared_is_a_failure() {
  check "$(holds exits_with_failure)" \
    "two empty capture folders do not exit 0" \
    "an empty capture would otherwise pass as proof that nothing changed" \
    "exit 1 when no answer was compared"
  check "$(holds nothing_called_identical)" \
    "two empty capture folders are not called IDENTICAL" \
    "IDENTICAL over nothing is a false green" \
    "print a FAILED verdict, never IDENTICAL, when no answer was compared"
  check "$(holds output_has 'nothing was compared')" \
    "two empty capture folders are reported as nothing compared" \
    "the reader must tell an empty capture from a matching one" \
    "say that nothing was compared"
}

usage_names() {
  local answer=$1 pattern=$2
  check "$(holds output_has_any_case "$pattern")" \
    "the command usage names the captured answer: $answer" \
    "the examiner decides what a green run proves from the usage alone; an answer it does not name is not known to be compared" \
    "name $answer in the comment block at the top of platform-answers-compare"
}

then_the_usage_names_every_captured_answer() {
  usage_names "system info" 'system[ -]?info'
  usage_names "the version" 'version'
  usage_names "that the version answers calling GitHub are left out" 'github'
  usage_names "encryption and secrets state" 'encryption.*secrets|secrets.*encryption'
  usage_names "database-management status" 'database[ -]management'
  usage_names "usage-data state" 'usage[ -]?data'
  usage_names "log level" 'log level'
  usage_names "problems" 'problems'
  usage_names "update status" 'update status'
}

is_admitted() {
  local name=$1
  [ "$(jq -r --arg n "$name" '.commands[] | select(.name == $n) | .path' "$repo/.nwave/examine.json" 2>/dev/null)" \
    = "Scripts/examine/bin/$name" ]
}

then_the_commands_are_admitted_examine_probes() {
  local name
  for name in platform-answers-compare platform-answers-selftest; do
    output="$(jq -c --arg n "$name" '.commands[] | select(.name == $n)' "$repo/.nwave/examine.json" 2>&1)"
    status=$?
    check "$(holds is_admitted "$name")" \
      "the examine commands list admits $name at Scripts/examine/bin/$name" \
      "the examiner may run only the commands that list admits" \
      "add {\"name\": \"$name\", \"path\": \"Scripts/examine/bin/$name\"} to .nwave/examine.json"
  done
}

then_the_selftest_runs_these_checks_and_passes() {
  check "$(holds exits_with 0)" \
    "platform-answers-selftest exits 0 when these checks pass" \
    "the examiner runs the selftest to see that the compare stage can tell answers apart" \
    "let platform-answers-selftest run bash Scripts/examine/test-platform-answers-compare.sh and pass on its exit"
  check "$(holds output_has '^== one platform answer changed')" \
    "platform-answers-selftest runs the platform compare checks, not another command's" \
    "a selftest that runs a different check proves nothing about platform-answers-compare" \
    "let platform-answers-selftest run bash Scripts/examine/test-platform-answers-compare.sh"
  check "$(holds output_has '^RESULT: all [0-9]+ checks passed')" \
    "platform-answers-selftest prints the total of the checks it ran" \
    "the examiner reads that total to see the checks ran to the end" \
    "pass the output of Scripts/examine/test-platform-answers-compare.sh through unchanged"
}

# --- scenarios ---------------------------------------------------------------------------------------------

given_the_examine_commands_are_built

echo "== one platform answer changed"
two_captures_where_the_log_level_changed
when_the_compare_stage_compares "$scratch/changed-base" "$scratch/changed-candidate"
then_the_changed_answer_is_reported_different

echo "== identical platform answers"
two_identical_captures
when_the_compare_stage_compares "$scratch/identical-base" "$scratch/identical-candidate"
then_the_identical_answers_are_reported_identical

echo "== only values of the run changed"
two_captures_from_different_runs_with_the_same_answers
when_the_compare_stage_compares "$scratch/other-run-base" "$scratch/other-run-candidate"
then_the_values_of_the_run_are_not_compared

echo "== stable answers beside values of the run changed"
two_captures_from_different_runs_where_stable_answers_beside_them_changed
when_the_compare_stage_compares "$scratch/stable-base" "$scratch/stable-candidate"
then_the_stable_answers_beside_them_are_still_compared

echo "== nothing captured"
two_empty_captures
when_the_compare_stage_compares "$scratch/empty-base" "$scratch/empty-candidate"
then_nothing_compared_is_a_failure

echo "== usage names the captured answers"
when_the_examiner_reads_the_command_usage
then_the_usage_names_every_captured_answer

echo "== admitted examine probes"
then_the_commands_are_admitted_examine_probes

if [ -z "${PLATFORM_ANSWERS_SELFTEST_INNER_RUN:-}" ]; then
  echo "== selftest"
  when_the_examiner_runs_the_selftest
  then_the_selftest_runs_these_checks_and_passes
fi

if [ "$failures" -gt 0 ]; then
  echo "RESULT: $failures of $checks checks failed"
  exit 1
fi
echo "RESULT: all $checks checks passed"
