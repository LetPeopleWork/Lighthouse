import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
SAVE = B + "Services/Implementation/Connections/ConnectionSaveService.cs"
USE = B + "Models/AdditionalFieldInUse.cs"
REF = B + "API/Helpers/OwnerSettingRefusals.cs"
INC = B + "Models/IncomingSetting.cs"
EFF = B + "Models/EffectiveSetting.cs"
SUP = B + "Models/SupportedConnectionDefaults.cs"
TEAM = B + "Models/Team.cs"
FD = B + "Models/ConnectionFieldDefaults.cs"
DTO = B + "API/DTO/ConnectionDefaultsDto.cs"

UNITS = "|".join("FullyQualifiedName~" + name for name in (
    "ConnectionSaveServiceTest.", "IncomingSettingTest.", "OwnerSettingRefusalsTest.", "EffectiveSettingTest.",
    "SupportedConnectionDefaultsTest.", "ConnectionDefaultsDtoTest.", "SettingsOwnerDtoRoundTripTest.",
    "WorkTrackingSystemConnectionUsageTest.", "AdditionalFieldInUseTest.", "WhereATeamsWorkItemHangsTest.", "TeamExtensionsTest.", "PortfolioExtensionsTest.",
    "WorkTrackingSystemOptionsOwnerExtensionsParentSourcesTest.",
))
EXT = B + "API/Helpers/WorkTrackingSystemOptionsOwnerExtensions.cs"
OWNER = B + "Models/WorkTrackingSystemOptionsOwner.cs"
SEL = B + "Services/Implementation/Parents/ParentSourceSelector.cs"

OWNED = ".Select(portfolio => new OwnerNamingFields(portfolio.Name, [\n                    .. portfolio.ParentSources?.AdditionalFieldIds() ?? [],\n"
DEP = "                    portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId,\n"
SIZE = "                    portfolio.OwnSizeEstimateAdditionalFieldDefinitionId,\n"
OWN = "                    portfolio.OwnFeatureOwnerAdditionalFieldDefinitionId,\n"
EST = "                    portfolio.OwnEstimationAdditionalFieldDefinitionId,\n"

