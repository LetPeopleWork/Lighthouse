#!/usr/bin/env bash
# Checks that the Delivery-answers examine command tells a changed Delivery answer from an unchanged one.
# It builds the examine commands, then runs the command's compare stage on capture folders written here:
# two that differ in one Delivery answer, two that match, and two empty ones. Capturing on real revisions
# takes minutes, so this check feeds the compare stage directly; the examiner runs the full command.
# Usage: bash Scripts/examine/test-deliveries-answers-compare.sh
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"
command="$repo/Scripts/examine/bin/deliveries-answers-compare"
scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
failures=0
checks=0

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

# Five Delivery answers of one demo scenario: a Portfolio's Delivery list, its Delivery rules schema and its
# Delivery sources, and the metrics history of each of its two Deliveries.
a_capture_of_the_delivery_answers() {
  local folder=$1
  an_answer "$folder" scenario-1/portfolio-3/deliveries "/api/v1/deliveries/portfolio/3" \
    '[{"id": 7, "name": "Apollo Release", "likelihoodPercentage": 81.5}, {"id": 8, "name": "Apollo Beta", "likelihoodPercentage": 42.0}]'
  an_answer "$folder" scenario-1/portfolio-3/delivery-rules-schema "/api/v1/portfolios/3/delivery-rules/schema" \
    '{"fields": [{"fieldKey": "feature.state", "displayName": "State"}], "operators": ["equals", "contains"]}'
  an_answer "$folder" scenario-1/portfolio-3/delivery-sources "/api/v1/portfolios/3/delivery-sources" \
    '[{"sourceKey": "manual", "displayName": "Manual"}]'
  an_answer "$folder" scenario-1/delivery-7/metrics-history "/api/v1/deliveries/7/metrics-history" \
    '[{"date": "2026-11-16", "likelihoodPercentage": 79.0}, {"date": "2026-11-17", "likelihoodPercentage": 81.5}]'
  an_answer "$folder" scenario-1/delivery-8/metrics-history "/api/v1/deliveries/8/metrics-history" \
    '[{"date": "2026-11-17", "likelihoodPercentage": 42.0}]'
}

two_identical_captures() {
  a_capture_of_the_delivery_answers "$scratch/identical-base"
  # A copy, not the same folder, so a compare that only looks at the folder names cannot pass.
  cp -R "$scratch/identical-base" "$scratch/identical-candidate"
}

two_captures_where_one_delivery_metrics_history_changed() {
  a_capture_of_the_delivery_answers "$scratch/changed-base"
  a_capture_of_the_delivery_answers "$scratch/changed-candidate"
  an_answer "$scratch/changed-candidate" scenario-1/delivery-7/metrics-history "/api/v1/deliveries/7/metrics-history" \
    '[{"date": "2026-11-16", "likelihoodPercentage": 79.0}, {"date": "2026-11-17", "likelihoodPercentage": 64.0}]'
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
exits_with() { [ "$status" = "$1" ]; }
nothing_called_identical() { ! output_has 'VERDICT: IDENTICAL'; }
exits_with_failure() { [ "$status" != 0 ]; }

then_the_changed_answer_is_reported_different() {
  check "$(holds exits_with 1)" \
    "a capture pair with one changed Delivery answer exits 1" \
    "the examiner reads the exit to tell a changed Delivery answer from an unchanged one" \
    "let deliveries-answers-compare --compare-captures BASE CANDIDATE exit 1 when any answer differs"
  check "$(holds output_has '^VERDICT: DIFFERENT')" \
    "a capture pair with one changed Delivery answer ends on a DIFFERENT verdict" \
    "a move that changes a Delivery answer must not pass as behaviour-free" \
    "print a last line starting VERDICT: DIFFERENT when any answer differs"
  check "$(holds output_has '^metrics-history: 2 compared, 1 different')" \
    "the report says 1 of the 2 Delivery metrics histories differs" \
    "the reader must see which kind of Delivery answer changed, out of how many compared" \
    "print one line per kind of answer: '<kind>: <n> compared, <m> different'"
  check "$(holds output_has 'delivery-7/metrics-history')" \
    "the report names the changed answer, Delivery 7's metrics history" \
    "without the answer's name nobody can find what changed" \
    "list the differing answers by their place in the capture folder"
}

then_the_identical_answers_are_reported_identical() {
  check "$(holds exits_with 0)" \
    "a capture pair with matching Delivery answers exits 0" \
    "a command that always says DIFFERENT cannot show a move left the Delivery answers alone" \
    "let deliveries-answers-compare --compare-captures BASE CANDIDATE exit 0 when every answer matches"
  check "$(holds output_has '^VERDICT: IDENTICAL, all 5 ')" \
    "a capture pair with matching Delivery answers ends on IDENTICAL over all 5 answers" \
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

then_the_command_is_an_admitted_examine_probe() {
  local admitted=no
  output="$(jq -c '.commands[] | select(.name == "deliveries-answers-compare")' "$repo/.nwave/examine.json" 2>&1)"
  status=$?
  [ "$(jq -r '.commands[] | select(.name == "deliveries-answers-compare") | .path' "$repo/.nwave/examine.json" 2>/dev/null)" \
    = "Scripts/examine/bin/deliveries-answers-compare" ] && admitted=yes
  check "$admitted" \
    "the examine commands list admits deliveries-answers-compare at Scripts/examine/bin/deliveries-answers-compare" \
    "the examiner may run only the commands that list admits" \
    "add {\"name\": \"deliveries-answers-compare\", \"path\": \"Scripts/examine/bin/deliveries-answers-compare\"} to .nwave/examine.json"
}

# --- scenarios ---------------------------------------------------------------------------------------------

given_the_examine_commands_are_built

echo "== one Delivery answer changed"
two_captures_where_one_delivery_metrics_history_changed
when_the_compare_stage_compares "$scratch/changed-base" "$scratch/changed-candidate"
then_the_changed_answer_is_reported_different

echo "== identical Delivery answers"
two_identical_captures
when_the_compare_stage_compares "$scratch/identical-base" "$scratch/identical-candidate"
then_the_identical_answers_are_reported_identical

echo "== nothing captured"
two_empty_captures
when_the_compare_stage_compares "$scratch/empty-base" "$scratch/empty-candidate"
then_nothing_compared_is_a_failure

echo "== admitted examine probe"
then_the_command_is_an_admitted_examine_probe

if [ "$failures" -gt 0 ]; then
  echo "RESULT: $failures of $checks checks failed"
  exit 1
fi
echo "RESULT: all $checks checks passed"
