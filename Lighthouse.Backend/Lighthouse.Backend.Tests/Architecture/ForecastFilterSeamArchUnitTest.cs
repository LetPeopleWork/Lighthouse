using System.Reflection;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ForecastFilterSeamArchUnitTest
    {
        private const byte CallOpcode = 0x28;
        private const byte CallVirtOpcode = 0x6F;

        private static readonly HashSet<Type> AllowedCallerTypes =
        [
            typeof(TeamMetricsService),
            typeof(ForecastFilterRuleService)
        ];

        [Test]
        public void OnlyWhitelistedTypes_InvokeForecastFilterRuleServiceFilter()
        {
            var filterMethod = typeof(IForecastFilterRuleService).GetMethod(
                nameof(IForecastFilterRuleService.Filter),
                BindingFlags.Public | BindingFlags.Instance);

            Assert.That(filterMethod, Is.Not.Null,
                "IForecastFilterRuleService.Filter must exist as a public instance method.");

            var productionAssembly = typeof(TeamMetricsService).Assembly;
            var unauthorisedCallers = new List<string>();

            foreach (var type in productionAssembly.GetTypes())
            {
                if (AllowedCallerTypes.Contains(type))
                {
                    continue;
                }

                if (CallsFilterMethod(type, filterMethod!))
                {
                    unauthorisedCallers.Add(type.FullName ?? type.Name);
                }
            }

            Assert.That(unauthorisedCallers, Is.Empty,
                "DDD-4 architectural enforcement: only TeamMetricsService and ForecastFilterRuleService " +
                "may invoke IForecastFilterRuleService.Filter directly (single filter seam). " +
                "The following types call .Filter outside the whitelist: " +
                string.Join(", ", unauthorisedCallers) + ". " +
                "Route the call through TeamMetricsService.GetThroughputForTeam (or another " +
                "whitelisted seam) instead. If a new seam is genuinely required, update the " +
                "architectural decision record first and amend this test.");
        }

        private static bool CallsFilterMethod(Type type, MethodInfo filterMethod)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                               BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.DeclaredOnly;

            foreach (var method in type.GetMethods(bindingFlags))
            {
                if (MethodInvokesTarget(method, filterMethod))
                {
                    return true;
                }
            }

            foreach (var constructor in type.GetConstructors(bindingFlags))
            {
                if (MethodInvokesTarget(constructor, filterMethod))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MethodInvokesTarget(MethodBase method, MethodInfo target)
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

            if (body == null)
            {
                return false;
            }

            var il = body.GetILAsByteArray();
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
                var opcode = il[i];
                if (opcode != CallOpcode && opcode != CallVirtOpcode)
                {
                    continue;
                }

                var token = BitConverter.ToInt32(il, i + 1);

                MethodBase? resolved;
                try
                {
                    resolved = module.ResolveMethod(token, genericTypeArgs, genericMethodArgs);
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                if (resolved is null)
                {
                    continue;
                }

                if (IsSameMethod(resolved, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type[] SafeGetGenericArguments(Type? type)
        {
            if (type == null)
            {
                return Type.EmptyTypes;
            }

            if (type.IsGenericTypeDefinition)
            {
                return Type.EmptyTypes;
            }

            return type.GetGenericArguments();
        }

        private static bool IsSameMethod(MethodBase candidate, MethodInfo target)
        {
            if (candidate.MetadataToken != target.MetadataToken)
            {
                return string.Equals(candidate.Name, target.Name, StringComparison.Ordinal)
                       && candidate.DeclaringType == target.DeclaringType;
            }

            return candidate.Module == target.Module
                   || (candidate.DeclaringType == target.DeclaringType
                       && string.Equals(candidate.Name, target.Name, StringComparison.Ordinal));
        }
    }
}
