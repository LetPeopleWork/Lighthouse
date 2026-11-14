import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
CON = B + "Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs"
EXT = B + "Services/Implementation/WorkTrackingConnectors/AzureDevOps/WorkItemExtensions.cs"
LIST = B + "Services/Implementation/WorkTrackingConnectors/AzureDevOps/RelationTypeListing.cs"
TEAMUP = B + "Services/Implementation/BackgroundServices/Update/TeamUpdater.cs"

UNITS = "FullyQualifiedName~AzureDevOpsNamedRelationEndTest.|FullyQualifiedName~TeamUpdaterTest.TriggerUpdate_ACheapRefresh|FullyQualifiedName~TeamUpdaterTest.TriggerUpdate_TheRefreshFound"

KEPT = "        private static RelationTypeListing? TheKeptListing;\n\n        private async Task<AdditionalFieldResolution> ResolveTheAdditionalFields("
RESOLVE = "        private async Task<AdditionalFieldResolution> ResolveTheAdditionalFields("
RT_DECL = "RelationTypeListing? relationTypes = null;"
RT_LIST = "relationTypes ??= await TheRelationTypesTheOrganisationLists(witClient, cancellationToken);"
VAL_LIST = "var relationTypes = await TheRelationTypesTheOrganisationLists(witClient, CancellationToken.None);\n            var resolvedToNothing"

