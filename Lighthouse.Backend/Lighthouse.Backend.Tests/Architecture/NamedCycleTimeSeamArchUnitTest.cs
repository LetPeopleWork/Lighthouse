using System.Reflection;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class NamedCycleTimeSeamArchUnitTest
    {
        private const byte CallOpcode = 0x28;
        private const byte CallVirtOpcode = 0x6F;
        private const string NamedCycleTimeDurationHelperName = "NamedCycleTimeDays";
        private const string TransitionOrderingHelperName = "OrderedStateEntries";
        private const string ValidityMethodName = "IsCycleTimeDefinitionValid";

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(typeof(BaseMetricsService).Assembly)
            .Build();

        private static readonly Assembly ProductionAssembly = typeof(BaseMetricsService).Assembly;

        private static readonly BindingFlags AllMemberFlags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void NamedCycleTimeDuration_IsAProtectedHelperOnBaseMetricsService_NotOnAnyInterface()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameContaining(NamedCycleTimeDurationHelperName)
                .Should().BeProtected()
                .Because(
                    "ADR-061 §1: the named ordered-boundary duration helper NamedCycleTimeDays lives as a protected member " +
                    "on BaseMetricsService (intra-inheritance only), never public, so it cannot be promoted to a shared " +
                    "interface. Keep it protected, or update ADR-061 first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(Interfaces())
                .Should().NotHaveNameContaining(NamedCycleTimeDurationHelperName)
                .Because(
                    "ADR-061 §1: no interface in the production assembly may declare a member named NamedCycleTimeDays. " +
                    "The named-duration helper is intra-inheritance only; service interfaces expose definition-scoped " +
                    "GetNamedCycleTime* methods, not the boundary-duration helper.")
                .Check(Architecture);
        }

        [Test]
        public void NamedCycleTimeDuration_ReusesTheTransitionOrderingPrimitive_NoSecondTransitionWalkOutsideBaseMetricsService()
        {
            var transitionOrderingHelper = typeof(BaseMetricsService).GetMethod(TransitionOrderingHelperName, AllMemberFlags);
            var namedDurationHelper = typeof(BaseMetricsService).GetMethod(NamedCycleTimeDurationHelperName, AllMemberFlags);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitionOrderingHelper, Is.Not.Null,
                    "ADR-061 §1: BaseMetricsService.OrderedStateEntries (the TransitionedAt-ordering primitive the named " +
                    "duration reuses) must exist for this no-parallel-engine test to be meaningful.");
                Assert.That(namedDurationHelper, Is.Not.Null,
                    "ADR-061 §1: BaseMetricsService.NamedCycleTimeDays must exist for this no-parallel-engine test to be meaningful.");
            }

            Assert.That(MethodCalls(namedDurationHelper!, transitionOrderingHelper!), Is.True,
                "ADR-061 §1/§3: NamedCycleTimeDays must reuse the BaseMetricsService transition-ordering primitive " +
                "(OrderedStateEntries) rather than walking the transition log itself.");

            var namedFeatureWalkingTransitionsOutsideBaseMetricsService = AllProductionMethods()
                .Where(method => method.DeclaringType != typeof(BaseMetricsService)
                    && MethodCalls(method, namedDurationHelper!)
                    && MethodReferencesMember(method, TransitionOrderingByTransitionedAt()))
                .Select(FullNameOf)
                .ToList();

            Assert.That(namedFeatureWalkingTransitionsOutsideBaseMetricsService, Is.Empty,
                "ADR-061 §1/§3 (no parallel engine): a method outside BaseMetricsService both calls NamedCycleTimeDays " +
                "AND performs its own OrderBy(TransitionedAt) transition walk, duplicating the ordering primitive. " +
                "Named-feature orchestrators must delegate the transition walk to BaseMetricsService. Offending members: " +
                string.Join(", ", namedFeatureWalkingTransitionsOutsideBaseMetricsService) + ".");
        }

        [Test]
        public void WorkItemBaseCycleTime_IsNotModifiedByThisFeature()
        {
            var cycleTime = typeof(WorkItemBase).GetProperty(nameof(WorkItemBase.CycleTime), BindingFlags.Instance | BindingFlags.Public);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cycleTime, Is.Not.Null,
                    "ADR-061 §3: WorkItemBase.CycleTime must still exist as the summary-date property.");
                Assert.That(cycleTime!.PropertyType, Is.EqualTo(typeof(int)),
                    "ADR-061 §3: WorkItemBase.CycleTime stays an int summary-date duration.");
                Assert.That(cycleTime.GetGetMethod(), Is.Not.Null,
                    "ADR-061 §3: WorkItemBase.CycleTime stays a computed get-only property.");
                Assert.That(cycleTime.GetSetMethod(), Is.Null,
                    "ADR-061 §3: WorkItemBase.CycleTime stays get-only — it is not turned into a stored/settable value.");
            }

            var getterDependsOnOwner = ScanCallTargets(cycleTime!.GetGetMethod()!)
                .Any(target => DeclaresOn(target, typeof(WorkTrackingSystemOptionsOwner)));

            Assert.That(getterDependsOnOwner, Is.False,
                "ADR-061 §3 blast-radius: WorkItemBase.CycleTime's getter must remain a summary-date computation with NO " +
                "dependency on WorkTrackingSystemOptionsOwner (its ordered AllStates / GetRawStatesForCategory resolver). " +
                "The named computation must not be routed through the model.");
        }

        [Test]
        public void IsCycleTimeDefinitionValid_IsTheOnlyBoundaryPresenceCheckAgainstAllStates()
        {
            var validityMethod = typeof(WorkTrackingSystemOptionsOwner).GetMethod(ValidityMethodName, AllMemberFlags);

            Assert.That(validityMethod, Is.Not.Null,
                "ADR-063 §1: WorkTrackingSystemOptionsOwner.IsCycleTimeDefinitionValid(CycleTimeDefinition) must exist " +
                "as the single backend predicate that resolves a definition's boundaries against the current AllStates.");

            var allStatesGetter = typeof(WorkTrackingSystemOptionsOwner)
                .GetProperty(nameof(WorkTrackingSystemOptionsOwner.AllStates), BindingFlags.Instance | BindingFlags.Public)!
                .GetGetMethod()!;

            var otherMembersBuildingAPresenceSetFromAllStates = AllProductionMethods()
                .Where(method => !MethodIs(method, validityMethod!)
                    && MethodConsumesType(method, typeof(CycleTimeDefinition))
                    && MethodCalls(method, allStatesGetter)
                    && MethodBuildsAMembershipSet(method))
                .Select(FullNameOf)
                .ToList();

            Assert.That(otherMembersBuildingAPresenceSetFromAllStates, Is.Empty,
                "ADR-063 §1/§2: only WorkTrackingSystemOptionsOwner.IsCycleTimeDefinitionValid may resolve a " +
                "CycleTimeDefinition's boundaries against AllStates for presence-validity (building a membership set over " +
                "AllStates to test boundary presence — distinct from the ordered-list read the ADR-061 computation uses). " +
                "Another member recomputes presence-validity independently instead of consuming the stamped IsValid. " +
                "Offending members: " +
                string.Join(", ", otherMembersBuildingAPresenceSetFromAllStates) + ".");
        }

        private static IEnumerable<MethodInfo> AllProductionMethods()
        {
            return ProductionAssembly.GetTypes()
                .Where(type => !type.IsInterface)
                .SelectMany(type => type.GetMethods(AllMemberFlags | BindingFlags.DeclaredOnly));
        }

        private static MethodInfo TransitionOrderingByTransitionedAt()
        {
            return typeof(WorkItemStateTransition).GetProperty(nameof(WorkItemStateTransition.TransitionedAt))!.GetGetMethod()!;
        }

        private static bool MethodConsumesType(MethodInfo method, Type consumedType)
        {
            if (method.GetParameters().Any(parameter => parameter.ParameterType == consumedType))
            {
                return true;
            }

            return ScanCallTargets(method).Any(target =>
                target is MethodInfo info && info.ReturnType == consumedType);
        }

        private static bool MethodReferencesMember(MethodInfo method, MethodInfo referenced)
        {
            return MethodCalls(method, referenced);
        }

        private static bool MethodBuildsAMembershipSet(MethodInfo method)
        {
            return ScanCallTargets(method).Any(target =>
                target.Name == "ToHashSet" && target.DeclaringType == typeof(Enumerable));
        }

        private static bool MethodIs(MethodInfo candidate, MethodInfo target)
        {
            return candidate == target;
        }

        private static bool DeclaresOn(MethodBase target, Type owner)
        {
            return target.DeclaringType is not null && owner.IsAssignableFrom(target.DeclaringType);
        }

        private static string FullNameOf(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName}.{method.Name}";
        }

        private static bool MethodCalls(MethodBase caller, MethodBase callee)
        {
            return ScanCallTargets(caller).Contains(callee);
        }

        private static IReadOnlyCollection<MethodBase> ScanCallTargets(MethodBase caller)
        {
            var body = SafeGetMethodBody(caller);
            var il = body?.GetILAsByteArray();
            if (il is null || il.Length == 0)
            {
                return [];
            }

            var module = caller.Module;
            var genericTypeArgs = SafeGetGenericArguments(caller.DeclaringType);
            var genericMethodArgs = caller is MethodInfo { IsGenericMethod: true } ? caller.GetGenericArguments() : Type.EmptyTypes;

            var targets = new List<MethodBase>();
            for (var offset = 0; offset < il.Length - 4; offset++)
            {
                if (il[offset] != CallOpcode && il[offset] != CallVirtOpcode)
                {
                    continue;
                }

                var resolved = ResolveCallTarget(module, il, offset, genericTypeArgs, genericMethodArgs);
                if (resolved is not null)
                {
                    targets.Add(resolved);
                }
            }

            return targets;
        }

        private static MethodBody? SafeGetMethodBody(MethodBase caller)
        {
            try
            {
                return caller.GetMethodBody();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static MethodBase? ResolveCallTarget(Module module, byte[] il, int offset, Type[] genericTypeArgs, Type[] genericMethodArgs)
        {
            var token = BitConverter.ToInt32(il, offset + 1);

            try
            {
                return module.ResolveMethod(token, genericTypeArgs, genericMethodArgs);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        private static Type[] SafeGetGenericArguments(Type? type)
        {
            if (type is null || type.IsGenericTypeDefinition)
            {
                return Type.EmptyTypes;
            }

            return type.GetGenericArguments();
        }
    }
}
