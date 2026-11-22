using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up the platform lane before any of them moves into Lighthouse.Backend.Platform. The types are
    /// read from the compiled code: every top-level type in the Services namespaces the lane lives in today and under
    /// Lighthouse.Backend.Platform. Each is on PlatformLaneMembership, either as a type that moves, with its area, or as a type
    /// that stays, with its owner, and each listed type is found once, in its old or in its new namespace, so the map holds
    /// through the move. No domain-event handler lives outside the modules, and every suppression or analyser-config target
    /// that names a moving type is listed, because a target does not follow a type that changes namespace.
    /// A shared Models type joins the lane only when it is not stored by the database context, not sent by a controller, not
    /// reached by a Models type that stays and not used by code outside the lane.
    /// </summary>
    [TestFixture]
    public class PlatformLaneMembershipArchUnitTest
    {
        private const string PlatformLaneNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)(\.(Encryption|DatabaseManagement|UsageData|Logging|DomainEvents|Seeding|Update|BackgroundServices(\.Update)?))?|Platform(\.[A-Za-z]+)?)$";

        private const string ModuleNamespaces = @"^Lighthouse\.Backend\.Modules(\.|$)";

        private const string KernelNamespaces = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string MadeUpLaneNamespace = @"^Lighthouse\.Backend\.Platform\.LaneMembershipFixtures$";

        private const string MadeUpCandidateNamespace = @"^Lighthouse\.Backend\.Models\.PlatformLaneFixtures$";

        private const string MadeUpHandlerContract = "Lighthouse.Backend.Tests.Architecture.PlatformLaneHandlerContractFixtures.IAMadeUpEventHandler`1";

        private const string HandlerContractName = "IDomainEventHandler`1";

        private const string Admitted = "admitted";
        private const string Stored = "stored";
        private const string OnTheWire = "on the wire";
        private const string ReachedByTheKernel = "reached by the kernel";
        private const string UsedOutsideTheLane = "used outside the lane";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] AnalyserConfigFiles =
        [
            "Lighthouse.Backend/Lighthouse.Backend/GlobalSuppressions.cs", "Lighthouse.Backend/.editorconfig", "Lighthouse.Backend/Lighthouse.Backend.Tests/.editorconfig",
        ];

        private const string MapFixHint =
            "Every type in the platform lane's namespaces is on PlatformLaneMembership on purpose: put a type that moves on MovingTypes with its area and the namespace it has today, and a type that stays on TypesThatStay with the part of the code that owns it. A listed type the code no longer holds was renamed or deleted: put its new name on the map, or take it off. A type that moves is looked for in its old namespace and in Lighthouse.Backend.Platform.<Area>, nowhere else.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformLaneMembershipArchUnitTest).Assembly));

        private static readonly Lazy<Dictionary<string, CandidateJudgement>> ProductionJudgement = new(() => WhenTheModelsCandidatesAreJudged(
            ProductionModule.Value,
            type => PlatformLaneMembership.ModelsCandidates.Contains(type.FullName) || IsAListedModelsTypeInItsNewNamespace(type),
            WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value).ToHashSet(StringComparer.Ordinal)));

        private static readonly Lazy<Dictionary<string, CandidateJudgement>> MadeUpJudgement = new(() => WhenTheModelsCandidatesAreJudged(
            TestModule.Value,
            type => IsIn(type.Namespace, MadeUpCandidateNamespace),
            TestModule.Value.Types.Where(type => IsIn(type.Namespace, MadeUpLaneNamespace)).Select(type => type.FullName).ToHashSet(StringComparer.Ordinal)));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheTypesOfThePlatformLaneInTheCompiledCode_AreExactlyTheTypesTheMapListsToMoveOrToStay()
        {
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay);
        }

        [Test]
        public void EveryTypeOnTheMap_IsListedOnce_InAKnownArea_AndEveryAreaHoldsATypeThatMoves()
        {
            var entries = PlatformLaneMembership.MovingTypes.Select(type => type.OldFullName)
                .Concat(PlatformLaneMembership.TypesThatStay.Select(type => type.FullName))
                .ToList();

            ThenTheMapIsWellFormed(entries, PlatformLaneMembership.MovingTypes);
        }

        [Test]
        public void EachNamespaceThatStays_HoldsTypesInTheCompiledCode_AndNoneOfThemIsListedToMove()
        {
            var problems = PlatformLaneMembership.NamespacesThatStay
                .Where(stays => !ProductionModule.Value.Types.Any(type => type.Namespace == stays.Namespace)
                    || PlatformLaneMembership.MovingTypes.Any(moving => moving.OldNamespace == stays.Namespace))
                .Select(stays => stays.Namespace)
                .ToList();

            Assert.That(problems, Is.Empty,
                $"These namespaces are listed as staying but hold no type in the compiled code, or have a type listed to move: {string.Join(", ", problems)}. A namespace that stays is named so a reader knows who owns it; a name that holds nothing hides a rename. Fix the namespace on NamespacesThatStay, or take the moving type off one of the two lists.");
        }

        [Test]
        public void AMapWithOneMovingTypeLeftOff_NamesThatTypeAsNotOnTheMap()
        {
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value);
            var leftOff = MovingTypeNamed("CryptoService");

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, [.. PlatformLaneMembership.MovingTypes.Where(type => type != leftOff)], PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [CurrentFullNameOf(leftOff, found)], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMadeUpNameOnTheMap_IsNamedAsNotInTheCode()
        {
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value);
            var madeUp = MovingTypeNamed("CryptoService") with { Name = "AMadeUpKeyVault" };

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, [.. PlatformLaneMembership.MovingTypes, madeUp], PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["AMadeUpKeyVault"], foundTwice: []);
        }

        [Test]
        public void AListedTypeTheCodeNoLongerHolds_IsNamedAsNotInTheCode()
        {
            var listed = MovingTypeNamed("CryptoService");
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value).Where(name => name != listed.OldFullName && name != listed.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: ["CryptoService"], foundTwice: []);
        }

        [Test]
        public void ATypeFoundInItsNewNamespaceInsteadOfItsOld_IsStillOnTheMap()
        {
            var moved = MovingTypeNamed("CryptoService");
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => name == moved.OldFullName ? moved.NewFullName : name)
                .ToList();

            Assert.DoesNotThrow(() => ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay),
                "CryptoService found in Lighthouse.Backend.Platform.Encryption instead of its old namespace no longer matched the map. The map lists each type with its old namespace and looks for it in its new one too, so the move itself must not turn this check red.");
        }

        [Test]
        public void ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice()
        {
            var moved = MovingTypeNamed("CryptoService");
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value).Append(moved.NewFullName).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [], notInTheCode: [], foundTwice: ["CryptoService"]);
        }

        [Test]
        public void ATypeMovedIntoTheWrongArea_IsNamedAsNotInTheCode_AndAsNotOnTheMap()
        {
            var moved = MovingTypeNamed("CryptoService");
            var wrongArea = $"Lighthouse.Backend.Platform.Logging.{moved.Name}";
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => name == moved.OldFullName ? wrongArea : name)
                .ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [wrongArea], notInTheCode: ["CryptoService"], foundTwice: []);
        }

        [Test]
        public void ANamesakeOfAMovingTypeInAnotherLaneNamespace_IsNamedByItsFullName()
        {
            var moving = MovingTypeNamed("CryptoService");
            var otherNamespace = MovingTypeNamed("DomainEventDispatcher").OldNamespace;
            var namesake = $"{otherNamespace}.{moving.Name}";
            var found = WhenTheLaneTypesAreCollectedFrom(ProductionModule.Value).Append(namesake).ToList();

            var failure = FailureOf(() => ThenTheyMatchTheMap(found, PlatformLaneMembership.MovingTypes, PlatformLaneMembership.TypesThatStay));

            ThenOnlyTheseAreNamed(failure, notOnTheMap: [namesake], notInTheCode: [], foundTwice: []);
        }

        [Test]
        public void AMadeUpTypeUnderThePlatformNamespace_IsCollected_ItsNestedTypeIsNot_AndANamesakeOutsideThePlatformIsNot()
        {
            var collected = WhenTheLaneTypesAreCollectedFrom(TestModule.Value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collected, Does.Contain("Lighthouse.Backend.Platform.LaneMembershipFixtures.AMadeUpLaneService"),
                    "A made-up type under Lighthouse.Backend.Platform was not collected. The collection must read the new namespaces as well as the old ones, or a type added to the lane during the move would join it unseen.");
                Assert.That(collected, Has.None.Contains("ANestedLaneStep"),
                    "A type nested inside a made-up lane type was collected on its own. Only top-level types belong on the map; a nested type moves with the type that holds it.");
                Assert.That(collected, Has.None.Contains("ANamesakeOutsideThePlatform"),
                    "A type in Lighthouse.Backend.PlatformFixtures was collected as a lane type. The namespace pattern must end at a dot or the end of the name, not take in every namespace that starts with the same letters.");
            }
        }

        [Test]
        public void NoDomainEventHandlerInTheCompiledCode_LivesOutsideTheModules()
        {
            var handlers = WhenTheHandlersAreCollectedFrom(ProductionModule.Value, HandlerContractsOldAndNew());

            ThenSomeLiveInTheModules(handlers);
            ThenNoneLivesOutsideTheModules(handlers);
        }

        [Test]
        public void AMadeUpHandlerOutsideTheModules_IsNamed_EvenThroughItsBaseClass_AndOneInAModuleIsNot()
        {
            var handlers = WhenTheHandlersAreCollectedFrom(TestModule.Value, [MadeUpHandlerContract]);

            var failure = FailureOf(() => ThenNoneLivesOutsideTheModules(handlers));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("Lighthouse.Backend.Services.Implementation.PlatformLaneHandlerFixtures.AMadeUpHandlerOutsideTheModules"),
                    "A made-up handler in a Services namespace was not named. A handler belongs with the module whose state it changes.");
                Assert.That(failure, Does.Contain("AMadeUpHandlerThroughItsBaseClass"),
                    "A made-up handler that takes the handler contract from its base class was not named. The check must follow base classes, or a handler can hide behind one.");
                Assert.That(failure, Does.Contain("Lighthouse.Backend.ModulesLookalike.PlatformLaneHandlerFixtures.AMadeUpHandlerInALookalike"),
                    "A made-up handler in a namespace that only starts like Modules was not named. Modules must end at a dot or the end of the name.");
                Assert.That(failure, Does.Not.Contain("AMadeUpHandlerInAModule"),
                    "A made-up handler under Lighthouse.Backend.Modules was named. A handler inside a module is where it belongs.");
            }
        }

        [Test]
        public void TheSuppressionAndAnalyserTargetsNamingAMovingType_AreExactlyTheOnesListed()
        {
            var files = WhenTheAnalyserConfigFilesAreRead();

            ThenTheTargetsMatchTheList(files, PlatformLaneMembership.SuppressionTargets);
        }

        [Test]
        public void AnUnlistedTargetNamingAMovingType_IsNamed_AndOneNamingATypeThatStaysIsNot()
        {
            var moving = MovingTypeNamed("CryptoService");
            var staying = PlatformLaneMembership.TypesThatStay[0];
            var text = $"Target = \"~T:{moving.OldFullName}\"{Environment.NewLine}Target = \"~T:{staying.FullName}\"";
            var files = WhenTheAnalyserConfigFilesAreRead().Append(("MadeUp/GlobalSuppressions.cs", text)).ToList();

            var failure = FailureOf(() => ThenTheTargetsMatchTheList(files, PlatformLaneMembership.SuppressionTargets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain($"Not on the list, and offending: MadeUp/GlobalSuppressions.cs: {moving.Name} ({PlatformMove.KeyRing})"),
                    "A suppression target naming CryptoService by its full name was not named. Every target naming a moving type must be listed, or the move leaves it pointing at nothing and the analyser warning comes back.");
                Assert.That(failure, Does.Not.Contain(staying.Name),
                    "A target naming a type that stays was named. Only targets naming a moving type have to be rewritten.");
            }
        }

        [Test]
        public void AListedTargetRewrittenToTheNewNamespace_IsStillFound_AndOneTakenOutOfItsFileIsNamed()
        {
            var listed = PlatformLaneMembership.SuppressionTargets[0];
            var type = PlatformLaneMembership.MovingTypes.Single(moving => listed.Target.StartsWith($"{moving.Name}.", StringComparison.Ordinal) || listed.Target == moving.Name);
            var files = WhenTheAnalyserConfigFilesAreRead();
            var rewritten = files.Select(file => (file.File, file.Text.Replace(type.OldFullName, type.NewFullName, StringComparison.Ordinal))).ToList();
            var removed = files.Select(file => (file.File, file.Text.Replace(type.OldFullName, $"Lighthouse.Backend.Gone.{type.Name}", StringComparison.Ordinal))).ToList();

            Assert.DoesNotThrow(() => ThenTheTargetsMatchTheList(rewritten, PlatformLaneMembership.SuppressionTargets),
                $"The target of {type.Name} rewritten to {type.NewNamespace} no longer matched the list. The list writes targets without their namespace, so the move's rewrite must keep this check green.");
            Assert.That(FailureOf(() => ThenTheTargetsMatchTheList(removed, PlatformLaneMembership.SuppressionTargets)), Does.Contain($"On the list, but no longer offending: {listed.File}: {listed.Target}"),
                "A listed target that no longer names a moving type in its file was not named. A list that never shrinks hides a target the move forgot.");
        }

        [Test]
        public void TheModelsTypesListedToMove_AreExactlyTheCandidatesThatMeetAllFourConditions()
        {
            var judged = ProductionJudgement.Value;

            ThenTheAdmittedOnesAreListed(judged);
        }

        [TestCase("AMadeUpStoredRecord", Stored)]
        [TestCase("AMadeUpAnswer", OnTheWire)]
        [TestCase("AMadeUpReachedValue", ReachedByTheKernel)]
        [TestCase("AMadeUpValueUsedOutsideTheLane", UsedOutsideTheLane)]
        [TestCase("AMadeUpValueOnlyTheLaneUses", Admitted)]
        public void AMadeUpModelsCandidate_IsJudgedByWhatTheCompiledCodeShows(string candidate, string expected)
        {
            var judged = MadeUpJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [Test]
        public void EachStringTheMoveRenames_IsFoundInItsFile_UnderItsOldOrItsNewText()
        {
            var missing = WhenTheStringsTheMoveRenamesAreLookedFor(PlatformLaneMembership.StringsTheMoveRenames);

            ThenNoneIsMissing(missing);
        }

        [Test]
        public void AMadeUpStringTheMoveRenames_IsNamedWhenItsFileHoldsNeitherText()
        {
            var listed = PlatformLaneMembership.StringsTheMoveRenames[0];
            StringTheMoveRenames madeUp = listed with { OldText = "Lighthouse.Backend.Gone.AMadeUpKeyVault", NewText = "Lighthouse.Backend.Platform.Encryption.AMadeUpKeyVault" };

            var missing = WhenTheStringsTheMoveRenamesAreLookedFor([listed, madeUp]);

            Assert.That(FailureOf(() => ThenNoneIsMissing(missing)), Does.Contain("AMadeUpKeyVault").And.Not.Contain(listed.OldText),
                "A listed string its file does not hold was not named, or a string the file does hold was named with it. The list must fail when it names a string that is not there, or the move cannot rely on it.");
        }

        private static List<string> WhenTheLaneTypesAreCollectedFrom(ModuleDefinition module)
        {
            var inTheNamespaces = IlDependencies.TypesIn(module, PlatformLaneNamespaces).Select(OuterTypeOf);
            var listedModelsTypes = PlatformLaneMembership.MovingTypes.Where(type => IsIn(type.OldNamespace, KernelNamespaces)).Select(type => type.OldFullName).ToHashSet(StringComparer.Ordinal);
            var admittedStillInTheKernel = module.Types.Select(type => type.FullName).Where(listedModelsTypes.Contains);

            return [.. inTheNamespaces.Concat(admittedStillInTheKernel).Where(name => !ShortNameOf(name).StartsWith('<')).Distinct(StringComparer.Ordinal)];
        }

        private static void ThenTheyMatchTheMap(IEnumerable<string> found, IReadOnlyList<PlatformLaneType> moving, IReadOnlyList<TypeThatStays> staying)
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

            foreach (var type in staying)
            {
                if (!claimed.Add(type.FullName) || !inTheCode.Contains(type.FullName))
                {
                    notInTheCode.Add($"{type.Name} (looked for in {type.Namespace})");
                }
            }

            var notOnTheMap = inTheCode.Except(claimed).Order(StringComparer.Ordinal).ToList();
            var problems = new List<string>();

            if (notOnTheMap.Count > 0)
            {
                problems.Add($"Not on the map, but in the platform lane's namespaces: {string.Join(", ", notOnTheMap)}.");
            }

            if (notInTheCode.Count > 0)
            {
                problems.Add($"On the map, but not in the compiled code: {string.Join(", ", notInTheCode)}.");
            }

            if (foundTwice.Count > 0)
            {
                problems.Add($"Found in both its old and its new namespace: {string.Join(", ", foundTwice)}. A move that leaves a copy behind must fail.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail($"{string.Join(Environment.NewLine, problems)}{Environment.NewLine}{MapFixHint}");
            }
        }

        private static void ThenTheMapIsWellFormed(List<string> entries, IReadOnlyList<PlatformLaneType> moving)
        {
            var listedTwice = entries.GroupBy(entry => entry, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            var unknownAreas = moving.Where(type => !PlatformLaneMembership.Areas.ContainsKey(type.Area)).Select(type => $"{type.Name} ({type.Area})").ToList();
            var emptyAreas = PlatformLaneMembership.Areas.Keys.Where(area => moving.All(type => type.Area != area)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listedTwice, Is.Empty,
                    $"Listed more than once: {string.Join(", ", listedTwice)}. A type moves to one area or stays with one owner; keep one entry, or the move would not know where to put it.");
                Assert.That(unknownAreas, Is.Empty,
                    $"Listed in an area the platform lane does not have: {string.Join(", ", unknownAreas)}. The lane's areas are {string.Join(", ", PlatformLaneMembership.Areas.Keys)}; put the type in one of them.");
                Assert.That(emptyAreas, Is.Empty,
                    $"Areas with no type listed to move: {string.Join(", ", emptyAreas)}. Every area of the lane is there because a type goes into it; an empty one means a type is listed in the wrong area or is missing.");
            }
        }

        private static string[] HandlerContractsOldAndNew()
        {
            var contract = MovingTypeNamed(HandlerContractName);

            return [contract.OldFullName, contract.NewFullName];
        }

        private static List<string> WhenTheHandlersAreCollectedFrom(ModuleDefinition module, IReadOnlyCollection<string> contracts)
        {
            var definitions = module.Types.SelectMany(TypeAndItsNestedTypes).ToDictionary(type => type.FullName, StringComparer.Ordinal);

            return [.. module.Types.SelectMany(TypeAndItsNestedTypes)
                .Where(type => !type.IsInterface && ImplementsAContract(type, contracts, definitions))
                .Select(type => OuterTypeOf(type.FullName.Replace('/', '+')))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }

        private static bool ImplementsAContract(TypeDefinition type, IReadOnlyCollection<string> contracts, Dictionary<string, TypeDefinition> definitions)
        {
            for (TypeDefinition? current = type; current is not null;)
            {
                if (current.Interfaces.Any(implemented => contracts.Contains(implemented.InterfaceType.GetElementType().FullName)))
                {
                    return true;
                }

                var baseType = current.BaseType?.GetElementType();
                current = baseType is not null && definitions.TryGetValue(baseType.FullName, out var inThisModule) ? inThisModule : null;
            }

            return false;
        }

        private static void ThenSomeLiveInTheModules(List<string> handlers)
            => Assert.That(handlers.Where(handler => IsIn(NamespaceOfName(handler), ModuleNamespaces)), Is.Not.Empty,
                $"No domain-event handler was found inside the modules, so the check below would pass without having recognised a single handler. The handler contract is looked for as {string.Join(" or ", HandlerContractsOldAndNew())}: if it was renamed or moved elsewhere, put its new name on the map.");

        private static void ThenNoneLivesOutsideTheModules(List<string> handlers)
        {
            var outside = handlers.Where(handler => !IsIn(NamespaceOfName(handler), ModuleNamespaces)).ToList();

            Assert.That(outside, Is.Empty,
                $"Domain-event handlers outside Lighthouse.Backend.Modules: {string.Join(", ", outside)}. A handler reacts to an event for the context it belongs to, so it lives in that context's module; only the dispatcher and the handler contract are platform. Move the handler into its module's Application/Policies.");
        }

        private static List<(string File, string Text)> WhenTheAnalyserConfigFilesAreRead()
        {
            var root = RepositoryRoot();
            var globalConfigs = Directory.EnumerateFiles(Path.Combine(root, "Lighthouse.Backend"), "*.globalconfig", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));
            var suppressionFiles = Directory.EnumerateFiles(Path.Combine(root, "Lighthouse.Backend"), "GlobalSuppressions.cs", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

            var files = AnalyserConfigFiles.Concat(globalConfigs).Concat(suppressionFiles).Distinct(StringComparer.Ordinal).ToList();
            var missing = AnalyserConfigFiles.Where(path => !File.Exists(Path.Combine(root, path))).ToList();

            Assert.That(missing, Is.Empty,
                $"These analyser-config files were not found: {string.Join(", ", missing)}. Without them the scan for targets naming a moving type would pass without having looked. If one was moved or renamed, put its new path on the list of files this test reads.");

            return [.. files.Select(path => (path, File.ReadAllText(Path.Combine(root, path))))];
        }

        private static void ThenTheTargetsMatchTheList(IEnumerable<(string File, string Text)> files, IReadOnlyList<SuppressionTargetToRewrite> listed)
        {
            var found = files.SelectMany(file => PlatformLaneMembership.MovingTypes.SelectMany(type => TargetsNaming(type, file.Text)
                .Select(target => $"{file.File}: {target} ({PlatformLaneMembership.Areas[type.Area]})")));

            InlineBaseline.AssertMatches(found, listed.Select(target => $"{target.File}: {target.Target} ({target.RewrittenBy})"),
                "A suppression or analyser-config target names a type by its full name, and does not follow the type when the move changes its namespace: the analyser warning it silences comes back and the build fails. Put each target that names a moving type on PlatformLaneMembership.SuppressionTargets, written from the type's name on, with the move that rewrites it; take off a target that no longer names one.");
        }

        private static IEnumerable<string> TargetsNaming(PlatformLaneType type, string text)
        {
            var pattern = $@"(?<![\w.])(?:{Regex.Escape(type.OldNamespace)}|{Regex.Escape(type.NewNamespace)})\.(?<target>{Regex.Escape(type.Name)}(?![\w`])[^""\s]*)";

            return Regex.Matches(text, pattern, RegexOptions.None, PatternTimeout).Select(match => match.Groups["target"].Value);
        }

        private static List<string> WhenTheStringsTheMoveRenamesAreLookedFor(IEnumerable<StringTheMoveRenames> strings)
        {
            var root = RepositoryRoot();

            return [.. strings
                .Where(entry =>
                {
                    var path = Path.Combine(root, entry.File);
                    var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                    return !text.Contains(entry.OldText, StringComparison.Ordinal) && !text.Contains(entry.NewText, StringComparison.Ordinal);
                })
                .Select(entry => $"{entry.File}: {entry.OldText}")];
        }

        private static void ThenNoneIsMissing(List<string> missing)
            => Assert.That(missing, Is.Empty,
                $"Listed as renamed by the move, but its file holds neither the old nor the new text: {string.Join("; ", missing)}. The list tells each move which strings to rewrite; a string that is not there was renamed or removed already. Put the text its file holds now on PlatformLaneMembership.StringsTheMoveRenames, or take it off.");

        private static Dictionary<string, CandidateJudgement> WhenTheModelsCandidatesAreJudged(ModuleDefinition module, Func<TypeDefinition, bool> isACandidate, HashSet<string> laneMembers)
        {
            var candidates = module.Types.Where(isACandidate).ToList();
            var candidateNames = candidates.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
            var members = laneMembers.Where(name => !candidateNames.Contains(name)).ToHashSet(StringComparer.Ordinal);
            var stored = StoredTypesIn(module);
            var onTheWire = TypesSentByControllersIn(module);
            var (reachedBy, usedBy) = ReachOf(module, candidateNames, members);

            var admitted = candidateNames.Where(name => !stored.Contains(name) && !onTheWire.Contains(name)).ToHashSet(StringComparer.Ordinal);
            int refused;
            do
            {
                refused = admitted.RemoveWhere(name => usedBy[name].Count > 0 || reachedBy[name].Any(reacher => !admitted.Contains(reacher)));
            }
            while (refused > 0);

            return candidates.ToDictionary(
                type => type.FullName,
                type => new CandidateJudgement(
                    stored.Contains(type.FullName),
                    onTheWire.Contains(type.FullName),
                    [.. reachedBy[type.FullName].Where(reacher => !admitted.Contains(reacher)).Select(ShortNameOf).Distinct(StringComparer.Ordinal)],
                    [.. usedBy[type.FullName].Select(ShortNameOf).Distinct(StringComparer.Ordinal)],
                    admitted.Contains(type.FullName)),
                StringComparer.Ordinal);
        }

        private static bool IsAListedModelsTypeInItsNewNamespace(TypeDefinition type)
            => PlatformLaneMembership.MovingTypes.Any(moving => IsIn(moving.OldNamespace, KernelNamespaces) && moving.NewFullName == type.FullName);

        private static void ThenTheAdmittedOnesAreListed(Dictionary<string, CandidateJudgement> judged)
        {
            var refusals = string.Join(" ", judged.Where(entry => !entry.Value.Admitted).Select(entry => entry.Value.Describe(ShortNameOf(entry.Key))));
            var listed = PlatformLaneMembership.MovingTypes.Where(type => IsIn(type.OldNamespace, KernelNamespaces)).Select(type => type.Name);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(judged.Keys.Select(ShortNameOf), Is.EquivalentTo(PlatformLaneMembership.ModelsCandidates.Select(ShortNameOf)),
                    $"The shared Models candidates found in the compiled code are not the ones listed on PlatformLaneMembership.ModelsCandidates. A candidate that was renamed or deleted cannot be judged: put its new full name on the list, or take it off. Found: {string.Join(", ", judged.Keys)}.");
                Assert.That(judged.Where(entry => entry.Value.Admitted).Select(entry => ShortNameOf(entry.Key)), Is.EquivalentTo(listed),
                    $"The Models types listed to move are not the candidates the compiled code admits. A shared Models type joins the platform lane only when it is not stored, not sent by a controller, not reached by a Models type that stays and not used by code outside the lane: list each admitted type on MovingTypes with its Models namespace, and take a refused one off. The refused candidates and why: {refusals}");
            }
        }

        private static void ThenTheCandidateIsJudged(Dictionary<string, CandidateJudgement> judged, string candidate, string expected)
        {
            var match = judged.Where(entry => ShortNameOf(entry.Key) == candidate).Select(entry => entry.Value).ToList();

            Assert.That(match, Has.Count.EqualTo(1),
                $"{candidate} was not found once among the candidates of the compiled code, so whether it may join the lane cannot be judged. It was renamed or deleted: tell the design owner.");

            var judgement = match[0];
            var holds = expected switch
            {
                Stored => judgement.Stored,
                OnTheWire => judgement.OnTheWire,
                ReachedByTheKernel => judgement.ReachedBy.Count > 0,
                UsedOutsideTheLane => judgement.UsedBy.Count > 0,
                _ => judgement.Admitted,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holds, Is.True,
                    $"{candidate} should be {expected}, but: {judgement.Describe(candidate)} A type the database context stores, or a controller sends, or a staying Models type reaches, or code outside the lane uses, cannot leave the shared kernel without breaking the stored model, the wire format or the rule that nothing left behind reaches into the lane's internals.");
                Assert.That(judgement.Admitted, Is.EqualTo(expected == Admitted),
                    $"{candidate} should {(expected == Admitted ? string.Empty : "not ")}be admitted, but: {judgement.Describe(candidate)} One failed condition is enough to keep a type in Models.");
            }
        }

        private static HashSet<string> StoredTypesIn(ModuleDefinition module)
        {
            var types = module.Types.SelectMany(TypeAndItsNestedTypes).ToList();

            var inDbSets = types.SelectMany(type => type.Properties)
                .Select(property => property.PropertyType)
                .OfType<GenericInstanceType>()
                .Where(property => property.ElementType.FullName == "Microsoft.EntityFrameworkCore.DbSet`1")
                .Select(property => property.GenericArguments[0]);

            var configured = types.SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .Select(instruction => instruction.Operand)
                .OfType<GenericInstanceMethod>()
                .Where(method => method.DeclaringType.FullName == "Microsoft.EntityFrameworkCore.ModelBuilder" && method.Name == "Entity")
                .Select(method => method.GenericArguments[0]);

            return [.. inDbSets.Concat(configured).Select(type => OutermostOf(type).FullName)];
        }

        private static HashSet<string> TypesSentByControllersIn(ModuleDefinition module)
        {
            var controllers = module.Types.Where(IsAController).ToList();

            var inSignatures = controllers.SelectMany(controller => controller.Methods)
                .Where(method => method.IsPublic)
                .SelectMany(method => method.Parameters.Select(parameter => parameter.ParameterType).Prepend(method.ReturnType));

            var handedToResults = controllers.SelectMany(TypeAndItsNestedTypes)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(ValuesHandedToAResult);

            var sent = new HashSet<string>(StringComparer.Ordinal);
            var definitions = module.Types.SelectMany(TypeAndItsNestedTypes).ToDictionary(type => type.FullName, StringComparer.Ordinal);
            var pending = new Stack<TypeReference>(inSignatures.Concat(handedToResults));

            while (pending.Count > 0)
            {
                foreach (var type in Unwrapped(pending.Pop()).Where(type => sent.Add(type.FullName)))
                {
                    if (definitions.TryGetValue(type.FullName, out var definition))
                    {
                        foreach (var property in definition.Properties.Where(property => property.GetMethod is { IsPublic: true, IsStatic: false }))
                        {
                            pending.Push(property.PropertyType);
                        }
                    }
                }
            }

            return sent;
        }

        private static IEnumerable<TypeReference> ValuesHandedToAResult(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;

            for (var index = 1; index < instructions.Count; index++)
            {
                if (instructions[index].Operand is MethodReference called
                    && IsIn(called.DeclaringType.Namespace, @"^Microsoft\.AspNetCore\.Mvc$")
                    && called.Parameters.Count > 0
                    && called.Parameters[^1].ParameterType.FullName == "System.Object"
                    && ProducedBy(method, instructions[index - 1]) is { } produced)
                {
                    yield return produced;
                }
            }
        }

        private static TypeReference? ProducedBy(MethodDefinition method, Instruction instruction)
            => instruction.OpCode.Code switch
            {
                Code.Box => (TypeReference)instruction.Operand,
                Code.Newobj => ((MethodReference)instruction.Operand).DeclaringType,
                Code.Call or Code.Callvirt => ((MethodReference)instruction.Operand).ReturnType,
                Code.Ldfld or Code.Ldsfld => ((FieldReference)instruction.Operand).FieldType,
                Code.Ldloc_0 => method.Body.Variables[0].VariableType,
                Code.Ldloc_1 => method.Body.Variables[1].VariableType,
                Code.Ldloc_2 => method.Body.Variables[2].VariableType,
                Code.Ldloc_3 => method.Body.Variables[3].VariableType,
                Code.Ldloc_S or Code.Ldloc => ((VariableDefinition)instruction.Operand).VariableType,
                _ => null,
            };

        private static bool IsAController(TypeDefinition type)
        {
            for (var baseType = type.BaseType; baseType is not null;)
            {
                if (baseType.FullName is "Microsoft.AspNetCore.Mvc.ControllerBase" or "Microsoft.AspNetCore.Mvc.Controller")
                {
                    return true;
                }

                baseType = (baseType is GenericInstanceType generic ? generic.ElementType : baseType) is TypeDefinition inThisModule
                    ? inThisModule.BaseType
                    : null;
            }

            return false;
        }

        // Splits who references each candidate into Models types (the kernel) and every other type outside the lane; the
        // candidates, the lane's own types and the types the compiler makes are left out.
        private static (Dictionary<string, List<string>> ReachedBy, Dictionary<string, List<string>> UsedBy) ReachOf(
            ModuleDefinition module, HashSet<string> candidateNames, HashSet<string> laneMembers)
        {
            var reachedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);
            var usedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var dependent in IlDependencies.DependentsIn(module, "^", target => target is not GenericParameter && candidateNames.Contains(OutermostOf(target).FullName)))
            {
                var reacher = OuterTypeOf(dependent.TypeName);

                if (laneMembers.Contains(reacher) || ShortNameOf(reacher).StartsWith('<'))
                {
                    continue;
                }

                var inTheKernel = candidateNames.Contains(reacher) || IsIn(NamespaceOfName(reacher), KernelNamespaces);

                foreach (var target in dependent.Targets.Select(OuterTypeOf).Where(target => target != reacher).Distinct(StringComparer.Ordinal))
                {
                    (inTheKernel ? reachedBy : usedBy)[target].Add(reacher);
                }
            }

            return (reachedBy, usedBy);
        }

        private static PlatformLaneType MovingTypeNamed(string name)
        {
            var type = PlatformLaneMembership.MovingTypes.SingleOrDefault(moving => moving.Name == name);

            Assert.That(type, Is.Not.Null, $"{name} is not on PlatformLaneMembership.MovingTypes, so the made-up cases built from it cannot run. Put it on the map, or build the cases from another moving type.");
            return type!;
        }

        private static string CurrentFullNameOf(PlatformLaneType type, IEnumerable<string> found)
            => found.FirstOrDefault(name => name == type.OldFullName || name == type.NewFullName) ?? type.OldFullName;

        private static string FailureOf(Action check) => Assert.Throws<AssertionException>(() => check())!.Message;

        private static void ThenOnlyTheseAreNamed(string failure, string[] notOnTheMap, string[] notInTheCode, string[] foundTwice)
        {
            var lines = failure.Split(Environment.NewLine);
            string LineStartingWith(string start) => lines.FirstOrDefault(line => line.StartsWith(start, StringComparison.Ordinal)) ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                ThenTheLineNamesExactly(LineStartingWith("Not on the map"), notOnTheMap, "not on the map");
                ThenTheLineNamesExactly(LineStartingWith("On the map, but not"), notInTheCode, "on the map but not in the code");
                ThenTheLineNamesExactly(LineStartingWith("Found in both"), foundTwice, "found in both namespaces");
            }
        }

        private static void ThenTheLineNamesExactly(string line, string[] expected, string what)
        {
            var named = line.Length == 0 ? [] : line[(line.IndexOf(':') + 2)..].Split(". ")[0].TrimEnd('.').Split(", ")
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

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Scripts and Lighthouse.Backend/Lighthouse.sln) above the test directory, so the analyser-config files and the strings the move renames cannot be read.");
            return directory!.FullName;
        }

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [OutermostOf(generic.ElementType), .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                GenericParameter => [],
                _ => [OutermostOf(type)],
            };

        private static TypeReference OutermostOf(TypeReference type)
            => type.DeclaringType is null ? type : OutermostOf(type.DeclaringType);

        private static string OuterTypeOf(string fullName) => fullName.Split(['+', '/'])[0];

        private static string NamespaceOfName(string fullName)
        {
            var outermost = OuterTypeOf(fullName);

            return outermost[..Math.Max(0, outermost.LastIndexOf('.'))];
        }

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);

        private static string ShortNameOf(string fullName)
        {
            var outermost = OuterTypeOf(fullName);

            return outermost[(outermost.LastIndexOf('.') + 1)..];
        }

        private sealed record CandidateJudgement(bool Stored, bool OnTheWire, IReadOnlyList<string> ReachedBy, IReadOnlyList<string> UsedBy, bool Admitted)
        {
            public string Describe(string candidate)
            {
                var reasons = new List<string>();

                if (Stored)
                {
                    reasons.Add("the database context stores it");
                }

                if (OnTheWire)
                {
                    reasons.Add("a controller sends it");
                }

                if (ReachedBy.Count > 0)
                {
                    reasons.Add($"Models types that stay reach it ({string.Join(", ", ReachedBy)})");
                }

                if (UsedBy.Count > 0)
                {
                    reasons.Add($"code outside the lane uses it ({string.Join(", ", UsedBy)})");
                }

                return reasons.Count == 0
                    ? $"{candidate} is admitted: nothing stores it, no controller sends it, and nothing outside the lane reaches it."
                    : $"{candidate} stays in Models: {string.Join("; ", reasons)}.";
            }
        }
    }
}

