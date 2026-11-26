using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up the Reports module before any of them moves into Lighthouse.Backend.Modules.Reports. The types
    /// are read from the compiled code: every top-level type in the Services namespaces Reports lives in today and under
    /// Lighthouse.Backend.Modules.Reports. Each is on ReportsMembership with its layer, and is found once, in its old namespace or
    /// in its new one, so the map holds through the move. Layers are decided per file: a file is Domain exactly when it declares no
    /// port and every type it declares reaches nothing but the shared Models, the base library and the other Domain files; the
    /// other files under Metrics go to the Application's Metrics and the rest to the Application. The Reports types that stay where
    /// they are are named with their owner and reason, and every suppression, analyser, CI-selection, test-budget, host-user,
    /// Stryker or log-override target naming a moving type is listed, because a target does not follow a type that changes
    /// namespace.
    /// </summary>
    [TestFixture]
    public class ReportsMembershipArchUnitTest
    {
        private const string ReportsNamespaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Reports|Modules\.Reports)(\.[A-Za-z]+)*$";

        private const string ReportsNamespacesThatStay = @"^Lighthouse\.Backend\.(Models|API\.DTO)\.Reports$";

        private const string NotOnTheMap = "Not on the map, but in the Reports namespaces";
        private const string NotInTheCode = "On the map, but not in the compiled code";
        private const string FoundTwice = "Found in both its old and its new namespace";
        private const string ListedTwice = "Listed more than once";
        private const string DomainReachingBeyond = "Listed as Domain, but reaching beyond the shared Models, the base library and the Domain";
        private const string ApplicationReachingOnlyTheKernel = "Listed outside the Domain, but no port file and reaching only the shared Models, the base library and the Domain";
        private const string FileLayerMismatch = "A file whose layer or port-file mark does not hold";
        private const string StayingNotFound = "Listed as staying, but not in the compiled code where listed";
        private const string StayingInsideTheModule = "Listed as staying, but in a Reports namespace, where every type moves";
        private const string StayingNotListed = "Not listed as staying, but in a Reports namespace that stays";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly (string Path, ReportsTargetKind Kind)[] TargetFilesNamedByPath =
        [
            ("Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs", ReportsTargetKind.Suppression),
            ("Lighthouse.Backend/.editorconfig", ReportsTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/.editorconfig", ReportsTargetKind.Analyser),
            ("Lighthouse.Backend/Directory.Build.props", ReportsTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend/BannedSymbols.txt", ReportsTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/BannedSymbols.Tests.txt", ReportsTargetKind.Analyser),
            ("Scripts/test-selection/path-classifier.sh", ReportsTargetKind.CiSelection),
            ("Scripts/test-timings/test-time-budget.json", ReportsTargetKind.TestBudget),
            ("Scripts/test-timings/tests/test_check_budget_time_budgets.py", ReportsTargetKind.TestBudget),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs", ReportsTargetKind.HostUser),
            ("Lighthouse.Backend/Lighthouse.Backend/appsettings.json", ReportsTargetKind.LogOverride),
            ("Lighthouse.EndToEndTests/perf/helpers/collect.ts", ReportsTargetKind.LogOverride),
        ];

        private const string MapFixHint =
            "Every type in the Reports namespaces is on ReportsMembership.MovingTypes on purpose, with the layer it goes to, its file and the namespace it has today. Put an unlisted type on the map with its file; a listed type the code no longer holds was renamed or deleted, so put its new name on the map or take it off. A listed type is looked for by its full name in its old namespace and in Lighthouse.Backend.Modules.Reports.<Layer>, nowhere else, and must be there exactly once.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<List<string>> ReportsTypesInTheCode = new(() => WhenTheReportsTypesAreCollectedFrom(ProductionModule.Value));

        private static readonly Lazy<HashSet<string>> InterfacesInTheCode = new(() => ProductionModule.Value.Types.Where(type => type.IsInterface).Select(type => type.FullName).ToHashSet(StringComparer.Ordinal));

        private static readonly Lazy<Dictionary<string, HashSet<string>>> ReachInTheCode = new(() => WhenTheReachOfTheReportsTypesIsRead(ProductionModule.Value));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void TheReportsTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove_EachFoundOnce()
        {
            var found = ReportsTypesInTheCode.Value;

            ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes);
        }

        [Test]
        public void TheReportsModuleRootTypes_AreExactlyTheModuleRootList()
        {
            var atTheModuleRoot = ProductionModule.Value.Types
                .Where(type => type.Namespace == ReportsType.NewRootNamespace && !type.Name.StartsWith('<'))
                .Select(type => type.Name)
                .ToList();

            Assert.That(atTheModuleRoot, Is.Not.Empty,
                $"No type lives at the module root {ReportsType.NewRootNamespace}, so the module-root list is checked against nothing. "
                + "Why it matters: the map sets these types aside, and a check that finds none guards nothing. "
                + "How to fix: the Reports module's start-up lives at the module root; find where it went.");
            InlineBaseline.AssertMatches(atTheModuleRoot, ReportsMembership.ModuleRoot,
                $"The types at the module root {ReportsType.NewRootNamespace}, in no layer, are exactly ReportsMembership.ModuleRoot. "
                + "Only the module's own start-up lives there; any other type belongs in a layer, on MovingTypes. "
                + "Put a new start-up type on ModuleRoot, move any other type into its layer, and take a name the code no longer holds off the list.");
        }

        [Test]
        public void AMapWithOneTypeLeftOff_NamesThatTypeAsNotOnTheMap()
        {
            var leftOff = MovingTypeNamed("ReportCommands");

            var failure = FailureOf(() => ThenTheyMatchTheMap(ReportsTypesInTheCode.Value, [.. ReportsMembership.MovingTypes.Where(type => type != leftOff)]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [NameInTheCode(leftOff)], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMadeUpNameOnTheMap_IsNamedAsNotInTheCode()
        {
            var madeUp = MovingTypeNamed("ReportCommands") with { Name = "AMadeUpReportArchive" };

            var failure = FailureOf(() => ThenTheyMatchTheMap(ReportsTypesInTheCode.Value, [.. ReportsMembership.MovingTypes, madeUp]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["AMadeUpReportArchive"], foundTwice: []);
        }

        [Test]
        public void AListedTypeTheCodeNoLongerHolds_IsNamedAsNotInTheCode()
        {
            var listed = MovingTypeNamed("DailySeriesSummary");
            var found = ReportsTypesInTheCode.Value.Where(name => name != listed.OldFullName && name != listed.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["DailySeriesSummary"], foundTwice: []);
        }

        [Test]
        public void AMapWithOneTypeSwappedForAMadeUpOne_NamesBoth_ThoughItListsAsManyTypesAsTheCodeHolds()
        {
            var swapped = MovingTypeNamed("ReportQueries");
            var madeUp = swapped with { Name = "AMadeUpReportArchive" };

            var failure = FailureOf(() => ThenTheyMatchTheMap(ReportsTypesInTheCode.Value, [.. ReportsMembership.MovingTypes.Select(type => type == swapped ? madeUp : type)]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [NameInTheCode(swapped)], notInTheCode: ["AMadeUpReportArchive"], foundTwice: []);
        }

        [Test]
        public void ATypeFoundInItsNewNamespaceInsteadOfItsOld_IsStillOnTheMap()
        {
            var moved = MovingTypeNamed("DailySeriesSummary");
            var found = ReportsTypesInTheCode.Value.Select(name => name == moved.OldFullName ? moved.NewFullName : name).ToList();

            Assert.DoesNotThrow(() => ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes),
                $"DailySeriesSummary found in {moved.NewNamespace} instead of its old namespace no longer matched the map. The map lists each type with its old namespace and looks for it in its new one too, so the move itself must not turn this check red.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice()
        {
            var moved = MovingTypeNamed("ReportCapPolicy");
            var found = ReportsTypesInTheCode.Value.Append(moved.OldFullName).Append(moved.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: [], foundTwice: ["ReportCapPolicy"]);
        }

        [Test]
        public void ATypeMovedIntoAnotherLayer_IsNamedAsNotInTheCode_AndAsNotOnTheMap()
        {
            var moved = MovingTypeNamed("WipReportMetric");
            var otherLayer = moved with { Layer = ReportsLayer.Application };
            var found = ReportsTypesInTheCode.Value.Select(name => name == moved.OldFullName || name == moved.NewFullName ? otherLayer.NewFullName : name).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [otherLayer.NewFullName], notInTheCode: ["WipReportMetric"], foundTwice: []);
        }

        [Test]
        public void ANamesakeOfAMovingTypeInAnotherOldNamespace_IsNamedByItsFullName()
        {
            var moving = MovingTypeNamed("ReportCapPolicy");
            var namesake = $"{ReportsMembership.InterfacesNamespace}.{moving.Name}";
            var found = ReportsTypesInTheCode.Value.Append(namesake).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, ReportsMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [namesake], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMapListingOneTypeTwice_NamesIt()
        {
            var twice = MovingTypeNamed("ReportCapPolicy");

            var failure = FailureOf(() => ThenTheyMatchTheMap(ReportsTypesInTheCode.Value, [.. ReportsMembership.MovingTypes, twice with { Layer = ReportsLayer.Application }]));

            Assert.That(LineStartingWith(failure, ListedTwice), Does.Contain("ReportCapPolicy"),
                $"A type listed twice, once in each of two layers, was not named. A type moves to one layer only; the map must say which. The failure was: {failure}");
        }

        [Test]
        public void TheCollection_TakesEveryTopLevelTypeInTheOldAndTheNewNamespaces_AndNothingElse()
        {
            string[] names =
            [
                $"{ReportsMembership.ImplementationNamespace}.AMadeUpOldService",
                $"{ReportsMembership.MetricsNamespace}.AMadeUpOldMetric",
                $"{ReportsMembership.InterfacesNamespace}.IAMadeUpOldPort",
                "Lighthouse.Backend.Modules.Reports.Domain.AMadeUpRule",
                "Lighthouse.Backend.Modules.Reports.Application.AMadeUpService/ANestedStep",
                "Lighthouse.Backend.Modules.Reports.Application.Metrics.AMadeUpMetric+ANestedValue",
                "Lighthouse.Backend.Modules.Reports.AMadeUpRootType",
                $"{ReportsMembership.ImplementationNamespace}.<>c__DisplayClass0_0",
                "Lighthouse.Backend.Modules.ReportsFixtures.AMadeUpOffender",
                "Lighthouse.Backend.Services.Implementation.ReportsLookalike.AMadeUpLookalike",
                "Lighthouse.Backend.Services.Implementation.AMadeUpServiceBesideReports",
                "Lighthouse.Backend.Models.Reports.AMadeUpStayingModel",
                "Lighthouse.Backend.Tests.Modules.Reports.Domain.AMadeUpRuleTest",
            ];

            var collected = ReportsTypesAmong(names);

            Assert.That(collected, Is.EquivalentTo(new[]
            {
                $"{ReportsMembership.ImplementationNamespace}.AMadeUpOldService",
                $"{ReportsMembership.MetricsNamespace}.AMadeUpOldMetric",
                $"{ReportsMembership.InterfacesNamespace}.IAMadeUpOldPort",
                "Lighthouse.Backend.Modules.Reports.Domain.AMadeUpRule",
                "Lighthouse.Backend.Modules.Reports.Application.AMadeUpService",
                "Lighthouse.Backend.Modules.Reports.Application.Metrics.AMadeUpMetric",
                "Lighthouse.Backend.Modules.Reports.AMadeUpRootType",
            }),
                "The Reports types collected from a made-up list of names are not the top-level types of the old and new Reports namespaces. The collection must read both old namespaces with the Metrics one and every namespace under Lighthouse.Backend.Modules.Reports, take a nested type as the type that holds it, leave out what the compiler makes, and stop at the end of a namespace name, or a type added during the move would join the module unseen and a lookalike or a staying model would join it by mistake.");
        }

        [Test]
        public void EachFileIsDomain_ExactlyWhenItIsNoPortFile_AndEveryTypeItDeclaresReachesOnlyTheSharedModelsTheBaseLibraryAndTheDomain()
        {
            var reach = ReachInTheCode.Value;

            ThenTheLayersFollowTheFiles(reach, ReportsTypesInTheCode.Value, InterfacesInTheCode.Value, ReportsMembership.MovingTypes);
        }

        [Test]
        public void AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches()
        {
            var (moving, found, reach) = RelistedInLayer("DailySeriesSummary", ReportsLayer.Domain);

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(reach, found, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, DomainReachingBeyond);

            Assert.That(line, Does.Contain("DailySeriesSummary.cs: DailySeriesSummary reaches").And.Contain("Lighthouse.Backend.Modules.FlowMetrics.Domain.").And.Not.Contain("ReportCapPolicy"),
                $"DailySeriesSummary.cs, whose type uses the Flow metrics Domain, was listed as Domain and not named alone with what it reaches. A Domain file depends on nothing but the shared Models, the base library and the other Domain files. The failure was: {failure}");
        }

        [Test]
        public void AKernelOnlyFileThatIsNoPortFileListedOutsideTheDomain_IsNamed_AndAPortFileIsNot()
        {
            var moving = WithLayer(ReportsMembership.MovingTypes, "ReportCapPolicy.cs", ReportsLayer.Application);

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, ReportsTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, ApplicationReachingOnlyTheKernel);

            Assert.That(line, Does.Contain("ReportCapPolicy.cs").And.Not.Contain("IReportOwnerWriteAccess.cs").And.Not.Contain("IReportMetric.cs"),
                $"ReportCapPolicy.cs, which reaches only the base library, was listed as Application and not named, or a port file was named with it. A file that depends on nothing else belongs in the Domain, unless it declares a port, which stays in the Application with its records. The failure was: {failure}");
        }

        [Test]
        public void AMetricsFileListedAsApplication_OrAnotherFileListedAsApplicationMetrics_IsNamed()
        {
            var moving = WithLayer(WithLayer(ReportsMembership.MovingTypes, "WipReportMetric.cs", ReportsLayer.Application), "ReportCommands.cs", ReportsLayer.ApplicationMetrics);

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, ReportsTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, FileLayerMismatch);

            Assert.That(line, Does.Contain("WipReportMetric.cs").And.Contain("ReportCommands.cs").And.Not.Contain("CycleTimeReportMetric.cs").And.Not.Contain("ReportQueries.cs"),
                $"A file under Metrics listed in the Application, or a file outside Metrics listed in the Application's Metrics, was not named, or a correctly listed file was. The move keeps the Metrics grouping: every file under Metrics that is not Domain goes to the Application's Metrics, and no other file does. The failure was: {failure}");
        }

        [Test]
        public void APortFileSplitAcrossLayers_OrAFileMarkedAsAPortFileWithoutAPortInterface_IsNamed()
        {
            var moving = ReportsMembership.MovingTypes
                .Select(type => type.Name == "ReportOwnerRow" ? type with { Layer = ReportsLayer.Domain } : type)
                .Select(type => type.File == "ReportCapPolicy.cs" ? type with { InAPortFile = true } : type)
                .ToList();

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, ReportsTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, FileLayerMismatch);

            Assert.That(line, Does.Contain("IReportOwnerLookup.cs").And.Contain("ReportCapPolicy.cs").And.Not.Contain("IReportTemplate.cs"),
                $"A port file with one record listed as Domain, or a file marked as a port file that declares no port interface, was not named, or a sound port file was. A file moves whole into one layer, and a port file stays in the Application with every record it declares. The failure was: {failure}");
        }

        [Test]
        public void AMadeUpDomainTypeReachingALookalikeOfTheKernelOrANamesakeOfADomainType_IsNamed_AndTheKernelAndTheMovedDomainAreNot()
        {
            var capPolicy = MovingTypeNamed("ReportCapPolicy");
            var madeUp = new ReportsType("AMadeUpReportRule", ReportsLayer.Domain, ReportsMembership.ImplementationNamespace, "AMadeUpReportRule.cs", InAPortFile: false);
            var namesake = $"{ReportsMembership.InterfacesNamespace}.{capPolicy.Name}";
            const string fixtureLookalike = "Lighthouse.Backend.Modules.ReportsFixtures.Domain.ReportCapPolicy";
            var reach = ReachInTheCode.Value.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            reach[madeUp.OldFullName] = ["System.Math", "Lighthouse.Backend.Models.Reports.ReportWindow", capPolicy.NewFullName, namesake, fixtureLookalike, "Lighthouse.Backend.ModelsLookalike.AMadeUpValue", "Systematic.AMadeUpHelper"];
            var found = ReportsTypesInTheCode.Value.Append(madeUp.OldFullName).ToList();

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(reach, found, InterfacesInTheCode.Value, [.. ReportsMembership.MovingTypes, madeUp]));
            var line = LineStartingWith(failure, DomainReachingBeyond);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain(namesake).And.Contain(fixtureLookalike).And.Contain("Lighthouse.Backend.ModelsLookalike.AMadeUpValue").And.Contain("Systematic.AMadeUpHelper"),
                    $"A made-up Domain type reaching a namesake of a Domain type in another namespace, a made-up fixture type of the same name, or a namespace that only starts like Models or System, was not named with it. Types must be matched by their full names and namespaces must end at a dot, or anything can hide behind a familiar name. The failure was: {failure}");
                Assert.That(line, Does.Not.Contain("Lighthouse.Backend.Models.Reports.ReportWindow").And.Not.Contain("System.Math").And.Not.Contain(capPolicy.NewFullName),
                    $"A made-up Domain type was named for reaching a Models type, the base library, or a Domain type in its new namespace. The Domain may use all three, before and after the move. The failure was: {failure}");
            }
        }

        [Test]
        public void EachTypeThatStays_IsInTheCompiledCodeWhereListed_AndEveryTypeOfTheStayingReportsNamespacesIsListed()
        {
            ThenEachTypeThatStaysIsWhereListed(ProductionModule.Value.Types.Select(type => type.FullName), ReportsMembership.TypesThatStay);
        }

        [Test]
        public void ATypeThatStaysListedInTheWrongPlace_OrUnderAMadeUpName_OrInsideTheModule_OrLeftOff_IsNamed()
        {
            var repository = TypeThatStaysNamed("ReportRepository");
            var payload = TypeThatStaysNamed("ThenAndNowPayload");
            var inTheWrongPlace = repository with { Namespace = "Lighthouse.Backend.Services.Interfaces.Repositories" };
            var madeUp = repository with { Name = "AMadeUpReportArchive" };
            var insideTheModule = repository with { Name = "ReportCommands", Namespace = ReportsMembership.ImplementationNamespace };

            var failure = FailureOf(() => ThenEachTypeThatStaysIsWhereListed(
                ProductionModule.Value.Types.Select(type => type.FullName),
                [.. ReportsMembership.TypesThatStay.Where(type => type != repository && type != payload), inTheWrongPlace, madeUp, insideTheModule]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LineStartingWith(failure, StayingNotFound), Does.Contain("ReportRepository (looked for in Lighthouse.Backend.Services.Interfaces.Repositories)").And.Contain("AMadeUpReportArchive"),
                    $"A staying type listed in a namespace that does not hold it, or under a name the code does not have, was not named. A staying entry tells feature work who owns the type; a wrong one hides a rename. The failure was: {failure}");
                Assert.That(LineStartingWith(failure, StayingInsideTheModule), Does.Contain("ReportCommands"),
                    $"A type in a Reports namespace was accepted as staying. Every type in those namespaces moves; it belongs on the map with its layer. The failure was: {failure}");
                Assert.That(LineStartingWith(failure, StayingNotListed), Does.Contain(payload.FullName).And.Not.Contain("ReportOwnerGoneException"),
                    $"A type of Models.Reports left off the staying list was not named, or a listed one was. Every type of the Reports namespaces that stay must be named with its owner and reason, or a type added there joins no owner. The failure was: {failure}");
                Assert.That(failure, Does.Not.Contain("TeamReportsController").And.Not.Contain("ReportEditRequest"),
                    $"A staying type listed correctly was named. The failure was: {failure}");
            }
        }

        [Test]
        public void TheTargetsNamingAMovingType_AreExactlyTheOnesListed()
        {
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath);

            ThenTheTargetsMatchTheList(files, ReportsMembership.MovingTypes, ReportsMembership.TargetsNamingAMovingType);
        }

        [Test]
        public void AnUnlistedTargetNamingAMovingType_IsNamed_ByItsOldOrNewNameOrPath_AndOneNamingWhatStaysIsNot()
        {
            var commands = MovingTypeNamed("ReportCommands");
            var summary = MovingTypeNamed("DailySeriesSummary");
            var text = string.Join(Environment.NewLine,
                $"\"{commands.OldFullName}\": \"Debug\",",
                $"\"{summary.NewFullName}.Summarise\"",
                "Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Reports/ThenAndNowTemplate.cs",
                "Lighthouse.Backend.Tests.Services.Implementation.Reports.ReportWindowPolicyTest",
                "\"Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository\": \"Debug\",",
                "Lighthouse.Backend.Tests.API.Integration.Reports.ReportsAtThePortTest",
                "Lighthouse.Backend.Services.Implementation.ReportsLookalike.ReportQueries");
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath).Append(("MadeUp/appsettings.MadeUp.json", ReportsTargetKind.LogOverride, text)).ToList();

            var failure = FailureOf(() => ThenTheTargetsMatchTheList(files, ReportsMembership.MovingTypes, ReportsMembership.TargetsNamingAMovingType));
            var line = LineStartingWith(failure, "Not on the list");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain("MadeUp/appsettings.MadeUp.json: ReportCommands (LogOverride)")
                    .And.Contain("MadeUp/appsettings.MadeUp.json: DailySeriesSummary.Summarise (LogOverride)")
                    .And.Contain("MadeUp/appsettings.MadeUp.json: ThenAndNowTemplate (LogOverride)")
                    .And.Contain("MadeUp/appsettings.MadeUp.json: ReportWindowPolicyTest (LogOverride)"),
                    $"A made-up target naming a moving type by its old full name, its new full name, its old path or its test's old name was not named. Every such target must be listed, or the move leaves it pointing at nothing. The failure was: {failure}");
                Assert.That(line, Does.Not.Contain("ReportRepository").And.Not.Contain("ReportsAtThePortTest").And.Not.Contain("ReportQueries"),
                    $"A target naming a type that stays, a test that stays, or a namesake in a lookalike namespace was named. Only a target naming a moving type in its own namespace has to follow the move. The failure was: {failure}");
            }
        }

        [Test]
        public void AListedTargetNoLongerInItsFile_IsNamed_AndOneRewrittenToTheNewNamespaceIsStillFound()
        {
            var queries = MovingTypeNamed("ReportQueries");
            ReportsTarget listed = new("MadeUp/stryker.reports.json", "ReportQueries", ReportsTargetKind.Stryker);
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath);
            var rewritten = files.Append((listed.File, listed.Kind, $"\"mutate\": [\"{queries.NewFullName}\"]")).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.DoesNotThrow(() => ThenTheTargetsMatchTheList(rewritten, ReportsMembership.MovingTypes, [.. ReportsMembership.TargetsNamingAMovingType, listed]),
                    "A listed target rewritten to the type's new namespace no longer matched the list. The list writes targets without their namespace, so the move's rewrite must keep this check green.");
                Assert.That(FailureOf(() => ThenTheTargetsMatchTheList(files, ReportsMembership.MovingTypes, [.. ReportsMembership.TargetsNamingAMovingType, listed])),
                    Does.Contain("On the list, but no longer offending: MadeUp/stryker.reports.json: ReportQueries (Stryker)"),
                    "A listed target that its file no longer holds was not named. A list that never shrinks hides a target the move forgot.");
            }
        }

        [Test]
        public void AFileTheTargetScanMustRead_IsNamedWhenItIsMissing_SoAnEmptyScanIsNeverTakenForNoTargets()
        {
            (string Path, ReportsTargetKind Kind)[] named = [.. TargetFilesNamedByPath, ("Lighthouse.EndToEndTests/perf/helpers/a-made-up-log-override.ts", ReportsTargetKind.LogOverride)];

            var failure = FailureOf(() => WhenTheTargetFilesAreRead(named));

            Assert.That(failure, Does.Contain("Lighthouse.EndToEndTests/perf/helpers/a-made-up-log-override.ts").And.Not.Contain("collect.ts"),
                $"A file the target scan must read, missing from the repository, was not named, or a file that exists was. Without the file the scan would find no target and pass without having looked. The failure was: {failure}");
        }

        // The module's own start-up lives at the module root, in no layer, so the map of moving types leaves it out.
        private static List<string> WhenTheReportsTypesAreCollectedFrom(ModuleDefinition module)
            => [.. ReportsTypesAmong(module.Types.Select(type => type.FullName))
                .Except(ReportsMembership.ModuleRoot.Select(entry => ReportsType.NewRootNamespace + "." + entry), StringComparer.Ordinal)];

        private static List<string> ReportsTypesAmong(IEnumerable<string> fullNames)
            => [.. fullNames.Select(OuterTypeOf)
                .Where(name => !ShortNameOf(name).StartsWith('<') && IsIn(NamespaceOfName(name), ReportsNamespaces))
                .Distinct(StringComparer.Ordinal)];

        // Each Reports type with every type its compiled code references, nested and compiler-made types folded into the type that
        // holds them, and the type itself left out.
        private static Dictionary<string, HashSet<string>> WhenTheReachOfTheReportsTypesIsRead(ModuleDefinition module)
            => IlDependencies.DependentsIn(module, ReportsNamespaces, target => target is not GenericParameter)
                .GroupBy(dependent => OuterTypeOf(dependent.TypeName), StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(dependent => dependent.Targets)
                        .Select(OuterTypeOf)
                        .Where(target => target != group.Key && !ShortNameOf(target).StartsWith('<'))
                        .ToHashSet(StringComparer.Ordinal),
                    StringComparer.Ordinal);

        private static void ThenTheyMatchTheMap(IEnumerable<string> found, IReadOnlyList<ReportsType> moving)
        {
            var inTheCode = found.Select(OuterTypeOf).ToHashSet(StringComparer.Ordinal);
            var claimed = new HashSet<string>(StringComparer.Ordinal);
            var notInTheCode = new List<string>();
            var foundTwice = new List<string>();

            foreach (var type in moving)
            {
                var hits = new[] { type.OldFullName, type.NewFullName }.Where(inTheCode.Contains).ToList();
                claimed.UnionWith(hits);

                if (hits.Count == 0)
                {
                    notInTheCode.Add($"{type.Name} (looked for in {type.OldNamespace} and {type.NewNamespace})");
                }
                else if (hits.Count > 1)
                {
                    foundTwice.Add($"{type.Name} ({string.Join(" and ", hits)})");
                }
            }

            var listedTwice = moving.GroupBy(type => type.OldFullName, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.First().Name).ToList();
            var notOnTheMap = inTheCode.Except(claimed).Order(StringComparer.Ordinal).ToList();
            var problems = new List<string>();

            if (notOnTheMap.Count > 0)
            {
                problems.Add($"{NotOnTheMap}: {string.Join(", ", notOnTheMap)}.");
            }

            if (notInTheCode.Count > 0)
            {
                problems.Add($"{NotInTheCode}: {string.Join(", ", notInTheCode)}.");
            }

            if (foundTwice.Count > 0)
            {
                problems.Add($"{FoundTwice}: {string.Join(", ", foundTwice)}. A move that leaves a copy behind must fail.");
            }

            if (listedTwice.Count > 0)
            {
                problems.Add($"{ListedTwice}: {string.Join(", ", listedTwice)}. A type moves to one layer; keep one entry.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail($"{string.Join(Environment.NewLine, problems)}{Environment.NewLine}{MapFixHint}");
            }
        }

        // Layers are judged per file, because a file moves whole. A file qualifies for the Domain when it is no port file and every
        // type it declares reaches only the shared Models, the base library and the types of other qualifying files; the largest
        // such set of files is kept, so two kernel-only files that reach each other both qualify.
        private static void ThenTheLayersFollowTheFiles(IReadOnlyDictionary<string, HashSet<string>> reach, IEnumerable<string> found, HashSet<string> interfaces, IReadOnlyList<ReportsType> moving)
        {
            var inTheCode = found.ToHashSet(StringComparer.Ordinal);
            var files = moving
                .GroupBy(type => type.File, StringComparer.Ordinal)
                .Select(group => new SourceFile(
                    group.Key,
                    [.. group],
                    [.. group.Select(type => new[] { type.OldFullName, type.NewFullName }.FirstOrDefault(inTheCode.Contains)).OfType<string>()]))
                .ToList();
            var domainTypes = moving.Where(type => type.Layer == ReportsLayer.Domain).SelectMany(type => new[] { type.OldFullName, type.NewFullName }).ToHashSet(StringComparer.Ordinal);

            var mismatched = new List<string>();
            foreach (var file in files)
            {
                var declaresAPort = file.Held.Any(name => interfaces.Contains(name) && ShortNameOf(name).StartsWith('I'));
                var marked = file.Types.Any(type => type.InAPortFile);
                var underMetrics = file.Types.Any(type => type.OldNamespace == ReportsMembership.MetricsNamespace);

                if (file.Types.Select(type => type.Layer).Distinct().Count() > 1)
                {
                    mismatched.Add($"{file.Name} (its types are listed in more than one layer)");
                }

                if (marked != declaresAPort || file.Types.Any(type => type.InAPortFile != marked))
                {
                    mismatched.Add($"{file.Name} (marked as a port file: {marked}; declares a port interface: {declaresAPort})");
                }

                if (marked && file.Types.Any(type => type.Layer == ReportsLayer.Domain))
                {
                    mismatched.Add($"{file.Name} (a port file listed as Domain)");
                }

                if (underMetrics && file.Types.Any(type => type.Layer == ReportsLayer.Application))
                {
                    mismatched.Add($"{file.Name} (under Metrics, but listed in the Application instead of the Application's Metrics)");
                }

                if (!underMetrics && file.Types.Any(type => type.Layer == ReportsLayer.ApplicationMetrics))
                {
                    mismatched.Add($"{file.Name} (not under Metrics, but listed in the Application's Metrics)");
                }
            }

            var reachingBeyond = files
                .Where(file => file.Types.Any(type => type.Layer == ReportsLayer.Domain))
                .SelectMany(file => file.Held.Select(name => (File: file.Name, Type: ShortNameOf(name), Beyond: ReachOf(reach, name).Where(target => !IsKernelOrBaseLibrary(target) && !domainTypes.Contains(target)).Order(StringComparer.Ordinal).ToList())))
                .Where(entry => entry.Beyond.Count > 0)
                .Select(entry => $"{entry.File}: {entry.Type} reaches {string.Join(" and ", entry.Beyond)}")
                .ToList();

            var qualifying = files.Where(file => !file.Types.Any(type => type.InAPortFile)).ToList();
            while (qualifying.RemoveAll(file => file.Held.Any(name => ReachOf(reach, name).Any(target => !IsKernelOrBaseLibrary(target) && !qualifying.Any(other => other.Held.Contains(target))))) > 0)
            {
            }

            var belongInTheDomain = qualifying.Where(file => file.Types.Any(type => type.Layer != ReportsLayer.Domain)).Select(file => file.Name).Order(StringComparer.Ordinal).ToList();
            var problems = new List<string>();

            if (mismatched.Count > 0)
            {
                problems.Add($"{FileLayerMismatch}: {string.Join("; ", mismatched)}. A file moves whole into one layer; a file that declares a port interface is a port file and stays in the Application with every record and enum it declares; a file under Metrics that is not Domain goes to the Application's Metrics and no other file does. Give every type of the file the same layer, and mark exactly the files that declare a port interface.");
            }

            if (reachingBeyond.Count > 0)
            {
                problems.Add($"{DomainReachingBeyond}: {string.Join("; ", reachingBeyond)}. The Domain of a module depends on nothing but the shared kernel, the base library and its own Domain, so it can be reasoned about alone: list the file in the Application, or take the dependency out of it.");
            }

            if (belongInTheDomain.Count > 0)
            {
                problems.Add($"{ApplicationReachingOnlyTheKernel}: {string.Join(", ", belongInTheDomain)}. A file that is no port file and depends on nothing else belongs in the module's Domain: list its types as Domain.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, problems));
            }
        }

        private static List<ReportsType> WithLayer(IEnumerable<ReportsType> moving, string file, ReportsLayer layer)
            => [.. moving.Select(type => type.File == file ? type with { Layer = layer } : type)];

        // Lists one type's file in another layer. When the code already holds the type in its new namespace, the code is made to
        // hold it where the other layer would put it, so the check judges the listed layer and not a type it cannot find.
        private static (List<ReportsType> Moving, List<string> Found, Dictionary<string, HashSet<string>> Reach) RelistedInLayer(string name, ReportsLayer layer)
        {
            var type = MovingTypeNamed(name);
            var moving = WithLayer(ReportsMembership.MovingTypes, type.File, layer);
            var inTheCode = NameInTheCode(type);
            var asListed = inTheCode == type.OldFullName ? inTheCode : moving.Single(listed => listed.Name == name).NewFullName;
            var found = ReportsTypesInTheCode.Value.Select(fullName => fullName == inTheCode ? asListed : fullName).ToList();
            var reach = ReachInTheCode.Value.ToDictionary(entry => entry.Key == inTheCode ? asListed : entry.Key, entry => entry.Value, StringComparer.Ordinal);

            return (moving, found, reach);
        }

        private static void ThenEachTypeThatStaysIsWhereListed(IEnumerable<string> typesInTheCode, IReadOnlyList<ReportsTypeThatStays> staying)
        {
            var topLevel = typesInTheCode.Select(OuterTypeOf).Where(name => !ShortNameOf(name).StartsWith('<')).ToHashSet(StringComparer.Ordinal);
            var listed = staying.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
            var notFound = staying.Where(type => !topLevel.Contains(type.FullName)).Select(type => $"{type.Name} (looked for in {type.Namespace})").ToList();
            var insideTheModule = staying.Where(type => IsIn(type.Namespace, ReportsNamespaces)).Select(type => type.Name).ToList();
            var notListed = topLevel.Where(name => IsIn(NamespaceOfName(name), ReportsNamespacesThatStay) && !listed.Contains(name)).Order(StringComparer.Ordinal).ToList();
            var listedTwice = staying.GroupBy(type => type.FullName, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            var problems = new List<string>();

            if (notFound.Count > 0)
            {
                problems.Add($"{StayingNotFound}: {string.Join(", ", notFound)}. A staying entry names who owns a Reports type that does not move; a name the code does not hold there hides a rename or a move. Put the type's current name and namespace on ReportsMembership.TypesThatStay, or take it off.");
            }

            if (insideTheModule.Count > 0)
            {
                problems.Add($"{StayingInsideTheModule}: {string.Join(", ", insideTheModule)}. Every type in the Reports namespaces moves into the module: list it on MovingTypes with its layer instead.");
            }

            if (notListed.Count > 0)
            {
                problems.Add($"{StayingNotListed}: {string.Join(", ", notListed)}. Every type of Models.Reports and of the Reports requests stays with an owner and a reason: put it on ReportsMembership.TypesThatStay.");
            }

            if (listedTwice.Count > 0)
            {
                problems.Add($"Listed as staying more than once: {string.Join(", ", listedTwice)}. Keep one entry with one owner.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, problems));
            }
        }

        private static List<(string File, ReportsTargetKind Kind, string Text)> WhenTheTargetFilesAreRead(IEnumerable<(string Path, ReportsTargetKind Kind)> named)
        {
            var root = RepositoryRoot();
            var backend = Path.Combine(root, "Lighthouse.Backend");
            var found = FilesUnder(root, backend, "GlobalSuppressions.cs", ReportsTargetKind.Suppression)
                .Concat(FilesUnder(root, backend, "*.globalconfig", ReportsTargetKind.Analyser))
                .Concat(FilesUnder(root, backend, "stryker*.json", ReportsTargetKind.Stryker))
                .Concat(FilesUnder(root, backend, "appsettings*.json", ReportsTargetKind.LogOverride))
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "Scripts", "test-selection"), "*", SearchOption.AllDirectories).Select(path => (Path: RelativePath(root, path), Kind: ReportsTargetKind.CiSelection)));
            var namedFiles = named.ToList();
            var missing = namedFiles.Where(file => !File.Exists(Path.Combine(root, file.Path))).Select(file => file.Path).ToList();
            var files = namedFiles.Where(file => !missing.Contains(file.Path)).Concat(found).DistinctBy(file => file.Path, StringComparer.Ordinal).ToList();
            var kindsNotRead = Enum.GetValues<ReportsTargetKind>().Where(kind => files.All(file => file.Kind != kind)).ToList();

            var problems = new List<string>();

            if (missing.Count > 0)
            {
                problems.Add($"These files the target scan reads were not found: {string.Join(", ", missing)}. Without them the scan for targets naming a moving Reports type would pass without having looked. If one was moved or renamed, put its new path on the list of files this test reads.");
            }

            if (kindsNotRead.Count > 0)
            {
                problems.Add($"No file was read for these kinds of target: {string.Join(", ", kindsNotRead)}. The scan must read at least one file of each kind, or it reports no target for a kind it never looked at. Put the files of that kind on the list this test reads.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, problems));
            }

            return [.. files.Select(file => (file.Path, file.Kind, File.ReadAllText(Path.Combine(root, file.Path))))];
        }

        private static IEnumerable<(string Path, ReportsTargetKind Kind)> FilesUnder(string root, string directory, string pattern, ReportsTargetKind kind)
            => Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
                .Select(path => RelativePath(root, path))
                .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
                .Select(path => (Path: path, Kind: kind));

        private static void ThenTheTargetsMatchTheList(IEnumerable<(string File, ReportsTargetKind Kind, string Text)> files, IReadOnlyList<ReportsType> moving, IReadOnlyList<ReportsTarget> listed)
        {
            var found = files.SelectMany(file => moving.SelectMany(type => TargetsNaming(type, file.Text)).Select(target => $"{file.File}: {target} ({file.Kind})"));

            InlineBaseline.AssertMatches(found, listed.Select(target => $"{target.File}: {target.Target} ({target.Kind})"),
                "A suppression, analyser, CI-selection, test-budget, host-user, Stryker or log-override target names a moving Reports type, or the test named after it, by its namespace or folder, and does not follow the type when the move changes them: the warning it silences comes back, the selection or budget misses the test, the mutation run misses the code, or the log level no longer applies. Put each such target on ReportsMembership.TargetsNamingAMovingType, written from the type's name on, with its kind; take off a target that no longer names one.");
        }

        // A moving type named through its old or new namespace, written with dots or as a folder path, from its name on; a test
        // named after the type follows it too.
        private static IEnumerable<string> TargetsNaming(ReportsType type, string text)
        {
            var places = new[] { type.OldNamespace, type.NewNamespace }
                .Select(ns => string.Join(@"[./\\]", ns["Lighthouse.Backend.".Length..].Split('.').Select(part => Regex.Escape(part))));
            var pattern = $@"(?<![\w])(?:{string.Join("|", places)})[./\\](?<target>{Regex.Escape(type.Name)}[\w`]*(?:\.[\w`]+)*)";

            return Regex.Matches(text, pattern, RegexOptions.None, PatternTimeout)
                .Select(match => match.Groups["target"].Value)
                .Select(target => target.EndsWith(".cs", StringComparison.Ordinal) ? target[..^3] : target);
        }

        private static HashSet<string> ReachOf(IReadOnlyDictionary<string, HashSet<string>> reach, string fullName)
            => reach.TryGetValue(fullName, out var targets) ? targets : new HashSet<string>(StringComparer.Ordinal);

        private static bool IsKernelOrBaseLibrary(string fullName)
        {
            var typeNamespace = NamespaceOfName(fullName);

            return IsIn(typeNamespace, KernelRules.TheModels) || IsIn(typeNamespace, ModuleRules.TheBaseLibrary);
        }

        private static string NameInTheCode(ReportsType type)
            => new[] { type.OldFullName, type.NewFullName }.FirstOrDefault(ReportsTypesInTheCode.Value.Contains) ?? type.OldFullName;

        private static ReportsType MovingTypeNamed(string name)
        {
            var type = ReportsMembership.MovingTypes.SingleOrDefault(moving => moving.Name == name);

            Assert.That(type, Is.Not.Null, $"{name} is not on ReportsMembership.MovingTypes, so the made-up cases built from it cannot run. Put it on the map, or build the cases from another moving type.");
            return type!;
        }

        private static ReportsTypeThatStays TypeThatStaysNamed(string name)
        {
            var type = ReportsMembership.TypesThatStay.SingleOrDefault(staying => staying.Name == name);

            Assert.That(type, Is.Not.Null, $"{name} is not on ReportsMembership.TypesThatStay, so the made-up cases built from it cannot run. Put it on the staying list, or build the cases from another staying type.");
            return type!;
        }

        private static string FailureOf(Action check) => Assert.Throws<AssertionException>(() => check())!.Message;

        private static string LineStartingWith(string failure, string start)
            => failure.Split(Environment.NewLine).FirstOrDefault(line => line.StartsWith(start, StringComparison.Ordinal)) ?? string.Empty;

        private static void ThenOnlyTheseAreNamed(string failure, string[] notOnTheMap, string[] notInTheCode, string[] foundTwice)
        {
            using (Assert.EnterMultipleScope())
            {
                ThenTheLineNamesExactly(LineStartingWith(failure, NotOnTheMap), notOnTheMap, "not on the map");
                ThenTheLineNamesExactly(LineStartingWith(failure, NotInTheCode), notInTheCode, "on the map but not in the code");
                ThenTheLineNamesExactly(LineStartingWith(failure, FoundTwice), foundTwice, "found in both namespaces");
            }
        }

        private static void ThenTheLineNamesExactly(string line, string[] expected, string what)
        {
            var named = line.Length == 0 ? [] : line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..].Split(". ")[0].TrimEnd('.').Split(", ")
                .Select(entry => entry.Split(" (")[0])
                .ToArray();

            Assert.That(named, Is.EquivalentTo(expected),
                $"The failure named [{string.Join(", ", named)}] as {what}, where [{string.Join(", ", expected)}] was expected. Each kind of mismatch must name exactly the types it concerns, so a reader knows which entry to fix. The failure was: {line}");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !(Directory.Exists(Path.Combine(directory.FullName, "Scripts")) && File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln"))))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Scripts and Lighthouse.Backend/Lighthouse.sln) above the test directory, so the files that may name a moving Reports type cannot be read.");
            return directory!.FullName;
        }

        private sealed record SourceFile(string Name, IReadOnlyList<ReportsType> Types, IReadOnlyList<string> Held);

        private static string RelativePath(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

        private static string OuterTypeOf(string fullName) => fullName.Split(['+', '/'])[0];

        private static string NamespaceOfName(string fullName)
        {
            var outermost = OuterTypeOf(fullName);

            return outermost[..Math.Max(0, outermost.LastIndexOf('.'))];
        }

        private static string ShortNameOf(string fullName)
        {
            var outermost = OuterTypeOf(fullName);

            return outermost[(outermost.LastIndexOf('.') + 1)..];
        }

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}
