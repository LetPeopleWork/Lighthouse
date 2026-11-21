#!/usr/bin/env bash
# Checks that the Access and Licensing answers examine command tells a changed answer from an unchanged one.
# It builds the examine commands, then runs the command's compare stage on capture folders written here:
# two that differ in one answer (the licence status after a licence import), two that match, and two empty
# ones. Capturing on real revisions takes minutes, so this check feeds the compare stage directly; the
# examiner runs the full command. It also checks that the command's usage names every answer it captures,
# that both the command and its selftest are admitted examine probes, and that the selftest runs this check.
# Usage: bash Scripts/examine/test-access-answers-compare.sh
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"
command="$repo/Scripts/examine/bin/access-answers-compare"
selftest="$repo/Scripts/examine/bin/access-answers-selftest"
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

# The ten Access and Licensing answers of one revision, read with authentication off: the auth mode, the
# session, my summary, the RBAC status, the system-wide rules of a Team and of a Portfolio, everyone signed in,
# the API keys, and the licence status before and after the premium licence is imported.
a_capture_of_the_access_answers() {
  local folder=$1
  an_answer "$folder" auth-mode "/api/latest/auth/mode" \
    '{"mode": "disabled", "misconfigurationMessage": null}'
  an_answer "$folder" session "/api/latest/auth/session" \
    '{"isAuthenticated": true, "displayName": "Authentication disabled", "email": null}'
  an_answer "$folder" my-summary "/api/latest/authorization/my-summary" \
    '{"isSystemAdmin": true, "teams": [], "portfolios": []}'
  an_answer "$folder" rbac-status "/api/latest/authorization/status" \
    '{"enabled": false, "hasSystemAdmin": false}'
  an_answer "$folder" team-1/system-wide-rules "/api/latest/authorization/teams/1/system-wide-rules" \
    '[]'
  an_answer "$folder" portfolio-2/system-wide-rules "/api/latest/authorization/portfolios/2/system-wide-rules" \
    '[]'
  an_answer "$folder" everyone-signed-in "/api/latest/authorization/everyone-signed-in" \
    '{"role": "Viewer"}'
  an_answer "$folder" api-keys "/api/latest/apikeys" \
    '[]'
  an_answer "$folder" licence-status-before-import "/api/latest/license" \
    '{"hasLicense": false, "canUsePremiumFeatures": false}'
  an_answer "$folder" licence-status-after-import "/api/latest/license" \
    '{"hasLicense": true, "canUsePremiumFeatures": true, "name": "Premium Test", "expiryDate": "2027-12-31"}'
}

two_identical_captures() {
  a_capture_of_the_access_answers "$scratch/identical-base"
  # A copy, not the same folder, so a compare that only looks at the folder names cannot pass.
  cp -R "$scratch/identical-base" "$scratch/identical-candidate"
}

two_captures_where_the_licence_status_after_the_import_changed() {
  a_capture_of_the_access_answers "$scratch/changed-base"
  a_capture_of_the_access_answers "$scratch/changed-candidate"
  an_answer "$scratch/changed-candidate" licence-status-after-import "/api/latest/license" \
    '{"hasLicense": true, "canUsePremiumFeatures": false, "name": "Premium Test", "expiryDate": "2027-12-31"}'
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
  output="$(ACCESS_ANSWERS_SELFTEST_INNER_RUN=1 timeout 300 "$selftest" 2>&1)"
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
    "a capture pair with one changed Access or Licensing answer exits 1" \
    "the examiner reads the exit to tell a changed Access or Licensing answer from an unchanged one" \
    "let access-answers-compare --compare-captures BASE CANDIDATE exit 1 when any answer differs"
  check "$(holds output_has '^VERDICT: DIFFERENT')" \
    "a capture pair with one changed Access or Licensing answer ends on a DIFFERENT verdict" \
    "a move that changes what a user is allowed to do, or what their licence unlocks, must not pass as behaviour-free" \
    "print a last line starting VERDICT: DIFFERENT when any answer differs"
  check "$(holds output_has '^licence-status-after-import: 1 compared, 1 different')" \
    "the report says the licence status after the import is the answer that differs" \
    "the reader must see which kind of answer changed, out of how many compared" \
    "print one line per kind of answer: '<kind>: <n> compared, <m> different'"
  check "$(holds output_has '^auth-mode: 1 compared, 0 different')" \
    "the report says the unchanged auth mode was compared and matches" \
    "a report that marks every answer different cannot point at the one that changed" \
    "compare each answer on its own and count only the ones whose body differs"
  check "$(holds output_has 'licence-status-after-import\.json')" \
    "the differences name the changed answer's file, licence-status-after-import" \
    "without the answer's name nobody can find what changed" \
    "list the differing answers by their place in the capture folder"
}

