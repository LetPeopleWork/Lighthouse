#!/usr/bin/env bash
# Starts the signing runner. Mode "run" (the always-on service) starts it from the registration kept in
# the state volume; mode "register" (the one-shot service) registers it once from the token file.
set -euo pipefail

pristine_dir=/opt/runner
runtime_dir=/home/runner/runtime
state_dir=/home/runner/state
token_file=/run/secrets/github_token
host_token_file='the file .config/lighthouse-codesign/github-token in your home folder on the signing machine'
register_command='docker compose --profile register run --rm register'
repository_url=https://github.com/LetPeopleWork/Lighthouse
registration_token_url=https://api.github.com/repos/LetPeopleWork/Lighthouse/actions/runners/registration-token
# Registration replaces a runner of the same name, so a second machine or a test run passes its own.
runner_name="${CODESIGN_RUNNER_NAME:-codesign-runner-linux}"
runner_labels=linux,codesign
registration_files=(.runner .credentials .credentials_rsaparams)

refuse() {
  {
    echo "ERROR: $1"
    echo "  WHY:  $2"
    echo "  HOW:  $3"
  } >&2
  exit 1
}

is_registered() {
  local file
  for file in "${registration_files[@]}"; do
    if [ ! -f "$state_dir/$file" ]; then return 1; fi
  done
  return 0
}

# run.sh and config.sh resolve their own real location and write next to it, so they need a writable
# copy, not links into the image. A fresh copy on every start means a new image runs its own runner.
prepare_runtime() {
  find "$runtime_dir" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
  cp -R "$pristine_dir/." "$runtime_dir/"
}

run_runner() {
  if ! is_registered; then
    refuse "this runner is not registered: $state_dir holds no ${registration_files[*]}" \
      "without a registration the runner cannot connect to GitHub and would stay offline" \
      "put the GitHub token in $host_token_file and run: $register_command"
  fi
  prepare_runtime
  # The registration files are the runner's credentials; only the runner user may read its copies.
  umask 077
  local file
  for file in "${registration_files[@]}"; do
    cp "$state_dir/$file" "$runtime_dir/$file"
  done
  cd "$runtime_dir"
  exec "$runtime_dir/run.sh"
}

register_runner() {
  if [ ! -s "$token_file" ] || [ ! -r "$token_file" ]; then
    refuse "no readable GitHub token at $token_file" \
      "registration asks GitHub for a runner registration token with that token" \
      "create $host_token_file (owned by the runner uid, mode 0600), then run: $register_command"
  fi
  prepare_runtime

  local work github_token response registration_token
  work="$(mktemp -d)"
  # shellcheck disable=SC2064 # expand now: the directory name is fixed once created
  trap "rm -rf '$work'" EXIT
  IFS= read -r github_token < "$token_file" || true
  github_token="${github_token%$'\r'}"
  # curl reads the token from a config file, so it never appears on a command line other processes can see.
  (umask 077 && printf 'header = "Authorization: Bearer %s"\n' "$github_token" > "$work/auth.curlrc")
  unset github_token

  if ! curl --proto "=https" -fsS -X POST --config "$work/auth.curlrc" \
      -H "Accept: application/vnd.github+json" -o "$work/response.json" "$registration_token_url"; then
    refuse "GitHub refused the registration token request" \
      "the token in $host_token_file may be expired or lack the repository administration permission" \
      "replace the token in $host_token_file and run: $register_command"
  fi
  response="$(< "$work/response.json")"
  if [[ ! $response =~ \"token\"[[:space:]]*:[[:space:]]*\"([^\"]+)\" ]]; then
    refuse "GitHub's answer carried no registration token" \
      "the runner cannot be configured without one" \
      "check the token's permissions and run: $register_command"
  fi
  registration_token="${BASH_REMATCH[1]}"

  "$runtime_dir/config.sh" --unattended --url "$repository_url" --token "$registration_token" \
    --name "$runner_name" --labels "$runner_labels" --replace --disableupdate

  umask 077
  local file
  for file in "${registration_files[@]}"; do
    cp "$runtime_dir/$file" "$state_dir/$file"
  done
  echo "Registered $runner_name with labels $runner_labels; the runner service now starts from this registration."
}

case "${1:-run}" in
  run) run_runner ;;
  register) register_runner ;;
  *)
    refuse "unknown mode '$1'" \
      "this image starts the runner (run) or registers it once (register)" \
      "start it with the command run or register, as compose.yaml does"
    ;;
esac
