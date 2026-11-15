#!/usr/bin/env bash
# What the signing runner promises the maintainer: an image built from a pinned base that starts the
# GitHub runner from a writable copy on every boot, registers once from a token file without leaking it,
# a compose file and systemd unit that bring it up after a reboot, and a workflow that builds, checks and
# publishes it. Needs only Docker and bash. Set IMAGE to check a published image instead of building.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd -P)"
codesign_dir="$repo_root/tools/codesign"
dockerfile="$codesign_dir/Dockerfile"
compose_file="$codesign_dir/compose.yaml"
unit_file="$codesign_dir/lighthouse-codesign-runner.service"
workflow_file="$repo_root/.github/workflows/codesign-runner-image.yml"
renovate_file="$repo_root/renovate.json"

published_image="ghcr.io/letpeoplework/lighthouse-codesign-runner:latest"
register_command="docker compose --profile register run --rm register"
token_secret_path="/run/secrets/github_token"
yq_image="mikefarah/yq:4.47.1@sha256:b9285dd3b0bea3c34d0c54415dd48d767dabd9644d489bd6e253660847b58419"
start_limit_seconds=60

observed=0
assertions_failed=0
setup_failed=0
prefix="lh-codesign-oracle-$$"
work_dir=""
image=""
created_volumes=()
created_containers=()

# shellcheck disable=SC2317 # called through the EXIT trap, which shellcheck cannot follow
cleanup() {
  local name
  for name in "${created_containers[@]+"${created_containers[@]}"}"; do docker rm -f "$name" >/dev/null 2>&1 || true; done
  for name in "${created_volumes[@]+"${created_volumes[@]}"}"; do docker volume rm -f "$name" >/dev/null 2>&1 || true; done
  if [ -n "$work_dir" ]; then rm -rf "$work_dir"; fi
}
trap cleanup EXIT

pass() {
  observed=$((observed + 1))
  echo "PASS: $1"
}

fail() {
  observed=$((observed + 1))
  assertions_failed=$((assertions_failed + 1))
  echo "FAIL: $1"
  echo "  WHAT: $2"
  echo "  WHY:  $3"
  echo "  HOW:  $4"
}

setup_failure() {
  setup_failed=$((setup_failed + 1))
  echo "SETUP FAILED: $1"
}

summary_and_exit() {
  echo "observed=$observed assertions_failed=$assertions_failed setup_failed=$setup_failed"
  if [ "$assertions_failed" -eq 0 ] && [ "$setup_failed" -eq 0 ]; then exit 0; fi
  exit 1
}

# Prints every value recorded under KEY in a block of KEY=value lines.
values_of() {
  local key=$1 block=$2 line
  while IFS= read -r line; do
    if [[ $line == "$key="* ]]; then printf '%s\n' "${line#*=}"; fi
  done <<< "$block"
}

value_of() {
  local all
  all="$(values_of "$1" "$2")"
  printf '%s' "${all%%$'\n'*}"
}

joined_values_of() {
  local all
  all="$(values_of "$1" "$2")"
  printf '%s' "${all//$'\n'/ }"
}

contains_line() {
  local wanted=$1 block=$2 line
  while IFS= read -r line; do
    if [ "$line" = "$wanted" ]; then return 0; fi
  done <<< "$block"
  return 1
}

yes_no() {
  if "$@"; then echo yes; else echo no; fi
}

has_text() {
  [[ $2 == *"$1"* ]]
}

yq_facts() {
  local file=$1 expression=$2
  docker run --rm -i --network none "$yq_image" -r "$expression" < "$file"
}

runner_version_in_dockerfile() {
  local line
  while IFS= read -r line; do
    if [[ $line =~ ^ARG[[:space:]]+RUNNER_VERSION=([^[:space:]]+) ]]; then printf '%s' "${BASH_REMATCH[1]}"; return 0; fi
  done < "$dockerfile"
  return 0
}

# ---------------------------------------------------------------- preparation

if ! docker info >/dev/null 2>&1; then
  setup_failure "the Docker daemon is not reachable; start Docker and re-run"
  summary_and_exit
fi
if ! docker image inspect "$yq_image" >/dev/null 2>&1 && ! docker pull -q "$yq_image" >/dev/null 2>&1; then
  setup_failure "cannot pull the YAML reader $yq_image; check the network and re-run"
  summary_and_exit
fi

work_dir="$(mktemp -d)"
chmod 755 "$work_dir"
stubs_dir="$work_dir/stubs"
path_stubs_dir="$work_dir/path-stubs"
mkdir -p "$stubs_dir" "$path_stubs_dir"

# Stand-in for run.sh: records where it really runs from and what it finds there, and asks the real
# runner binary copied next to it for its version.
cat > "$stubs_dir/run.sh" <<'STUB'
#!/bin/bash
here="$(cd -P "$(dirname "$0")" && pwd -P)"
{
  echo "realdir=$here"
  if [ -L "$0" ]; then echo "symlink=yes"; else echo "symlink=no"; fi
  if touch "$here/.write-probe" 2>/dev/null; then echo "writable=yes"; else echo "writable=no"; fi
  for f in .runner .credentials .credentials_rsaparams; do
    if [ -f "$here/$f" ]; then echo "beside=$f"; fi
  done
  version_output="$("$here/bin/Runner.Listener" --version 2>&1)" || true
  echo "version=${version_output##*$'\n'}"
} > /record/run-sh
exit 0
STUB

# Stand-in for config.sh: records its location and arguments and leaves the three registration files
# next to itself, as the real one does after a successful registration.
cat > "$stubs_dir/config.sh" <<'STUB'
#!/bin/bash
here="$(cd -P "$(dirname "$0")" && pwd -P)"
{
  echo "realdir=$here"
  for a in "$@"; do echo "arg=$a"; done
} > /record/config-sh
printf '{"agentName":"codesign-runner-linux"}' > "$here/.runner"
printf '{"scheme":"OAuth"}' > "$here/.credentials"
printf '{}' > "$here/.credentials_rsaparams"
exit 0
STUB

