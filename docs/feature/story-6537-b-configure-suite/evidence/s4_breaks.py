import os
import re
import subprocess
import sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = "Lighthouse.Backend/Lighthouse.Backend/"
PORTFOLIO = B + "Models/Portfolio.cs"
TEAM = B + "Models/Team.cs"
BUNDLE = B + "Models/EstimationBundle.cs"
EFF = B + "Models/EffectiveSetting.cs"
REF = B + "API/Helpers/OwnerSettingRefusals.cs"
BASE = B + "API/DTO/SettingsOwnerDtoBase.cs"
CONN = B + "API/DTO/WorkTrackingSystemConnectionDto.cs"
PEXT = B + "API/Helpers/PortfolioExtensions.cs"
SAVE = B + "Services/Implementation/Connections/ConnectionSaveService.cs"

UNITS = "|".join("FullyQualifiedName~" + name for name in (
    "EstimationBundleTest.", "OwnerSettingRefusalsTest.", "SettingsOwnerDtoRoundTripTest.", "ConnectionSaveServiceTest.",
    "EffectiveSettingTest.", "PortfolioExtensionsTest.", "ConnectionDefaultsDtoTest.",
))

BREAKS = [
    ("E1 a Team reads the Portfolios' Estimation default", TEAM, "WorkTrackingSystemConnection?.EstimationForTeams ?? EstimationBundle.None;", "WorkTrackingSystemConnection?.EstimationForPortfolios ?? EstimationBundle.None;"),
    ("E2 a Portfolio reads the Teams' Estimation default", PORTFOLIO, "WorkTrackingSystemConnection?.EstimationForPortfolios ?? EstimationBundle.None;", "WorkTrackingSystemConnection?.EstimationForTeams ?? EstimationBundle.None;"),
    ("E3 a default is named without its unit", BUNDLE, "string.IsNullOrEmpty(Unit) ? fieldName : $\"{fieldName}, {Unit}\";", "string.IsNullOrEmpty(Unit) ? fieldName : $\"{fieldName}\";"),
    ("E4 two bundles that differ only in unit count as equal", BUNDLE, "               && string.Equals(Unit, other.Unit, StringComparison.Ordinal)\n", ""),
    ("E5 a null category list is read as a list with a category", BASE, "EstimationCategoryValues ?? []", "EstimationCategoryValues ?? [\"x\"]"),
    ("E6 an owner that sends nothing about its estimation starts following", BUNDLE, "                null => this,", "                null => FollowingTheDefault,"),
    ("E7 a bundle without a field counts as set", BUNDLE, "public bool IsSet => AdditionalFieldDefinitionId is not null;", "public bool IsSet => AdditionalFieldDefinitionId is null;"),
    ("R1 a source this version does not know is accepted", REF, "!Enum.IsDefined(source)", "Enum.IsDefined(source)"),
    ("R2 the unknown source refusal carries another code", REF, "-setting-source-unknown", "-source-unknown"),
    ("R3 the Estimation Field is not judged by its source", REF, "owner?.EstimationOwnedOnceTaken(setting.EstimationSource, setting.EstimationAsSent())", "owner?.EstimationOwnedOnceTaken(null, setting.EstimationAsSent())"),
    ("R4 a field the connection lacks is accepted", REF, "&& !connection.HoldsStoredField(field);", "&& connection.HoldsStoredField(field);"),
    ("R5 the missing field refusal carries another code", REF, "-field-not-on-connection", "-field-missing"),
    ("R6 the connection the save leaves the owner on is always the old one", REF, "owner.WorkTrackingSystemConnection?.Id == setting.WorkTrackingSystemConnectionId", "owner.WorkTrackingSystemConnection?.Id != setting.WorkTrackingSystemConnectionId"),
    ("R7 the refusal for an Estimation source names no setting", REF, 'new("Estimation Field", setting.EstimationSource,', 'new("Setting", setting.EstimationSource,'),
    ("C1 a follower reads its own field", EFF, "SettingSource.ConnectionDefault => connectionDefault,", "SettingSource.ConnectionDefault => ownValue,"),
    ("C2 an owner reads the connection's default", EFF, "SettingSource.Own => ownValue,", "SettingSource.Own => connectionDefault,"),
    ("P1 a Portfolio never follows the Dependency Field default", PORTFOLIO, "ConnectionDefaultSetting.DependencyFieldForPortfolios => FollowsDependencyFieldDefault,", "ConnectionDefaultSetting.DependencyFieldForPortfolios => false,"),
    ("P2 a Portfolio never follows the Size Estimate Field default", PORTFOLIO, "ConnectionDefaultSetting.SizeEstimateFieldForPortfolios => FollowsSizeEstimateFieldDefault,", "ConnectionDefaultSetting.SizeEstimateFieldForPortfolios => false,"),
    ("P3 a Portfolio never follows the Feature Owner Field default", PORTFOLIO, "ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios => FollowsFeatureOwnerFieldDefault,", "ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios => false,"),
    ("P4 the Dependency Field default is read from the Parent default", PORTFOLIO, "WorkTrackingSystemConnection?.DependencyFieldForPortfolios;", "WorkTrackingSystemConnection?.ParentOverrideForPortfolios;"),
    ("P5 the Size Estimate Field default is read from the Dependency default", PORTFOLIO, "WorkTrackingSystemConnection?.SizeEstimateFieldForPortfolios;", "WorkTrackingSystemConnection?.DependencyFieldForPortfolios;"),
    ("P6 the Feature Owner Field default is read from the Size default", PORTFOLIO, "WorkTrackingSystemConnection?.FeatureOwnerFieldForPortfolios;", "WorkTrackingSystemConnection?.SizeEstimateFieldForPortfolios;"),
    ("P7 the page names no Dependency Field default", PORTFOLIO, "DisplayNameOfField(DependencyFieldConnectionDefault);", "DisplayNameOfField(null);"),
    ("P8 the page names no Size Estimate Field default", PORTFOLIO, "DisplayNameOfField(SizeEstimateFieldConnectionDefault);", "DisplayNameOfField(null);"),
    ("P9 the page names no Feature Owner Field default", PORTFOLIO, "DisplayNameOfField(FeatureOwnerFieldConnectionDefault);", "DisplayNameOfField(null);"),
    ("P10 the page names the Parent default for the Dependency Field", PORTFOLIO, "DisplayNameOfField(DependencyFieldConnectionDefault);", "DisplayNameOfField(ParentOverrideConnectionDefault);"),
    ("P11 a save without Ignore Dependencies clears it", PEXT, "?? project.IgnoreDependencies;", "?? false;"),
    ("P12 a connection page offers its defaults under another name", CONN, "JsonNamingPolicy.CamelCase.ConvertName(setting.ToString());", "JsonNamingPolicy.CamelCase.ConvertName(setting.ToString()) + \"X\";"),
    ("S1 an owner is judged a follower when it is not", SAVE, ".Where(owner => owner.FollowsConnectionDefault(setting))", ".Where(owner => !owner.FollowsConnectionDefault(setting))"),
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
                print(line.strip()[:200])
        found = re.findall(r"(?:Failed!|Passed!).*", out)
        print(found[-1] if found else "NO SUMMARY (the edit did not compile)", flush=True)
    finally:
        subprocess.run(["git", "checkout", "--", path], cwd=R, check=True)
