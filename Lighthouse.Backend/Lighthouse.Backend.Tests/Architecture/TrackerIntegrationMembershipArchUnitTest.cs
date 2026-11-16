using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up Tracker integration before any of them moves into Modules/TrackerIntegration. The types
    /// are read from the compiled code: every top-level type in the connector, OAuth, connection-health and factory
    /// namespaces, old or new, plus the source selectors and the health prober found by name. Each is on the lists in
    /// TrackerIntegrationMembership, and a listed name the code no longer holds fails. A shared Models type may go with
    /// the context only when it is not stored by the database context, not sent by a controller, not reached by a
    /// Models type that stays and not used by any other code that stays outside the module, because nothing left
    /// behind may have to reach into the module for it.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationMembershipArchUnitTest
    {
        private const string TrackerIntegrationNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(WorkTrackingConnectors|OAuth|ConnectionHealth)|Services\.Factories|Factories|Modules\.TrackerIntegration)(\.|$)";

        private const string CandidateModelsNamespaces = @"^Lighthouse\.Backend\.Models\.(OAuth|ConnectionHealth|Validation|WriteBack)$";

        private const string MadeUpCandidateNamespaces = @"^Lighthouse\.Backend\.Models\.TrackerIntegrationMembershipFixtures$";

        private const string KernelNamespaces = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string Admitted = "admitted";
        private const string Stored = "stored";
        private const string OnTheWire = "on the wire";
        private const string ReachedByTheKernel = "reached by the kernel";
        private const string UsedByCodeThatStays = "used by code that stays";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        // Found by name because they live outside the context's namespaces today.
        private static readonly string[] MembersOutsideTheNamespaces =
            ["ConnectionHealthProber", "ParentSourceSelector", "DependencySourceSelector", "DependencyFieldReferences"];

        private static readonly string[] ConnectionModelsTypes =
        [
            "WorkTrackingSystemConnection", "WorkTrackingSystemConnectionOption", "WorkTrackingSystems", "WorkTrackingSystemOptionsOwner",
            "IWorkTrackingSystemOptionsOwner", "ConnectionFieldDefaults", "SupportedConnectionDefaults", "AdditionalFieldDefinition",
            "RemoteRecordStamp", "ConnectionFetchShapeChanged",
        ];

        private static readonly string[] TypesKeptForDemoData = ["DemoDataFactory", "IDemoDataFactory", "DemoProjectNames", "DemoTeamNames"];

        private const string FixHint =
            "Every Tracker integration type is either moved or kept on purpose: put it on TrackerIntegrationMembership.Moves if it goes to Modules/TrackerIntegration, on Stays with the context that keeps it, or on ModelsTypesThatMove if it is a shared Models type the context takes with it. A listed type the code no longer holds was renamed or deleted: put its new name on the list, or take it off.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationMembershipArchUnitTest).Assembly));

        // Judging reads every type of a module, so each module is judged once per run.
        private static readonly Lazy<Dictionary<string, CandidateJudgement>> ProductionJudgement =
            new(() => WhenTheCandidatesAreJudged(ProductionModule.Value, CandidatesIn(ProductionModule.Value, CandidateModelsNamespaces)));

        private static readonly Lazy<Dictionary<string, CandidateJudgement>> MadeUpJudgement =
            new(() => WhenTheCandidatesAreJudged(TestModule.Value, CandidatesIn(TestModule.Value, MadeUpCandidateNamespaces)));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheTrackerIntegrationTypesInTheCompiledCode_AreExactlyTheTypesListed()
        {
            var found = WhenTheTrackerIntegrationTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheLists(found);
        }

        [Test]
        public void OnlyTheDemoDataTypesStay_AndEachTypeThatStaysNamesTheContextThatKeepsIt()
        {
            var staying = TrackerIntegrationMembership.Stays;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(staying.Keys, Is.EquivalentTo(TypesKeptForDemoData),
                    $"Stays holds {string.Join(", ", staying.Keys)}. Every connector, OAuth, connection-health and factory type moves into Modules/TrackerIntegration except the demo-data factory, its interface and the demo project and team names, which stay with the demo data. Put any other type on Moves.");
                Assert.That(staying.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key), Is.Empty,
                    "A Stays entry names no owner. A type left behind without an owner is a type nobody will move later. Write the context that keeps it next to its name.");
            }
        }

        [Test]
        public void NoTypeIsOnTwoLists()
        {
            var lists = new[] { TrackerIntegrationMembership.Moves, [.. TrackerIntegrationMembership.Stays.Keys], TrackerIntegrationMembership.ModelsTypesThatMove };

            var onTwoLists = lists.SelectMany(list => list.Distinct(StringComparer.Ordinal))
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.That(onTwoLists, Is.Empty,
                $"{string.Join(", ", onTwoLists)} is on more than one of Moves, Stays and ModelsTypesThatMove. A type either moves as part of the context, stays, or moves as a shared Models type: keep it on one list.");
        }

        [Test]
        public void TheModelsTypesListedToMove_AreExactlyTheCandidatesNeitherStoredNorOnTheWireNorUsedOutsideTheModule()
        {
            var judged = ProductionJudgement.Value;

            ThenTheAdmittedOnesAreListed(judged);
        }

        [TestCase("OAuthCredential", Stored)]
        [TestCase("ConnectionHealthVerdict", Stored)]
        [TestCase("WorkTrackingSystemConnection", Stored)]
        [TestCase("ConnectionHealthDto", OnTheWire)]
        [TestCase("ConnectionValidationResult", OnTheWire)]
        [TestCase("OAuthCredentialStatus", ReachedByTheKernel)]
        [TestCase("ConnectionFetchShapeChanged", UsedByCodeThatStays)]
        [TestCase("OAuthCompleteResult", UsedByCodeThatStays)]
        [TestCase("WriteBackResult", UsedByCodeThatStays)]
        [TestCase("WriteBackItemResult", UsedByCodeThatStays)]
        [TestCase("WriteBackFieldUpdate", UsedByCodeThatStays)]
        [TestCase("WriteBackValueSources", UsedByCodeThatStays)]
        [TestCase("OAuthFlowContext", Admitted)]
        [TestCase("OAuthRefreshContext", Admitted)]
        [TestCase("OAuthStateClaims", Admitted)]
        [TestCase("OAuthTokens", Admitted)]
        public void ASharedModelsTypeOfTheContext_IsJudgedAsTheDesignExpects(string candidate, string expected)
        {
            var judged = ProductionJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [TestCase("AMadeUpStoredRecord", Stored)]
        [TestCase("AMadeUpConfiguredRecord", Stored)]
        [TestCase("AMadeUpAnswer", OnTheWire)]
        [TestCase("AMadeUpRefusal", OnTheWire)]
        [TestCase("AMadeUpAnswerPart", OnTheWire)]
        [TestCase("AMadeUpReachedValue", ReachedByTheKernel)]
        [TestCase("AMadeUpPublishedEvent", UsedByCodeThatStays)]
        [TestCase("AMadeUpFlow", Admitted)]
        [TestCase("AMadeUpToken", Admitted)]
        public void AMadeUpModelsType_IsJudgedByWhatTheCompiledCodeShows(string candidate, string expected)
        {
            var judged = MadeUpJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [Test]
        public void AMadeUpTypeInTheModuleOrAnOldNamespace_IsNamedAsUnlisted_AndANamesakeOutsideIsNot()
        {
            var found = WhenTheTrackerIntegrationTypesAreCollectedFrom(ProductionModule.Value)
                .Concat(WhenTheTrackerIntegrationTypesAreCollectedFrom(TestModule.Value));

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("AMadeUpConnectorInTheModule"),
                    "A made-up type under Modules.TrackerIntegration was not named as unlisted. The collection must take in the module's namespaces, or a type added there during the move would join Tracker integration unseen.");
                Assert.That(failure, Does.Contain("AMadeUpConnectorInTheOldPlace"),
                    "A made-up type under Services.Implementation.WorkTrackingConnectors was not named as unlisted. The collection must keep taking in the old namespaces until the last type has moved.");
                Assert.That(failure, Does.Not.Contain("ANamesakeOutsideTrackerIntegration"),
                    "A type in Modules.TrackerIntegrationMembershipFixtures was named as Tracker integration. The namespace pattern must end at a dot or the end of the name, not take in every namespace that starts with the same letters.");
                Assert.That(failure, Does.Not.Contain("ANestedConnectorStep"),
                    "A type nested inside a made-up Tracker integration type was named on its own. Only top-level types belong on the lists; a nested type moves with the type that holds it.");
                Assert.That(failure, Does.Not.Contain("On the list, but no longer"),
                    "Adding made-up types made a listed type look missing. The collection must not lose a real type when another assembly's types are added.");
            }
        }

        [Test]
        public void AListedTypeRenamedInTheCode_IsNamedAsMissing_AndItsNewNameAsUnlisted()
        {
            var found = WhenTheTrackerIntegrationTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => name == "JiraWorkTrackingConnector" ? "JiraTrackerConnector" : name);

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("On the list, but no longer offending: JiraWorkTrackingConnector"),
                    "A listed type the code no longer holds was not named, or other listed types were named with it. A rename during the move must fail until the list follows it.");
                Assert.That(failure, Does.Contain("Not on the list, and offending: JiraTrackerConnector"),
                    "A renamed type was not named as unlisted under its new name.");
            }
        }

        [TestCase("JiraWorkTrackingConnector", "IDeliveryForecastBlockRenderer", true)]
        [TestCase("ConnectionHealthService", "ConnectionHealthVerdictRepository", true)]
        [TestCase("ConnectionSaveService", "FetchFingerprint", true)]
        [TestCase("ConnectionSaveService", "ConnectionFetchShapeChanged", true)]
        [TestCase("ConnectionHealthService", "JiraWorkTrackingConnector", false)]
        public void AnEdgeNamedAtDesign_IsInTheCompiledCode(string dependent, string target, bool expected)
        {
            var definition = GivenTheTypeIsInTheCompiledCode(ProductionModule.Value, dependent);

            var reaches = WhenItsReferencesAreRead(ProductionModule.Value, definition).Contains(target);

            Assert.That(reaches, Is.EqualTo(expected),
                expected
                    ? $"{dependent} does not reference {target} in the compiled code. The design keeps this edge on a list when the module rules are written; if the code changed, the list of edges changes with it: tell the design owner."
                    : $"{dependent} was reported as referencing {target}, which it never does. Reading references must name only what the type's code uses, or every edge check passes for free.");
        }

        [TestCase("DemoBlockedHistoryBackfillHandler")]
        [TestCase("DemoPercentilesBackfillHandler")]
        public void ADemoBackfillHandler_CarriesTheCsvOptionNameAsText_AndNoReferenceToTheCsvConnector(string handler)
        {
            var definition = GivenTheTypeIsInTheCompiledCode(ProductionModule.Value, handler);
            var optionName = GivenTheCsvOptionThatAsksForASynthesizedStateJourney(ProductionModule.Value);

            var references = WhenItsReferencesAreRead(ProductionModule.Value, definition);
            var texts = WhenItsTextsAreRead(definition);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(texts, Does.Contain(optionName),
                    $"{handler} does not carry the CSV option name \"{optionName}\" in its compiled code. The handler reads that option from a demo connection; if it reads it another way now, the edge from Flow metrics to the CSV connector has changed: tell the design owner.");
                Assert.That(references, Does.Not.Contain("CsvWorkTrackingOptionNames"),
                    $"{handler} references CsvWorkTrackingOptionNames in its compiled code. A constant is copied into the caller, so the compiled code shows the option name as text only; a reference here means the handler now reaches the CSV connector's internals at run time, which a rule on the compiled code catches once the connector moves.");
            }
        }

        private static List<string> WhenTheTrackerIntegrationTypesAreCollectedFrom(ModuleDefinition module)
        {
            var inTheNamespaces = IlDependencies.TypesIn(module, TrackerIntegrationNamespaces).Where(name => !name.Contains('+'));
            var byName = module.Types.Select(type => type.FullName)
                .Where(name => MembersOutsideTheNamespaces.Contains(ShortNameOf(name)) || TrackerIntegrationMembership.ModelsTypesThatMove.Contains(ShortNameOf(name)));

            return [.. inTheNamespaces.Concat(byName).Select(ShortNameOf).Distinct(StringComparer.Ordinal)];
        }

        private static void ThenTheyMatchTheLists(IEnumerable<string> found)
            => InlineBaseline.AssertMatches(
                found,
                [.. TrackerIntegrationMembership.Moves, .. TrackerIntegrationMembership.Stays.Keys, .. TrackerIntegrationMembership.ModelsTypesThatMove],
                FixHint);

        private static List<TypeDefinition> CandidatesIn(ModuleDefinition module, string candidateNamespaces)
            => [.. module.Types.Where(type =>
                IsIn(type.Namespace, candidateNamespaces)
                || ConnectionModelsTypes.Contains(ShortNameOf(type.FullName))
                || TrackerIntegrationMembership.ModelsTypesThatMove.Contains(ShortNameOf(type.FullName)))];

        private static Dictionary<string, CandidateJudgement> WhenTheCandidatesAreJudged(ModuleDefinition module, List<TypeDefinition> candidates)
        {
            var candidateNames = candidates.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
            var stored = StoredTypesIn(module);
            var onTheWire = TypesSentByControllersIn(module);
            var (reachedBy, usedBy) = ReachOf(module, candidateNames, MovingMembersOf(module));

            var admitted = candidateNames.Where(name => !stored.Contains(name) && !onTheWire.Contains(name)).ToHashSet(StringComparer.Ordinal);
            int refused;
            do
            {
                refused = admitted.RemoveWhere(name => usedBy[name].Count > 0 || reachedBy[name].Any(reacher => !admitted.Contains(reacher)));
            }
            while (refused > 0);

            return candidates.ToDictionary(
                type => ShortNameOf(type.FullName),
                type => new CandidateJudgement(
                    stored.Contains(type.FullName),
                    onTheWire.Contains(type.FullName),
                    [.. reachedBy[type.FullName].Where(reacher => !admitted.Contains(reacher)).Select(ShortNameOf)],
                    [.. usedBy[type.FullName].Select(ShortNameOf).Distinct(StringComparer.Ordinal)],
                    admitted.Contains(type.FullName)),
                StringComparer.Ordinal);
        }

        private static void ThenTheAdmittedOnesAreListed(Dictionary<string, CandidateJudgement> judged)
        {
            var refusals = string.Join(" ", judged.Where(entry => !entry.Value.Admitted).Select(entry => entry.Value.Describe(entry.Key)));

            InlineBaseline.AssertMatches(
                judged.Where(entry => entry.Value.Admitted).Select(entry => entry.Key),
                TrackerIntegrationMembership.ModelsTypesThatMove,
                $"A shared Models type the context uses moves with it only when it is not stored, not sent by a controller, not reached by a Models type that stays and not used by any other code that stays outside the module: put each admitted type on ModelsTypesThatMove, and take a refused one off. The refused candidates and why: {refusals}");
        }

        private static void ThenTheCandidateIsJudged(Dictionary<string, CandidateJudgement> judged, string candidate, string expected)
        {
            Assert.That(judged, Does.ContainKey(candidate),
                $"{candidate} was not found among the candidate Models types of the compiled code, so whether it may move cannot be judged. It was renamed or deleted: tell the design owner.");

            var judgement = judged[candidate];
            var holds = expected switch
            {
                Stored => judgement.Stored,
                OnTheWire => judgement.OnTheWire,
                ReachedByTheKernel => judgement.ReachedBy.Count > 0,
                UsedByCodeThatStays => judgement.UsedBy.Count > 0,
                _ => judgement.Admitted,
            };

            Assert.That(holds, Is.True,
                $"{candidate} should be {expected}, but: {judgement.Describe(candidate)} A type the database context stores, or a controller sends, or a staying Models type reaches, or other staying code uses, cannot leave the shared kernel without breaking the stored model, the wire format or the rule that nothing left behind reaches into the module. The compiled code does not show a type sent through a variable typed object or built by a helper the controller does not call directly.");
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

        // An object handed to Ok, BadRequest, StatusCode or an ObjectResult is the value that goes on the wire; the
        // instruction just before the call is the one that produced it.
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

        // The types that move into the module as members of the context; what they use may move with them.
        private static HashSet<string> MovingMembersOf(ModuleDefinition module)
            => [.. module.Types
                .Where(type => IsIn(type.Namespace, TrackerIntegrationNamespaces) || MembersOutsideTheNamespaces.Contains(ShortNameOf(type.FullName)))
                .Where(type => !TypesKeptForDemoData.Contains(ShortNameOf(type.FullName)))
                .Select(type => type.FullName)];

        // Splits who references each candidate into Models types (the kernel) and every other type that stays; the
        // candidate itself, the moving members and the types the compiler makes are left out.
        private static (Dictionary<string, List<string>> ReachedBy, Dictionary<string, List<string>> UsedBy) ReachOf(
            ModuleDefinition module, HashSet<string> candidateNames, HashSet<string> movingMembers)
        {
            var reachedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);
            var usedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var dependent in IlDependencies.DependentsIn(module, "^", target => target is not GenericParameter && candidateNames.Contains(OutermostOf(target).FullName)))
            {
                var reacher = dependent.TypeName.Split('+')[0];

                if (movingMembers.Contains(reacher) || ShortNameOf(reacher).StartsWith('<'))
                {
                    continue;
                }

                var inTheKernel = candidateNames.Contains(reacher) || IsIn(reacher[..Math.Max(0, reacher.LastIndexOf('.'))], KernelNamespaces);

                foreach (var target in dependent.Targets.Select(target => target.Split('+')[0]).Where(target => target != reacher).Distinct(StringComparer.Ordinal))
                {
                    (inTheKernel ? reachedBy : usedBy)[target].Add(reacher);
                }
            }

            return (reachedBy, usedBy);
        }

        private static TypeDefinition GivenTheTypeIsInTheCompiledCode(ModuleDefinition module, string name)
        {
            var definition = module.Types.SingleOrDefault(type => ShortNameOf(type.FullName) == name);

            Assert.That(definition, Is.Not.Null,
                $"{name} was not found in the compiled code, so its edges cannot be read. It was renamed or deleted: tell the design owner, whose list of edges names it.");

            return definition!;
        }

        private static string GivenTheCsvOptionThatAsksForASynthesizedStateJourney(ModuleDefinition module)
        {
            var optionNames = GivenTheTypeIsInTheCompiledCode(module, "CsvWorkTrackingOptionNames");

            return (string)optionNames.Fields.Single(field => field.Name == "SynthesizeStateJourneyForDemo").Constant;
        }

        private static HashSet<string> WhenItsReferencesAreRead(ModuleDefinition module, TypeDefinition definition)
            => [.. IlDependencies.DependentsIn(module, $"^{Regex.Escape(definition.Namespace)}$", _ => true)
                .Where(dependent => dependent.TypeName == definition.FullName)
                .SelectMany(dependent => dependent.Targets)
                .Select(ShortNameOf)];

        private static HashSet<string> WhenItsTextsAreRead(TypeDefinition definition)
            => [.. TypeAndItsNestedTypes(definition)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .Where(instruction => instruction.OpCode.Code == Code.Ldstr)
                .Select(instruction => (string)instruction.Operand)];

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

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);

        private static string ShortNameOf(string fullName)
        {
            var outermost = fullName.Split(['+', '/'])[0];
            var name = outermost[(outermost.LastIndexOf('.') + 1)..];
            var arity = name.IndexOf('`');

            return arity < 0 ? name : name[..arity];
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
                    reasons.Add($"code that stays outside the module uses it ({string.Join(", ", UsedBy)})");
                }

                return reasons.Count == 0
                    ? $"{candidate} is admitted: nothing stores it, no controller sends it, and nothing that stays reaches it."
                    : $"{candidate} stays in Models: {string.Join("; ", reasons)}.";
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegration.MembershipFixtures
{
    internal static class AMadeUpConnectorInTheModule
    {
        public static int Fetched(int[] items) => items.Count(item => item > 0);

        public static string Started(string value) => global::Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures.AMadeUpFlow.Start(value).Token.Value;
    }
}

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.MembershipFixtures
{
    internal static class AMadeUpConnectorInTheOldPlace
    {
        public static ANestedConnectorStep Fetch(int id) => new(id);

        internal sealed record ANestedConnectorStep(int Id);
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationMembershipFixtures
{
    internal static class ANamesakeOutsideTrackerIntegration
    {
        public static int Fetched(int items) => items;
    }
}

namespace Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures
{
    internal sealed record AMadeUpStoredRecord(int Id);

    internal sealed record AMadeUpConfiguredRecord(int Id);

    internal sealed record AMadeUpAnswer(int Id);

    internal sealed record AMadeUpRefusal(string Reason)
    {
        public static AMadeUpRefusal Because(string reason) => new(reason);
    }

    internal sealed record AMadeUpAnswerPart(int Value);

    internal sealed record AMadeUpReachedValue(int Value);

    internal sealed record AMadeUpPublishedEvent(int ConnectionId);

    internal sealed record AMadeUpToken(string Value);

    internal sealed record AMadeUpFlow(AMadeUpToken Token)
    {
        public static AMadeUpFlow Start(string value) => new(new AMadeUpToken(value));
    }
}

namespace Lighthouse.Backend.Models.TrackerIntegrationMembershipKernelFixtures
{
    internal sealed record AMadeUpKernelRecord(global::Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures.AMadeUpReachedValue Reached);
}

namespace Lighthouse.Backend.Data.TrackerIntegrationMembershipFixtures
{
    using Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures;
    using Microsoft.EntityFrameworkCore;

    internal sealed class AMadeUpDatabaseContext(DbContextOptions<AMadeUpDatabaseContext> options) : DbContext(options)
    {
        public DbSet<AMadeUpStoredRecord> StoredRecords => Set<AMadeUpStoredRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<AMadeUpConfiguredRecord>().HasKey(record => record.Id);
    }
}

namespace Lighthouse.Backend.API.TrackerIntegrationMembershipFixtures
{
    using Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures;
    using Microsoft.AspNetCore.Mvc;

    internal sealed record AMadeUpEnvelope(AMadeUpAnswerPart Part);

    internal abstract class AMadeUpConnectionsController : ControllerBase
    {
        public ActionResult<AMadeUpAnswer> Get() => NotFound();

        public IActionResult Refuse() => StatusCode(403, AMadeUpRefusal.Because("not allowed"));

        public IActionResult Wrap() => Ok(new AMadeUpEnvelope(new AMadeUpAnswerPart(1)));
    }
}

namespace Lighthouse.Backend.Services.Implementation.TrackerIntegrationMembershipFixtures
{
    using Lighthouse.Backend.Models.TrackerIntegrationMembershipFixtures;

    internal static class AMadeUpStayingSaveService
    {
        public static AMadeUpPublishedEvent Saved(int connectionId) => new(connectionId);
    }
}
