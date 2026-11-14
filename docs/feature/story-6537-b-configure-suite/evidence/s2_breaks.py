import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
J = B + "Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs"
IX = B + "Services/Implementation/WorkTrackingConnectors/Jira/IssueExtensions.cs"
DSS = B + "Services/Implementation/Dependencies/DependencySourceSelector.cs"
RLS = B + "Services/Implementation/RefreshLogService.cs"
DTO = B + "API/DTO/RefreshLogDto.cs"
PU = B + "Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs"
WIS = B + "Services/Implementation/WorkItems/WorkItemService.cs"

UNITS = "|".join("FullyQualifiedName~" + name for name in (
    "JiraNamedDependencyLinkTest.", "RefreshLogServiceTest.", "RefreshLogDtoTest.", "DependencySourceSelectorTest.",
    "WorkItemServiceTest.UpdateFeaturesForPortfolio_FullRefreshWhere",
    "PortfolioUpdaterTest.TriggerUpdate_ACheapRefresh_LeavesTheFindingsOfItsRowUnsaid",
    "PortfolioUpdaterTest.TriggerUpdate_AWholeDownload",
))

BREAKS = [
    ("H1 every Feature carries a finding object of its own, so one refresh tells it several times", J,
     "features.ForEach(feature => feature.RefreshFindings = [.. feature.RefreshFindings, nothingMatched]);",
     "features.ForEach(feature => feature.RefreshFindings = [.. feature.RefreshFindings, nothingMatched with { Seen = [.. namesSeen] }]);"),
    ("H2 the refresh records one finding per Feature", WIS,
     ".DistinctBy(finding => (finding.Kind, finding.Setting, finding.Named))", ""),
    ("H3 the log line for a named link that matched nothing is worded differently", WIS,
     "and no Feature carries that link.", "and none carries it."),
    ("H4 a named link that one Feature carries is still told as matching nothing", J,
     "if (features.Exists(feature => feature.DependsOnReferences.Count > 0))",
     "if (features.Exists(feature => feature.DependsOnReferences.Count > 99))"),
    ("H5 a Portfolio whose Features carry no links is told a link matched nothing", J,
     "            if (namesSeen.Count == 0)\n            {\n                return;",
     "            if (namesSeen.Count == 99)\n            {\n                return;"),
    ("H6 a Dependency Field naming an ordinary field is told its links matched nothing", J,
     "var namesALinkThatCanMatch = theDependencyField.NamesALinkTypeWhoseWaitingEndIsClear;",
     "var namesALinkThatCanMatch = theDependencyField.NamesALinkTypeWhoseWaitingEndIsClear || !theDependencyField.NamesALinkTypeWhoseWaitingEndIsUnclear;"),
    ("H7 a Dependency Field naming an ordinary field is warned about links in the log", J,
     "if (!namesALinkThatCanMatch && !DependencySourceSelector.ReadsTheTrackersOwnLink(portfolio))",
     "if (!namesALinkThatCanMatch && DependencySourceSelector.ReadsTheTrackersOwnLink(portfolio))"),
    ("H8 the oldest run that recorded findings is the one shown", DTO,
     "entity => entity.MaxBy(row => (row.ExecutedAt, row.Id))!", "entity => entity.MinBy(row => (row.ExecutedAt, row.Id))!"),
    ("H9 a run that recorded no findings counts as the latest full run", DTO,
     ".Where(row => row.Findings != null)", ".Where(row => true)"),
    ("H10 the finding is labelled with the time of the row that shows it", DTO,
     "LatestFullRunAt = latestWithFindings?.ExecutedAt,", "LatestFullRunAt = row.ExecutedAt,"),
    ("H11 an entity shows the findings of another entity", DTO,
     "latestWithFindings.GetValueOrDefault((row.Type, row.EntityId))", "latestWithFindings.Values.FirstOrDefault()"),
    ("H12 a cheap refresh records findings on its row", PU,
     "Findings = success && outcome.Mode == SyncMode.Full ?", "Findings = success ?"),
    ("H13 the history prunes the full run that recorded findings", RLS,
     ".Where(r => r != newestWithFindings)", ".Where(r => r == newestWithFindings)"),
    ("H14 the history keeps the newest row without findings instead", RLS,
     "history.Find(r => r.Findings != null)", "history.Find(r => r.Findings == null)"),
    ("K1 a phrase typed as one end reads the other end", J,
     "return typedAsTheInwardEndReads ? LinkDirection.Inward : LinkDirection.Outward;",
     "return typedAsTheInwardEndReads ? LinkDirection.Outward : LinkDirection.Inward;"),
    ("K2 a phrase typed exactly as one end reads does not pick its end", J,
     "if (typedAsTheInwardEndReads != typedAsTheOutwardEndReads)", "if (typedAsTheInwardEndReads == typedAsTheOutwardEndReads)"),
    ("K3 a renamed phrase is matched case-sensitively", J,
     "private static bool Reads(string label, string reference)\n                => label.Length > 0 && string.Equals(label, reference, StringComparison.OrdinalIgnoreCase);",
     "private static bool Reads(string label, string reference)\n                => label.Length > 0 && string.Equals(label, reference, StringComparison.Ordinal);"),
    ("K4 a link type wins over a field of the same name", J,
     ".Where(reference => reference.Value.ResolvedToNothing)", ".Where(reference => true)"),
    ("K5 a name that points both ways reads both ends", J,
     "{ NamesALinkTypeWhoseWaitingEndIsUnclear: true } => [],",
     "{ NamesALinkTypeWhoseWaitingEndIsUnclear: true } => issue.Fields.ExtractDependencyReferencesFromLinksOfType(theDependencyField.LinkTypeName, theDependencyField.DependencyDirection),"),
    ("K6 a name that points both ways raises no finding", J,
     "if (!theDependencyField.NamesALinkTypeWhoseWaitingEndIsUnclear)", "if (theDependencyField.NamesALinkTypeWhoseWaitingEndIsUnclear)"),
    ("K7 the two phrases are offered inward end first", J,
     "[answeringType.Outward, answeringType.Inward]", "[answeringType.Inward, answeringType.Outward]"),
    ("K8 a finding names another setting", J,
     "return new RefreshFinding(kind, RefreshFindingSetting.DependencyField, named, seen);",
     "return new RefreshFinding(kind, RefreshFindingSetting.SizeEstimateField, named, seen);"),
    ("K9 a finding does not quote what was typed", J,
     "return new RefreshFinding(kind, RefreshFindingSetting.DependencyField, named, seen);",
     "return new RefreshFinding(kind, RefreshFindingSetting.DependencyField, named.ToUpperInvariant(), seen);"),
    ("K10 a phrase both ends share is read as the inward end", J,
     "if (Reads(Name, reference) || readsInward == readsOutward)",
     "if (Reads(Name, reference) || (readsInward == readsOutward && !readsInward))"),
    ("K11 naming a link adds it to is blocked by", DSS,
     "return AsReferences(fromALinkTypeThePortfolioNamed, DependencySource.TrackerLink);",
     "return AsReferences([.. fromTheTrackersOwnLink, .. fromALinkTypeThePortfolioNamed], DependencySource.TrackerLink);"),
    ("K12 a field's keys are recorded as read from a tracker link", DSS,
     "AsReferences(DependencyFieldReferences.In(typedIntoTheField), DependencySource.PortfolioField)",
     "AsReferences(DependencyFieldReferences.In(typedIntoTheField), DependencySource.TrackerLink)"),
    ("K13 an ordinary field is read as a link type that matched nothing", J,
     "                _ => null,\n            };\n\n            return DependencySourceSelector.TheDependenciesOf(",
     "                _ => [],\n            };\n\n            return DependencySourceSelector.TheDependenciesOf("),
    ("K14 a Portfolio naming nothing no longer reads is blocked by", DSS,
     "=> !portfolio.DependencyOverrideAdditionalFieldDefinitionId.HasValue;", "=> false;"),
    ("K15 is blocked by is read from the wrong links", IX,
     "if (!InwardNameOf(link).Equals(BlockedByLinkName, StringComparison.OrdinalIgnoreCase))",
     "if (InwardNameOf(link).Equals(BlockedByLinkName, StringComparison.OrdinalIgnoreCase))"),
    ("K16 links of another type are read too", IX,
     ".Where(link => Reads(LabelOf(link, JiraFieldNames.NamePropertyName), linkTypeName))", ".Where(link => true)"),
    ("K17 naming a link asks Jira for its link types again", J,
     "StopUnlessTheDependencyFieldCouldBeLookedUp(portfolio, theDependencyField, resolution.LinkTypes);\n            WarnWhen",
     "StopUnlessTheDependencyFieldCouldBeLookedUp(portfolio, theDependencyField, resolution.LinkTypes);\n            if (theDependencyField.NamesALinkType) { await ResolveTheAdditionalFieldReferences(portfolio.WorkTrackingSystemConnection, LinkTypesMayAnswer.EveryReferenceLeftUnresolved); }\n            WarnWhen"),
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
                            "-c", "Debug", "--filter", UNITS], cwd=R, capture_output=True, text=True, timeout=900)
        out = p.stdout + p.stderr
        print("=== " + name, flush=True)
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                print(line.strip()[:220])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)", flush=True)
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
