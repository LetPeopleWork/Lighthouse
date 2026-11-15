#!/usr/bin/env bash
# For each production file under Modules/Forecasting at HEAD, find the file of the same name at BASE and
# print every changed line that is not a using/namespace/blank line.
cd /storage/repos/Lighthouse
BASE=${1:-39a7f598f}
git ls-tree -r --name-only HEAD Lighthouse.Backend/Lighthouse.Backend/Modules/Forecasting | while read -r new; do
  name=$(basename "$new")
  old=$(git ls-tree -r --name-only "$BASE" Lighthouse.Backend/Lighthouse.Backend | grep "/$name\$" | head -1)
  if [ -z "$old" ]; then echo "NEW $new"; continue; fi
  changes=$(diff <(git show "$BASE:$old" | tr -d '\r\357\273\277') <(git show "HEAD:$new" | tr -d '\r\357\273\277') | grep -E '^[<>]' | grep -vE '^[<>]\s*(using |namespace |$)')
  if [ -n "$changes" ]; then echo "CHANGED $new"; echo "$changes" | head -8; else echo "same $name"; fi
done
