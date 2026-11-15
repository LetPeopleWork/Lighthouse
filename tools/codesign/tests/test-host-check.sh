#!/usr/bin/env bash
# What the host check promises the maintainer: run on the signing machine, tools/codesign/check-host.sh
# names every leftover of the retired host setup (the host actions runner service, the osslsigncode
# wrapper in ~/.local/bin, the local osslsigncode image, host pcscd, scdaemon without disable-ccid),
# each on its own line with what to do about it, and exits 1; on a retired host it says host is clean
# and exits 0. It only reports: it never stops, disables or deletes anything.
# The check never runs against this machine: every case gets a made-up home directory and recording
# stand-ins for systemctl, docker, pgrep, gpgconf and sudo first on PATH, which answer from that
# case's own fixture. Needs only bash; Docker only to read the image workflow.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd -P)"
codesign_dir="$repo_root/tools/codesign"
check_script="$codesign_dir/check-host.sh"
retired_script="$codesign_dir/start-runner-linux.ps1"
image_workflow="$repo_root/.github/workflows/codesign-runner-image.yml"

yq_image="mikefarah/yq:4.47.1@sha256:b9285dd3b0bea3c34d0c54415dd48d767dabd9644d489bd6e253660847b58419"
case_limit_seconds=30
host_runner_unit="actions.runner.LetPeopleWork-Lighthouse.codesign-runner-linux.service"

# One pattern per leftover: a finding line names its leftover this way.
declare -A names_leftover=(
  [runner]='actions[.-]runner'
  [wrapper]='\.local/bin/osslsigncode'
  [image]='osslsigncode-ossl3'
  [pcscd]='pcscd\.(socket|service)'
  [scdaemon]='scdaemon\.conf|disable-ccid'
)
# One pattern per leftover: the same line also says what to do about it.
declare -A says_how=(
  [runner]='svc\.sh|uninstall|remove|stop|disable'
  [wrapper]='(^|[^[:alnum:]])rm([^[:alnum:]]|$)|delete|remove'
  [image]='docker (image )?rmi?([^[:alnum:]]|$)|remove|delete'
  [pcscd]='disable|mask'
  [scdaemon]='(add|append|echo|write|put|set|insert).*disable-ccid|disable-ccid.*(>>|tee)'
)
declare -A leftover_words=(
  [runner]="the host actions runner service"
  [wrapper]="the ~/.local/bin/osslsigncode wrapper"
  [image]="the local/osslsigncode-ossl3 image"
  [pcscd]="host pcscd.socket or pcscd.service"
  [scdaemon]="scdaemon.conf without disable-ccid"
)
all_leftovers=(runner wrapper image pcscd scdaemon)

observed=0
assertions_failed=0
setup_failed=0
work_dir=""

# shellcheck disable=SC2317 # called through the EXIT trap, which shellcheck cannot follow
cleanup() {
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

work_dir="$(mktemp -d)"
standins_dir="$work_dir/standins"
mkdir -p "$standins_dir"

# ------------------------------------------------------------------ stand-ins
# Each stand-in logs its argv to ../log/<tool> and answers from ../state of the fixture it was copied into.
# Anything that would change the host is logged and refused.

cat > "$standins_dir/systemctl" <<'STANDIN'
#!/usr/bin/env bash
fixture="$(cd "$(dirname "$0")/.." && pwd)"
printf '%s\n' "$*" >> "$fixture/log/systemctl"
units="$fixture/state/units"
sub="" all=0 quiet=0 legend=1 state_filter="" patterns=()
for arg in "$@"; do
  case "$arg" in
    --all|-a) all=1 ;;
    --quiet|-q) quiet=1 ;;
    --no-legend) legend=0 ;;
    --state=*) state_filter="${arg#--state=}" ;;
    -*) ;;
    *) if [ -z "$sub" ]; then sub="$arg"; else patterns+=("$arg"); fi ;;
  esac