# Stand-in for curl: records argv, stdin and the content of every file it is pointed at, then answers
# like the GitHub registration-token endpoint.
cat > "$path_stubs_dir/curl" <<'STUB'
#!/bin/bash
out=""
previous=""
for a in "$@"; do
  echo "arg=$a" >> /record/curl-argv
  case "$previous" in
    -o|--output) out="$a" ;;
    -K|--config|-H|--header|-d|--data|--data-binary|--data-raw)
      if [ "$a" != "-" ] && [ -f "${a#@}" ]; then cat "${a#@}" >> /record/curl-inputs; fi ;;
  esac
  case "$a" in
    @*) if [ -f "${a#@}" ]; then cat "${a#@}" >> /record/curl-inputs; fi ;;
  esac
  previous="$a"
done
if [ ! -t 0 ]; then cat >> /record/curl-inputs; fi
body='{"token":"AORACLEREGISTRATIONTOKEN","expires_at":"2099-01-01T00:00:00Z"}'
if [ -n "$out" ] && [ "$out" != "-" ]; then printf '%s' "$body" > "$out"; else printf '%s' "$body"; fi
exit 0
STUB

# A run.sh an older image could have left in the state volume; it must never be the one that runs.
cat > "$work_dir/stale-run.sh" <<'STUB'
#!/bin/bash
echo "stale" > /record/stale-run-sh
exit 0
STUB
chmod 755 "$stubs_dir/run.sh" "$stubs_dir/config.sh" "$path_stubs_dir/curl" "$work_dir/stale-run.sh"

image_built=false
if [ -n "${IMAGE:-}" ]; then
  image="$IMAGE"
  if ! docker image inspect "$image" >/dev/null 2>&1 && ! docker pull -q "$image" >/dev/null 2>&1; then
    setup_failure "cannot pull IMAGE=$image; check the tag and the network"
    summary_and_exit
  fi
  image_built=true
  pass "the runner image to check is available ($image)"
elif [ ! -f "$dockerfile" ]; then
  fail "the runner image builds from tools/codesign/Dockerfile" \
    "tools/codesign/Dockerfile does not exist, so there is no runner image" \
    "the signing runner on the machine is this image; without it nothing starts on boot" \
    "create tools/codesign/Dockerfile for the runner image"
else
  image="lighthouse-codesign-runner:oracle-$$"
  if docker build --platform linux/amd64 -t "$image" -f "$dockerfile" "$codesign_dir" > "$work_dir/build.log" 2>&1; then
    image_built=true
    pass "the runner image builds from tools/codesign/Dockerfile"
  else
    fail "the runner image builds from tools/codesign/Dockerfile" \
      "docker build failed; the last lines of its output follow" \
      "the workflow publishes nothing when the image does not build, so the machine keeps an old image" \
      "fix the Dockerfile until docker build succeeds"
    tail -n 20 "$work_dir/build.log"
  fi
fi

no_image() {
  fail "$1" "there is no runner image to start, because the build above failed or the Dockerfile is missing" \
    "this check observes the running image" "make the image build first"
}

new_volume() {
  local name="$prefix-$1"
  docker volume create "$name" >/dev/null
  created_volumes+=("$name")
  printf '%s' "$name"
}

# Writes the registration files a registered runner keeps into a state volume, as root, so a wrongly
# owned state directory shows in the ownership check rather than here. --network none keeps the real
# runner from reaching GitHub with these made-up credentials.
fill_registered_volume() {
  docker run --rm --network none --user 0 --entrypoint /bin/sh -v "$1:/home/runner/state" "$image" -c '
    printf "%s" "{\"agentId\":1,\"agentName\":\"codesign-runner-linux\",\"poolId\":1,\"poolName\":\"Default\",\"serverUrl\":\"https://pipelines.actions.githubusercontent.com/oracle\",\"gitHubUrl\":\"https://github.com/LetPeopleWork/Lighthouse\",\"workFolder\":\"_work\",\"disableUpdate\":true}" > /home/runner/state/.runner
    printf "%s" "{\"scheme\":\"OAuth\",\"data\":{\"clientId\":\"11111111-1111-1111-1111-111111111111\",\"authorizationUrl\":\"https://pipelines.actions.githubusercontent.com/oracle/_apis/oauthv2/token\",\"requireFipsCryptography\":\"False\"}}" > /home/runner/state/.credentials
    printf "%s" "{}" > /home/runner/state/.credentials_rsaparams'
}