BREAKS = [
    ("A1 a save that removes a field in use is not refused", SAVE, "if (fieldsInUse.Count > 0)", "if (fieldsInUse.Count > 99)"),
    ("A2 a save that removes a field nobody uses is treated as refused", SAVE, "if (fieldsInUse.Count > 0)", "if (fieldsInUse.Count != 99)"),
    ("A3 a Team's own Parent sources are not looked at", SAVE,
     ".. team.ParentSources?.AdditionalFieldIds() ?? [], team.OwnEstimationAdditionalFieldDefinitionId",
     "team.OwnEstimationAdditionalFieldDefinitionId"),
    ("A4 a Team's own Estimation field is not looked at", SAVE,
     "?? [], team.OwnEstimationAdditionalFieldDefinitionId]))]", "?? []]))]"),
    ("A5 a Portfolio's own Parent sources are not looked at", SAVE, OWNED, ".Select(portfolio => new OwnerNamingFields(portfolio.Name, [\n"),
    ("A6 a Portfolio's own Dependency Field is not looked at", SAVE, OWNED + DEP, OWNED),
    ("A7 a Portfolio's own Size Estimate Field is not looked at", SAVE, OWNED + DEP + SIZE, OWNED + DEP),
    ("A8 a Portfolio's own Feature Owner Field is not looked at", SAVE, OWNED + DEP + SIZE + OWN, OWNED + DEP + SIZE),
    ("A9 a Portfolio's own Estimation Field is not looked at", SAVE, OWNED + DEP + SIZE + OWN + EST, OWNED + DEP + SIZE + OWN),
    ("A10 the Teams' Parent sources default does not count as naming its fields", FD,
     "connection => FieldsListedIn(connection.ParentSourcesForTeams)", "connection => FieldsListedIn(null)"),
    ("A11 a default is judged by the wrong field", USE, "theDefault.FieldId == field.Id", "theDefault.FieldId != field.Id"),
    ("A12 an owner is judged by the wrong field", USE, "owner.OwnFieldIds.Contains(field.Id)", "!owner.OwnFieldIds.Contains(field.Id)"),
    ("A13 a field the connection lacks is accepted", REF, "&& !connection.HoldsStoredField(field);", "&& connection.HoldsStoredField(field);"),
    ("A14 a source this version does not know is accepted", REF, "!Enum.IsDefined(source)", "Enum.IsDefined(source)"),
    ("A15 the refusal for a missing field carries another code", REF, "-field-not-on-connection", "-field-missing"),
    ("B1 a sent value equal to what the owner reads still counts as a choice", INC,
     "sentIsSet && !EqualityComparer<T>.Default.Equals(sentValue, effectiveBefore)", "sentIsSet && EqualityComparer<T>.Default.Equals(sentValue, effectiveBefore)"),
    ("B2 a sent source is always read as Own", INC, "{ } source => source,", "{ } source => source == SettingSource.ConnectionDefault ? SettingSource.Own : source,"),
    ("B3 a save without a source and without a new value takes the Own source", INC, "null => null,", "null => SettingSource.Own,"),
    ("B4 an owner that takes Own holds the value it read before", INC,
     "SettingSource.Own => StoredSetting.Owning(sentValue),", "SettingSource.Own => StoredSetting.Owning(effectiveBefore),"),
    ("B5 an owner that takes the default keeps what it held", INC,
     "SettingSource.ConnectionDefault => StoredSetting.FollowingTheDefault,", "SettingSource.ConnectionDefault => stored,"),
    ("B6 a set value is not counted as a choice", INC, "null when sentIsSet &&", "null when !sentIsSet &&"),
    ("C1 a follower reads its own field", EFF, "SettingSource.ConnectionDefault => connectionDefault,", "SettingSource.ConnectionDefault => ownValue,"),
    ("C2 an owner reads the connection's default", EFF, "SettingSource.Own => ownValue,", "SettingSource.Own => connectionDefault,"),
    ("C3 an owner that owns its value counts as following", INC, "public bool FollowsTheDefault => Source == SettingSource.ConnectionDefault;",
     "public bool FollowsTheDefault => Source != SettingSource.ConnectionDefault;"),
    ("C4 a Team never follows the Parent default", TEAM,
     "ConnectionDefaultSetting.ParentOverrideForTeams => FollowsParentOverrideDefault,", "ConnectionDefaultSetting.ParentOverrideForTeams => false,"),
    ("C5 a new Team does not follow the Parent default", TEAM,
     "ConnectionDefaultSetting.ParentOverrideForTeams => FollowsParentOverrideDefault,", "ConnectionDefaultSetting.ParentOverrideForTeams => !FollowsParentOverrideDefault,"),
    ("D1 Jira and Azure DevOps offer only what a CSV file offers", SUP, "WorkTrackingSystems.Jira or WorkTrackingSystems.AzureDevOps => Every,",
     "WorkTrackingSystems.Jira or WorkTrackingSystems.AzureDevOps => [.. Every.Skip(1)],"),
    ("D2 a CSV file offers every default", SUP, "WorkTrackingSystems.Csv => OnCsv,", "WorkTrackingSystems.Csv => [.. OnCsv, ConnectionDefaultSetting.ParentOverrideForTeams],"),
    ("D3 a tracker with no defaults offers what a CSV file offers", SUP, "_ => None,", "_ => [.. None, ConnectionDefaultSetting.EstimationForTeams],"),
    ("D4 a CSV file no longer offers Feature Owner", SUP,
     "            ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios,\n            ConnectionDefaultSetting.EstimationForPortfolios,\n        ];",
     "            ConnectionDefaultSetting.EstimationForPortfolios,\n        ];"),
    ("E1 a save that changes no default still writes a log entry", SAVE, "if (changed.After.IsTheSameDefaultAs(changed.Before))", "if (!changed.After.IsTheSameDefaultAs(changed.Before))"),
    ("E2 a default nobody follows says one Team follows it", SAVE, '0 => $"no {owners.One} follows it",', '0 => $"1 {owners.One} follows it",'),
    ("E3 a cleared default does not name the field it was", SAVE, "changed.Before.Description ?? NoField,", "NoField,"),
    ("E4 a default naming a field added in the same save is not pointed at it", SAVE, "each.Default.PointAt(handle.Connection, each.Field.Id);", "_ = each.Field.Id;"),
    ("E5 a default left out of a save is cleared", DTO, "sent[fieldDefault.Setting] = fieldDefault.FirstFieldOn(connection);", "sent[fieldDefault.Setting] = null;"),
    ("E6 a default left out of a save drops the entries of its list", DTO,
     "? ParentSourceEntryDto.ListOf(list) : null;", "? ParentSourceEntryDto.ListOf(list).Take(0).ToList() : null;"),
    ("G1 a Team that owns its field reads the list of a follower", OWNER,
     "=> (FollowsParentOverrideDefault ? null : ParentSources)", "=> (!FollowsParentOverrideDefault ? null : ParentSources)"),
    ("G2 a Team that follows the default never reads it", OWNER,
     "                ?? ParentSourcesConnectionDefault\n", ""),
    ("G3 a Team follows the default of the Portfolios", TEAM,
     "WorkTrackingSystemConnection?.ParentSourcesForTeams;", "WorkTrackingSystemConnection?.ParentSourcesForPortfolios;"),
    ("G4 a Team that chose None still reads a field", OWNER,
     ": ParentSources.FromTheLegacyField(setting.OwnValue);", ": ParentSources.FromTheLegacyField(setting.OwnValue ?? 9);"),
    ("G5 an item is hung under nothing", SEL, "record.ParentReferenceId = choice.Key;", "record.ParentReferenceId = string.Empty;"),
    ("F1 a list the save sends is not taken as the owner's own", EXT, "                owner.OwnParentSources(list);\n", "                _ = list;\n"),
    ("F2 a list the save sends is never recognised", EXT, "?.List is { } list)", "?.List is { } list && list.AdditionalFieldIds().Count() > 99)"),
    ("F3 a save that sends its own list also takes the old field", EXT, "                owner.OwnParentSources(list);\n                return;\n", "                owner.OwnParentSources(list);\n"),
    ("F4 a save without its own list ignores the old field and source", EXT,
     "owner.TakeParentOverrideAsSent(settings.ParentOverrideSource, owner.ParentOverrideFieldAsSent(settings));", "owner.TakeParentOverrideAsSent(null, null);"),
    ("F5 the old field beside a list is judged the wrong way round", EXT,
     "=> settings.ParentOverrideFieldWasSent()\n                && ParentSourceEntryDto.TheOldFieldIsTheEdit(", "=> settings.ParentOverrideFieldWasSent()\n                && !ParentSourceEntryDto.TheOldFieldIsTheEdit("),
    ("F6 a save that names no source always chooses to own", EXT, "== SettingSource.Own;\n\n        private static void TakeParentSourcesAsSent", "!= SettingSource.Own;\n\n        private static void TakeParentSourcesAsSent"),
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
        print("=== " + name, flush=True)
        shown = 0
        for line in out.splitlines():
            if re.match(r"\s*(Failed [A-Za-z]|.*error [A-Z]+[0-9]+)", line):
                shown += 1
                print(line.strip()[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)", flush=True)
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