done
full_name() { case "$1" in *.service|*.socket|*.timer|*.target|*.path|*.mount|*'*'*) printf '%s' "$1" ;; *) printf '%s.service' "$1" ;; esac; }
unit_line() { local want; want="$(full_name "$1")"; awk -v u="$want" '$1 == u' "$units"; }
matches() {
  local unit=$1 pat
  if [ "${#patterns[@]}" -eq 0 ]; then return 0; fi
  for pat in "${patterns[@]}"; do
    # shellcheck disable=SC2053 # the pattern is a glob on purpose, as systemctl reads it
    if [[ $unit == $(full_name "$pat") ]] || [[ $unit == $pat ]]; then return 0; fi
  done
  return 1
}
sub_state() { case "$1:$2" in *.socket:active) echo listening ;; *:active) echo running ;; *) echo dead ;; esac; }
case "$sub" in
  is-enabled|is-active)
    any=1
    for pat in "${patterns[@]}"; do
      line="$(unit_line "$pat")"
      if [ -z "$line" ]; then
        state=$([ "$sub" = is-enabled ] && echo not-found || echo inactive)
      elif [ "$sub" = is-enabled ]; then state="$(awk '{print $2}' <<< "$line")"
      else state="$(awk '{print $3}' <<< "$line")"; fi
      if [ "$quiet" -eq 0 ]; then echo "$state"; fi
      if [ "$state" = enabled ] || [ "$state" = active ]; then any=0; fi
    done
    exit "$any" ;;
  list-units|list-sockets)
    if [ "$legend" -eq 1 ]; then echo "UNIT LOAD ACTIVE SUB DESCRIPTION"; fi
    while read -r unit enabled active description; do
      sub_s="$(sub_state "$unit" "$active")"
      matches "$unit" || continue
      if [ -n "$state_filter" ]; then [[ ",$state_filter," == *",$active,"* || ",$state_filter," == *",$sub_s,"* ]] || continue
      elif [ "$all" -eq 0 ] && [ "$active" != active ]; then continue; fi
      if [ "$sub" = list-sockets ] && [[ $unit != *.socket ]]; then continue; fi
      echo "$unit loaded $active $sub_s $description"
    done < "$units"
    exit 0 ;;
  list-unit-files)
    if [ "$legend" -eq 1 ]; then echo "UNIT FILE STATE PRESET"; fi
    while read -r unit enabled _active _description; do
      matches "$unit" || continue
      if [ -n "$state_filter" ]; then [[ ",$state_filter," == *",$enabled,"* ]] || continue; fi
      echo "$unit $enabled disabled"
    done < "$units"
    exit 0 ;;
  status|show|cat)
    code=0
    for pat in "${patterns[@]}"; do
      line="$(unit_line "$pat")"
      if [ -z "$line" ]; then
        if [ "$sub" = show ]; then printf 'Id=%s\nLoadState=not-found\nActiveState=inactive\nSubState=dead\nUnitFileState=\n' "$(full_name "$pat")"; continue; fi
        echo "Unit $(full_name "$pat") could not be found." >&2; code=4; continue
      fi
      read -r unit enabled active description <<< "$line"
      case "$sub" in
        show) printf 'Id=%s\nLoadState=loaded\nActiveState=%s\nSubState=%s\nUnitFileState=%s\n' "$unit" "$active" "$(sub_state "$unit" "$active")" "$enabled" ;;
        cat) printf '# /etc/systemd/system/%s\n[Unit]\nDescription=%s\n' "$unit" "$description" ;;
        status)
          printf '* %s - %s\n     Loaded: loaded (/etc/systemd/system/%s; %s)\n     Active: %s (%s)\n' "$unit" "$description" "$unit" "$enabled" "$active" "$(sub_state "$unit" "$active")"
          if [ "$active" != active ] && [ "$code" -eq 0 ]; then code=3; fi ;;
      esac
    done
    exit "$code" ;;
  ""|list-timers|list-dependencies|is-failed|get-default|--version) exit 0 ;;
  *) echo "systemctl stand-in: the host check must not run systemctl $sub" >&2; exit 1 ;;