volume_listing() {
  docker run --rm --network none --user 0 --entrypoint /bin/sh -v "$1:/s:ro" "$image" -c '
    for f in /s/.[!.]* /s/*; do
      if [ -e "$f" ]; then
        if [ -d "$f" ]; then echo "dir=${f#/s/}"; elif [ -x "$f" ]; then echo "exec=${f#/s/}"; else echo "file=${f#/s/}"; fi
      fi
    done'
}

# Starts the image through its real entrypoint against a state volume with the stand-ins in place.
# Sets RUN_EXIT (exit code or timeout), RUN_SECONDS, RUN_OUTPUT and RECORD_DIR.
start_entrypoint() {
  local label=$1 volume=$2 mode=$3
  shift 3
  local name="$prefix-$label"
  RECORD_DIR="$work_dir/record-$label"
  mkdir -p "$RECORD_DIR"
  chmod 777 "$RECORD_DIR"
  created_containers+=("$name")
  local started=$SECONDS
  if ! docker run -d --name "$name" --network none \
    -v "$volume:/home/runner/state" \
    -v "$RECORD_DIR:/record" \
    -v "$stubs_dir/run.sh:/opt/runner/run.sh:ro" \
    -v "$stubs_dir/config.sh:/opt/runner/config.sh:ro" \
    -v "$path_stubs_dir:/stubs:ro" \
    -e PATH=/stubs:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    "$@" "$image" "$mode" >/dev/null 2>"$work_dir/start-$label.err"; then
    RUN_EXIT="did not start: $(cat "$work_dir/start-$label.err")"
  elif RUN_EXIT="$(timeout "$start_limit_seconds" docker wait "$name")"; then :; else RUN_EXIT=timeout; fi
  RUN_SECONDS=$((SECONDS - started))
  RUN_OUTPUT="$(docker logs "$name" 2>&1 || true)"
  docker rm -f "$name" >/dev/null 2>&1 || true
}

recorded() { [ -f "$RECORD_DIR/$1" ]; }

record_of() {
  if [ -f "$RECORD_DIR/$1" ]; then cat "$RECORD_DIR/$1"; fi
}

# ---------------------------------------------------------------- the image itself

base_check="every build stage starts from ubuntu:26.04 pinned by digest"
if [ ! -f "$dockerfile" ]; then
  fail "$base_check" "tools/codesign/Dockerfile does not exist" \
    "Renovate can only bump a base digest that is written down, and an unpinned tag can change under a rebuild" \
    "write every FROM as ubuntu:26.04@sha256:<digest>"
else
  from_total=0
  from_unpinned=""
  pinned_from='^[[:space:]]*FROM[[:space:]]+(--platform=[^[:space:]]+[[:space:]]+)?ubuntu:26\.04@sha256:[0-9a-f]{64}([[:space:]]+[Aa][Ss][[:space:]]+[^[:space:]]+)?[[:space:]]*$'
  while IFS= read -r line; do
    if [[ $line =~ ^[[:space:]]*FROM[[:space:]] ]]; then
      from_total=$((from_total + 1))
      if [[ ! $line =~ $pinned_from ]]; then from_unpinned+="$line; "; fi
    fi
  done < "$dockerfile"
  if [ "$from_total" -gt 0 ] && [ -z "$from_unpinned" ]; then
    pass "$base_check ($from_total of $from_total FROM lines)"
  else
    fail "$base_check" "of $from_total FROM lines, these are not ubuntu:26.04@sha256:<digest>: ${from_unpinned:-none found}" \
      "a tag-only base can change between two builds of the same commit, and Renovate bumps only a written digest" \
      "write every FROM as ubuntu:26.04@sha256:<64 hex digits>"
  fi
fi

expected_runner_version=""
if [ -f "$dockerfile" ]; then expected_runner_version="$(runner_version_in_dockerfile)"; fi

if [ "$image_built" != true ]; then
  no_image "the image links its GHCR package to the Lighthouse repository"
  no_image "the image carries the GitHub runner at the version the Dockerfile pins"
  no_image "the state and run directories belong to the runner user"
  no_image "the image runs as the runner user with uid 1000"
else
  label="$(docker image inspect -f '{{index .Config.Labels "org.opencontainers.image.source"}}' "$image" 2>/dev/null || true)"
  if [ "$label" = "https://github.com/LetPeopleWork/Lighthouse" ]; then
    pass "the image links its GHCR package to the Lighthouse repository"
  else
    fail "the image links its GHCR package to the Lighthouse repository" \
      "label org.opencontainers.image.source is '${label}', expected https://github.com/LetPeopleWork/Lighthouse" \
      "GHCR attaches the package to the repository through this label; without it the workflow token cannot manage it" \
      "add LABEL org.opencontainers.image.source=https://github.com/LetPeopleWork/Lighthouse to the Dockerfile"
  fi

  pristine="$(docker run --rm --network none --entrypoint /bin/sh "$image" -c '
    for f in config.sh run.sh; do if [ -f "/opt/runner/$f" ]; then echo "present=$f"; fi; done
    out="$(/opt/runner/bin/Runner.Listener --version 2>&1)" || true
    echo "version=$(printf "%s\n" "$out" | tail -n 1)"' 2>&1 || true)"
  pristine_version="$(value_of version "$pristine")"
  if contains_line "present=config.sh" "$pristine" && contains_line "present=run.sh" "$pristine" \
     && [ -n "$expected_runner_version" ] && [ "$pristine_version" = "$expected_runner_version" ]; then
    pass "the image carries the GitHub runner at the version the Dockerfile pins ($expected_runner_version)"
  else
    fail "the image carries the GitHub runner at the version the Dockerfile pins" \
      "/opt/runner holds [$(joined_values_of present "$pristine")] and reports version '$pristine_version'; the Dockerfile pins ARG RUNNER_VERSION='$expected_runner_version'" \
      "every start copies the runner from /opt/runner, so this is the version GitHub sees" \
      "unpack actions/runner at ARG RUNNER_VERSION into /opt/runner, with config.sh and run.sh"
  fi

  owners="$(docker run --rm --network none --entrypoint /bin/sh "$image" -c '
    for d in /home/runner/state /home/runner/runtime; do
      if [ -d "$d" ]; then echo "owner=$d:$(stat -c %U "$d")"; else echo "owner=$d:missing"; fi
    done
    echo "uid=$(id -u)"; echo "name=$(id -un)"
    if getent passwd ubuntu >/dev/null; then echo "ubuntu_user=present"; else echo "ubuntu_user=absent"; fi' 2>&1 || true)"
  if contains_line "owner=/home/runner/state:runner" "$owners" && contains_line "owner=/home/runner/runtime:runner" "$owners"; then
    pass "the state and run directories belong to the runner user"
  else
    fail "the state and run directories belong to the runner user" \
      "found [$(joined_values_of owner "$owners")], expected both owned by runner" \
      "a fresh named volume takes the owner of /home/runner/state; a root-owned one cannot receive the registration" \
      "create /home/runner/state and /home/runner/runtime in the Dockerfile and chown them to runner"
  fi

  config_user="$(docker image inspect -f '{{.Config.User}}' "$image" 2>/dev/null || true)"
  if [ "$config_user" = "runner" ] && contains_line "uid=1000" "$owners" && contains_line "name=runner" "$owners" \
     && contains_line "ubuntu_user=absent" "$owners"; then
    pass "the image runs as the runner user with uid 1000"
  else
    fail "the image runs as the runner user with uid 1000" \
      "default user is '$config_user', running as uid '$(value_of uid "$owners")' named '$(value_of name "$owners")', user ubuntu is $(value_of ubuntu_user "$owners")" \
      "jobs must not run as root, uid 1000 owns the token file on the machine, and a leftover ubuntu account would be a second name for that uid" \
      "rename the base image user ubuntu (uid 1000) to runner and end the Dockerfile with USER runner"
  fi
fi

# ---------------------------------------------------------------- the entrypoint

if [ "$image_built" != true ]; then
  no_image "a runner that was never registered refuses to start and says how to register"
  no_image "a registered runner starts the runner and does not register again"
  no_image "the runner runs from a writable copy of the image runner, not from the volume"
  no_image "the real runner starts with the registration in place"
  no_image "registration keeps the GitHub token off the command line and out of the output"
  no_image "registration configures the runner with the codesign labels and keeps the result in the volume"
  no_image "registration without a token file refuses and names where the token goes"
else
  # A runner that was never registered.
  volume="$(new_volume empty)"
  start_entrypoint empty "$volume" run
  if [ "$RUN_EXIT" = "1" ] && has_text "$register_command" "$RUN_OUTPUT" && ! recorded run-sh && ! recorded config-sh; then
    pass "a runner that was never registered refuses to start and says how to register (${RUN_SECONDS}s)"
  else
    fail "a runner that was never registered refuses to start and says how to register" \
      "exit '$RUN_EXIT' after ${RUN_SECONDS}s (limit ${start_limit_seconds}s), run.sh started: $(yes_no recorded run-sh), config.sh started: $(yes_no recorded config-sh), output names the register command: $(yes_no has_text "$register_command" "$RUN_OUTPUT")" \
      "an unregistered runner that starts anyway loops offline, and one that registers by itself would need the token in the always-on container" \
      "in run mode without .runner, .credentials and .credentials_rsaparams, exit 1 naming '$register_command'"
  fi

  # A registered runner.
  volume="$(new_volume registered)"
  fill_registered_volume "$volume"
  start_entrypoint registered "$volume" run
  listing="$(volume_listing "$volume")"
  binaries_in_volume="$(joined_values_of dir "$listing") $(joined_values_of exec "$listing")"
  binaries_in_volume="${binaries_in_volume# }"
  binaries_in_volume="${binaries_in_volume% }"
  if [ "$RUN_EXIT" = "0" ] && recorded run-sh && ! recorded config-sh && [ -z "$binaries_in_volume" ]; then
    pass "a registered runner starts the runner and does not register again"
  else
    fail "a registered runner starts the runner and does not register again" \
      "exit '$RUN_EXIT', run.sh started: $(yes_no recorded run-sh), config.sh started: $(yes_no recorded config-sh), directories or executables in the state volume: [$binaries_in_volume]" \
      "after a reboot the runner must come online from its saved registration, and binaries in the volume would outlive an image update" \
      "in run mode with the three registration files, exec the copied run.sh and never config.sh; keep only those files in the volume"
  fi

  # A volume an older image used, holding its own run.sh.
  volume="$(new_volume older)"
  fill_registered_volume "$volume"
  docker run --rm --network none --user 0 --entrypoint /bin/sh -v "$volume:/home/runner/state" -v "$work_dir/stale-run.sh:/stale-run.sh:ro" "$image" \
    -c 'cp /stale-run.sh /home/runner/state/run.sh && chmod 755 /home/runner/state/run.sh'
  start_entrypoint older "$volume" run
  run_record="$(record_of run-sh)"
  if [ "$(value_of realdir "$run_record")" = "/home/runner/runtime" ] && contains_line "symlink=no" "$run_record" \
     && contains_line "writable=yes" "$run_record" && contains_line "beside=.runner" "$run_record" \
     && contains_line "beside=.credentials" "$run_record" && contains_line "beside=.credentials_rsaparams" "$run_record" \
     && [ -n "$expected_runner_version" ] && [ "$(value_of version "$run_record")" = "$expected_runner_version" ] \
     && ! recorded stale-run-sh; then
    pass "the runner runs from a writable copy of the image runner, not from the volume"
  else
    fail "the runner runs from a writable copy of the image runner, not from the volume" \
      "run.sh ran: $(yes_no recorded run-sh), from '$(value_of realdir "$run_record")', symlink: '$(value_of symlink "$run_record")', writable: '$(value_of writable "$run_record")', beside it: [$(joined_values_of beside "$run_record")], version '$(value_of version "$run_record")' (image pins '$expected_runner_version'), the volume's old run.sh ran: $(yes_no recorded stale-run-sh)" \
      "run.sh writes next to its real location; a symlinked or read-only run directory fails with Permission denied or Must configure, and an old copy would run an old runner version" \
      "on every start copy /opt/runner into /home/runner/runtime (a real copy), put the three registration files beside it and exec /home/runner/runtime/run.sh"
  fi

  # The real runner, without stand-ins.
  volume="$(new_volume real)"
  fill_registered_volume "$volume"
  real_name="$prefix-real"
  created_containers+=("$real_name")
  docker run -d --name "$real_name" --network none -v "$volume:/home/runner/state" "$image" run >/dev/null 2>&1 || true
  real_output=""
  while IFS= read -r line; do
    real_output+="$line"$'\n'
    case "$line" in
      *"Must configure"*|*"Permission denied"*|*"re-launch"*|*"An error occurred"*|*"Exiting"*|*"Connected to GitHub"*|*"Listening for Jobs"*)
        docker rm -f "$real_name" >/dev/null 2>&1 || true
        break ;;
    esac
  done < <(timeout "$start_limit_seconds" docker logs -f "$real_name" 2>&1 || true)
  docker rm -f "$real_name" >/dev/null 2>&1 || true
  if [ -n "$real_output" ] && ! has_text "Must configure" "$real_output" && ! has_text "Permission denied" "$real_output"; then
    pass "the real runner starts with the registration in place"
  else
    fail "the real runner starts with the registration in place" \
      "the runner printed: ${real_output:-nothing within ${start_limit_seconds}s}" \
      "Must configure or Permission denied means run.sh could not use the registration, so the runner never comes online after a reboot" \
      "make the entrypoint exec a writable copy of run.sh with the three registration files beside it"
  fi

  # Registration with a token file.
  pat="oracle-pat-$$-$RANDOM$RANDOM"
  printf '%s\n' "$pat" > "$work_dir/github-token"
  chmod 644 "$work_dir/github-token"
  volume="$(new_volume register)"
  start_entrypoint register "$volume" register -v "$work_dir/github-token:$token_secret_path:ro"
  curl_argv="$(record_of curl-argv)"
  curl_inputs="$(record_of curl-inputs)"
  config_record="$(record_of config-sh)"
  config_args="$(values_of arg "$config_record")"
  if recorded curl-argv && ! has_text "$pat" "$curl_argv" && has_text "$pat" "$curl_inputs" \
     && ! has_text "$pat" "$RUN_OUTPUT" && ! has_text "$pat" "$config_args"; then
    pass "registration keeps the GitHub token off the command line and out of the output"
  else
    fail "registration keeps the GitHub token off the command line and out of the output" \
      "curl asked: $(yes_no recorded curl-argv), token on curl argv: $(yes_no has_text "$pat" "$curl_argv"), token given to curl through a file or stdin: $(yes_no has_text "$pat" "$curl_inputs"), token in the output: $(yes_no has_text "$pat" "$RUN_OUTPUT"), token on config.sh argv: $(yes_no has_text "$pat" "$config_args")" \
      "argv is visible to every process in the container and logs end up in GitHub; the token can create runners for the repository" \
      "read $token_secret_path and pass it to curl only as a header read from a file or stdin (curl -H @FILE or --config); never echo it"
  fi
  listing="$(volume_listing "$volume")"
  if [ "$(value_of realdir "$config_record")" = "/home/runner/runtime" ] \
     && { contains_line "linux,codesign" "$config_args" || contains_line "--labels=linux,codesign" "$config_args"; } \
     && contains_line "--disableupdate" "$config_args" \
     && contains_line "file=.runner" "$listing" && contains_line "file=.credentials" "$listing" && contains_line "file=.credentials_rsaparams" "$listing"; then
    pass "registration configures the runner with the codesign labels and keeps the result in the volume"
  else
    fail "registration configures the runner with the codesign labels and keeps the result in the volume" \
      "exit '$RUN_EXIT', config.sh ran from '$(value_of realdir "$config_record")' with [${config_args//$'\n'/ }]; the state volume holds [${listing//$'\n'/ }]" \
      "the release job selects the runner by the label codesign, self-update is lost on every recreate, and only the volume survives a reboot" \
      "run /home/runner/runtime/config.sh --labels linux,codesign --disableupdate and copy .runner, .credentials and .credentials_rsaparams back to /home/runner/state"
  fi

  # Registration without a token file.
  volume="$(new_volume no-token)"
  start_entrypoint no-token "$volume" register
  if [ "$RUN_EXIT" = "1" ] && has_text "$token_secret_path" "$RUN_OUTPUT" && ! recorded config-sh; then
    pass "registration without a token file refuses and names where the token goes"
  else
    fail "registration without a token file refuses and names where the token goes" \
      "exit '$RUN_EXIT', output names $token_secret_path: $(yes_no has_text "$token_secret_path" "$RUN_OUTPUT"), config.sh ran: $(yes_no recorded config-sh)" \
      "the maintainer must learn which file to create rather than see a half-registered runner" \
      "in register mode without $token_secret_path, exit 1 naming it and the host file to create"
  fi
fi

# ---------------------------------------------------------------- compose and the boot unit

# HOME is set explicitly so the token file the compose file points at is present or absent by the
# test's choice, and an empty env file keeps a local rollback tag from changing the rendered image.
compose_render() {
  local home=$1 out=$2
  shift 2
  : > "$work_dir/empty.env"
  env -u CODESIGN_RUNNER_TAG HOME="$home" docker compose -f "$compose_file" --env-file "$work_dir/empty.env" "$@" config --format json > "$out" 2> "$out.err"
}

runner_facts_expression='"present=" + ((.services // {}) | has("runner") | tostring),
  "restart=" + (.services.runner.restart // ""),
  "init=" + ((.services.runner.init // false) | tostring),
  "privileged=" + ((.services.runner.privileged // false) | tostring),
  "devices=" + ((.services.runner.devices // []) | length | tostring),
  "image=" + (.services.runner.image // ""),
  "secrets=" + ((.services.runner.secrets // []) | length | tostring),
  "cgroup=" + ((.services.runner.device_cgroup_rules // [])[]),
  "bind=" + ((.services.runner.volumes // [])[] | select(.type == "bind") | .source),
  "volume=" + ((.services.runner.volumes // [])[] | select(.type == "volume") | .target)'

register_facts_expression='"present=" + ((.services // {}) | has("register") | tostring),
  "profile=" + ((.services.register.profiles // [])[]),
  "secret=" + ((.services.register.secrets // [])[] | .source),
  "secretfile=" + (.secrets.github_token.file // "")'

token_home="$work_dir/home-with-token"
mkdir -p "$token_home/.config/lighthouse-codesign"
printf 'oracle-placeholder\n' > "$token_home/.config/lighthouse-codesign/github-token"
missing_home="$work_dir/home-without-token"
mkdir -p "$missing_home"

compose_checks=(
  "the always-on runner service restarts, reaches the USB key and holds no token"
  "the one-shot register service alone gets the token, from a file outside the repository"
  "the runner service still renders when the token file is missing"
)
if [ ! -f "$compose_file" ]; then
  for name in "${compose_checks[@]}"; do
    fail "$name" "tools/codesign/compose.yaml does not exist" \
      "the machine starts the runner from this compose file on boot" "create tools/codesign/compose.yaml"
  done
else
  if compose_render "$token_home" "$work_dir/default.json"; then
    runner_facts="$(yq_facts "$work_dir/default.json" "$runner_facts_expression" 2>&1 || true)"
    if [ "$(value_of restart "$runner_facts")" = "unless-stopped" ] && [ "$(value_of init "$runner_facts")" = "true" ] \
       && contains_line "bind=/dev/bus/usb" "$runner_facts" && contains_line "cgroup=c 189:* rmw" "$runner_facts" \
       && [ "$(value_of privileged "$runner_facts")" = "false" ] && [ "$(value_of devices "$runner_facts")" = "0" ] \
       && contains_line "volume=/home/runner/state" "$runner_facts" && [ "$(value_of image "$runner_facts")" = "$published_image" ] \
       && [ "$(value_of secrets "$runner_facts")" = "0" ]; then
      pass "${compose_checks[0]}"
    else
      fail "${compose_checks[0]}" \
        "rendered runner service: ${runner_facts//$'\n'/; }" \
        "expected restart unless-stopped, init true, a bind of /dev/bus/usb, device_cgroup_rules c 189:* rmw, not privileged, no devices entry, the state volume at /home/runner/state, image $published_image and no secrets: any job on this runner could read a mounted token, and a fixed device path breaks when the key is replugged" \
        "set these fields on service runner in tools/codesign/compose.yaml"
    fi
  else
    fail "${compose_checks[0]}" \
      "docker compose config failed: $(cat "$work_dir/default.json.err")" "the machine cannot start what compose cannot render" "fix tools/codesign/compose.yaml"
  fi

  if compose_render "$token_home" "$work_dir/register.json" --profile register; then
    register_facts="$(yq_facts "$work_dir/register.json" "$register_facts_expression" 2>&1 || true)"
    secret_file="$(value_of secretfile "$register_facts")"
    if contains_line "profile=register" "$register_facts" && contains_line "secret=github_token" "$register_facts" \
       && [[ $secret_file == /* ]] && [[ $secret_file != "$repo_root"/* ]]; then
      pass "${compose_checks[1]}"
    else
      fail "${compose_checks[1]}" \
        "rendered register service: ${register_facts//$'\n'/; }" \
        "the token must only reach the one-shot registration and never sit in the repository checkout" \
        "give service register profiles [register] and secret github_token whose file is under the maintainer's home, outside the repository"
    fi
  else
    fail "${compose_checks[1]}" \
      "docker compose --profile register config failed: $(cat "$work_dir/register.json.err")" "registration cannot run" "fix tools/codesign/compose.yaml"
  fi

  missing_facts=""
  if compose_render "$missing_home" "$work_dir/missing.json"; then
    missing_facts="$(yq_facts "$work_dir/missing.json" "$runner_facts_expression" 2>&1 || true)"
  fi
  if [ "$(value_of present "$missing_facts")" = "true" ]; then
    pass "${compose_checks[2]}"
  else
    fail "${compose_checks[2]}" \
      "docker compose config without the token file did not render service runner: $(cat "$work_dir/missing.json.err" 2>/dev/null)" \
      "the token file is deleted after registration is done; the boot start must not need it" \
      "keep the secret on service register only, so the runner service renders without the file"
  fi
fi

unit_check="the boot unit pulls the latest image without blocking on a failed pull, then starts the runner"
if [ ! -f "$unit_file" ]; then
  fail "$unit_check" "tools/codesign/lighthouse-codesign-runner.service does not exist" \
    "nothing starts the runner after a reboot" "create the systemd unit"
else
  unit_pull=no
  unit_up=no
  unit_after=no
  unit_wanted=no
  pull_line='^ExecStartPre=-(/usr/bin/)?docker[[:space:]]+compose([[:space:]].*)?[[:space:]]pull([[:space:]]|$)'
  up_line='^ExecStart=(/usr/bin/)?docker[[:space:]]+compose([[:space:]].*)?[[:space:]]up[[:space:]]+-d[[:space:]]+runner[[:space:]]*$'
  after_line='^After=(.*[[:space:]])?docker\.service([[:space:]]|$)'
  wanted_line='^WantedBy=(.*[[:space:]])?multi-user\.target([[:space:]]|$)'
  while IFS= read -r line; do
    if [[ $line =~ $pull_line ]]; then unit_pull=yes; fi
    if [[ $line =~ $up_line ]]; then unit_up=yes; fi
    if [[ $line =~ $after_line ]]; then unit_after=yes; fi
    if [[ $line =~ $wanted_line ]]; then unit_wanted=yes; fi
  done < "$unit_file"
  if [ "$unit_pull$unit_up$unit_after$unit_wanted" = "yesyesyesyes" ]; then
    pass "$unit_check"
  else
    fail "$unit_check" \
      "pull with a leading minus: $unit_pull, up -d runner: $unit_up, After=docker.service: $unit_after, WantedBy=multi-user.target: $unit_wanted" \
      "a pull without the leading minus stops the start when GHCR is unreachable, and without the install target nothing runs after a reboot" \
      "use ExecStartPre=-docker compose pull, ExecStart=docker compose up -d runner, After=docker.service and WantedBy=multi-user.target"
  fi
fi

# ---------------------------------------------------------------- the image workflow and Renovate

workflow_checks=(
  "the image workflow reports on every pull request as Codesign Runner Image / verify"
  "the image is rebuilt on changes under tools/codesign on main and on a schedule"
  "the change detection hands verify the string true or false"
  "verify runs when codesign changed and turns red when change detection failed"
  "verify builds and checks the image without publishing it"
  "only a non-pull-request run on main whose verify passed publishes latest and the commit tag"
  "the image workflow stands apart from the main CI workflows"
)
workflow_facts_expression='"name=" + (.name // ""),
  "pr=" + ((.on // {}) | has("pull_request") | tostring),
  "pr_filtered=" + ((.on.pull_request // {}) | has("paths") | tostring) + ((.on.pull_request // {}) | has("paths-ignore") | tostring),
  "schedule=" + ((.on // {}) | has("schedule") | tostring),
  "push_branch=" + ((.on.push.branches // [])[]),
  "push_path=" + ((.on.push.paths // [])[]),
  "verify_present=" + ((.jobs // {}) | has("verify") | tostring),
  "verify_name=" + (.jobs.verify.name // "verify"),
  "verify_needs=" + ([.jobs.verify.needs // []] | flatten | .[]),
  "verify_if_cancelled=" + ((.jobs.verify["if"] // "") | test("!\\s*cancelled\\(\\)") | tostring),
  "verify_if_result=" + ((.jobs.verify["if"] // "") | test("needs\\.changes\\.result") | tostring),
  "verify_if_string=" + ((.jobs.verify["if"] // "") | test("needs\\.changes\\.outputs\\.codesign\\s*==\\s*\x27true\x27|\x27true\x27\\s*==\\s*needs\\.changes\\.outputs\\.codesign") | tostring),
  "verify_if_boolean=" + ((.jobs.verify["if"] // "") | test("needs\\.changes\\.outputs\\.codesign\\s*==\\s*true|true\\s*==\\s*needs\\.changes\\.outputs\\.codesign") | tostring),
  "verify_first_step_gate=" + (((.jobs.verify.steps // [])[0] // {}) | to_json(0) | test("needs\\.changes\\.result") | tostring) + (((.jobs.verify.steps // [])[0] // {}) | to_json(0) | test("exit 1") | tostring),
  "verify_runs_oracle=" + ((.jobs.verify // {}) | to_json(0) | test("tools/codesign/tests/test-runner-image\\.sh") | tostring),
  "verify_pushes=" + ((.jobs.verify // {}) | to_json(0) | test("\"push\":\\s*\"?true|docker/login-action|docker push") | tostring),
  "changes_output=" + (.jobs.changes.outputs.codesign // ""),
  "changes_writes=" + ((.jobs.changes.steps // []) | to_json(0) | test("codesign=") | tostring) + ((.jobs.changes.steps // []) | to_json(0) | test("true") | tostring) + ((.jobs.changes.steps // []) | to_json(0) | test("false") | tostring),
  "publish_needs=" + ([.jobs.publish.needs // []] | flatten | .[]),
  "publish_if_main=" + ((.jobs.publish["if"] // "") | test("github\\.ref\\s*==\\s*\x27refs/heads/main\x27") | tostring),
  "publish_if_event=" + ((.jobs.publish["if"] // "") | test("github\\.event_name\\s*!=\\s*\x27pull_request\x27") | tostring),
  "publish_if_verify=" + ((.jobs.publish["if"] // "") | test("needs\\.verify\\.result\\s*==\\s*\x27success\x27") | tostring),
  "publish_tags=" + ((.jobs.publish // {}) | to_json(0) | test("ghcr\\.io/letpeoplework/lighthouse-codesign-runner") | tostring) + ((.jobs.publish // {}) | to_json(0) | test("latest") | tostring) + ((.jobs.publish // {}) | to_json(0) | test("sha-") | tostring)'

if [ ! -f "$workflow_file" ]; then
  for name in "${workflow_checks[@]}"; do
    fail "$name" ".github/workflows/codesign-runner-image.yml does not exist" \
      "nothing builds or publishes the runner image the machine pulls" "create the workflow"
  done
else
  workflow_facts="$(yq_facts "$workflow_file" "$workflow_facts_expression" 2>&1 || true)"
  wf() { value_of "$1" "$workflow_facts"; }

  if [ "$(wf name)" = "Codesign Runner Image" ] && [ "$(wf pr)" = "true" ] && [ "$(wf pr_filtered)" = "falsefalse" ] \
     && [ "$(wf verify_present)" = "true" ] && [ "$(wf verify_name)" = "verify" ]; then
    pass "${workflow_checks[0]}"
  else
    fail "${workflow_checks[0]}" \
      "workflow name '$(wf name)', pull_request trigger: $(wf pr), pull_request paths filter: $(wf pr_filtered), job verify: $(wf verify_present) named '$(wf verify_name)'" \
      "a required check that does not report on a pull request blocks the merge forever, so the trigger has no paths filter" \
      "name the workflow Codesign Runner Image, trigger on pull_request without paths, and keep a job verify"
  fi

  if contains_line "push_branch=main" "$workflow_facts" && contains_line "push_path=tools/codesign/**" "$workflow_facts" \
     && [ "$(wf schedule)" = "true" ]; then
    pass "${workflow_checks[1]}"
  else
    fail "${workflow_checks[1]}" \
      "push branches [$(joined_values_of push_branch "$workflow_facts")], push paths [$(joined_values_of push_path "$workflow_facts")], schedule: $(wf schedule)" \
      "a change to the image files and Ubuntu security fixes reach the machine only through a rebuild" \
      "trigger on push to main for tools/codesign/** and on a weekly schedule"
  fi

  if [ -n "$(wf changes_output)" ] && [ "$(wf changes_writes)" = "truetruetrue" ]; then
    pass "${workflow_checks[2]}"
  else
    fail "${workflow_checks[2]}" \
      "job changes output codesign '$(wf changes_output)', its steps write codesign= with true and false: $(wf changes_writes)" \
      "job outputs are strings; verify compares against the string true" \
      "give job changes an output codesign written as codesign=true or codesign=false to GITHUB_OUTPUT"
  fi

  if contains_line "verify_needs=changes" "$workflow_facts" && [ "$(wf verify_if_cancelled)" = "true" ] \
     && [ "$(wf verify_if_result)" = "true" ] && [ "$(wf verify_if_string)" = "true" ] \
     && [ "$(wf verify_if_boolean)" = "false" ] && [ "$(wf verify_first_step_gate)" = "truetrue" ]; then
    pass "${workflow_checks[3]}"
  else
    fail "${workflow_checks[3]}" \
      "needs changes: $(yes_no contains_line "verify_needs=changes" "$workflow_facts"), if has !cancelled(): $(wf verify_if_cancelled), a needs.changes.result clause: $(wf verify_if_result), compares codesign with the string 'true': $(wf verify_if_string), compares with the boolean true: $(wf verify_if_boolean), first step exits 1 on a failed change detection: $(wf verify_first_step_gate)" \
      "a boolean comparison is always false and skips the required check forever; a failed change detection must turn it red, not skipped" \
      "use if: !cancelled() && (needs.changes.result != 'success' || needs.changes.outputs.codesign == 'true') and a first step that exits 1 when needs.changes.result is not success"
  fi

  if [ "$(wf verify_runs_oracle)" = "true" ] && [ "$(wf verify_pushes)" = "false" ]; then
    pass "${workflow_checks[4]}"
  else
    fail "${workflow_checks[4]}" \
      "verify runs tools/codesign/tests/test-runner-image.sh: $(wf verify_runs_oracle), verify pushes or logs in: $(wf verify_pushes)" \
      "an image that fails these checks must never reach GHCR, where the machine pulls it" \
      "in verify, build the image without push or login and run the slice oracles"
  fi

  if contains_line "publish_needs=verify" "$workflow_facts" && [ "$(wf publish_if_main)" = "true" ] \
     && [ "$(wf publish_if_event)" = "true" ] && [ "$(wf publish_if_verify)" = "true" ] && [ "$(wf publish_tags)" = "truetruetrue" ]; then
    pass "${workflow_checks[5]}"
  else
    fail "${workflow_checks[5]}" \
      "publish needs verify: $(yes_no contains_line "publish_needs=verify" "$workflow_facts"), if requires refs/heads/main: $(wf publish_if_main), a non-pull_request event: $(wf publish_if_event), verify success: $(wf publish_if_verify); pushes latest and sha- tags of the GHCR image: $(wf publish_tags)" \
      "the machine pulls latest; a branch or pull-request run, or an unverified image, must never overwrite it" \
      "give job publish needs verify and if: github.ref == 'refs/heads/main' && github.event_name != 'pull_request' && needs.verify.result == 'success'; push latest and sha-COMMIT"
  fi

  main_ci_reference=no
  main_ci_name='(^|[^_[:alnum:]-])ci\.yml|ci_changes\.yml'
  while IFS= read -r line; do
    if [[ $line =~ $main_ci_name ]]; then main_ci_reference=yes; fi
  done < "$workflow_file"
  if [ "$main_ci_reference" = "no" ]; then
    pass "${workflow_checks[6]}"
  else
    fail "${workflow_checks[6]}" "the workflow names ci.yml or ci_changes.yml" \
      "the image pipeline must not depend on or change the main CI" "remove the reference; detect changes in this workflow's own job"
  fi
fi

renovate_check="Renovate reads the pinned runner version from the Dockerfile"
if [ "$image_built" != true ] || [ ! -f "$dockerfile" ]; then
  fail "$renovate_check" "there is no Dockerfile or no image with node to evaluate the Renovate pattern in" \
    "the runner version must be bumped by Renovate, not by hand" "create the Dockerfile and make the image build"
else
  # Renovate patterns are JavaScript regular expressions, so they are evaluated with node from the image.
  cat > "$work_dir/renovate-check.js" <<'JS'
const fs = require("fs");
const config = JSON.parse(fs.readFileSync("/check/renovate.json", "utf8"));
const dockerfile = fs.readFileSync("/check/Dockerfile", "utf8");
const target = "tools/codesign/Dockerfile";
const toRegex = (p) => (p.startsWith("/") && p.lastIndexOf("/") > 0 ? new RegExp(p.slice(1, p.lastIndexOf("/"))) : null);
const found = [];
for (const m of config.customManagers || []) {
  const patterns = [...(m.managerFilePatterns || []), ...(m.fileMatch || []).map((f) => `/${f}/`)];
  if (!patterns.some((p) => { const r = toRegex(p); return r ? r.test(target) : p === target; })) continue;
  for (const s of m.matchStrings || []) {
    for (const hit of dockerfile.matchAll(new RegExp(s, "g"))) {
      const g = hit.groups || {};
      found.push(`${g.datasource || m.datasourceTemplate || ""} ${g.depName || m.depNameTemplate || ""} ${g.currentValue || ""}`);
    }
  }
}
console.log(found.join("\n"));
JS
  chmod 644 "$work_dir/renovate-check.js"
  renovate_deps="$(docker run --rm --network none --entrypoint node \
    -v "$renovate_file:/check/renovate.json:ro" -v "$dockerfile:/check/Dockerfile:ro" -v "$work_dir/renovate-check.js:/check/check.js:ro" \
    "$image" /check/check.js 2>&1 || true)"
  if [ -n "$expected_runner_version" ] && contains_line "github-releases actions/runner $expected_runner_version" "$renovate_deps"; then
    pass "$renovate_check"
  else
    fail "$renovate_check" \
      "Renovate custom managers for tools/codesign/Dockerfile extract [${renovate_deps//$'\n'/; }], expected 'github-releases actions/runner $expected_runner_version'" \
      "a runner older than GitHub accepts goes offline; Renovate keeps it current only if a manager reads the ARG" \
      "add a regex custom manager in renovate.json for '# renovate: datasource=github-releases depName=actions/runner' followed by ARG RUNNER_VERSION"
  fi
fi

summary_and_exit
