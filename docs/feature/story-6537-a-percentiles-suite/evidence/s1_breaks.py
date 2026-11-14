import subprocess, sys, re, os
R = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", ".."))
A = "Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/Application/"
W = A + "ProcessBehaviorSnapshotWriter.cs"
E = A + "RetentionEdge.cs"
M = A + "BaseMetricsService.cs"
UW = "FullyQualifiedName~FlowMetrics.Application.ProcessBehaviorSnapshotWriterTest."
UR = "FullyQualifiedName~FlowMetrics.Application.RetentionEdgeTest."
UT = "FullyQualifiedName~GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimits"
UF = "FullyQualifiedName~OverTimeHistoryFillerTest.ADayWorkedOut_IsWorkedOutFor"
F = "Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/Adapters/Background/OverTimeHistoryFiller.cs"
BREAKS = [
 ("B1 team family line removed (Arrivals)", W,
  "                new(ProcessBehaviorMetricType.Arrivals, lookbackDays, (startDate, endDate, asOf) => teamMetricsService.GetArrivalsProcessBehaviourChart(team, startDate, endDate, asOf)),\n", "", UW),
 ("B1b portfolio family line removed (FeatureSize)", W,
  "                new(ProcessBehaviorMetricType.FeatureSize, PortfolioLookbackDays, (startDate, endDate, asOf) => portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate, asOf)),\n", "", UW),
 ("B2 zero-band gate removed", W, "chart.Average == 0 && chart.UpperNaturalProcessLimit == 0", "chart.Average < 0", UW),
 ("B2b not-ready gate removed", W, "chart.Status != BaselineStatus.Ready", "chart.Status == (BaselineStatus)(-1)", UW),
 ("B3 retention edge anchored 25 days later (today) instead of the last refresh", E, "lastObservedOn.AddDays(-owner.DoneItemsCutoffDays)", "lastObservedOn.AddDays(25).AddDays(-owner.DoneItemsCutoffDays)", UR),
 ("B4 FillDayIfAbsent passes asOf null", W, "stored: null, asOf: day);", "stored: null, asOf: null);", UW),
 ("B4b FillDayIfAbsent window ends today", W, "ownerId, ownerType, family, day, InstanceCalendar.AsUtcMidnight(day), stored: null", "ownerId, ownerType, family, day, clock.TodayAsUtcMidnight, stored: null", UW),
 ("B5 pinned stretch ignored by the edge guard", E, "ReachesPastTheEdge(pinnedStretchStartsOn ?? windowStart)", "pinnedStretchStartsOn is null && ReachesPastTheEdge(windowStart)", UR),
 ("B7 history fill loop reaches one family only", F, "foreach (var family in target.ProcessBehaviorFamilies)", "foreach (var family in target.ProcessBehaviorFamilies.Take(1))", UF),
 ("B6 pinned baseline ignored (display window used)", M, "return new ResolvedBaseline(configuredStart, configuredEnd, true, validation);", "return new ResolvedBaseline(displayStart, displayEnd, true, validation);", UT),
]
BREAKS.append(("B8 a day that already carries limits is read again", W,
  "if (StoredRow(ownerId, ownerType, family.MetricType, day) != null)", "if (StoredRow(ownerId, ownerType, family.MetricType, day) == null)", UW))
BREAKS.append(("B9 a refused row is taken as stored when any family of that day is stored", W,
  "snapshot.MetricType == metricType &&", "(snapshot.MetricType == metricType || snapshot.MetricType == ProcessBehaviorMetricType.Wip) &&", UW))
BREAKS.append(("B10 a refused row is thrown on even when another copy stored it", W,
  "StoredRow(row.OwnerId, row.OwnerType, row.MetricType, row.RecordedAt) != null;", "StoredRow(row.OwnerId, row.OwnerType, row.MetricType, row.RecordedAt) == null;", UW))
only = sys.argv[1:] 
for name, path, old, new, flt in BREAKS:
    if only and name.split()[0] not in only: continue
    full = os.path.join(R, path)
    src = open(full, encoding="utf-8").read()
    assert src.count(old) == 1, (name, src.count(old))
    open(full, "w", encoding="utf-8").write(src.replace(old, new))
    try:
        p = subprocess.run(["env","-u","LinearAPIKey","dotnet","test","Lighthouse.Backend/Lighthouse.Backend.Tests","-c","Debug","--filter",flt,"--no-restore"],cwd=R,capture_output=True,text=True)
        out = p.stdout + p.stderr
        print("=== " + name)
        for l in out.splitlines():
            if re.match(r"\s*(Failed |.*error |Passed!|Failed!)", l) or "Assert" in l[:60] and False:
                print(l.strip()[:260])
        m = re.findall(r"(Failed!|Passed!).*", out); print(m[-1] if m else "NO SUMMARY")
    finally:
        subprocess.run(["git","checkout","--",path],cwd=R,check=True)
