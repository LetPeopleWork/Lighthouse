#!/usr/bin/env python3
"""Runs the Slice04 and Slice03 Azure DevOps host classes and the unit classes that now prove their rules (Debug, tokens removed),
compares their sorted test ids and outcomes with s3-expected-ids.txt, checks that no production or budget file
changed and that the host-user guard list only lost lines of classes that left the host, prints every difference and
exits non-zero on any difference or failed test. With --write-expected it writes the observed ids instead."""
import glob
import os
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
HERE = os.path.dirname(os.path.abspath(__file__))
EXPECTED = os.path.join(HERE, "s3-expected-ids.txt")
TOKENS = [
    "AzureDevOpsLighthouseIntegrationTestToken", "JiraLighthouseIntegrationTestToken",
    "JiraLighthouseRestrictedIntegrationTestToken", "JiraScopedTokenIntegrationTestToken",
    "ServiceNowLighthouseIntegrationTestToken", "LinearAPIKey", "AzureDevOpsLighthouseE2ETestToken",
]
FILTER = (
    "FullyQualifiedName~Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios"
    "|FullyQualifiedName~Slice03AzureDevOpsRelationTypeAcceptedScenarios"
    "|FullyQualifiedName~AzureDevOpsNamedRelationEndTest."
    "|FullyQualifiedName~TeamUpdaterTest.TriggerUpdate_ACheapRefresh_LeavesTheFindingsOfItsRowUnsaid"
)
SLICE_BASE = "d7326c838"


def compare_base():
    """The commit the production and guard checks compare with: where HEAD leaves origin/main.

    A rebase rewrites the slice base and slides trunk commits under it, so a diff from the fixed sha would charge other
    people's production changes to this slice. The point where HEAD leaves origin/main covers exactly the commits not yet
    pushed. A pushed slice was checked before its push; run after the push, this check covers only what is still local.
    Without an origin/main ref it falls back to the recorded slice base."""
    done = subprocess.run(["git", "merge-base", "origin/main", "HEAD"], cwd=ROOT, capture_output=True, text=True)
    if done.returncode != 0 or not done.stdout.strip():
        return SLICE_BASE
    return done.stdout.strip()
GUARD = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs"
LEFT_THE_HOST = []
UNTOUCHED = [
    "Lighthouse.Backend/Lighthouse.Backend/",
    "Scripts/test-timings/test-time-budget.json",
]


def guard_problems():
    """The guard list may only lose lines of classes that left the host; an added line fails."""
    problems = []
    done = subprocess.run(["git", "diff", "-U0", compare_base(), "--", GUARD], cwd=ROOT, capture_output=True, text=True)
    if done.returncode != 0:
        print("BROKEN: git failed: " + done.stderr.strip())
        sys.exit(2)
    for line in done.stdout.splitlines():
        if line.startswith(("+++", "---", "@@", "diff", "index")):
            continue
        if line.startswith("+"):
            problems.append(f"GUARD    a line was added to the host-user guard list: {line[1:].strip()}")
        elif line.startswith("-") and not any(name in line for name in LEFT_THE_HOST):
            problems.append(f"GUARD    a line of a class that still starts a host left the guard list: {line[1:].strip()}")
    return problems


def listed_by_any_slice():
    """Every test id any slice of this feature lists. A row in a shared unit class that another slice owns is not EXTRA here."""
    ids = set()
    for name in sorted(os.listdir(HERE)):
        if re.fullmatch(r"s\d+-expected-ids\.txt", name):
            with open(os.path.join(HERE, name), encoding="utf-8") as handle:
                ids.update(line.split("\t")[0] for line in handle if line.strip())
    return ids


def touched_paths():
    """Production files and the time budget list that differ from the compare base, committed or in the working tree."""
    paths = set()
    for args in (["git", "diff", "--name-only", compare_base(), "HEAD", "--"] + UNTOUCHED,
                 ["git", "diff", "--name-only", "HEAD", "--"] + UNTOUCHED,
                 ["git", "ls-files", "--others", "--exclude-standard", "--", UNTOUCHED[0]]):
        done = subprocess.run(args, cwd=ROOT, capture_output=True, text=True)
        if done.returncode != 0:
            print("BROKEN: git failed: " + done.stderr.strip())
            sys.exit(2)
        paths.update(line for line in done.stdout.splitlines() if line.strip())
    return sorted(paths)


def run_ids():
    results = tempfile.mkdtemp(prefix="s3-trx-")
    cmd = ["env"]
    for token in TOKENS:
        cmd += ["-u", token]
    cmd += ["dotnet", "test", "Lighthouse.Backend/Lighthouse.Backend.Tests", "-c", "Debug",
            "--filter", FILTER, "--logger", "trx", "--results-directory", results]
    done = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True)
    trx = glob.glob(os.path.join(results, "*.trx"))
    if not trx:
        print("BROKEN: no trx produced (build or setup failure), not a semantic result")
        print(done.stdout[-3000:])
        print(done.stderr[-2000:])
        sys.exit(2)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(trx[0]).getroot()
    classes = {}
    for unit in root.iterfind(".//t:UnitTest", ns):
        method = unit.find("t:TestMethod", ns)
        classes[unit.get("id")] = method.get("className").split(",")[0] + "." + unit.get("name")
    outcomes = {}
    for result in root.iterfind(".//t:UnitTestResult", ns):
        outcomes[classes[result.get("testId")]] = result.get("outcome")
    return outcomes


def main():
    observed = run_ids()
    if "--write-expected" in sys.argv:
        with open(EXPECTED, "w", encoding="utf-8") as handle:
            for test_id in sorted(observed):
                handle.write(f"{test_id}\t{observed[test_id]}\n")
        print(f"wrote {len(observed)} ids")
        return
    with open(EXPECTED, encoding="utf-8") as handle:
        expected = dict(line.rstrip("\n").split("\t") for line in handle if line.strip())
    other_ids = listed_by_any_slice()
    failed = 0
    for path in touched_paths():
        print(f"CHANGED  {path} (no production file or budget list may change)")
        failed += 1
    for problem in guard_problems():
        print(problem)
        failed += 1
    for test_id in sorted(set(expected) | set(observed)):
        want, got = expected.get(test_id), observed.get(test_id)
        if want is None and test_id in other_ids:
            continue
        if want is None:
            print(f"EXTRA    {test_id} ({got})")
            failed += 1
        elif got is None:
            print(f"MISSING  {test_id}")
            failed += 1
        elif got != want:
            print(f"OUTCOME  {test_id}: expected {want}, observed {got}")
            failed += 1
    print(f"observed={len(observed)} expected={len(expected)} differences={failed}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
