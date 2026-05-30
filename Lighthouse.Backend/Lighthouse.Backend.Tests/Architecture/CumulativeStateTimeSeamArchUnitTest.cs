using System.Reflection;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class CumulativeStateTimeSeamArchUnitTest
    {
        private const byte CallOpcode = 0x28;
        private const byte CallVirtOpcode = 0x6F;

        private static readonly string[] CumulativeHelperNames =
        [
            "ComputeCumulativeStateTime",
            "ComputeCumulativeStateTimeItems"
        ];

        private const string AgeInStatePercentilesHelperName = "ComputeAgeInStatePercentiles";

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(typeof(BaseMetricsService).Assembly)
            .Build();

        [Test]
        public void CumulativeHelpers_AreProtected_AndNotExposedViaAnyInterface()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameContaining("ComputeCumulativeStateTime")
                .Should().BeProtected()
                .Because(
                    "ADR-024 architectural enforcement: the cumulative-state-time helpers " +
                    "BaseMetricsService.ComputeCumulativeStateTime and ComputeCumulativeStateTimeItems must be protected " +
                    "(intra-inheritance only), never public, so they cannot be promoted to a shared interface " +
                    "(no IPerStateAggregationService). Keep the helpers protected, or update ADR-024 first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(Interfaces())
                .Should().NotHaveNameContaining("ComputeCumulativeStateTime")
                .Because(
                    "ADR-024 architectural enforcement: no interface in the production assembly may declare a member " +
                    "named ComputeCumulativeStateTime or ComputeCumulativeStateTimeItems. The cumulative helpers are " +
                    "intra-inheritance only; the service surface exposes scope-specific GetCumulativeStateTimeFor* methods, " +
                    "not the shared helpers.")
                .Check(Architecture);
        }

        [Test]
        public void CumulativeHelpers_DoNotCall_SiblingFsAgeInStatePercentilesHelper()
        {
            var ageInStatePercentilesHelper = typeof(BaseMetricsService).GetMethod(
                AgeInStatePercentilesHelperName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(ageInStatePercentilesHelper, Is.Not.Null,
                "ADR-024 architectural enforcement: sibling F's BaseMetricsService.ComputeAgeInStatePercentiles must " +
                "exist for this divergence test to be meaningful.");

            var helpersCallingAgeInStatePercentiles = new List<string>();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var helperName in CumulativeHelperNames)
            {
                var helper = typeof(BaseMetricsService).GetMethod(helperName, bindingFlags);

                if (helper is not null && MethodCalls(helper, ageInStatePercentilesHelper!))
                {
                    helpersCallingAgeInStatePercentiles.Add(helperName);
                }
            }

            Assert.That(helpersCallingAgeInStatePercentiles, Is.Empty,
                "ADR-021 / ADR-024 semantic-divergence enforcement: the cumulative-state-time helpers compute their own " +
                "per-state sums and must NOT call sibling F's ComputeAgeInStatePercentiles (different item-membership " +
                "rule, different in-flight attribution, different aggregation). The two sets of helpers are " +
                "parallel-named and co-located in BaseMetricsService but share zero call sites. The following " +
                "cumulative helpers call ComputeAgeInStatePercentiles: " +
                string.Join(", ", helpersCallingAgeInStatePercentiles) + ". " +
                "Keep the computations independent, or update ADR-024 first and amend this test.");
        }

        private static bool MethodCalls(MethodInfo caller, MethodInfo callee)
        {
            MethodBody? body;
            try
            {
                body = caller.GetMethodBody();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var il = body?.GetILAsByteArray();
            if (il is null || il.Length == 0)
            {
                return false;
            }

            var module = caller.Module;
            var genericTypeArgs = SafeGetGenericArguments(caller.DeclaringType);
            var genericMethodArgs = caller.IsGenericMethod ? caller.GetGenericArguments() : Type.EmptyTypes;

            for (var offset = 0; offset < il.Length - 4; offset++)
            {
                if (il[offset] != CallOpcode && il[offset] != CallVirtOpcode)
                {
                    continue;
                }

                if (ResolvedCallTargets(module, il, offset, genericTypeArgs, genericMethodArgs) == callee)
                {
                    return true;
                }
            }

            return false;
        }

        private static MethodBase? ResolvedCallTargets(Module module, byte[] il, int offset, Type[] genericTypeArgs, Type[] genericMethodArgs)
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