namespace Lighthouse.Backend.Platform.LaneMembershipFixtures
{
    using Lighthouse.Backend.Models.PlatformLaneFixtures;

    internal static class AMadeUpLaneService
    {
        public static ANestedLaneStep Step(int id) => new(id);

        public static AMadeUpValueOnlyTheLaneUses Kept(int value) => new(value);

        internal sealed record ANestedLaneStep(int Id);
    }
}

namespace Lighthouse.Backend.PlatformFixtures
{
    internal static class ANamesakeOutsideThePlatform
    {
        public static int Kept(int value) => value;
    }
}

namespace Lighthouse.Backend.Models.PlatformLaneFixtures
{
    internal sealed record AMadeUpStoredRecord(int Id);

    internal sealed record AMadeUpAnswer(int Id);

    internal sealed record AMadeUpReachedValue(int Value);

    internal sealed record AMadeUpValueUsedOutsideTheLane(int Value);

    internal sealed record AMadeUpValueOnlyTheLaneUses(int Value);
}

namespace Lighthouse.Backend.Models.PlatformLaneKernelFixtures
{
    internal sealed record AMadeUpKernelRecord(global::Lighthouse.Backend.Models.PlatformLaneFixtures.AMadeUpReachedValue Reached);
}

namespace Lighthouse.Backend.Data.PlatformLaneFixtures
{
    using Lighthouse.Backend.Models.PlatformLaneFixtures;
    using Microsoft.EntityFrameworkCore;

