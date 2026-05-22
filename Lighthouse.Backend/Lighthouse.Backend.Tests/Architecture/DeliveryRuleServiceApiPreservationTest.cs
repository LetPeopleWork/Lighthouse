using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DeliveryRuleServiceApiPreservationTest
    {
        [Test]
        public void GetRuleSchema_PreservesPublicSignature()
        {
            AssertPublicMethodSignature(
                methodName: "GetRuleSchema",
                expectedParameterTypes: [typeof(Portfolio)],
                expectedReturnType: typeof(DeliveryRuleSchema));
        }

        [Test]
        public void GetMatchingFeaturesForRuleset_PreservesPublicSignature()
        {
            AssertPublicMethodSignature(
                methodName: "GetMatchingFeaturesForRuleset",
                expectedParameterTypes: [typeof(DeliveryRuleSet), typeof(IEnumerable<Feature>)],
                expectedReturnType: typeof(IEnumerable<Feature>));
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_PreservesPublicSignature()
        {
            AssertPublicMethodSignature(
                methodName: "RecomputeRuleBasedDeliveries",
                expectedParameterTypes: [typeof(Portfolio), typeof(IEnumerable<Delivery>)],
                expectedReturnType: typeof(void));
        }

        private static void AssertPublicMethodSignature(
            string methodName,
            Type[] expectedParameterTypes,
            Type expectedReturnType)
        {
            var serviceType = typeof(DeliveryRuleService);

            var method = serviceType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: expectedParameterTypes,
                modifiers: null);

            Assert.That(method, Is.Not.Null,
                $"DeliveryRuleService.{methodName}({string.Join(", ", expectedParameterTypes.Select(t => t.Name))}) " +
                "must exist with the exact public signature. " +
                "Renaming or changing this signature breaks downstream callers; ADR-012 pins this contract. " +
                "If the API must change, update ADR-012 first and amend this test.");

            Assert.That(method!.ReturnType, Is.EqualTo(expectedReturnType),
                $"DeliveryRuleService.{methodName} return type must remain '{expectedReturnType.Name}', " +
                $"but reflection found '{method.ReturnType.Name}'. " +
                "ADR-012 pins this contract. If the return type must change, update ADR-012 first and amend this test.");
        }
    }
}
