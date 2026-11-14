#!/usr/bin/env python3
"""Checks the scope of the whole suite after its slices are pushed, which the per-slice oracles cannot.

Each sN_compare_ids.py compares production and the guard list with the point where HEAD leaves origin/main, so once a slice
is pushed that diff is empty and the check passes without looking at anything. Other stories land on main between the
slices, so a diff from the suite's first base would charge their production changes to this suite.

This script looks at the suite's own commits instead: every commit (merges bring in other work and are skipped) between
SUITE_BASE and HEAD that touches a path only this
suite works on. None of them may change production code or the test-time budget, and together they may only remove lines
of classes that left the host from the host-user guard list. Uncommitted changes to those files fail as well, so a
deliberate edit shows up at once. --self-test runs the same rules on made-up commits and exits non-zero if a rule fails to
fire. Exits 0 when the scope holds, 1 on any finding."""
import subprocess
import sys

SUITE_BASE = "3cc76466d"
SUITE_PATHS = [
    "docs/feature/story-6537-b-configure-suite/",
    "Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ConfigureOncePerConnection/",
]
GUARD = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs"
LEFT_THE_HOST = ["Slice09PortfoliosDependencyFieldDefaultScenarios"]
UNTOUCHED = ["Lighthouse.Backend/Lighthouse.Backend/", "Scripts/test-timings/test-time-budget.json"]


def git(*args):
    done = subprocess.run(["git", *args], capture_output=True, text=True)
    if done.returncode != 0:
        print("BROKEN: git " + " ".join(args) + ": " + done.stderr.strip())
        sys.exit(2)
    return done.stdout


def is_suite_commit(paths):
    return any(p.startswith(s) for p in paths for s in SUITE_PATHS)


def findings_for(commit, paths, guard_diff):
    """The rules for one commit of the suite: its changed paths and its -U0 diff of the guard file."""
    found = []
    for p in paths:
        if any(p == u or p.startswith(u) for u in UNTOUCHED):
            found.append(f"SCOPE    {commit} changes {p}")
    for line in guard_diff.splitlines():
        if line.startswith(("+++", "---", "@@", "diff", "index")):
            continue
        if line.startswith("+"):
            found.append(f"GUARD    {commit} adds a line to the host-user guard list: {line[1:].strip()}")
        elif line.startswith("-") and not any(name in line for name in LEFT_THE_HOST):
            found.append(f"GUARD    {commit} removes a class that still starts a host: {line[1:].strip()}")
    return found


def self_test():
    cases = [
        ("production edit", ["Lighthouse.Backend/Lighthouse.Backend/Models/Team.cs"], "", 1),
        ("budget edit", ["Scripts/test-timings/test-time-budget.json"], "", 1),
        ("guard line added", [GUARD], "+            \"Some.New.HostUser\",", 1),
        ("guard line of a kept class removed", [GUARD], "-            \"X.Slice06DefaultChangeUpdatesTheFollowersScenarios\",", 1),
        ("guard line of a class that left removed", [GUARD], "-            \"X.Slice09PortfoliosDependencyFieldDefaultScenarios\",", 0),
        ("test-only change", ["Lighthouse.Backend/Lighthouse.Backend.Tests/Models/EstimationBundleTest.cs"], "", 0),
    ]
    bad = 0
    for name, paths, guard_diff, want in cases:
        got = len(findings_for("selftest", paths, guard_diff))
        print(f"{'ok ' if got == want else 'BAD'}  {name}: {got} finding(s), expected {want}")
        bad += got != want
    if not is_suite_commit(["docs/feature/story-6537-b-configure-suite/evidence/x"]) or is_suite_commit(["Lighthouse.Frontend/a"]):
        print("BAD  suite-commit detection")
        bad += 1
    sys.exit(1 if bad else 0)


def main():
    if "--self-test" in sys.argv:
        self_test()
    findings, suite, others_on_guard = [], 0, []
    for commit in git("rev-list", "--reverse", "--no-merges", f"{SUITE_BASE}..HEAD").split():
        paths = [p for p in git("diff-tree", "--no-commit-id", "--name-only", "-r", commit).splitlines() if p]
        if not is_suite_commit(paths):
            if GUARD in paths:
                others_on_guard.append(commit[:9])
            continue
        suite += 1
        guard_diff = git("diff", "-U0", f"{commit}^", commit, "--", GUARD) if GUARD in paths else ""
        findings += findings_for(commit[:9], paths, guard_diff)
    for p in git("diff", "--name-only", "HEAD").splitlines():
        if any(p == u or p.startswith(u) for u in UNTOUCHED):
            findings.append(f"SCOPE    uncommitted change to {p}")
    with open(GUARD, encoding="utf-8") as handle:
        for line in handle:
            if any(name in line for name in LEFT_THE_HOST):
                findings.append(f"GUARD    a class that left the host is still on the guard list: {line.strip()}")
    for f in findings:
        print(f)
    if others_on_guard:
        print("INFO     commits of other work that changed the guard list: " + ", ".join(others_on_guard))
    print(f"suite commits={suite} findings={len(findings)}")
    sys.exit(1 if findings else 0)


if __name__ == "__main__":
    main()