esac
STANDIN

cat > "$standins_dir/docker" <<'STANDIN'
#!/usr/bin/env bash
fixture="$(cd "$(dirname "$0")/.." && pwd)"
printf '%s\n' "$*" >> "$fixture/log/docker"
images="$fixture/state/images"
if [ -f "$fixture/state/docker_down" ]; then
  echo "Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?" >&2
  exit 1
fi
words=() format="" quiet=0 filter="" expect_value=""
for arg in "$@"; do
  if [ -n "$expect_value" ]; then
    case "$expect_value" in format) format="$arg" ;; filter) filter="$arg" ;; esac
    expect_value=""; continue
  fi
  case "$arg" in
    --format|-f) expect_value=format ;;
    --format=*) format="${arg#--format=}" ;;
    --filter) expect_value=filter ;;
    --filter=*) filter="${arg#--filter=}" ;;
    -q|--quiet) quiet=1 ;;
    -*) ;;
    *) words+=("$arg") ;;
  esac
done
command="${words[0]:-}" refs=("${words[@]:1}")
if [ "$command" = image ] || [ "$command" = container ]; then
  command="$command-${words[1]:-}"; refs=("${words[@]:2}")
fi
render() {
  local repo=$1 tag=$2 id=$3 template=$4 out
  case "$template" in
    json|"{{json .}}") printf '{"Repository":"%s","Tag":"%s","ID":"%s"}\n' "$repo" "$tag" "$id"; return ;;
  esac
  out="${template#table }"
  out="${out//\{\{.Repository\}\}/$repo}"; out="${out//\{\{.Tag\}\}/$tag}"
  out="${out//\{\{.ID\}\}/$id}"; out="${out//\{\{.Id\}\}/sha256:$id}"
  out="${out//\{\{.RepoTags\}\}/[$repo:$tag]}"; out="${out//\{\{json .RepoTags\}\}/[\"$repo:$tag\"]}"
  out="${out//\\t/	}"
  printf '%s\n' "$out"
}
ref_matches() {
  local repo=$1 tag=$2 ref=$3
  if [[ $ref != *:* ]]; then ref="$ref:latest"; fi
  # shellcheck disable=SC2053 # a reference filter is a glob, as docker reads it
  [[ "$repo:$tag" == $ref ]]
}
case "$command" in
  images|image-ls|image-list)
    pattern="${refs[0]:-}"
    if [[ $filter == reference=* ]]; then pattern="${filter#reference=}"; fi
    if [ "$quiet" -eq 0 ] && [ -z "$format" ]; then echo "REPOSITORY TAG IMAGE ID CREATED SIZE"; fi
    while read -r repo tag id; do
      if [ -n "$pattern" ] && ! ref_matches "$repo" "$tag" "$pattern" && [ "$repo" != "$pattern" ]; then continue; fi
      if [ "$quiet" -eq 1 ]; then echo "$id"
      elif [ -n "$format" ]; then render "$repo" "$tag" "$id" "$format"
      else echo "$repo $tag $id 2 weeks ago 512MB"; fi
    done < "$images"
    exit 0 ;;
  inspect|image-inspect)
    code=0 found=()
    for ref in "${refs[@]}"; do
      hit=""
      while read -r repo tag id; do
        if ref_matches "$repo" "$tag" "$ref" || [ "$ref" = "$id" ]; then hit="$repo $tag $id"; fi
      done < "$images"
      if [ -z "$hit" ]; then echo "Error: No such image: $ref" >&2; code=1; continue; fi
      found+=("$hit")
    done
    for hit in "${found[@]+"${found[@]}"}"; do
      read -r repo tag id <<< "$hit"
      if [ -n "$format" ]; then render "$repo" "$tag" "$id" "$format"
      else printf '[{"Id":"sha256:%s","RepoTags":["%s:%s"]}]\n' "$id" "$repo" "$tag"; fi
    done
    exit "$code" ;;
  info|version|ps|container-ls|container-list|context) exit 0 ;;
  *) echo "docker stand-in: the host check must not run docker ${words[*]}" >&2; exit 1 ;;
