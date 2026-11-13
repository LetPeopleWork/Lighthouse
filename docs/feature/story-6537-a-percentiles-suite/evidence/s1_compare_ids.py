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
)


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
    failed = 0
    for test_id in sorted(set(expected) | set(observed)):
        want, got = expected.get(test_id), observed.get(test_id)
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