    internal sealed class AMadeUpLaneDatabaseContext(DbContextOptions<AMadeUpLaneDatabaseContext> options) : DbContext(options)
    {
        public DbSet<AMadeUpStoredRecord> StoredRecords => Set<AMadeUpStoredRecord>();
    }
}

namespace Lighthouse.Backend.API.PlatformLaneFixtures
{
    using Lighthouse.Backend.Models.PlatformLaneFixtures;
    using Microsoft.AspNetCore.Mvc;

    internal abstract class AMadeUpLaneController : ControllerBase
    {
        public ActionResult<AMadeUpAnswer> Get() => NotFound();
    }
}

namespace Lighthouse.Backend.Services.Implementation.PlatformLaneFixtures
{
    using Lighthouse.Backend.Models.PlatformLaneFixtures;

    internal static class AMadeUpContextServiceOutsideTheLane
    {
        public static AMadeUpValueUsedOutsideTheLane Kept(int value) => new(value);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.PlatformLaneHandlerContractFixtures
{
    internal interface IAMadeUpEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}

namespace Lighthouse.Backend.Services.Implementation.PlatformLaneHandlerFixtures
{
    using Lighthouse.Backend.Tests.Architecture.PlatformLaneHandlerContractFixtures;

    internal class AMadeUpHandlerOutsideTheModules : IAMadeUpEventHandler<int>
    {
        public Task HandleAsync(int domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal sealed class AMadeUpHandlerThroughItsBaseClass : AMadeUpHandlerOutsideTheModules
    {
        public int Handled { get; init; }
    }
}

namespace Lighthouse.Backend.ModulesLookalike.PlatformLaneHandlerFixtures
{
    using Lighthouse.Backend.Tests.Architecture.PlatformLaneHandlerContractFixtures;

    internal sealed class AMadeUpHandlerInALookalike : IAMadeUpEventHandler<int>
    {
        public Task HandleAsync(int domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

namespace Lighthouse.Backend.Modules.PlatformLaneHandlerFixtures
{
    using Lighthouse.Backend.Tests.Architecture.PlatformLaneHandlerContractFixtures;

    internal sealed class AMadeUpHandlerInAModule : IAMadeUpEventHandler<int>
    {
        public Task HandleAsync(int domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
