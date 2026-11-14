#!/usr/bin/env python3
"""Runs the Slice07 host class and the unit classes that now prove its rules (Debug, tokens removed),
compares their sorted test ids and outcomes with s1-expected-ids.txt, prints every difference and
exits non-zero on any difference or failed test."""
import glob
import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
EXPECTED = os.path.join(os.path.dirname(os.path.abspath(__file__)), "s1-expected-ids.txt")
TOKENS = [
    "AzureDevOpsLighthouseIntegrationTestToken", "JiraLighthouseIntegrationTestToken",
    "JiraLighthouseRestrictedIntegrationTestToken", "JiraScopedTokenIntegrationTestToken",
    "ServiceNowLighthouseIntegrationTestToken", "LinearAPIKey", "AzureDevOpsLighthouseE2ETestToken",
]
FILTER = (
    "FullyQualifiedName~Slice07WhereTheLimitsActuallyMovedTest"
    "|FullyQualifiedName~FlowMetrics.Application.ProcessBehaviorSnapshotWriterTest."
    "|FullyQualifiedName~FlowMetrics.Application.RetentionEdgeTest."
    "|FullyQualifiedName~TeamMetricsServiceTests.GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimits"
    "|FullyQualifiedName~PortfolioMetricsServiceTests.GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimits"
    "|FullyQualifiedName~OverTimeHistoryFillerTest.ADayWorkedOut_IsWorkedOutFor"
)
SLICE_BASE = "46b01fa37"
UNTOUCHED = [
    "Lighthouse.Backend/Lighthouse.Backend/",
    "Scripts/test-timings/test-time-budget.json",
]
GUARD = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs"
# The two classes a later slice of this suite takes off the host-user guard list. Their lines may go; nothing else may change.
LEFT_THE_HOST_LATER = [
    "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice08NothingToShowTest",
    "Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice10OnlyWhatTheTeamStillKeepsTest",
]
ALL_EXPECTED = ["s1-expected-ids.txt", "s2-expected-ids.txt", "s3-expected-ids.txt", "s4-expected-ids.txt"]


def guard_problems():
    """The guard list may not grow, and may only lose the lines of the classes that later slices of this suite move off the host."""
    problems = []
    done = subprocess.run(["git", "diff", "-U0", SLICE_BASE, "--", GUARD], cwd=ROOT, capture_output=True, text=True)
    if done.returncode != 0:
        print("BROKEN: git failed: " + done.stderr.strip())
        sys.exit(2)
    for line in done.stdout.splitlines():
        if line.startswith(("+++", "---", "@@", "diff", "index")):
            continue
        if line.startswith("+"):
            problems.append(f"GUARD    a line was added to the host-user guard list: {line[1:].strip()}")
        elif line.startswith("-") and not any(f'"{name}"' in line for name in LEFT_THE_HOST_LATER):
            problems.append(f"GUARD    a line other than a later slice's class left the host-user guard list: {line[1:].strip()}")
    return problems


def other_slices_ids():
    """Every test id any slice of this suite lists. A row in a shared unit class that another slice owns is not EXTRA here."""
    here = os.path.dirname(os.path.abspath(__file__))
    ids = set()
    for name in ALL_EXPECTED:
        with open(os.path.join(here, name), encoding="utf-8") as handle:
            ids.update(line.split("\t")[0] for line in handle if line.strip())
    return ids


def touched_paths():
    """Production files and the time budget list that differ from the slice base,
    committed or in the working tree."""
    paths = set()
    for args in (["git", "diff", "--name-only", SLICE_BASE, "HEAD", "--"] + UNTOUCHED,
                 ["git", "diff", "--name-only", "HEAD", "--"] + UNTOUCHED,
                 ["git", "ls-files", "--others", "--exclude-standard", "--", UNTOUCHED[0]]):
        done = subprocess.run(args, cwd=ROOT, capture_output=True, text=True)
        if done.returncode != 0:
            print("BROKEN: git failed: " + done.stderr.strip())
            sys.exit(2)
        paths.update(line for line in done.stdout.splitlines() if line.strip())
    return sorted(paths)


def run_ids():
    results = tempfile.mkdtemp(prefix="s1-trx-")
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
    with open(EXPECTED, encoding="utf-8") as handle:
        expected = dict(line.rstrip("\n").split("\t") for line in handle if line.strip())
    observed = run_ids()
    listed_by_any_slice = other_slices_ids()
    failed = 0
    for path in touched_paths():
        print(f"CHANGED  {path} (no production file or budget list may change)")
        failed += 1
    for problem in guard_problems():
        print(problem)
        failed += 1
    for test_id in sorted(set(expected) | set(observed)):
        want, got = expected.get(test_id), observed.get(test_id)
        if want is None and test_id in listed_by_any_slice:
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
