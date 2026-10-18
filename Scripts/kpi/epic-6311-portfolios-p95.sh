#!/usr/bin/env bash
# Measures how quickly the Portfolios list answers: 20 GET requests, then the 95th percentile of their
# times in milliseconds. Run it once on an idle instance and once while a refresh is forecasting, and
# compare the two numbers.
#
# It only reads. Nothing on the instance is changed.
#
# Usage: epic-6311-portfolios-p95.sh [base-url]        (default http://localhost:5169)
# An instance with authentication on needs a header, passed through the environment so that it never
# appears in the process list or in this script's output, for example
#   LIGHTHOUSE_AUTH_HEADER="Authorization: Bearer <token>" epic-6311-portfolios-p95.sh https://lighthouse.example
set -euo pipefail

readonly REQUESTS=20
readonly ENDPOINT="/api/latest/portfolios"

time_one_request() {
  local url="$1"
  local curl_args=(--silent --show-error --output /dev/null --write-out '%{http_code} %{time_total}')
  if [[ -n "${LIGHTHOUSE_AUTH_HEADER:-}" ]]; then
    # curl reads the header from its standard input, so the token never shows up in the process list.
    curl_args+=(--header @-)
  fi

  local answer
  if ! answer="$(curl "${curl_args[@]}" "${url}" <<< "${LIGHTHOUSE_AUTH_HEADER:-}")"; then
    echo "error: could not reach ${url}" >&2
    return 1
  fi

  local status="${answer%% *}"
  local seconds="${answer##* }"
  if [[ "${status}" != "200" ]]; then
    echo "error: ${url} answered HTTP ${status}, not 200" >&2
    return 1
  fi

  awk -v s="${seconds}" 'BEGIN { printf "%d\n", s * 1000 + 0.5 }'
  return 0
}

# Nearest rank: the smallest timing that at least 95% of the requests were no slower than.
p95_of() {
  local rank=$(( (95 * $# + 99) / 100 ))
  printf '%s\n' "$@" | sort -n | sed -n "${rank}p"
  return 0
}

main() {
  local base_url="${1:-http://localhost:5169}"
  local url="${base_url%/}${ENDPOINT}"
  local timings=()
  local i
  for (( i = 1; i <= REQUESTS; i++ )); do
    timings+=("$(time_one_request "${url}")")
  done

  echo "GET ${ENDPOINT} x${REQUESTS} against ${base_url%/}: p95 $(p95_of "${timings[@]}") ms"
  return 0
}

main "$@"
