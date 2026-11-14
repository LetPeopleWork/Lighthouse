import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
PORTFOLIO = B + "Models/Portfolio.cs"
TEAM = B + "Models/Team.cs"
EFF = B + "Models/EffectiveSetting.cs"
FETCH = B + "Models/OwnersWhoseFetchMoved.cs"
FIELDS = B + "Models/ConnectionFieldDefaults.cs"
DTO = B + "API/DTO/ConnectionDefaultsDto.cs"
SAVE = B + "Services/Implementation/Connections/ConnectionSaveService.cs"

UNITS = "|".join("FullyQualifiedName~" + name for name in (
    "ConnectionSaveServiceTest.", "EffectiveSettingTest.", "ParentSourcesBridgeTest.", "ConnectionDefaultsDtoTest.",
    "SettingsOwnerDtoRoundTripTest.", "WorkTrackingSystemConnectionUsageTest.",
))

BREAKS = [
    ("K1 a save without Save and update still queues the updates", SAVE, "var fetchShapeChange = startUpdates ? FetchShapeChangeFor(handle, moved) : null;", "var fetchShapeChange = FetchShapeChangeFor(handle, moved);"),
    ("K2 a Team never follows the Teams' default", TEAM, "ConnectionDefaultSetting.ParentOverrideForTeams => FollowsParentOverrideDefault,", "ConnectionDefaultSetting.ParentOverrideForTeams => false,"),
    ("K3 a Portfolio reads the Teams' Parent override default", PORTFOLIO, "protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForPortfolios;", "protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForTeams;"),
    ("K4 a Team reads the Portfolios' Parent override default", TEAM, "protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForTeams;", "protected override int? ParentOverrideConnectionDefault => WorkTrackingSystemConnection?.ParentOverrideForPortfolios;"),
    ("K5 every owner counts as moved by a save", FETCH, "|| fetchBefore != owner.Value)", "|| true)"),
    ("K6 an owner is judged a follower when it is not", SAVE, ".Where(owner => owner.FollowsConnectionDefault(setting))", ".Where(owner => !owner.FollowsConnectionDefault(setting))"),
    ("K7 the entry says nothing when nobody follows", SAVE, '0 => $"no {owners.One} follows it",', '0 => $"{owners.One} follows it",'),
    ("K8 an unchanged default is logged and a changed one is not", SAVE, "if (changed.After.IsTheSameDefaultAs(changed.Before))", "if (!changed.After.IsTheSameDefaultAs(changed.Before))"),
    ("K9 a default naming a field added in the save points at nothing", SAVE, "each.Default.PointAt(handle.Connection, each.Field.Id);", "each.Default.PointAt(handle.Connection, null);"),
    ("K10 the Portfolios' default is written to the Teams' default", FIELDS, "connection.ParentOverrideForPortfolios = field,", "connection.ParentOverrideForTeams = field,"),
    ("K11 a default the save leaves out counts as sent", DTO, "public bool Sends(ConnectionDefaultSetting setting) => sent.ContainsKey(setting) || lists.ContainsKey(setting);", "public bool Sends(ConnectionDefaultSetting setting) => true;"),
    ("K12 a list sent without its old field is no choice", DTO, "|| (sent.TryGetValue(setting, out var oldField) && ParentSourceEntryDto.TheOldFieldIsTheEdit(oldField, sentList, held)))", "|| true)"),
    ("K13 an emptied old field beside a list is not the edit", DTO, "&& ParentSourceEntryDto.TheOldFieldIsTheEdit(oldField, sentList, held)", "&& false"),
    ("K14 a cleared Portfolios' default stays", FIELDS, "connection.ParentOverrideForPortfolios = field,", "connection.ParentOverrideForPortfolios = field ?? connection.ParentOverrideForPortfolios,"),
    ("K15 a follower reads its own field", EFF, "SettingSource.ConnectionDefault => connectionDefault,", "SettingSource.ConnectionDefault => ownValue,"),
    ("K16 an owner reads the connection's default", EFF, "SettingSource.Own => ownValue,", "SettingSource.Own => connectionDefault,"),
    ("K17 a Portfolio never follows the Portfolios' Parent default", PORTFOLIO, "ConnectionDefaultSetting.ParentOverrideForPortfolios => FollowsParentOverrideDefault,", "ConnectionDefaultSetting.ParentOverrideForPortfolios => false,"),
    ("K18 a Team reads its parent list from the Portfolios' default", TEAM, "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForTeams;", "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForPortfolios;"),
    ("K19 a Portfolio reads its parent list from the Teams' default", PORTFOLIO, "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForPortfolios;", "protected override ParentSources? ParentSourcesConnectionDefault => WorkTrackingSystemConnection?.ParentSourcesForTeams;"),
    ("K20 the entry of a Teams' default is worded for the Portfolios", SAVE, "heldFor == DefaultHeldFor.Teams ? TeamWords : PortfolioWords;", "heldFor == DefaultHeldFor.Teams ? PortfolioWords : TeamWords;"),
    ("K21 a cleared Teams' default stays", FIELDS, "connection.ParentOverrideForTeams = field,", "connection.ParentOverrideForTeams = field ?? connection.ParentOverrideForTeams,"),
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
                            "-c", "Debug", "-p:TreatWarningsAsErrors=false", "--filter", UNITS], cwd=R, capture_output=True, text=True, timeout=900)
        out = p.stdout + p.stderr
        print("=== " + name, flush=True)
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                print(line.strip()[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)", flush=True)
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
