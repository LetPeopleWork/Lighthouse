using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up Promises before any of them moves into Modules/Promises. The types are read from the
    /// compiled code: every top-level type in the delivery-source namespaces or under Modules.Promises, plus the rule
    /// service, the metric-values projector and the two reactions to updated forecasts, found by name in the namespaces
    /// they live in today. Each is on the lists in PromisesMembership, and a listed name the code no longer holds fails.
    /// A shared Models type of the Deliveries may go with the context only when it is not stored by the database context,
    /// not sent by a controller, not reached by a Models type that stays and not used by any other code that stays
    /// outside the module, because nothing left behind may have to reach into the module for it.
    /// </summary>
    [TestFixture]
    public class PromisesMembershipArchUnitTest
    {
        private const string PromisesNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.DeliverySources|Modules\.Promises)(\.|$)";

        // The flat Services namespaces hold many other contexts' types, so only the named ones are taken from there.
        private const string NamespacesOfTheMembersFoundByName =
            @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)(\.DomainEvents)?$";

        private const string KernelNamespaces = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string DeliveryModelsNamespace = @"^Lighthouse\.Backend\.Models\.DeliverySources$";

        private const string ThePromisesDomain = @"^Lighthouse\.Backend\.Modules\.Promises\.Domain(\.|$)";

        private const string Admitted = "admitted";
        private const string Stored = "stored";
        private const string OnTheWire = "on the wire";
        private const string ReachedByTheKernel = "reached by the kernel";
        private const string UsedByCodeThatStays = "used by code that stays";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] MembersFoundByName =
        [
            "DeliveryRuleService", "IDeliveryRuleService", "DeliveryMetricValuesProjector", "DeliveryMetricValues",
            "DeliveryMetricSnapshotRecordingHandler", "DeliveryForecastPublishingHandler",
        ];

        private const string FixHint =
            "Every Promises type is on a list on purpose: put it on PromisesMembership.Moves if it goes to Modules/Promises, or on ModelsTypesThatMove if it is a shared Models type the context takes with it. A listed type the code no longer holds was renamed or deleted: put its new name on the list, or take it off. A name the code holds twice is listed with its namespace, so one type cannot hide behind the other.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PromisesMembershipArchUnitTest).Assembly));

        // Judging reads every type of the module, so it runs once per run.
        private static readonly Lazy<Dictionary<string, CandidateJudgement>> ProductionJudgement =
            new(() => WhenTheDeliveryModelsTypesAreJudged(ProductionModule.Value));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void ThePromisesTypesInTheCompiledCode_AreExactlyTheTypesListed()
        {
            var found = WhenThePromisesTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheLists(found);
        }

        [Test]
        public void NoTypeIsOnBothLists()
        {
            var onBoth = PromisesMembership.Moves.Intersect(PromisesMembership.ModelsTypesThatMove, StringComparer.Ordinal).ToList();

            Assert.That(onBoth, Is.Empty,
                $"{string.Join(", ", onBoth)} is on both Moves and ModelsTypesThatMove. A type either moves as part of the context or as a shared Models type: keep it on one list.");
        }

        [Test]
        public void TheModelsTypesListedToMove_AreExactlyTheDeliveryCandidatesNeitherStoredNorOnTheWireNorUsedOutsideTheModule()
        {
            var judged = ProductionJudgement.Value;

            ThenTheAdmittedOnesAreListed(judged);
        }

        [TestCase("Delivery", Stored)]
        [TestCase("DeliveryClosureRecord", Stored)]
        [TestCase("DeliveryMetricSnapshot", Stored)]
        [TestCase("DeliveryNote", Stored)]
        [TestCase("DeliverySelectionMode", OnTheWire)]
        [TestCase("DeliverySourceUnavailableReason", OnTheWire)]
        [TestCase("DeliveryArchivedException", ReachedByTheKernel)]
        [TestCase("DeliveryCompletionForecast", ReachedByTheKernel)]
        [TestCase("DeliveryFeatureMetric", ReachedByTheKernel)]
        [TestCase("DeliveryForecastRow", ReachedByTheKernel)]
        [TestCase("DeliveryMetricsProjection", ReachedByTheKernel)]
        [TestCase("DeliverySourceBoundException", ReachedByTheKernel)]
        [TestCase("DeliveryWhenPercentile", ReachedByTheKernel)]
        [TestCase("DeliverySourceProject", ReachedByTheKernel)]
        [TestCase("DeliverySourceSnapshot", ReachedByTheKernel)]
        [TestCase("SourceOptionBlockReason", ReachedByTheKernel)]
        [TestCase("DeliveryForecastPublication", UsedByCodeThatStays)]
        [TestCase("DeliveryForecastPublishResult", UsedByCodeThatStays)]
        [TestCase("DeliverySourceDescriptor", UsedByCodeThatStays)]
        [TestCase("DeliverySourceOption", UsedByCodeThatStays)]
        [TestCase("DeliverySourceResolution", UsedByCodeThatStays)]
        [TestCase("DeliverySourceSweepPurpose", UsedByCodeThatStays)]
        [TestCase("DeliveryForecastBlock", Admitted)]
        [TestCase("DeliveryForecastBlockPercentile", Admitted)]
        public void ADeliveryModelsType_IsJudgedAsTheCompiledCodeShows(string candidate, string expected)
        {
            var judged = ProductionJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [Test]
        public void AMadeUpTypeInTheModule_IsNamedAsUnlisted_AndANamesakeOutsideAndANestedTypeAreNot()
        {
            var found = WhenThePromisesTypesAreCollectedFrom(ProductionModule.Value)
                .Concat(WhenThePromisesTypesAreCollectedFrom(TestModule.Value));

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("AMadeUpDeliveryServiceInTheModule"),
                    "A made-up type under Modules.Promises was not named as unlisted. The collection must take in the module's namespaces, or a type added there during the move would join Promises unseen.");
                Assert.That(failure, Does.Not.Contain("ANamesakeOutsidePromises"),
                    "A type in Modules.PromisesFixtures was named as Promises. The namespace pattern must end at a dot or the end of the name, not take in every namespace that starts with the same letters.");
                Assert.That(failure, Does.Not.Contain("ANestedDeliveryStep"),
                    "A type nested inside a made-up Promises type was named on its own. Only top-level types belong on the lists; a nested type moves with the type that holds it.");
                Assert.That(failure, Does.Not.Contain("On the list, but no longer"),
                    "Adding made-up types made a listed type look missing. The collection must not lose a real type when another assembly's types are added.");
            }
        }

        [Test]
        public void AListedTypeRenamedInTheCode_IsNamedAsMissing_AndItsNewNameAsUnlisted()
        {
            var found = WhenThePromisesTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => ShortNameOf(name) == "DeliverySourceResolver" ? $"{name[..name.LastIndexOf('.')]}.DeliverySourceFinder" : name);

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("On the list, but no longer offending: DeliverySourceResolver."),
                    "A listed type the code no longer holds was not named, or other listed types were named with it. A rename during the move must fail until the list follows it.");
                Assert.That(failure, Does.Contain("Not on the list, and offending: DeliverySourceFinder."),
                    "A renamed type was not named as unlisted under its new name, or other types were named with it.");
            }
        }

        [Test]
        public void ACopyOfAListedTypeInAnotherNamespace_IsNamedByItsFullName()
        {
            var collected = WhenThePromisesTypesAreCollectedFrom(ProductionModule.Value);
            var original = collected.Single(name => ShortNameOf(name) == "DeliveryRuleService");
            var copy = $"{original[..original.LastIndexOf('.')]}.LeftBehind.DeliveryRuleService";

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheLists([.. collected, copy]))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain(original).And.Contain(copy),
                    "Two types named DeliveryRuleService in two namespaces were not both named by their full names. A move that leaves a copy behind must fail, not count the two as one listed name.");
                Assert.That(failure, Does.Contain("On the list, but no longer offending: DeliveryRuleService."),
                    "The short name of a type the code holds twice still matched the list. A name the code holds twice must be listed by its full names.");
            }
        }

        private static List<string> WhenThePromisesTypesAreCollectedFrom(ModuleDefinition module)
        {
            var inTheNamespaces = IlDependencies.TypesIn(module, PromisesNamespaces).Select(OuterTypeOf);
            var byName = module.Types
                .Where(type => (IsIn(type.Namespace, NamespacesOfTheMembersFoundByName) && MembersFoundByName.Contains(ShortNameOf(type.FullName)))
                    || (IsIn(type.Namespace, KernelNamespaces) && PromisesMembership.ModelsTypesThatMove.Contains(ShortNameOf(type.FullName))))
                .Select(type => type.FullName);

            return [.. inTheNamespaces.Concat(byName).Distinct(StringComparer.Ordinal)];
        }

        private static void ThenTheyMatchTheLists(IEnumerable<string> found)
            => InlineBaseline.AssertMatches(
                ListedNamesOf(found),
                [.. PromisesMembership.Moves, .. PromisesMembership.ModelsTypesThatMove],
                FixHint);

        // A type is listed by its name alone, unless the code holds that name twice: then each is listed by its full name.
        private static IEnumerable<string> ListedNamesOf(IEnumerable<string> fullNames)
            => fullNames.Distinct(StringComparer.Ordinal)
                .GroupBy(ShortNameOf, StringComparer.Ordinal)
                .SelectMany(group => group.Count() == 1 ? [group.Key] : group.AsEnumerable());

        private static Dictionary<string, CandidateJudgement> WhenTheDeliveryModelsTypesAreJudged(ModuleDefinition module)
        {
            var candidates = module.Types.Where(IsADeliveryModelsCandidate).ToList();
            var candidateNames = candidates.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
            var movingMembers = WhenThePromisesTypesAreCollectedFrom(module).Where(name => !candidateNames.Contains(name)).ToHashSet(StringComparer.Ordinal);
            var stored = StoredTypesIn(module);
            var onTheWire = TypesSentByControllersIn(module);
            var (reachedBy, usedBy) = ReachOf(module, candidateNames, movingMembers);

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

        // Every Delivery type of the shared Models, and whatever already sits in the module's Domain once it has moved.
        private static bool IsADeliveryModelsCandidate(TypeDefinition type)
            => (IsIn(type.Namespace, KernelNamespaces) && (ShortNameOf(type.FullName).StartsWith("Delivery", StringComparison.Ordinal) || IsIn(type.Namespace, DeliveryModelsNamespace)))
                || IsIn(type.Namespace, ThePromisesDomain);

        private static void ThenTheAdmittedOnesAreListed(Dictionary<string, CandidateJudgement> judged)
        {
            var refusals = string.Join(" ", judged.Where(entry => !entry.Value.Admitted).Select(entry => entry.Value.Describe(entry.Key)));

            InlineBaseline.AssertMatches(
                judged.Where(entry => entry.Value.Admitted).Select(entry => entry.Key),
                PromisesMembership.ModelsTypesThatMove,
                $"A shared Models type of the Deliveries moves with Promises only when it is not stored, not sent by a controller, not reached by a Models type that stays and not used by any other code that stays outside the module: put each admitted type on ModelsTypesThatMove, and take a refused one off. The refused candidates and why: {refusals}");
        }

        private static void ThenTheCandidateIsJudged(Dictionary<string, CandidateJudgement> judged, string candidate, string expected)
        {
            Assert.That(judged, Does.ContainKey(candidate),
                $"{candidate} was not found among the Delivery Models types of the compiled code, so whether it may move cannot be judged. It was renamed or deleted: tell the design owner.");

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

        // Splits who references each candidate into Models types (the kernel) and every other type that stays; the
        // candidate itself, the moving members and the types the compiler makes are left out.
        private static (Dictionary<string, List<string>> ReachedBy, Dictionary<string, List<string>> UsedBy) ReachOf(
            ModuleDefinition module, HashSet<string> candidateNames, HashSet<string> movingMembers)
        {
            var reachedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);
            var usedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var dependent in IlDependencies.DependentsIn(module, "^", target => target is not GenericParameter && candidateNames.Contains(OutermostOf(target).FullName)))
            {
                var reacher = OuterTypeOf(dependent.TypeName);

                if (movingMembers.Contains(reacher) || ShortNameOf(reacher).StartsWith('<'))
                {
                    continue;
                }

                var inTheKernel = candidateNames.Contains(reacher) || IsIn(reacher[..Math.Max(0, reacher.LastIndexOf('.'))], KernelNamespaces);

                foreach (var target in dependent.Targets.Select(OuterTypeOf).Where(target => target != reacher).Distinct(StringComparer.Ordinal))
                {
                    (inTheKernel ? reachedBy : usedBy)[target].Add(reacher);
                }
            }

            return (reachedBy, usedBy);
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

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);

        private static string ShortNameOf(string fullName)
        {
            var outermost = OuterTypeOf(fullName);
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

namespace Lighthouse.Backend.Modules.Promises.MembershipFixtures
{
    internal static class AMadeUpDeliveryServiceInTheModule
    {
        public static int Promised(int[] items) => items.Count(item => item > 0);

        public static ANestedDeliveryStep Step(int id) => new(id);

        internal sealed record ANestedDeliveryStep(int Id);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.Membership
{
    internal static class ANamesakeOutsidePromises
    {
        public static int Promised(int items) => items;
    }
}