BREAKS = [
    ("A1 naming one end reads both ends of the relation", [(EXT,
        "if (!string.Equals(relation.Rel, waitingEnd, StringComparison.OrdinalIgnoreCase))",
        "if (relation.Rel is null || !relation.Rel.StartsWith(waitingEnd.Split('-')[0], StringComparison.OrdinalIgnoreCase))")]),
    ("A2 the Predecessor link still counts once an end is named", [(CON,
        ": adoWorkItem.ExtractDependencyReferences(namedRelationEnd);",
        ": adoWorkItem.ExtractDependencyReferences().Concat(adoWorkItem.ExtractDependencyReferences(namedRelationEnd)).ToList();")]),
    ("A3 a Parent Override Field stops the relations being downloaded", [(CON,
        "&& !dependencies.AreRead)", "&& true)")]),
    ("A4 the hierarchy counts among the relation types seen", [(CON,
        " && !ResourceLinks.Contains(rel) && !HierarchyLinks.Contains(rel))", " && !ResourceLinks.Contains(rel))")]),
    ("A5 resource links count among the relation types seen", [(CON,
        " && !ResourceLinks.Contains(rel) && !HierarchyLinks.Contains(rel))", " && !HierarchyLinks.Contains(rel))")]),
    ("A6 Features carrying no relations at all still say nothing matched", [(CON,
        "relations.RelationTypesSeen.Count > 0 && relations.DependencyReferences", "relations.DependencyReferences")]),
    ("A7 a relation type named without its end is not recognised", [(CON,
        "if (bothEnds.Count > 0)", "if (bothEnds.Count > 99)")]),
    ("A8 a relation type without direction is read as having one", [(CON,
        "if (relationTypes.HasNoDirection(relationEnd))", "if (!relationTypes.HasNoDirection(relationEnd))")]),
    ("A9 the relation types are listed again for every reference", [(CON, RT_LIST, "relationTypes = await TheRelationTypesTheOrganisationLists(witClient, cancellationToken);")]),
    ("A10 a refresh keeps the relation type listing between calls", [
        (CON, RESOLVE, KEPT),
        (CON, RT_DECL, "RelationTypeListing? relationTypes = TheKeptListing;"),
        (CON, RT_LIST, "relationTypes ??= (TheKeptListing ??= await TheRelationTypesTheOrganisationLists(witClient, cancellationToken));")]),
    ("A11 Validate keeps the relation type listing between calls", [
        (CON, RESOLVE, KEPT),
        (CON, VAL_LIST, "var relationTypes = TheKeptListing ??= await TheRelationTypesTheOrganisationLists(witClient, CancellationToken.None);\n            var resolvedToNothing")]),
    ("A12 Validate lists the relation types twice", [(CON, VAL_LIST,
        "var relationTypes = await TheRelationTypesTheOrganisationLists(witClient, CancellationToken.None);\n            _ = await TheRelationTypesTheOrganisationLists(witClient, CancellationToken.None);\n            var resolvedToNothing")]),
    ("A13 Validate lists the relation types even when every reference is a field", [(CON,
        "if (notAField.Count == 0)", "if (notAField.Count == -1)")]),
    ("A14 a relation type is matched in the case it is written", [(LIST,
        "string.Equals(end, reference, StringComparison.OrdinalIgnoreCase)", "string.Equals(end, reference, StringComparison.Ordinal)")]),
    ("A15 links to anything but a work item are accepted as relation types", [(LIST,
        "var workItemLinks = listed.Where(type => !IsAResourceLink(type)).ToList();", "var workItemLinks = listed.ToList();")]),
    ("A16 an answer without a list is not read as an empty one", [(CON,
        "await witClient.GetRelationTypesAsync(cancellationToken: cancellationToken) ?? []", "await witClient.GetRelationTypesAsync(cancellationToken: cancellationToken)")]),
    ("A17 a listing that ran out of time is not read as unreadable", [(CON,
        "or HttpRequestException or TimeoutException", "or HttpRequestException")]),
    ("A18 a listing that never reached Azure DevOps is not read as unreadable", [(CON,
        "or HttpRequestException or TimeoutException", "or TimeoutException")]),
    ("A19 a listing cancelled by nobody is not read as unreadable", [(CON,
        "|| (failure is OperationCanceledException && !cancellationToken.IsCancellationRequested))", "|| false)")]),
    ("A20 a refused listing is not handled", [(CON,
        "catch (Exception failure) when (failure is VssException or HttpRequestException", "catch (Exception failure) when (failure is HttpRequestException")]),
    ("A21 a definition nobody names is looked up among the relation types", [(CON,
        "if (!namedByASetting.Contains(additionalFieldDefinition.Id))", "if (namedByASetting.Contains(additionalFieldDefinition.Id) && namedByASetting.Count < 0)")]),
    ("A22 a relation type reference is reported as a missing field", [(CON,
        "if (resolution.ResolvesToARelationType(additionalFieldDefinition, relationTypes))",
        "if (resolution.ResolvesToARelationType(additionalFieldDefinition, relationTypes) && relationTypes.Ends.Count < 0)")]),
    ("A23 a field called like a relation type end is not read as the field", [(CON,
        "if (!string.IsNullOrEmpty(fieldReference))\n                {\n                    continue;",
        "if (!string.IsNullOrEmpty(fieldReference) && relationTypes is not null)\n                {\n                    continue;")]),
    ("A24 a relation type named as a parent source is not said to be unreadable", [(CON,
        "if (resolution.NamesARelationType(definitionId))\n                {\n                    yield return",
        "if (resolution.NamesARelationType(definitionId) && entries.Count < 0)\n                {\n                    yield return")]),
    ("A25 a relation type named as a parent source reads as a value", [(CON,
        "return ParentReading.CannotBeRead;", "return ParentReading.NoValue;")]),
    ("A26 Size Estimate Field naming a relation type is not reported", [(CON,
        "portfolio.SizeEstimateAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere", "null, RefreshFindingKind.RelationTypeCannotBeReadHere")]),
    ("A27 Feature Owner Field naming a relation type is not reported", [(CON,
        "portfolio.FeatureOwnerAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere", "null, RefreshFindingKind.RelationTypeCannotBeReadHere")]),
    ("A28 Estimation field naming a relation type is not reported", [(CON,
        "owner.EstimationAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere", "null, RefreshFindingKind.RelationTypeCannotBeReadHere")]),
    ("A29 a name no field carries is not warned about", [(CON,
        "No field in this Azure DevOps organisation is called {AdditionalField}", "No field is called {AdditionalField}")]),
    ("A30 a parent source naming nothing reads a parent anyway", [(CON,
        "return string.IsNullOrEmpty(value) ? ParentReading.NoValue : ParentReading.FieldValue(value);",
        "return ParentReading.FieldValue(string.IsNullOrEmpty(value) ? \"1801\" : value);")]),
    ("A31 a Team's refresh findings are dropped", [(CON,
        "workItem.RefreshFindings = settingFindings;", "workItem.RefreshFindings = [];")]),
    ("A32 a cheap Team refresh records findings of its own", [(TEAMUP,
        "success && outcome.Mode == SyncMode.Full ?", "success && outcome.Mode != SyncMode.Full ?")]),
    ("A33 a relation type named without its end is not accepted by Validate", [(LIST,
        "TheEndCalled(reference) is not null || TheEndsOf(reference).Count > 0;", "TheEndCalled(reference) is not null;")]),
    ("A34 Validate accepts what names nothing and refuses what exists", [(CON,
        "notAField.Where(reference => !relationTypes.Names(reference)).ToList();", "notAField.Where(reference => relationTypes.Names(reference)).ToList();")]),
    ("A35 a relation type without direction is not accepted by Validate", [(LIST,
        "TheEndCalled(reference) is not null || TheEndsOf(reference).Count > 0;",
        "(TheEndCalled(reference) is { } end && !HasNoDirection(end)) || TheEndsOf(reference).Count > 0;")]),
    ("A36 a Portfolio naming nothing no longer reads the Predecessor link", [(CON,
        "? adoWorkItem.ExtractDependencyReferences()\n", "? []\n")]),
]

only = sys.argv[1:]
for name, edits in BREAKS:
    if only and name.split()[0] not in only:
        continue
    paths = sorted({path for path, _, _ in edits})
    skipped = False
    for path, old, new in edits:
        full = os.path.join(R, path)
        src = open(full, encoding="utf-8").read()
        if src.count(old) != 1:
            print(f"=== {name}\nSKIPPED: the line occurs {src.count(old)} times in {path}", flush=True)
            skipped = True
            break
        open(full, "w", encoding="utf-8").write(src.replace(old, new))
    try:
        if skipped:
            continue
        p = subprocess.run(["env", "-u", "LinearAPIKey", "dotnet", "test", "Lighthouse.Backend/Lighthouse.Backend.Tests",
                            "-c", "Debug", "-p:TreatWarningsAsErrors=false", "--filter", UNITS], cwd=R, capture_output=True, text=True, timeout=900)
        out = p.stdout + p.stderr
        print("=== " + name, flush=True)
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                print(re.sub(r"^.*?\.cs\(\d+,\d+\): ", "", line.strip())[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)", flush=True)
    finally:
        subprocess.run(["git", "checkout", "--"] + paths, cwd=R, check=True)
