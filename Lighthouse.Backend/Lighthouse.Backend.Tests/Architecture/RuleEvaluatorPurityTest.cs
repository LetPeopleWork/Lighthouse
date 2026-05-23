using System.Reflection;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class RuleEvaluatorPurityTest
    {
        private static readonly Type[] ForbiddenConstructorDependencies =
        [
            typeof(IRepository<>),
            typeof(DbContext),
            typeof(HttpClient),
            typeof(ILogger),
            typeof(ILogger<>)
        ];

        [Test]
        public void RuleEvaluator_HasOnlyParameterlessPublicConstructors()
        {
            var openGeneric = typeof(RuleEvaluator<>);

            var publicConstructors = openGeneric.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            Assert.That(publicConstructors, Is.Not.Empty,
                "RuleEvaluator<T> must expose at least one public constructor.");

            foreach (var constructor in publicConstructors)
            {
                var parameters = constructor.GetParameters();
                Assert.That(parameters, Is.Empty,
                    "RuleEvaluator<T> must remain a pure function-style evaluator with a parameterless public constructor " +
                    "(ADR-012 Architectural Enforcement). Found constructor with parameters: " +
                    string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}")) +
                    ". If a new dependency is genuinely required, update ADR-012 first and amend this test.");
            }
        }

        [Test]
        public void RuleEvaluator_PublicConstructorParameters_DoNotDependOnForbiddenInfrastructure()
        {
            var openGeneric = typeof(RuleEvaluator<>);

            var publicConstructors = openGeneric.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            foreach (var constructor in publicConstructors)
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var parameterType = parameter.ParameterType;
                    foreach (var forbidden in ForbiddenConstructorDependencies)
                    {
                        Assert.That(IsAssignableToForbidden(parameterType, forbidden), Is.False,
                            $"RuleEvaluator<T> constructor parameter '{parameter.Name}' of type '{parameterType.Name}' " +
                            $"is assignable to forbidden infrastructure type '{forbidden.Name}'. " +
                            "This would break the pure-function invariant of ADR-012. " +
                            "If the new dependency is genuinely needed, update ADR-012 first and amend this test.");
                    }
                }
            }
        }

        private static bool IsAssignableToForbidden(Type candidate, Type forbidden)
        {
            if (forbidden.IsGenericTypeDefinition)
            {
                return IsAssignableToOpenGeneric(candidate, forbidden);
            }

            return forbidden.IsAssignableFrom(candidate);
        }

        private static bool IsAssignableToOpenGeneric(Type candidate, Type openGeneric)
        {
            foreach (var iface in candidate.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openGeneric)
                {
                    return true;
                }
            }

            var current = candidate;
            while (current is not null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }
    }
}
