#!/bin/sh
# Redoes the picks of the smallest and the largest group with shell tools only (no Python), from the repo root.
cd "$(git rev-parse --show-toplevel)" || exit 1
pick() { # file-list quota
  n=$(wc -l < "$1")
  i=1
  while [ "$i" -le "$2" ]; do
    p=$(( (i * n + $2 - 1) / $2 ))
    printf '%s @%s %s\n' "$i" "$p" "$(sed -n "${p}p" "$1")"
    i=$((i + 1))
  done
}
T=$(mktemp -d)
git ls-files Lighthouse.Frontend/src/pages/Settings | grep '[.]test[.]tsx$' | sed 's#Lighthouse.Frontend/src/##' | LC_ALL=C sort > "$T/settings"
echo "pages/Settings: $(wc -l < "$T/settings") files, quota 3"
pick "$T/settings" 3
git ls-files Lighthouse.Frontend/src/components/Common | grep '[.]test[.]tsx$' | sed 's#Lighthouse.Frontend/src/##' | grep -v '^components/Common/Charts/' | LC_ALL=C sort > "$T/common"
echo "components/Common without Charts: $(wc -l < "$T/common") files, quota 15"
pick "$T/common" 15
rm -rf "$T"
