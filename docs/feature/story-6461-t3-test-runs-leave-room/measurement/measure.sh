#!/usr/bin/env bash
# Usage: measure.sh <label> <workdir> <command...>
# Runs the command in its own session and samples, once a second, the summed RSS of every
# process in that session and the machine's MemAvailable. Prints one CSV line at the end:
# label,exit,wall_s,peak_tree_rss_mb,min_mem_available_mb,max_threads
set -u
label=$1; workdir=$2; shift 2
log="${OUT_DIR:-.}/${label}.log"
cd "$workdir"
start=$(date +%s)
setsid "$@" >"$log" 2>&1 &
pid=$!
sid=$pid
peak=0; minavail=999999999; maxthreads=0
while kill -0 "$pid" 2>/dev/null; do
  rss=$(ps -o rss= -s "$sid" 2>/dev/null | awk '{s+=$1} END {print s+0}')
  thr=$(ps -o nlwp= -s "$sid" 2>/dev/null | awk '{s+=$1} END {print s+0}')
  avail=$(awk '/MemAvailable/ {print $2}' /proc/meminfo)
  (( rss > peak )) && peak=$rss
  (( thr > maxthreads )) && maxthreads=$thr
  (( avail < minavail )) && minavail=$avail
  sleep 1
done
wait "$pid"; rc=$?
end=$(date +%s)
printf '%s,%s,%d,%d,%d,%d\n' "$label" "$rc" "$((end - start))" $((peak/1024)) $((minavail/1024)) "$maxthreads"
