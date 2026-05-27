using System.Reflection;
using Lighthouse.Backend.Services.Implementation;

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

        [Test]
        public void CumulativeHelpers_AreProtected_AndNotExposedViaAnyInterface()
        {
            var notProtected = new List<string>();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var helperName in CumulativeHelperNames)
            {
                var helper = typeof(BaseMetricsService).GetMethod(helperName, bindingFlags);

                if (helper is null)
                {
                    notProtected.Add($"{helperName} (method not found on BaseMetricsService)");
                    continue;
                }

                if (helper.IsPublic || !helper.IsFamily)
                {
                    notProtected.Add($"{helperName} ({AccessModifierOf(helper)})");
                }
            }

            var interfaceMembersExposingHelpers = ProductionInterfaceMembersNamed(CumulativeHelperNames);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notProtected, Is.Empty,
                    "ADR-024 architectural enforcement: the cumulative-state-time helpers " +
                    "BaseMetricsService.ComputeCumulativeStateTime and ComputeCumulativeStateTimeItems must be " +
                    "protected (intra-inheritance only), never public, so they cannot be promoted to a shared " +
                    "interface (no IPerStateAggregationService). The following helpers have the wrong access modifier: " +
                    string.Join(", ", notProtected) + ". " +
                    "Keep the helpers protected, or update ADR-024 first and amend this test.");

                Assert.That(interfaceMembersExposingHelpers, Is.Empty,
                    "ADR-024 architectural enforcement: no interface in the production assembly may declare a member " +
                    "named ComputeCumulativeStateTime or ComputeCumulativeStateTimeItems. The cumulative helpers are " +
                    "intra-inheritance only; the service surface exposes scope-specific GetCumulativeStateTimeFor* " +
                    "methods, not the shared helpers. Offending interface members: " +
                    string.Join(", ", interfaceMembersExposingHelpers) + ".");
            }
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

        private static List<string> ProductionInterfaceMembersNamed(IReadOnlyCollection<string> memberNames)
        {
            var productionAssembly = typeof(BaseMetricsService).Assembly;

            return productionAssembly.GetTypes()
                .Where(type => type.IsInterface)
                .SelectMany(type => type.GetMembers()
                    .Where(member => memberNames.Contains(member.Name))
                    .Select(member => $"{type.FullName ?? type.Name}.{member.Name}"))
                .ToList();
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

        private static string AccessModifierOf(MethodInfo method)
        {
            if (method.IsPublic)
            {
                return "public";
            }

            if (method.IsFamily)
            {
                return "protected";
            }

            if (method.IsPrivate)
            {
                return "private";
            }

            return "internal";
        }
    }
}
