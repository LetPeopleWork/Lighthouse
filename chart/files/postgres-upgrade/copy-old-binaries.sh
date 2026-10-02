#!/usr/bin/env bash
# Runs as the postgres user in the upgrade-source image, one major below the database image. When the copy
# the database runs on is this image's major, it hands this major's programs to the upgrade step, which
# runs in the database image and needs both majors side by side. For any other volume there is nothing to
# hand over, and a volume the upgrade step will refuse is left for that step to explain, so the operator
# reads one reason, once.
set -euo pipefail

# shellcheck source-path=SCRIPTDIR source=volume.sh
source "$(dirname "${BASH_SOURCE[0]}")/volume.sh"

readonly TARGET=/old-binaries

# Programs built for another operating system cannot run in the database image, so the upgrade step is
# told which system this image is built on. That comes first: an image built on another system may run as
# a user that cannot even read the data's version.
grep -E '^(ID|VERSION_ID|VERSION_CODENAME)=' /etc/os-release >"$TARGET/os-release" || true

# An image that is not an official postgres image sets no PG_MAJOR and has nothing to hand over; the
# upgrade step then says which image to use. A copy whose control file cannot be read cannot be upgraded
# either.
read -r live_folder live_major <<<"$(live_copy)"
if [[ -z "${PG_MAJOR:-}" || "$live_major" != "$PG_MAJOR" || -z "$(control_hash "$live_folder")" ]]; then
  exit 0
fi

mkdir -p "$TARGET/usr/lib/postgresql" "$TARGET/usr/share/postgresql"
cp -a "/usr/lib/postgresql/$PG_MAJOR" "$TARGET/usr/lib/postgresql/"
cp -a "/usr/share/postgresql/$PG_MAJOR" "$TARGET/usr/share/postgresql/"
echo "lighthouse-postgres: the data in $live_folder is Postgres $PG_MAJOR; its programs are ready for the upgrade step"
