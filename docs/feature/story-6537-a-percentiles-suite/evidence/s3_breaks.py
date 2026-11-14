import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
FM = "Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/"
W = FM + "Application/PercentileSnapshotWriter.cs"
F = FM + "Adapters/Background/OverTimeHistoryFiller.cs"
G = FM + "Application/OverTimeGapReconciler.cs"
T = FM + "Application/OverTimeFillTarget.cs"
Q = FM + "Application/GapAskingPercentilesOverTimeSeriesQuery.cs"
GATE = "Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DatabaseManagement/DatabaseMaintenanceGate.cs"

UNITS = (
    "FullyQualifiedName~FlowMetrics.Application.PercentileSnapshotWriterTest."
    "|FullyQualifiedName~FlowMetrics.Application.OverTimeGapReconcilerTest."
    "|FullyQualifiedName~FlowMetrics.Application.OverTimeFillTargetTest."
    "|FullyQualifiedName~FlowMetrics.Application.GapAskingSeriesQueryTest."
    "|FullyQualifiedName~Adapters.Background.OverTimeHistoryFillerTest."
    "|FullyQualifiedName~Services.Implementation.DatabaseMaintenanceGateTest."
    "|FullyQualifiedName~TwoCopiesFillingTheSameDayAtThePortTest"
)

