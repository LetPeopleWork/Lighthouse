#!/usr/bin/env python3
"""Deliberate breaks for the follow-up that moved the last ConfigureOncePerConnection host rows the suite had kept
(Slice08's save-gate and same-save rows, Slice12's Estimation and refused-move rows) down to unit rows.

Each break changes one production line, runs the unit classes that now prove the rule (Debug, tokens removed), prints
the rows that went red and puts the line back. A break proves its rule only if at least one named row goes red.
Run it on a clean tree; pass break ids to run only those."""
import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
CONTROLLER = B + "API/WorkTrackingSystemConnectionController.cs"
TEAM = B + "API/TeamController.cs"
FINGERPRINT = B + "Services/Implementation/WorkItems/FetchFingerprint.cs"
PORTFOLIO = B + "Models/Portfolio.cs"
OWNER = B + "Models/WorkTrackingSystemOptionsOwner.cs"

UNITS = "|".join("FullyQualifiedName~" + name for name in (
    "WorkTrackingSystemConnectionControllerTest.", "TeamControllerTest.", "ConnectionSaveServiceTest.",
    "WhereAPortfoliosWorkItemHangsTest.",
))

BREAKS = [
    ("F1 a default the save leaves out is applied as cleared", CONTROLLER,
     "foreach (var fieldDefault in ConnectionFieldDefaults.All.Where(fieldDefault => defaults.Sends(fieldDefault.Setting)))",
     "foreach (var fieldDefault in ConnectionFieldDefaults.All)"),
    ("F2 a field added in the same save is not remembered as the default", CONTROLLER,
     "save.DefaultsAddedInThisSave[fieldDefault.Setting] = addedInThisSave;",
     "_ = addedInThisSave;"),
    ("F3 an Estimation default counts in what an owner fetches", FINGERPRINT,
     "                Render(queryOwner.WorkTrackingSystemConnectionId),\n",
     "                Render(queryOwner.WorkTrackingSystemConnectionId),\n                Render(queryOwner.WorkTrackingSystemConnection?.EstimationFieldForTeams),\n"),
    ("F4 a Team save is judged only after its Work Items are thrown away", TEAM,
     "if (RefusalOf(teamId, teamSetting, canUsePremiumFeatures) is { } refusal)",
     "if (RefusalOf(teamId, teamSetting, canUsePremiumFeatures) is { } refusal && teamSetting.Id < 0)"),
    ("F5 a Portfolio reads the Teams' Parent override default", PORTFOLIO,
     "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForPortfolios;",
     "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForTeams;"),
    ("F6 an owner with its own list reads the connection's default", OWNER,
     "=> (FollowsParentOverrideDefault ? null : ParentSources)",
     "=> ((ParentSources?)null)"),
]

only = sys.argv[1:]
for name, path, old, new in BREAKS:
    if only and name.split()[0] not in only:
        continue
    full = os.path.join(R, path)
    src = open(full, encoding="utf-8").read()
    if src.count(old) != 1:
        print(f"=== {name}\nSKIPPED: the line occurs {src.count(old)} times in {path}")
        continue
    open(full, "w", encoding="utf-8").write(src.replace(old, new))
    try:
        p = subprocess.run(["env", "-u", "LinearAPIKey", "dotnet", "test", "Lighthouse.Backend/Lighthouse.Backend.Tests",
                            "-c", "Debug", "-p:TreatWarningsAsErrors=false", "--filter", UNITS],
                           cwd=R, capture_output=True, text=True, timeout=900)
        out = p.stdout + p.stderr
        print("=== " + name, flush=True)
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                print(line.strip())
        summary = [line.strip() for line in out.splitlines() if "Passed!" in line or "Failed!" in line]
        print(summary[-1] if summary else "NO SUMMARY", flush=True)
    finally:
        open(full, "w", encoding="utf-8").write(src)
