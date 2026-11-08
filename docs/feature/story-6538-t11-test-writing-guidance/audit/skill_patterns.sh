#!/bin/sh
# usage: sh skill_patterns.sh <folder or file> ; prints hits of the three lists (nothing printed = clean)
T="$1"
echo "--- list 1: internal references"
grep -rnE '#[0-9]{4}|Story|slice|ADR-|\bFE[0-9]+\b|\bT[0-9]+\b|\bD-[0-9]+' "$T"
echo "--- list 2: nWave scenario tags"
grep -rnE '@us-|@slice-|@kpi-|@contract-shape|@walking_skeleton|@driving_port|@real-io|@BUG-' "$T"
echo "--- list 3: dated counts"
grep -rnE '3,648|5,857|9,505|22,600' "$T"
