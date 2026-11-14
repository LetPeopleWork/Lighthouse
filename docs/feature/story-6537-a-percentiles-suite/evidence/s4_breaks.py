import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
FM = "Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/"
F = FM + "Adapters/Background/OverTimeHistoryFiller.cs"
T = FM + "Application/OverTimeFillTarget.cs"
E = FM + "Application/RetentionEdge.cs"
W = FM + "Application/PercentileSnapshotWriter.cs"

UNITS = (
    "FullyQualifiedName~FlowMetrics.Application.OverTimeFillTargetTest."
    "|FullyQualifiedName~FlowMetrics.Application.RetentionEdgeTest."
    "|FullyQualifiedName~FlowMetrics.Application.PercentileSnapshotWriterTest."
    "|FullyQualifiedName~FlowMetrics.Application.ReconstructionMemoTest."
    "|FullyQualifiedName~ProcessBehaviorRecordingHandlerTests.TeamDataRefreshed_ReadsTheSameAsOfTodayWindowTheWidgetUses"
    "|FullyQualifiedName~Adapters.Background.OverTimeHistoryFillerTest."
)

TEAM_EDGE = "retention.NoEarlierThanTheEdge(EarliestFinishedDay(\n                    clock,\n                    workItems"
PORTFOLIO_EDGE = "retention.NoEarlierThanTheEdge(EarliestFinishedDay(\n                    clock,\n                    deliveries"

BREAKS = [
    ("A1 the filler works out days before the first supported day", F,
     "=> earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport || day > lastObservedOn;",
     "=> earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport.Value.AddDays(-1) || day > lastObservedOn;"),
    ("A2 the filler works out days when nothing has ever finished", F,
     "=> earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport || day > lastObservedOn;",
     "=> day < earliestDayTheItemsSupport || day > lastObservedOn;"),
    ("A3 the filler works out days after the last refresh", F,
     "=> earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport || day > lastObservedOn;",
     "=> earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport || day > lastObservedOn.AddDays(1);"),
    ("A4 days stepped over spend the pass allowance", F,
     "                    continue;\n                }\n\n                progress.TriedADay();",
     "                    progress.TriedADay();\n                    continue;\n                }\n\n                progress.TriedADay();"),
    ("B1 the team target no longer guards its percentile families", T,
     "[.. Application.PercentileFamilies.For(team, metrics).Select(retention.Guard)]",
     "[.. Application.PercentileFamilies.For(team, metrics)]"),
    ("B2 the portfolio target no longer guards its percentile families", T,
     "[.. Application.PercentileFamilies.For(portfolio, metrics).Select(retention.Guard)]",
     "[.. Application.PercentileFamilies.For(portfolio, metrics)]"),
    ("B3 the team target no longer guards its limit families", T,
     "FamiliesFor(team).Select(retention.Guard)", "FamiliesFor(team)"),
    ("B4 the portfolio target no longer guards its limit families", T,
     "FamiliesFor(portfolio).Select(retention.Guard)", "FamiliesFor(portfolio)"),
    ("B5 the team target starts its history before the edge", T,
     TEAM_EDGE, "(EarliestFinishedDay(\n                    clock,\n                    workItems"),
    ("B6 the portfolio target starts its history before the edge", T,
     PORTFOLIO_EDGE, "(EarliestFinishedDay(\n                    clock,\n                    deliveries"),
    ("C1 a window starting on the edge is refused", E,
     "DateOnly.FromDateTime(stretchStartsOn) < edge", "DateOnly.FromDateTime(stretchStartsOn) <= edge"),
    ("C2 limits are judged on the day's window end and not its start", E,
     "ReachesPastTheEdge(pinnedStretchStartsOn ?? windowStart)", "ReachesPastTheEdge(pinnedStretchStartsOn ?? windowEnd)"),
    ("C3 limits judge a pinned stretch only when it starts after the window", E,
     "ReachesPastTheEdge(pinnedStretchStartsOn ?? windowStart)",
     "ReachesPastTheEdge(new[] { pinnedStretchStartsOn ?? windowStart, windowStart }.Max())"),
    ("C4 the edge is drawn a day late", E,
     "lastObservedOn.AddDays(-owner.DoneItemsCutoffDays)", "lastObservedOn.AddDays(1 - owner.DoneItemsCutoffDays)"),
    ("C5 the first day is the oldest finished day even past the edge", E,
     "earliestFinishedDay > oldestDayStillKept ? earliestFinishedDay : oldestDayStillKept", "earliestFinishedDay"),
    ("C6 an owner that keeps work for ever gets an edge", E,
     "owner.DoneItemsCutoffDays > 0 ?", "owner.DoneItemsCutoffDays >= 0 ?"),
    ("C7 an owner with nothing finished gets a first day", E,
     "if (earliestFinishedDay is null || oldestDayStillKept is null)", "if (oldestDayStillKept is null)"),
    ("D1 the window a day reads starts a day early", W,
     "readPercentiles(windowEnd.AddDays(-horizon), windowEnd)", "readPercentiles(windowEnd.AddDays(-horizon - 1), windowEnd)"),
]

only = sys.argv[1:]
for name, path, old, new, *own in BREAKS:
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
                            "-c", "Debug", "--filter", own[0] if own else UNITS], cwd=R, capture_output=True, text=True,
                           timeout=900)
        out = p.stdout + p.stderr
        print("=== " + name)
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                print(line.strip()[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)")
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
