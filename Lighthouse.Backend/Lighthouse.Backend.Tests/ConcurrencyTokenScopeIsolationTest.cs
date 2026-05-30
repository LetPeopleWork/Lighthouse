using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class ConcurrencyTokenScopeIsolationTest
    {
        private static readonly Type[] HighChurnSyncEntities =
        [
            typeof(WorkItem),
            typeof(Feature),
            typeof(FeatureWork),
            typeof(WorkItemStateTransition),
        ];

        private static readonly Type[] TokenedConfigAggregateRoots =
        [
            typeof(Team),
            typeof(Portfolio),
            typeof(WorkTrackingSystemConnection),
            typeof(Delivery),
            typeof(UserProfile),
            typeof(RbacGroupMapping),
            typeof(ApiKey),
        ];

        [TestCaseSource(nameof(HighChurnSyncEntities))]
        public void HighChurnSyncEntity_IsNotTokened(Type syncEntity)
        {
            Assert.That(syncEntity.GetInterfaces(), Does.Not.Contain(typeof(IConcurrencyTokenEntity)),
                $"{syncEntity.Name} is on the high-churn sync path and must NOT be tokened — tokening it would impose 409 risk and throughput cost on every sync save.");
        }

        [TestCaseSource(nameof(TokenedConfigAggregateRoots))]
        public void ConfigAggregateRoot_IsTokened(Type configRoot)
        {
            Assert.That(configRoot.GetInterfaces(), Does.Contain(typeof(IConcurrencyTokenEntity)),
                $"{configRoot.Name} is a human-edited config aggregate root and must carry an optimistic-concurrency token so a stale edit surfaces as 409.");
        }
    }
}