esac
STANDIN

cat > "$standins_dir/pgrep" <<'STANDIN'
#!/usr/bin/env bash
fixture="$(cd "$(dirname "$0")/.." && pwd)"
printf '%s\n' "$*" >> "$fixture/log/pgrep"
full=0 exact=0 list=0 list_full=0 count=0 ignore_case=0 pattern="" skip=0
for arg in "$@"; do
  if [ "$skip" -eq 1 ]; then skip=0; continue; fi
  case "$arg" in
    -u|-U|-g|-G|-P|-s|-t|-d|--euid|--uid|--group|--parent|--session|--terminal|--delimiter) skip=1 ;;
    -*)
      if [[ $arg == --* ]]; then
        case "$arg" in --full) full=1 ;; --exact) exact=1 ;; --list-name) list=1 ;; --list-full) list_full=1 ;; --count) count=1 ;; --ignore-case) ignore_case=1 ;; esac
      else
        flags="${arg#-}"
        [[ $flags == *f* ]] && full=1; [[ $flags == *x* ]] && exact=1; [[ $flags == *l* ]] && list=1
        [[ $flags == *a* ]] && list_full=1; [[ $flags == *c* ]] && count=1; [[ $flags == *i* ]] && ignore_case=1
        [[ $flags == *[uUgGPstd] ]] && skip=1
      fi ;;
    *) pattern="$arg" ;;
  esac
done
hits=0
while read -r pid cmdline; do
  name="$(basename "${cmdline%% *}")"; name="${name:0:15}"
  subject=$([ "$full" -eq 1 ] && printf '%s' "$cmdline" || printf '%s' "$name")
  regex="$pattern"; if [ "$exact" -eq 1 ]; then regex="^($pattern)\$"; fi
  if [ "$ignore_case" -eq 1 ]; then grep -Eiq -- "$regex" <<< "$subject" || continue
  else grep -Eq -- "$regex" <<< "$subject" || continue; fi
  hits=$((hits + 1))
  if [ "$count" -eq 1 ]; then continue; fi
  if [ "$list_full" -eq 1 ]; then echo "$pid $cmdline"; elif [ "$list" -eq 1 ]; then echo "$pid $name"; else echo "$pid"; fi
done < "$fixture/state/processes"
if [ "$count" -eq 1 ]; then echo "$hits"; fi
[ "$hits" -gt 0 ]
STANDIN

cat > "$standins_dir/gpgconf" <<'STANDIN'
#!/usr/bin/env bash
fixture="$(cd "$(dirname "$0")/.." && pwd)"
printf '%s\n' "$*" >> "$fixture/log/gpgconf"
case "${1:-}" in
  --list-dirs)
    if [ -n "${2:-}" ]; then
      case "$2" in homedir) echo "$HOME/.gnupg" ;; *) echo "/run/user/1000/gnupg" ;; esac
    else
      printf 'sysconfdir:/etc/gnupg\nbindir:/usr/bin\nhomedir:%s/.gnupg\nsocketdir:/run/user/1000/gnupg\n' "$HOME"
    fi ;;
  --list-components) printf 'gpg:OpenPGP:/usr/bin/gpg\ngpg-agent:Private Keys:/usr/bin/gpg-agent\nscdaemon:Smartcards:/usr/lib/gnupg/scdaemon\n' ;;
  --list-options|--check-programs|--check-options|--version|--list-config|-V) ;;
  *) echo "gpgconf stand-in: the host check must not run gpgconf $*" >&2; exit 1 ;;
esac
STANDIN

for tool in sudo pkill killall; do
  cat > "$standins_dir/$tool" <<'STANDIN'
