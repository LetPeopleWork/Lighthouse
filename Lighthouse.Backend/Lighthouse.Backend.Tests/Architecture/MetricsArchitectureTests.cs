using System.Reflection;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class MetricsArchitectureTests
    {
        private const byte CallOpcode = 0x28;
        private const byte CallVirtOpcode = 0x6F;
        private const byte LdfldOpcode = 0x7B;
        private const byte LdfldaOpcode = 0x7C;
        private const byte LdsfldOpcode = 0x7E;
        private const byte LdsfldaOpcode = 0x7F;

        private static readonly Type[] ForbiddenTransitionDbSets =
        [
            typeof(DbSet<WorkItemStateTransition>),
            typeof(DbSet<FeatureStateTransition>)
        ];

        private static readonly Type[] MetricsServicesReadingTransitions =
        [
            typeof(TeamMetricsService),
            typeof(PortfolioMetricsService)
        ];

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(typeof(BaseMetricsService).Assembly)
            .Build();

        [Test]
        public void NoPerStateAggregationServiceExists_InProductionAssembly()
        {
            Types().That().HaveNameContaining("PerStateAggregation")
                .Should().NotExist()
                .Because(
                    "ADR-021 / DDD-10 architectural enforcement: the repositories are the shared per-state seam, " +
                    "so no class or interface named *PerStateAggregation* may exist. Per-state percentiles are " +
                    "computed independently inside TeamMetricsService and PortfolioMetricsService via the protected " +
                    "BaseMetricsService.ComputeAgeInStatePercentiles helper. Remove the aggregation service, or " +
                    "update ADR-021 first and amend this test.")
                .Check(Architecture);
        }

        [Test]
        public void ComputeAgeInStatePercentiles_IsProtected_AndNoPublicPerStateAggregationMemberIsExposed()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameStartingWith("ComputeAgeInStatePercentiles(")
                .Should().BeProtected()
                .Because(
                    "DDD-9 architectural enforcement: BaseMetricsService.ComputeAgeInStatePercentiles must be the single " +
                    "intra-inheritance per-state aggregation helper and must be protected (intra-inheritance only), not " +
                    "public and not an interface member. Keep the helper protected, or update ADR-021 first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().ArePublic()
                .Should().NotHaveNameContaining("PerStateAggregation").AndShould().NotHaveNameContaining("ComputeAgeInStatePercentiles")
                .Because(
                    "DDD-9 architectural enforcement: no public member named *PerStateAggregation* or " +
                    "*ComputeAgeInStatePercentiles* may be exposed on BaseMetricsService (the helper is " +
                    "intra-inheritance only; the service surface exposes scope-specific GetAgeInStatePercentilesFor* methods, " +
                    "not the shared helper).")
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }

        [Test]
        public void MetricsServices_ReadTransitions_OnlyViaRepositoryPorts_NeverViaDbSetDirectly()
        {
            var servicesTouchingTransitionDbSets = MetricsServicesReadingTransitions
                .Where(ReferencesTransitionDbSet)
                .Select(type => type.FullName ?? type.Name)
                .ToList();

            Assert.That(servicesTouchingTransitionDbSets, Is.Empty,
                "ADR-015 / ADR-021 architectural enforcement: the metrics-service classes read transition data " +
                "only through the repository ports (TeamMetricsService via IWorkItemStateTransitionRepository, " +
                "PortfolioMetricsService via IFeatureStateTransitionRepository), never via " +
                "DbSet<WorkItemStateTransition> or DbSet<FeatureStateTransition> directly. " +
                "The following services reference a transition DbSet directly: " +
                string.Join(", ", servicesTouchingTransitionDbSets) + ". " +
                "Route transition reads through the repository port, or update the architectural decision record first and amend this test.");
        }

        private static bool ReferencesTransitionDbSet(Type type)
        {
            return TypeWithNestedClosures(type).Any(MembersTouchTransitionDbSet);
        }

        private static IEnumerable<Type> TypeWithNestedClosures(Type type)
        {
            yield return type;

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var descendant in TypeWithNestedClosures(nested))
                {
                    yield return descendant;
                }
            }
        }

        private static bool MembersTouchTransitionDbSet(Type type)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                               BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.DeclaredOnly;

            if (type.GetFields(bindingFlags).Any(field => IsTransitionDbSet(field.FieldType)))
            {
                return true;
            }

            return type.GetMethods(bindingFlags).Cast<MethodBase>()
                .Concat(type.GetConstructors(bindingFlags))
                .Any(MethodTouchesTransitionDbSet);
        }

        private static bool MethodTouchesTransitionDbSet(MethodBase method)
        {
            MethodBody? body;
            try
            {
                body = method.GetMethodBody();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var il = body?.GetILAsByteArray();
            if (il == null || il.Length == 0)
            {
                return false;
            }

            var module = method.Module;
            var genericTypeArgs = SafeGetGenericArguments(method.DeclaringType);
            var genericMethodArgs = method is MethodInfo mi && mi.IsGenericMethod
                ? mi.GetGenericArguments()
                : Type.EmptyTypes;

            for (var i = 0; i < il.Length - 4; i++)
            {
                if (OpcodeTouchesTransitionDbSet(il[i], module, il, i, genericTypeArgs, genericMethodArgs))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool OpcodeTouchesTransitionDbSet(byte opcode, Module module, byte[] il, int offset, Type[] genericTypeArgs, Type[] genericMethodArgs)
        {
            if (opcode == CallOpcode || opcode == CallVirtOpcode)
            {
                return ResolvedCallTouchesTransitionDbSet(module, il, offset, genericTypeArgs, genericMethodArgs);
            }

            if (opcode == LdfldOpcode || opcode == LdfldaOpcode || opcode == LdsfldOpcode || opcode == LdsfldaOpcode)
            {
                return ResolvedFieldIsTransitionDbSet(module, il, offset, genericTypeArgs, genericMethodArgs);
            }

            return false;
        }

        private static bool ResolvedCallTouchesTransitionDbSet(Module module, byte[] il, int offset, Type[] genericTypeArgs, Type[] genericMethodArgs)
        {
            var token = BitConverter.ToInt32(il, offset + 1);

            MethodBase? resolved;
            try
            {
                resolved = module.ResolveMethod(token, genericTypeArgs, genericMethodArgs);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }

            if (resolved is MethodInfo resolvedMethod && IsTransitionDbSet(resolvedMethod.ReturnType))
            {
                return true;
            }

            return IsTransitionDbSet(resolved?.DeclaringType);
        }

        private static bool ResolvedFieldIsTransitionDbSet(Module module, byte[] il, int offset, Type[] genericTypeArgs, Type[] genericMethodArgs)
        {
            var token = BitConverter.ToInt32(il, offset + 1);

            try
            {
                return IsTransitionDbSet(module.ResolveField(token, genericTypeArgs, genericMethodArgs)?.FieldType);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        private static Type[] SafeGetGenericArguments(Type? type)
        {
            if (type == null || type.IsGenericTypeDefinition)
            {
                return Type.EmptyTypes;
            }

            return type.GetGenericArguments();
        }

        private static bool IsTransitionDbSet(Type? type)
        {
            return type is not null && ForbiddenTransitionDbSets.Contains(type);
        }
    }
}