then_the_identical_answers_are_reported_identical() {
  check "$(holds exits_with 0)" \
    "a capture pair with matching Access and Licensing answers exits 0" \
    "a command that always says DIFFERENT cannot show a move left the Access and Licensing answers alone" \
    "let access-answers-compare --compare-captures BASE CANDIDATE exit 0 when every answer matches"
  check "$(holds output_has '^VERDICT: IDENTICAL, all 10 ')" \
    "a capture pair with matching answers ends on IDENTICAL over all 10 captured answers" \
    "IDENTICAL is only worth something when every captured answer was compared" \
    "print VERDICT: IDENTICAL, all <n> ... with n the number of answers compared"
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
    "name $answer in the comment block at the top of access-answers-compare"
}

then_the_usage_names_every_captured_answer() {
  usage_names "auth mode" 'auth(entication)?[ -]mode'
  usage_names "session" 'session'
  usage_names "my-summary" 'my[ -]summary'
  usage_names "RBAC status" 'rbac[ -]status'
  usage_names "system-wide rules" 'system[ -]wide[ -]rules'
  usage_names "everyone-signed-in" 'everyone[ -]signed[ -]in'
  usage_names "API keys" 'api[ -]keys'
  usage_names "licence status before and after a licence import" \
    'licen[cs]e status.*before.*after.*import|licen[cs]e status.*import.*(again|after)'
}

is_admitted() {
  local name=$1
  [ "$(jq -r --arg n "$name" '.commands[] | select(.name == $n) | .path' "$repo/.nwave/examine.json" 2>/dev/null)" \
    = "Scripts/examine/bin/$name" ]
}

then_the_commands_are_admitted_examine_probes() {
  local name
  for name in access-answers-compare access-answers-selftest; do
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
    "access-answers-selftest exits 0 when these checks pass" \
    "the examiner runs the selftest to see that the compare stage can tell answers apart" \
    "let access-answers-selftest run bash Scripts/examine/test-access-answers-compare.sh and pass on its exit"
  check "$(holds output_has '^== one Access or Licensing answer changed')" \
    "access-answers-selftest runs the Access and Licensing compare checks, not another command's" \
    "a selftest that runs a different check proves nothing about access-answers-compare" \
    "let access-answers-selftest run bash Scripts/examine/test-access-answers-compare.sh"
  check "$(holds output_has '^RESULT: all [0-9]+ checks passed')" \
    "access-answers-selftest prints the total of the checks it ran" \
    "the examiner reads that total to see the checks ran to the end" \
    "pass the output of Scripts/examine/test-access-answers-compare.sh through unchanged"
}

# --- scenarios ---------------------------------------------------------------------------------------------

given_the_examine_commands_are_built

echo "== one Access or Licensing answer changed"
two_captures_where_the_licence_status_after_the_import_changed
when_the_compare_stage_compares "$scratch/changed-base" "$scratch/changed-candidate"
then_the_changed_answer_is_reported_different

echo "== identical Access and Licensing answers"
two_identical_captures
when_the_compare_stage_compares "$scratch/identical-base" "$scratch/identical-candidate"
then_the_identical_answers_are_reported_identical

echo "== nothing captured"
two_empty_captures
when_the_compare_stage_compares "$scratch/empty-base" "$scratch/empty-candidate"
then_nothing_compared_is_a_failure

echo "== usage names the captured answers"
when_the_examiner_reads_the_command_usage
then_the_usage_names_every_captured_answer

echo "== admitted examine probes"
then_the_commands_are_admitted_examine_probes

if [ -z "${ACCESS_ANSWERS_SELFTEST_INNER_RUN:-}" ]; then
  echo "== selftest"
  when_the_examiner_runs_the_selftest
  then_the_selftest_runs_these_checks_and_passes
fi

if [ "$failures" -gt 0 ]; then
  echo "RESULT: $failures of $checks checks failed"
  exit 1
fi
echo "RESULT: all $checks checks passed"
