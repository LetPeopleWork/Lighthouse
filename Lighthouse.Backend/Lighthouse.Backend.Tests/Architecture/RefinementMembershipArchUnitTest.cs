using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up the Refinement module before any of them moves into Lighthouse.Backend.Modules.Refinement. The
    /// types are read from the compiled code: every top-level type in the two Services namespaces Refinement lives in today and
    /// under Lighthouse.Backend.Modules.Refinement. Each is on RefinementMembership with its layer, and is found once, in its old
    /// namespace or in Modules.Refinement.Layer, so the map holds through the move. A type listed as Domain reaches nothing but the
    /// shared Models, the base library and the other Domain types, and a type that reaches no more than that is listed as Domain.
    /// The Refinement types that stay where they are are named with their owner and reason, and every suppression, analyser,
    /// CI-selection, test-budget, host-user or Stryker target naming a moving type is listed, because a target does not follow a
    /// type that changes namespace.
    /// </summary>
    [TestFixture]
    public class RefinementMembershipArchUnitTest
    {
        private const string RefinementNamespaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Refinement|Modules\.Refinement(\.[A-Za-z]+)*)$";

        private const string KernelOrBaseLibrary = @"^(System|Lighthouse\.Backend\.Models)(\.|$)";

        private const string NotOnTheMap = "Not on the map, but in the Refinement namespaces";
        private const string NotInTheCode = "On the map, but not in the compiled code";
        private const string FoundTwice = "Found in both its old and its new namespace";
        private const string ListedTwice = "Listed more than once";
        private const string DomainReachingBeyond = "Listed as Domain, but reaching beyond the shared Models, the base library and the Domain";
        private const string ApplicationReachingOnlyTheKernel = "Listed as Application, but no port file and reaching only the shared Models, the base library and the Domain";
        private const string FileLayerMismatch = "A file whose layer or port-file mark does not hold";
        private const string StayingNotFound = "Listed as staying, but not in the compiled code where listed";
        private const string StayingInsideTheModule = "Listed as staying, but in a Refinement namespace, where every type moves";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);


        private static readonly (string Path, RefinementTargetKind Kind)[] TargetFilesNamedByPath =
        [
            ("Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs", RefinementTargetKind.Suppression),
            ("Lighthouse.Backend/.editorconfig", RefinementTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/.editorconfig", RefinementTargetKind.Analyser),
            ("Lighthouse.Backend/Directory.Build.props", RefinementTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend/BannedSymbols.txt", RefinementTargetKind.Analyser),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/BannedSymbols.Tests.txt", RefinementTargetKind.Analyser),
            ("Scripts/test-selection/path-classifier.sh", RefinementTargetKind.CiSelection),
            ("Scripts/test-timings/test-time-budget.json", RefinementTargetKind.TestBudget),
            ("Scripts/test-timings/tests/test_check_budget_time_budgets.py", RefinementTargetKind.TestBudget),
            ("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.HostUsersStillToMove.cs", RefinementTargetKind.HostUser),
        ];

        private const string MapFixHint =
            "Every type in the Refinement namespaces is on RefinementMembership.MovingTypes on purpose, with the layer it goes to and the namespace it has today. Put an unlisted type on the map; a listed type the code no longer holds was renamed or deleted, so put its new name on the map or take it off. A listed type is looked for by its full name in its old namespace and in Lighthouse.Backend.Modules.Refinement.<Layer>, nowhere else, and must be there exactly once.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<List<string>> RefinementTypesInTheCode = new(() => WhenTheRefinementTypesAreCollectedFrom(ProductionModule.Value));

        private static readonly Lazy<HashSet<string>> InterfacesInTheCode = new(() => ProductionModule.Value.Types.Where(type => type.IsInterface).Select(type => type.FullName).ToHashSet(StringComparer.Ordinal));

        private static readonly Lazy<Dictionary<string, HashSet<string>>> ReachInTheCode = new(() => WhenTheReachOfTheRefinementTypesIsRead(ProductionModule.Value));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void TheRefinementTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove_EachFoundOnce()
        {
            var found = RefinementTypesInTheCode.Value;

            ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes);
        }

        [Test]
        public void AMapWithOneTypeLeftOff_NamesThatTypeAsNotOnTheMap()
        {
            var leftOff = MovingTypeNamed("RefinementNeedCalculator");

            var failure = FailureOf(() => ThenTheyMatchTheMap(RefinementTypesInTheCode.Value, [.. RefinementMembership.MovingTypes.Where(type => type != leftOff)]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [leftOff.OldFullName], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMadeUpNameOnTheMap_IsNamedAsNotInTheCode()
        {
            var madeUp = MovingTypeNamed("RefinementNeedCalculator") with { Name = "AMadeUpSizingClock" };

            var failure = FailureOf(() => ThenTheyMatchTheMap(RefinementTypesInTheCode.Value, [.. RefinementMembership.MovingTypes, madeUp]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["AMadeUpSizingClock"], foundTwice: []);
        }

        [Test]
        public void AListedTypeTheCodeNoLongerHolds_IsNamedAsNotInTheCode()
        {
            var listed = MovingTypeNamed("RefinementNeedCalculator");
            var found = RefinementTypesInTheCode.Value.Where(name => name != listed.OldFullName && name != listed.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["RefinementNeedCalculator"], foundTwice: []);
        }

        [Test]
        public void AMapWithOneTypeSwappedForAMadeUpOne_NamesBoth_ThoughItListsAsManyTypesAsTheCodeHolds()
        {
            var swapped = MovingTypeNamed("SizingLogCommands");
            var madeUp = swapped with { Name = "AMadeUpSizingClock" };

            var failure = FailureOf(() => ThenTheyMatchTheMap(RefinementTypesInTheCode.Value, [.. RefinementMembership.MovingTypes.Select(type => type == swapped ? madeUp : type)]));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [swapped.OldFullName], notInTheCode: ["AMadeUpSizingClock"], foundTwice: []);
        }

        [Test]
        public void ATypeFoundInItsNewNamespaceInsteadOfItsOld_IsStillOnTheMap()
        {
            var moved = MovingTypeNamed("RefinementNeedCalculator");
            var found = RefinementTypesInTheCode.Value.Select(name => name == moved.OldFullName ? moved.NewFullName : name).ToList();

            Assert.DoesNotThrow(() => ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes),
                $"RefinementNeedCalculator found in {moved.NewNamespace} instead of its old namespace no longer matched the map. The map lists each type with its old namespace and looks for it in its new one too, so the move itself must not turn this check red.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice()
        {
            var moved = MovingTypeNamed("RefinementNeedCalculator");
            var found = RefinementTypesInTheCode.Value.Append(moved.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: [], foundTwice: ["RefinementNeedCalculator"]);
        }

        [Test]
        public void ATypeMovedIntoTheOtherLayer_IsNamedAsNotInTheCode_AndAsNotOnTheMap()
        {
            var moved = MovingTypeNamed("RefinementNeedCalculator");
            var otherLayer = moved with { Layer = moved.Layer == RefinementLayer.Domain ? RefinementLayer.Application : RefinementLayer.Domain };
            var found = RefinementTypesInTheCode.Value.Select(name => name == moved.OldFullName ? otherLayer.NewFullName : name).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [otherLayer.NewFullName], notInTheCode: ["RefinementNeedCalculator"], foundTwice: []);
        }

        [Test]
        public void ANamesakeOfAMovingTypeInTheOtherOldNamespace_IsNamedByItsFullName()
        {
            var moving = MovingTypeNamed("RefinementNeedCalculator");
            var otherNamespace = moving.OldNamespace == RefinementMembership.ImplementationNamespace ? RefinementMembership.InterfacesNamespace : RefinementMembership.ImplementationNamespace;
            var namesake = $"{otherNamespace}.{moving.Name}";
            var found = RefinementTypesInTheCode.Value.Append(namesake).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, RefinementMembership.MovingTypes));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [namesake], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMapListingOneTypeTwice_NamesIt()
        {
            var twice = MovingTypeNamed("RefinementNeedCalculator");

            var failure = FailureOf(() => ThenTheyMatchTheMap(RefinementTypesInTheCode.Value, [.. RefinementMembership.MovingTypes, twice with { Layer = twice.Layer == RefinementLayer.Domain ? RefinementLayer.Application : RefinementLayer.Domain }]));

            Assert.That(LineStartingWith(failure, ListedTwice), Does.Contain("RefinementNeedCalculator"),
                $"A type listed twice, once in each layer, was not named. A type moves to one layer only; the map must say which. The failure was: {failure}");
        }

        [Test]
        public void TheCollection_TakesEveryTopLevelTypeInTheOldAndTheNewNamespaces_AndNothingElse()
        {
            string[] names =
            [
                $"{RefinementMembership.ImplementationNamespace}.AMadeUpOldService",
                $"{RefinementMembership.InterfacesNamespace}.IAMadeUpOldPort",
                "Lighthouse.Backend.Modules.Refinement.Domain.AMadeUpRule",
                "Lighthouse.Backend.Modules.Refinement.Application.AMadeUpService/ANestedStep",
                "Lighthouse.Backend.Modules.Refinement.Adapters.Persistence.AMadeUpAdapter",
                "Lighthouse.Backend.Modules.Refinement.AMadeUpRootType",
                $"{RefinementMembership.ImplementationNamespace}.<>c__DisplayClass0_0",
                "Lighthouse.Backend.Modules.RefinementLookalike.AMadeUpLookalike",
                "Lighthouse.Backend.Services.Implementation.RefinementLookalike.AMadeUpLookalike",
                "Lighthouse.Backend.Services.Implementation.AMadeUpServiceBesideRefinement",
                "Lighthouse.Backend.Tests.Modules.Refinement.Domain.AMadeUpRuleTest",
            ];

            var collected = RefinementTypesAmong(names);

            Assert.That(collected, Is.EquivalentTo(new[]
            {
                $"{RefinementMembership.ImplementationNamespace}.AMadeUpOldService",
                $"{RefinementMembership.InterfacesNamespace}.IAMadeUpOldPort",
                "Lighthouse.Backend.Modules.Refinement.Domain.AMadeUpRule",
                "Lighthouse.Backend.Modules.Refinement.Application.AMadeUpService",
                "Lighthouse.Backend.Modules.Refinement.Adapters.Persistence.AMadeUpAdapter",
                "Lighthouse.Backend.Modules.Refinement.AMadeUpRootType",
            }),
                "The Refinement types collected from a made-up list of names are not the top-level types of the old and new Refinement namespaces. The collection must read both old namespaces and every namespace under Lighthouse.Backend.Modules.Refinement, take a nested type as the type that holds it, leave out what the compiler makes, and stop at the end of a namespace name, or a type added during the move would join the module unseen and a lookalike would join it by mistake.");
        }

        [Test]
        public void EachFileIsDomain_ExactlyWhenItIsNoPortFile_AndEveryTypeItDeclaresReachesOnlyTheSharedModelsTheBaseLibraryAndTheDomain()
        {
            var reach = ReachInTheCode.Value;

            ThenTheLayersFollowTheFiles(reach, RefinementTypesInTheCode.Value, InterfacesInTheCode.Value, RefinementMembership.MovingTypes);
        }

        [Test]
        public void AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches()
        {
            var moving = WithLayer(RefinementMembership.MovingTypes, "SizingRefusal.cs", RefinementLayer.Domain);

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, RefinementTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, DomainReachingBeyond);

            Assert.That(line, Does.Contain("SizingRefusal.cs: SizingRefusal reaches").And.Contain("Microsoft.Extensions.Logging.ILogger").And.Not.Contain("NeedBand"),
                $"SizingRefusal.cs, whose type writes to the logger, was listed as Domain and not named alone with what it reaches. A Domain file depends on nothing but the shared Models, the base library and the other Domain files. The failure was: {failure}");
        }

        [Test]
        public void AKernelOnlyFileThatIsNoPortFileListedAsApplication_IsNamed_AndAPortFileIsNot()
        {
            var moving = WithLayer(RefinementMembership.MovingTypes, "NeedBand.cs", RefinementLayer.Application);

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, RefinementTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, ApplicationReachingOnlyTheKernel);

            Assert.That(line, Does.Contain("NeedBand.cs").And.Not.Contain("IRefinementCalendar.cs").And.Not.Contain("ISizingLogCommands.cs"),
                $"NeedBand.cs, which reaches only the base library and the shared Models, was listed as Application and not named, or a port file was named with it. A file that depends on nothing else belongs in the Domain, unless it declares a port, which stays in the Application with its records. The failure was: {failure}");
        }

        [Test]
        public void APortFileSplitAcrossLayers_OrAFileMarkedAsAPortFileWithoutAPortInterface_IsNamed()
        {
            var moving = RefinementMembership.MovingTypes
                .Select(type => type.Name == "RefinementCycle" ? type with { Layer = RefinementLayer.Domain } : type)
                .Select(type => type.File == "NeedBand.cs" ? type with { InAPortFile = true } : type)
                .ToList();

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(ReachInTheCode.Value, RefinementTypesInTheCode.Value, InterfacesInTheCode.Value, moving));
            var line = LineStartingWith(failure, FileLayerMismatch);

            Assert.That(line, Does.Contain("IRefinementCalendar.cs").And.Contain("NeedBand.cs").And.Not.Contain("ISizingLogCommands.cs"),
                $"A port file with one record listed as Domain, or a file marked as a port file that declares no port interface, was not named, or a sound port file was. A file moves whole into one layer, and a port file stays in the Application with every record it declares. The failure was: {failure}");
        }

        [Test]
        public void AMadeUpDomainTypeReachingALookalikeOfTheKernelOrANamesakeOfADomainType_IsNamed_AndTheKernelAndTheMovedDomainAreNot()
        {
            var needBand = MovingTypeNamed("NeedBand");
            var madeUp = new RefinementType("AMadeUpDomainRule", RefinementLayer.Domain, RefinementMembership.ImplementationNamespace, "AMadeUpDomainRule.cs", InAPortFile: false);
            var namesake = $"{RefinementMembership.InterfacesNamespace}.{needBand.Name}";
            var reach = ReachInTheCode.Value.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            reach[madeUp.OldFullName] = ["System.Math", "Lighthouse.Backend.Models.Team", needBand.NewFullName, namesake, "Lighthouse.Backend.ModelsLookalike.AMadeUpValue", "Systematic.AMadeUpHelper"];
            var found = RefinementTypesInTheCode.Value.Append(madeUp.OldFullName).ToList();

            var failure = FailureOf(() => ThenTheLayersFollowTheFiles(reach, found, InterfacesInTheCode.Value, [.. RefinementMembership.MovingTypes, madeUp]));
            var line = LineStartingWith(failure, DomainReachingBeyond);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain(namesake).And.Contain("Lighthouse.Backend.ModelsLookalike.AMadeUpValue").And.Contain("Systematic.AMadeUpHelper"),
                    $"A made-up Domain type reaching a namesake of a Domain type in another namespace, or a namespace that only starts like Models or System, was not named with it. Types must be matched by their full names and namespaces must end at a dot, or anything can hide behind a familiar name. The failure was: {failure}");
                Assert.That(line, Does.Not.Contain("Lighthouse.Backend.Models.Team").And.Not.Contain("System.Math").And.Not.Contain(needBand.NewFullName),
                    $"A made-up Domain type was named for reaching a Models type, the base library, or a Domain type in its new namespace. The Domain may use all three, before and after the move. The failure was: {failure}");
            }
        }

        [Test]
        public void EachTypeThatStays_IsInTheCompiledCodeWhereListed_OutsideTheRefinementNamespaces()
        {
            ThenEachTypeThatStaysIsWhereListed(ProductionModule.Value.Types.Select(type => type.FullName), RefinementMembership.TypesThatStay);
        }

        [Test]
        public void ATypeThatStaysListedInTheWrongPlace_OrUnderAMadeUpName_OrInsideTheModule_IsNamed()
        {
            var repository = RefinementMembership.TypesThatStay.Single(type => type.Name == "SizingLogRepository");
            var inTheWrongPlace = repository with { Namespace = "Lighthouse.Backend.Services.Interfaces.Repositories" };
            var madeUp = repository with { Name = "AMadeUpSizingArchive" };
            var insideTheModule = repository with { Name = "SizingLogCommands", Namespace = RefinementMembership.ImplementationNamespace };

            var failure = FailureOf(() => ThenEachTypeThatStaysIsWhereListed(
                ProductionModule.Value.Types.Select(type => type.FullName),
                [.. RefinementMembership.TypesThatStay.Where(type => type != repository), inTheWrongPlace, madeUp, insideTheModule]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LineStartingWith(failure, StayingNotFound), Does.Contain("SizingLogRepository (looked for in Lighthouse.Backend.Services.Interfaces.Repositories)").And.Contain("AMadeUpSizingArchive"),
                    $"A staying type listed in a namespace that does not hold it, or under a name the code does not have, was not named. A staying entry tells feature work who owns the type; a wrong one hides a rename. The failure was: {failure}");
                Assert.That(LineStartingWith(failure, StayingInsideTheModule), Does.Contain("SizingLogCommands"),
                    $"A type in a Refinement namespace was accepted as staying. Every type in those namespaces moves; it belongs on the map with its layer. The failure was: {failure}");
                Assert.That(failure, Does.Not.Contain("ISizingLogRepository"),
                    $"A staying type listed correctly was named. The failure was: {failure}");
            }
        }

        [Test]
        public void TheTargetsNamingAMovingType_AreExactlyTheOnesListed()
        {
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath);

            ThenTheTargetsMatchTheList(files, RefinementMembership.MovingTypes, RefinementMembership.TargetsNamingAMovingType);
        }

        [Test]
        public void AnUnlistedTargetNamingAMovingType_IsNamed_ByItsOldOrNewNameOrPath_AndOneNamingWhatStaysIsNot()
        {
            var calculator = MovingTypeNamed("RefinementNeedCalculator");
            var commands = MovingTypeNamed("SizingLogCommands");
            var text = string.Join(Environment.NewLine,
                $"Target = \"~T:{calculator.OldFullName}\"",
                $"\"{commands.NewFullName}.Append\"",
                "Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Refinement/RefinementViewQuery.cs",
                "Lighthouse.Backend.Tests.Services.Implementation.Refinement.StageRuleMatcherTest",
                "Target = \"~T:Lighthouse.Backend.Services.Implementation.Repositories.SizingLogRepository\"",
                "Lighthouse.Backend.Tests.API.Integration.Refinement.Slice13ReadinessTest",
                "Lighthouse.Backend.Services.Implementation.RefinementLookalike.SleYardstickResolver");
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath).Append(("MadeUp/GlobalSuppressions.cs", RefinementTargetKind.Suppression, text)).ToList();

            var failure = FailureOf(() => ThenTheTargetsMatchTheList(files, RefinementMembership.MovingTypes, RefinementMembership.TargetsNamingAMovingType));
            var line = LineStartingWith(failure, "Not on the list");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain("MadeUp/GlobalSuppressions.cs: RefinementNeedCalculator (Suppression)")
                    .And.Contain("MadeUp/GlobalSuppressions.cs: SizingLogCommands.Append (Suppression)")
                    .And.Contain("MadeUp/GlobalSuppressions.cs: RefinementViewQuery (Suppression)")
                    .And.Contain("MadeUp/GlobalSuppressions.cs: StageRuleMatcherTest (Suppression)"),
                    $"A made-up target naming a moving type by its old full name, its new full name, its old path or its test's old name was not named. Every such target must be listed, or the move leaves it pointing at nothing. The failure was: {failure}");
                Assert.That(line, Does.Not.Contain("SizingLogRepository").And.Not.Contain("Slice13ReadinessTest").And.Not.Contain("SleYardstickResolver"),
                    $"A target naming a type that stays, a test that stays, or a namesake in a lookalike namespace was named. Only a target naming a moving type in its own namespace has to follow the move. The failure was: {failure}");
            }
        }

        [Test]
        public void AListedTargetNoLongerInItsFile_IsNamed_AndOneRewrittenToTheNewNamespaceIsStillFound()
        {
            var calculator = MovingTypeNamed("RefinementNeedCalculator");
            RefinementTarget listed = new("MadeUp/stryker.refinement.json", "RefinementNeedCalculator", RefinementTargetKind.Stryker);
            var files = WhenTheTargetFilesAreRead(TargetFilesNamedByPath);
            var rewritten = files.Append((listed.File, listed.Kind, $"\"mutate\": [\"{calculator.NewFullName}\"]")).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.DoesNotThrow(() => ThenTheTargetsMatchTheList(rewritten, RefinementMembership.MovingTypes, [.. RefinementMembership.TargetsNamingAMovingType, listed]),
                    "A listed target rewritten to the type's new namespace no longer matched the list. The list writes targets without their namespace, so the move's rewrite must keep this check green.");
                Assert.That(FailureOf(() => ThenTheTargetsMatchTheList(files, RefinementMembership.MovingTypes, [.. RefinementMembership.TargetsNamingAMovingType, listed])),
                    Does.Contain("On the list, but no longer offending: MadeUp/stryker.refinement.json: RefinementNeedCalculator (Stryker)"),
                    "A listed target that its file no longer holds was not named. A list that never shrinks hides a target the move forgot.");
            }
        }

        [Test]
        public void AFileTheTargetScanMustRead_IsNamedWhenItIsMissing_SoAnEmptyScanIsNeverTakenForNoTargets()
        {
            (string Path, RefinementTargetKind Kind)[] named = [.. TargetFilesNamedByPath, ("Scripts/test-selection/a-made-up-classifier.sh", RefinementTargetKind.CiSelection)];

            var failure = FailureOf(() => WhenTheTargetFilesAreRead(named));

            Assert.That(failure, Does.Contain("Scripts/test-selection/a-made-up-classifier.sh").And.Not.Contain("path-classifier.sh,"),
                $"A file the target scan must read, missing from the repository, was not named, or a file that exists was. Without the file the scan would find no target and pass without having looked. The failure was: {failure}");
        }

        private static List<string> WhenTheRefinementTypesAreCollectedFrom(ModuleDefinition module)
            => RefinementTypesAmong(module.Types.Select(type => type.FullName));

        private static List<string> RefinementTypesAmong(IEnumerable<string> fullNames)
            => [.. fullNames.Select(OuterTypeOf)
                .Where(name => !ShortNameOf(name).StartsWith('<') && IsIn(NamespaceOfName(name), RefinementNamespaces))
                .Distinct(StringComparer.Ordinal)];

        // Each Refinement type with every type its compiled code references, nested and compiler-made types folded into the type
        // that holds them, and the type itself left out.
        private static Dictionary<string, HashSet<string>> WhenTheReachOfTheRefinementTypesIsRead(ModuleDefinition module)
            => IlDependencies.DependentsIn(module, RefinementNamespaces, target => target is not GenericParameter)
                .GroupBy(dependent => OuterTypeOf(dependent.TypeName), StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(dependent => dependent.Targets)
                        .Select(OuterTypeOf)
                        .Where(target => target != group.Key && !ShortNameOf(target).StartsWith('<'))
                        .ToHashSet(StringComparer.Ordinal),
                    StringComparer.Ordinal);

        private static void ThenTheyMatchTheMap(IEnumerable<string> found, IReadOnlyList<RefinementType> moving)
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
        private static void ThenTheLayersFollowTheFiles(IReadOnlyDictionary<string, HashSet<string>> reach, IEnumerable<string> found, HashSet<string> interfaces, IReadOnlyList<RefinementType> moving)
        {
            var inTheCode = found.ToHashSet(StringComparer.Ordinal);
            var files = moving
                .GroupBy(type => type.File, StringComparer.Ordinal)
                .Select(group => new SourceFile(
                    group.Key,
                    [.. group],
                    [.. group.Select(type => new[] { type.OldFullName, type.NewFullName }.FirstOrDefault(inTheCode.Contains)).OfType<string>()]))
                .ToList();
            var domainTypes = moving.Where(type => type.Layer == RefinementLayer.Domain).SelectMany(type => new[] { type.OldFullName, type.NewFullName }).ToHashSet(StringComparer.Ordinal);

            var mismatched = new List<string>();
            foreach (var file in files)
            {
                var declaresAPort = file.Held.Any(name => interfaces.Contains(name) && ShortNameOf(name).StartsWith('I'));
                var marked = file.Types.Any(type => type.InAPortFile);

                if (file.Types.Select(type => type.Layer).Distinct().Count() > 1)
                {
                    mismatched.Add($"{file.Name} (its types are listed in more than one layer)");
                }

                if (marked != declaresAPort || file.Types.Any(type => type.InAPortFile != marked))
                {
                    mismatched.Add($"{file.Name} (marked as a port file: {marked}; declares a port interface: {declaresAPort})");
                }

                if (marked && file.Types.Any(type => type.Layer == RefinementLayer.Domain))
                {
                    mismatched.Add($"{file.Name} (a port file listed as Domain)");
                }
            }

            var reachingBeyond = files
                .Where(file => file.Types.Any(type => type.Layer == RefinementLayer.Domain))
                .SelectMany(file => file.Held.Select(name => (File: file.Name, Type: ShortNameOf(name), Beyond: ReachOf(reach, name).Where(target => !IsIn(NamespaceOfName(target), KernelOrBaseLibrary) && !domainTypes.Contains(target)).Order(StringComparer.Ordinal).ToList())))
                .Where(entry => entry.Beyond.Count > 0)
                .Select(entry => $"{entry.File}: {entry.Type} reaches {string.Join(" and ", entry.Beyond)}")
                .ToList();

            var qualifying = files.Where(file => !file.Types.Any(type => type.InAPortFile)).ToList();
            while (qualifying.RemoveAll(file => file.Held.Any(name => ReachOf(reach, name).Any(target => !IsIn(NamespaceOfName(target), KernelOrBaseLibrary) && !qualifying.Any(other => other.Held.Contains(target))))) > 0)
            {
            }

            var belongInTheDomain = qualifying.Where(file => file.Types.Any(type => type.Layer == RefinementLayer.Application)).Select(file => file.Name).Order(StringComparer.Ordinal).ToList();
            var problems = new List<string>();

            if (mismatched.Count > 0)
            {
                problems.Add($"{FileLayerMismatch}: {string.Join("; ", mismatched)}. A file moves whole into one layer; a file that declares a port interface is a port file and stays in the Application with every record and enum it declares. Give every type of the file the same layer, and mark exactly the files that declare a port interface.");
            }

            if (reachingBeyond.Count > 0)
            {
                problems.Add($"{DomainReachingBeyond}: {string.Join("; ", reachingBeyond)}. The Domain of a module depends on nothing but the shared kernel, the base library and its own Domain, so it can be reasoned about alone: list the file as Application, or take the dependency out of it.");
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

        private static List<RefinementType> WithLayer(IEnumerable<RefinementType> moving, string file, RefinementLayer layer)
            => [.. moving.Select(type => type.File == file ? type with { Layer = layer } : type)];

        private static void ThenEachTypeThatStaysIsWhereListed(IEnumerable<string> typesInTheCode, IReadOnlyList<RefinementTypeThatStays> staying)
        {
            var inTheCode = typesInTheCode.ToHashSet(StringComparer.Ordinal);
            var notFound = staying.Where(type => !inTheCode.Contains(type.FullName)).Select(type => $"{type.Name} (looked for in {type.Namespace})").ToList();
            var insideTheModule = staying.Where(type => IsIn(type.Namespace, RefinementNamespaces)).Select(type => type.Name).ToList();
            var listedTwice = staying.GroupBy(type => type.FullName, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            var problems = new List<string>();

            if (notFound.Count > 0)
            {
                problems.Add($"{StayingNotFound}: {string.Join(", ", notFound)}. A staying entry names who owns a Refinement type that does not move; a name the code does not hold there hides a rename or a move. Put the type's current name and namespace on RefinementMembership.TypesThatStay, or take it off.");
            }

            if (insideTheModule.Count > 0)
            {
                problems.Add($"{StayingInsideTheModule}: {string.Join(", ", insideTheModule)}. Every type in the Refinement namespaces moves into the module: list it on MovingTypes with its layer instead.");
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

        private static List<(string File, RefinementTargetKind Kind, string Text)> WhenTheTargetFilesAreRead(IEnumerable<(string Path, RefinementTargetKind Kind)> named)
        {
            var root = RepositoryRoot();
            var backend = Path.Combine(root, "Lighthouse.Backend");
            var found = FilesUnder(root, backend, "GlobalSuppressions.cs", RefinementTargetKind.Suppression)
                .Concat(FilesUnder(root, backend, "*.globalconfig", RefinementTargetKind.Analyser))
                .Concat(FilesUnder(root, backend, "stryker*.json", RefinementTargetKind.Stryker))
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "Scripts", "test-selection"), "*", SearchOption.AllDirectories).Select(path => (Path: RelativePath(root, path), Kind: RefinementTargetKind.CiSelection)));
            var namedFiles = named.ToList();
            var missing = namedFiles.Where(file => !File.Exists(Path.Combine(root, file.Path))).Select(file => file.Path).ToList();
            var files = namedFiles.Where(file => !missing.Contains(file.Path)).Concat(found).DistinctBy(file => file.Path, StringComparer.Ordinal).ToList();
            var kindsNotRead = Enum.GetValues<RefinementTargetKind>().Where(kind => files.All(file => file.Kind != kind)).ToList();

            var problems = new List<string>();

            if (missing.Count > 0)
            {
                problems.Add($"These files the target scan reads were not found: {string.Join(", ", missing)}. Without them the scan for targets naming a moving Refinement type would pass without having looked. If one was moved or renamed, put its new path on the list of files this test reads.");
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

        private static IEnumerable<(string Path, RefinementTargetKind Kind)> FilesUnder(string root, string directory, string pattern, RefinementTargetKind kind)
            => Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
                .Select(path => RelativePath(root, path))
                .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
                .Select(path => (Path: path, Kind: kind));

        private static void ThenTheTargetsMatchTheList(IEnumerable<(string File, RefinementTargetKind Kind, string Text)> files, IReadOnlyList<RefinementType> moving, IReadOnlyList<RefinementTarget> listed)
        {
            var found = files.SelectMany(file => moving.SelectMany(type => TargetsNaming(type, file.Text)).Select(target => $"{file.File}: {target} ({file.Kind})"));

            InlineBaseline.AssertMatches(found, listed.Select(target => $"{target.File}: {target.Target} ({target.Kind})"),
                "A suppression, analyser, CI-selection, test-budget, host-user or Stryker target names a moving Refinement type, or the test named after it, by its namespace or folder, and does not follow the type when the move changes them: the warning it silences comes back, the selection or budget misses the test, or the mutation run misses the code. Put each such target on RefinementMembership.TargetsNamingAMovingType, written from the type's name on, with its kind; take off a target that no longer names one.");
        }

        // A moving type named through its old or new namespace, written with dots or as a folder path, from its name on; a test
        // named after the type follows it too.
        private static IEnumerable<string> TargetsNaming(RefinementType type, string text)
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

        private static RefinementType MovingTypeNamed(string name)
        {
            var type = RefinementMembership.MovingTypes.SingleOrDefault(moving => moving.Name == name);

            Assert.That(type, Is.Not.Null, $"{name} is not on RefinementMembership.MovingTypes, so the made-up cases built from it cannot run. Put it on the map, or build the cases from another moving type.");
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

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Scripts and Lighthouse.Backend/Lighthouse.sln) above the test directory, so the files that may name a moving Refinement type cannot be read.");
            return directory!.FullName;
        }

        private sealed record SourceFile(string Name, IReadOnlyList<RefinementType> Types, IReadOnlyList<string> Held);

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
