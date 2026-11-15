#!/usr/bin/env bash
# Run on the signing machine after switching to the container runner. Names every leftover of the old
# host setup, and every host service that would take the YubiKey away from the container, one line each
# with what to do about it, and exits 1; says "host is clean" and exits 0 when there is none.
# It only looks: it never stops, disables, deletes or restarts anything. Run it as yourself, not root.
#   bash tools/codesign/check-host.sh
set -euo pipefail

old_image=local/osslsigncode-ossl3
old_wrapper="$HOME/.local/bin/osslsigncode"

findings=0
could_not_look=0

found() {
  findings=$((findings + 1))
  echo "FOUND: $1 HOW: $2"
}

unknown() {
  could_not_look=$((could_not_look + 1))
  echo "UNKNOWN: $1 HOW: $2"
}

check_host_runner() {
  local listing unit state _preset
  if ! listing="$(systemctl list-unit-files --no-legend --no-pager 'actions.runner.*' 2>&1)"; then
    unknown "systemd could not be asked for an old host actions runner service ($listing)." \
      "run this check on the signing machine as your own user, with systemd running."
    return
  fi
  while read -r unit state _preset; do
    if [ -z "$unit" ]; then continue; fi
    found "the host actions runner service $unit ($state) is still installed; it is a second runner with the codesign label that signs outside the container." \
      "in its runner folder run sudo ./svc.sh stop, sudo ./svc.sh uninstall and ./config.sh remove, then delete that folder."
  done <<< "$listing"
}

check_old_wrapper() {
  if [ -e "$old_wrapper" ] || [ -L "$old_wrapper" ]; then
    found "the ~/.local/bin/osslsigncode wrapper is still there; it signed with the old engine setup." \
      "rm ~/.local/bin/osslsigncode"
  fi
}

check_old_image() {
  local listing
  if ! listing="$(docker images --format '{{.Repository}}:{{.Tag}}' "$old_image" 2>&1)"; then
    unknown "Docker could not be asked whether the old $old_image image is still there ($listing)." \
      "start Docker (sudo systemctl start docker), make sure your user may use it, and re-run this check."
    return
  fi
  local reference
  while read -r reference; do
    if [ -z "$reference" ]; then continue; fi
    found "the old image $reference is still on this machine; the container runner does not use it." \
      "docker image rm $reference"
  done <<< "$listing"
}

# Socket activation restarts pcscd as soon as anything asks for a card, so the socket counts on its own.
check_host_pcscd() {
  local unit enabled active
  for unit in pcscd.socket pcscd.service; do
    enabled="$(systemctl is-enabled "$unit" 2>/dev/null || true)"
    active="$(systemctl is-active "$unit" 2>/dev/null || true)"
    if [ "$enabled" = enabled ] || [ "$active" = active ] || [ "$active" = activating ]; then
      found "host $unit is ${enabled:-unknown} and ${active:-unknown}; a pcscd on the host claims the YubiKey before the runner container can." \
        "sudo systemctl disable --now $unit"
    fi
  done
}

check_scdaemon() {
  if ! command -v gpgconf > /dev/null 2>&1; then return; fi
  local gnupg_home config running=""
  gnupg_home="$(gpgconf --list-dirs homedir 2>/dev/null || true)"
  config="${gnupg_home:-$HOME/.gnupg}/scdaemon.conf"
  if [ -f "$config" ] && grep -Eq '^[[:space:]]*disable-ccid([[:space:]]|$)' "$config"; then return; fi
  if pgrep -x scdaemon > /dev/null 2>&1; then running=", then restart the running scdaemon with gpgconf --kill scdaemon"; fi
  found "$config has no disable-ccid line, so GnuPG's scdaemon may claim the YubiKey's USB interface." \
    "echo disable-ccid >> $config$running."
}

check_host_runner
check_old_wrapper
check_old_image
check_host_pcscd
check_scdaemon

if [ "$could_not_look" -gt 0 ]; then
  echo "Not every part of the host could be checked, so this is not a clean result. Fix what is named above and re-run this check."
  exit 1
fi
if [ "$findings" -gt 0 ]; then
  echo "$findings finding(s). Do what each line says, then re-run this check until it says host is clean."
  exit 1
fi
echo "host is clean"