BREAKS = [
    ("S1 filler walks only the first asked day", F,
     "foreach (var day in request.CandidateDays)", "foreach (var day in request.CandidateDays.Take(1))"),
    ("S2 reconciler asks for every second missing day", G,
     "day = day.AddDays(1))", "day = day.AddDays(2))"),
    ("S3 reconciler ignores the days the chart holds", G,
     "if (held.Contains(day) || memo.NoPassCanWrite(ownerId, ownerType, day))",
     "if (held.Count > 1000000 && held.Contains(day) || memo.NoPassCanWrite(ownerId, ownerType, day))"),
    ("S4 gap-asking query passes no held days", Q,
     "[.. held.Select(day => day.RecordedAt)]", "[]"),
    ("S6 filler walks one day past the last observation", F,
     "day > lastObservedOn;", "day > lastObservedOn.AddDays(1);"),
    ("S6b team last observation read one day late", T,
     "var lastObservedOn = clock.ToInstanceDay(team.UpdateTime);", "var lastObservedOn = clock.ToInstanceDay(team.UpdateTime.AddDays(1));"),
    ("S7 filler walks one day before the first supported day", F,
     "day < earliestDayTheItemsSupport ||", "day < earliestDayTheItemsSupport.Value.AddDays(-1) ||"),
    ("S7b earliest finished day taken from the latest item", T,
     "var earliest = finishedInstants.Min();", "var earliest = finishedInstants.Max();"),
    ("S8 cap of a pass raised to 91 days", F,
     "private const int MostDaysOnePassWorksOut = 90;", "private const int MostDaysOnePassWorksOut = 91;"),
    ("S8b cap check let one more day through", F,
     "if (progress.DaysTried >= MostDaysOnePassWorksOut)", "if (progress.DaysTried > MostDaysOnePassWorksOut)"),
    ("S9b time budget stops a pass before its first day", F,
     "progress.DaysTried > 0 && timeSpentOnThisPass", "progress.DaysTried >= 0 && timeSpentOnThisPass"),
    ("S10 a day with nothing to report gets a row (fill and daily recording)", W,
     "if (readings.AreEmpty)", "if (readings.AreEmpty && readings.P50 < 0)"),
    ("S10b the daily recording writes a day with nothing to report", W,
     "WriteUnlessThereWasNothingToReport(\n                    ownerId, ownerType, metricType, horizon, day,\n                    ComputeDay(day, horizon, readPercentiles),\n                    StoredRow(ownerId, ownerType, metricType, horizon, day));",
     "snapshotRepository.Add(NewRow(ownerId, ownerType, metricType, horizon, day, ComputeDay(day, horizon, readPercentiles)));"),
    ("S11 writer rewrites a day that already carries a row", W,
     "if (StoredRow(ownerId, ownerType, metricType, horizon, day) != null)",
     "if (StoredRow(ownerId, ownerType, metricType, horizon, day) != null && horizon > 1000)"),
    ("S12 a day filled in afterwards is read as of today", W,
     "ComputeDay(day, horizon, readPercentiles),\n                    stored: null);", "ComputeDay(clock.Today, horizon, readPercentiles),\n                    stored: null);"),
    ("S12b the window of a day ends a day late", W,
     "var windowEnd = InstanceCalendar.AsUtcMidnight(day);", "var windowEnd = InstanceCalendar.AsUtcMidnight(day).AddDays(1);"),
    ("S13 the gate ignores a pass in flight", GATE,
     "if (historyFill?.HasPassInFlight == true)", "if (historyFill?.HasPassInFlight == true && statusStore == null)"),
    ("S13b the filler never counts its pass as running", F,
     "Interlocked.Increment(ref passesRunning);", "Interlocked.Add(ref passesRunning, 0);"),
    ("S13c the filler keeps counting a finished pass as running", F,
     "Interlocked.Decrement(ref passesRunning);", "Interlocked.Add(ref passesRunning, 0);"),
    ("S14 a pass does not stand down before it starts", F,
     "\n            if (MaintenanceNeedsTheDatabase(maintenance))\n            {",
     "\n            if (MaintenanceNeedsTheDatabase(maintenance) && maintenance == null)\n            {"),
    ("S14b a pass does not stand down between two days", F,
     "\n                if (MaintenanceNeedsTheDatabase(maintenance))\n                {",
     "\n                if (MaintenanceNeedsTheDatabase(maintenance) && maintenance == null)\n                {"),
    ("S14c a running maintenance operation is not noticed", F,
     "maintenance.IsMaintenanceOperationActive || maintenance.IsMaintenanceOperationWaiting",
     "maintenance.IsMaintenanceOperationWaiting"),
    ("S14d a waiting operator is not noticed", F,
     "maintenance.IsMaintenanceOperationActive || maintenance.IsMaintenanceOperationWaiting",
     "maintenance.IsMaintenanceOperationActive"),
    ("S15 a row refused because another copy stored the day is thrown on", W,
     "refusedEntity is PercentilesOverTimeSnapshot row &&", "refusedEntity is PercentilesOverTimeSnapshot row && row.Horizon == -1 &&"),
    ("S5 reconciler asks even when nothing is missing", G,
     "if (missing.Count == 0)", "if (missing.Count == 0 && ownerId < 0)"),
    ("S9 time budget never reached", F,
     "timeSpentOnThisPass.Elapsed >= longestThisPassMayRun", "timeSpentOnThisPass.Elapsed >= longestThisPassMayRun && progress.DaysTried < 0"),
    ("S16 a day that cannot be written takes the rest of the pass down", F,
     "catch (Exception failure)\n            {\n                return failure;", "catch (Exception failure) when (commit == null)\n            {\n                return failure;"),
    ("S17 an owner stays marked as asked after its pass is over", F,
     "            finally\n            {\n                Forget(request.Key);\n            }",
     "            finally\n            {\n                if (request.OwnerId < 0)\n                {\n                    Forget(request.Key);\n                }\n            }"),
    ("S18 a day written in full is not remembered as worked out", F,
     "            memo.TheWalkHasAlreadyWorkedOut(request.OwnerId, request.OwnerType, day);",
     "            if (request.OwnerId < 0)\n            {\n                memo.TheWalkHasAlreadyWorkedOut(request.OwnerId, request.OwnerType, day);\n            }"),
    ("S19 a reading above zero on one percentile only counts as nothing to report", W,
     "public bool AreEmpty => P50 == 0 && P70 == 0 && P85 == 0 && P95 == 0;",
     "public bool AreEmpty => P50 == 0;"),
    ("S20 a reading above zero on the first percentile only counts as nothing to report", W,
     "public bool AreEmpty => P50 == 0 && P70 == 0 && P85 == 0 && P95 == 0;",
     "public bool AreEmpty => P70 == 0 && P85 == 0 && P95 == 0;"),
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
            if re.match(r"\s*(Failed [A-Za-z]|.*error CS)", line):
                print(line.strip()[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)")
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