#!/usr/bin/env bash
fixture="$(cd "$(dirname "$0")/.." && pwd)"
printf '%s\n' "$*" >> "$fixture/log/$(basename "$0")"
echo "$(basename "$0") stand-in: the host check must not run this" >&2
exit 1
STANDIN
done
chmod +x "$standins_dir"/*

# ------------------------------------------------------------------ fixtures
# A fixture is a retired host (the container runner and watchtower running, host pcscd disabled,
# scdaemon.conf with disable-ccid) plus the leftovers named after it. The container runner's
# listener is visible to the host's process list, as it is on the real machine.

new_fixture() {
  local name=$1; shift
  local fixture="$work_dir/$name" home
  home="$fixture/home/maintainer"
  mkdir -p "$fixture/bin" "$fixture/state" "$fixture/log" "$home/.gnupg" "$home/.local/bin" "$home/.config/lighthouse-codesign"
  cp "$standins_dir"/* "$fixture/bin/"
  chmod 700 "$home/.gnupg"
  printf 'card-timeout 5\ndisable-ccid\n' > "$home/.gnupg/scdaemon.conf"
  printf '#!/bin/sh\necho unrelated tool\n' > "$home/.local/bin/uv"
  chmod +x "$home/.local/bin/uv"
  printf 'fixture-token\n' > "$home/.config/lighthouse-codesign/github-token"
  chmod 600 "$home/.config/lighthouse-codesign/github-token"
  : > "$fixture/state/units"
  : > "$fixture/state/images"
  : > "$fixture/state/processes"
  local pcscd_socket="disabled inactive" pcscd_service="disabled inactive" runner=0 scdaemon_running=0 leftover
  for leftover in "$@"; do
    case "$leftover" in
      runner) runner=1 ;;
      wrapper) printf '#!/bin/sh\nexec docker run --rm -i local/osslsigncode-ossl3:trixie "$@"\n' > "$home/.local/bin/osslsigncode"; chmod +x "$home/.local/bin/osslsigncode" ;;
      image) echo "local/osslsigncode-ossl3 trixie 5d2c1f0e9a77" >> "$fixture/state/images" ;;
      pcscd) pcscd_socket="enabled active"; pcscd_service="enabled active" ;;
      pcscd-socket-enabled) pcscd_socket="enabled inactive" ;;
      pcscd-service-active) pcscd_service="disabled active" ;;
      scdaemon) printf 'card-timeout 5\n' > "$home/.gnupg/scdaemon.conf"; scdaemon_running=1 ;;
      scdaemon-no-file) rm "$home/.gnupg/scdaemon.conf"; scdaemon_running=1 ;;
      scdaemon-commented) printf 'card-timeout 5\n# disable-ccid\n' > "$home/.gnupg/scdaemon.conf"; scdaemon_running=1 ;;
      docker-down) : > "$fixture/state/docker_down" ;;
    esac
  done
  {
    echo "docker.service enabled active Docker Application Container Engine"
    echo "lighthouse-codesign-runner.service enabled active Lighthouse code signing runner"
    echo "pcscd.socket $pcscd_socket PC/SC Smart Card Daemon Activation Socket"
    echo "pcscd.service $pcscd_service PC/SC Smart Card Daemon"
    if [ "$runner" -eq 1 ]; then echo "$host_runner_unit enabled active GitHub Actions Runner (LetPeopleWork-Lighthouse.codesign-runner-linux)"; fi
  } > "$fixture/state/units"
  {
    echo "ghcr.io/letpeoplework/lighthouse-codesign-runner latest 9b1e7c3a4d20"
    echo "containrrr/watchtower latest 3f4e5a6b7c81"
  } >> "$fixture/state/images"
  {
    echo "812 /usr/bin/dockerd -H fd://"
    echo "4410 /home/runner/runtime/bin/Runner.Listener run"
    if [ "$runner" -eq 1 ]; then
      echo "1290 /bin/bash /home/maintainer/Lighthouse/tools/codesign/actions-runner/runsvc.sh"
      echo "1301 /home/maintainer/Lighthouse/tools/codesign/actions-runner/bin/Runner.Listener run --startuptype service"
    fi
    if [[ $pcscd_socket == *" active" || $pcscd_service == *" active" ]]; then echo "2020 /usr/bin/pcscd --foreground --auto-exit"; fi
    if [ "$scdaemon_running" -eq 1 ]; then echo "2200 scdaemon --multi-server"; fi
  } > "$fixture/state/processes"
  printf '%s' "$fixture"
}

# Runs the host check against one fixture with only declared inputs: no GNUPGHOME, no XDG paths and
# no PATH entry from the machine running this oracle can leak in.
run_check() {
  local fixture=$1 code=0
  env -i HOME="$fixture/home/maintainer" USER=maintainer LOGNAME=maintainer LC_ALL=C \
    PATH="$fixture/bin:/usr/bin:/bin" \
    timeout "$case_limit_seconds" bash "$check_script" > "$fixture/output" 2>&1 || code=$?
  printf '%s' "$code" > "$fixture/exit"
}

exit_of() { cat "$1/exit"; }
output_of() { cat "$1/output"; }

# The lines of the output that name a leftover.
lines_naming() {
  local fixture=$1 leftover=$2
  grep -Ei -- "${names_leftover[$leftover]}" "$fixture/output" || true
}

says_host_is_clean() {
  grep -Eiq '^[[:space:]]*host is clean\.?[[:space:]]*$' "$1/output"
}

# Every logged call of a stand-in that would have changed the host.
changing_calls() {
  local fixture=$1 tool log
  for tool in systemctl docker gpgconf sudo pkill killall; do
    log="$fixture/log/$tool"
    [ -f "$log" ] || continue
    while IFS= read -r line; do
      case "$tool" in
        systemctl) if ! grep -Eq '(^|[[:space:]])(is-enabled|is-active|is-failed|list-units|list-unit-files|list-sockets|list-timers|list-dependencies|status|show|cat|get-default)([[:space:]]|$)|^(--version)?$' <<< "$line"; then echo "systemctl $line"; fi ;;
        docker) if ! grep -Eq '(^|[[:space:]])(images|inspect|info|version|ps|context|image (ls|list|inspect)|container (ls|list))([[:space:]]|$)' <<< "$line"; then echo "docker $line"; fi ;;
        gpgconf) if ! grep -Eq '^(--list-dirs|--list-components|--list-options|--check-programs|--check-options|--version|--list-config|-V)' <<< "$line"; then echo "gpgconf $line"; fi ;;
        *) echo "$tool $line" ;;
      esac
    done < "$log"
  done
}

home_snapshot() {
  (cd "$1/home" && find . -printf '%p %y %m\n' | LC_ALL=C sort && find . -type f -exec sha256sum {} + | LC_ALL=C sort -k2)
}

# One leftover on an otherwise retired host: exit 1, a line naming it that says what to do,
# no line naming any other leftover, and no claim that the host is clean.
check_single_leftover() {
  local leftover=$1 variant=$2 description=$3 fixture code naming others other_lines other
  fixture="$(new_fixture "single-$variant" "$variant")"
  run_check "$fixture"
  code="$(exit_of "$fixture")"
  naming="$(lines_naming "$fixture" "$leftover")"
  other_lines="$(grep -Eiv -- "${names_leftover[$leftover]}" "$fixture/output" || true)"
  others=""
  for other in "${all_leftovers[@]}"; do
    if [ "$other" = "$leftover" ]; then continue; fi
    if grep -Eiq -- "${names_leftover[$other]}" <<< "$other_lines"; then others="$others ${leftover_words[$other]};"; fi
  done
  local name="a host whose only leftover is $description gets one finding for it, with what to do, and exit 1"
  if [ "$code" = 1 ] && [ -n "$naming" ] && grep -Eiq -- "${says_how[$leftover]}" <<< "$naming" && [ -z "$others" ] && ! says_host_is_clean "$fixture"; then
    pass "$name"
  else
    fail "$name" \
      "exit $code; lines naming it: ${naming:-none}; findings for leftovers that are not there:${others:- none}; output: $(output_of "$fixture" | head -c 1200)" \
      "the maintainer retires the host setup by the check's findings; a missed leftover keeps holding the key or signing the old way, a false one sends them hunting for nothing" \
      "make tools/codesign/check-host.sh report ${leftover_words[$leftover]} on one line that names it and says how to retire it, report nothing that is not there, and exit 1"
  fi
}

# ------------------------------------------------------------------ cases

if [ -f "$retired_script" ]; then
  fail "the old Linux runner script is gone" \
    "tools/codesign/start-runner-linux.ps1 still exists" \
    "it installs and starts the host runner and host pcscd this feature retires; keeping it invites running the old setup again" \
    "delete tools/codesign/start-runner-linux.ps1; its work lives in the runner image, compose.yaml, the unit and sign-windows.sh"
else
  pass "the old Linux runner script is gone"
fi

references="$(grep -rln --exclude=test-host-check.sh -- 'start-runner-linux' "$repo_root/tools" "$repo_root/.github" 2>/dev/null || true)"
if [ -z "$references" ]; then
  pass "nothing in tools or the workflows still points at the old Linux runner script"
else
  fail "nothing in tools or the workflows still points at the old Linux runner script" \
    "still referenced in: $(tr '\n' ' ' <<< "$references")" \
    "a step that names a deleted script cannot be followed" \
    "replace each reference with the container runner setup"
fi

old="$(new_fixture old-setup runner wrapper image pcscd scdaemon)"
run_check "$old"
old_code="$(exit_of "$old")"
missing="" no_how=""
for leftover in "${all_leftovers[@]}"; do
  naming="$(lines_naming "$old" "$leftover")"
  if [ -z "$naming" ]; then missing="$missing ${leftover_words[$leftover]};"
  elif ! grep -Eiq -- "${says_how[$leftover]}" <<< "$naming"; then no_how="$no_how ${leftover_words[$leftover]};"; fi
done
name="a host with the whole old setup gets a finding for each of the five leftovers, each with what to do, and exit 1"
if [ "$old_code" = 1 ] && [ -z "$missing" ] && [ -z "$no_how" ] && ! says_host_is_clean "$old"; then
  pass "$name"
else
  fail "$name" \
    "exit $old_code; not named:${missing:- none}; named without what to do:${no_how:- none}; output: $(output_of "$old" | head -c 1500)" \
    "the maintainer runs the check once after the switch-over and retires what it names; a leftover it misses keeps the old setup alive" \
    "make tools/codesign/check-host.sh print one line per leftover, each naming it and how to retire it, and exit 1 when there is any"
fi

changes="$(changing_calls "$old")"
snapshot_before="$work_dir/old-home-before"
fresh="$(new_fixture old-setup-reference runner wrapper image pcscd scdaemon)"
home_snapshot "$fresh" > "$snapshot_before"
if [ -s "$old/log/systemctl" ] && [ -z "$changes" ] && diff -q "$snapshot_before" <(home_snapshot "$old") >/dev/null; then
  pass "the check changes nothing on the host it reports on"
else
  fail "the check changes nothing on the host it reports on" \
    "systemctl queries: $(grep -c "" "$old/log/systemctl" 2>/dev/null || true); calls that change the host: ${changes:-none}; home directory differs from an untouched copy: $(diff "$snapshot_before" <(home_snapshot "$old") | head -c 600 || true)" \
    "the maintainer runs it to look, also on a machine mid-release; stopping the runner, deleting files or killing scdaemon by surprise is not what it promises" \
    "only query (systemctl is-enabled/is-active/list-units, docker image inspect/images, pgrep, gpgconf --list-dirs, reading files); leave every action to the maintainer in the HOW"
fi

check_single_leftover runner runner "the host actions runner service"
check_single_leftover wrapper wrapper "the ~/.local/bin/osslsigncode wrapper"
check_single_leftover image image "the local/osslsigncode-ossl3 image"
check_single_leftover pcscd pcscd-socket-enabled "a pcscd.socket that is enabled but not active"
check_single_leftover pcscd pcscd-service-active "a pcscd.service that is active but not enabled"
check_single_leftover scdaemon scdaemon "a scdaemon.conf without disable-ccid"
check_single_leftover scdaemon scdaemon-no-file "a missing scdaemon.conf"
check_single_leftover scdaemon scdaemon-commented "a scdaemon.conf whose disable-ccid is commented out"

clean="$(new_fixture clean)"
run_check "$clean"
clean_code="$(exit_of "$clean")"
found=""
for leftover in "${all_leftovers[@]}"; do
  if [ -n "$(lines_naming "$clean" "$leftover")" ]; then found="$found ${leftover_words[$leftover]};"; fi
done
asked=""
for tool in systemctl docker; do
  if [ ! -s "$clean/log/$tool" ]; then asked="$asked $tool never asked;"; fi
done
name="a retired host, with the container runner running, says host is clean after asking systemd and Docker, and exits 0"
if [ "$clean_code" = 0 ] && says_host_is_clean "$clean" && [ -z "$found" ] && [ -z "$asked" ]; then
  pass "$name"
else
  fail "$name" \
    "exit $clean_code; findings:${found:- none};${asked:- systemd and Docker were asked}; output: $(output_of "$clean" | head -c 1200)" \
    "host is clean is the maintainer's sign that the retirement is done; it must come from looking, and the container runner and watchtower are the new setup, not leftovers" \
    "make tools/codesign/check-host.sh query systemd and Docker, print the line host is clean when nothing is found, and exit 0"
fi

blind="$(new_fixture docker-unreachable image docker-down)"
run_check "$blind"
blind_code="$(exit_of "$blind")"
name="when Docker cannot be asked, the check says so, does not call the host clean, and exits 1"
if [ "$blind_code" = 1 ] && grep -Eiq docker "$blind/output" && ! says_host_is_clean "$blind"; then
  pass "$name"
else
  fail "$name" \
    "exit $blind_code with Docker unreachable and the local osslsigncode image present; output: $(output_of "$blind" | head -c 800)" \
    "a check that could not look at the images cannot know the old image is gone; calling that clean hides a leftover" \
    "when a docker query fails, print that Docker could not be asked and how to start it, and exit 1"
fi

name="the image workflow runs the host check oracle in verify"
if ! docker info >/dev/null 2>&1; then
  setup_failure "the Docker daemon is not reachable, so the workflow cannot be read; start Docker and re-run"
elif ! docker image inspect "$yq_image" >/dev/null 2>&1 && ! docker pull -q "$yq_image" >/dev/null 2>&1; then
  setup_failure "cannot pull the YAML reader $yq_image; check the network and re-run"
elif [ ! -f "$image_workflow" ]; then
  fail "$name" ".github/workflows/codesign-runner-image.yml does not exist" "nothing runs the host check oracle" "create the workflow"
else
  runs_it="$(docker run --rm -i --network none "$yq_image" -r \
    '(.jobs.verify.steps // []) | to_json(0) | test("tools/codesign/tests/test-host-check\\.sh") | tostring' \
    < "$image_workflow" 2>&1 || true)"
  if [ "$runs_it" = true ]; then
    pass "$name"
  else
    fail "$name" \
      "the verify job of codesign-runner-image.yml has no step running tools/codesign/tests/test-host-check.sh ($runs_it)" \
      "Codesign Runner Image / verify is the required check for codesign changes; a check-host.sh change it does not run can break the maintainer's retirement check unseen" \
      "add a verify step: run: bash tools/codesign/tests/test-host-check.sh"
  fi
fi

summary_and_exit
