using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Single rule-based IsBlocked read path (ADR-067). Locks the invariant that "is this item blocked?" is
    /// resolved in exactly ONE place — <see cref="IBlockedItemService"/>, delegating to the pure
    /// <see cref="IRuleEvaluator{T}"/> — and is NOT recomputed as a getter on the domain model. The legacy
    /// BlockedStates/BlockedTags columns have been fully removed (post-backfill drop-column migration);
    /// <c>BlockedRuleSetJson</c> is now the sole persisted configuration, so the evaluation surface and the
    /// storage surface are one and the same single path.
    /// </summary>
    [TestFixture]
    public class BlockedItemSinglePathArchUnitTest
    {
        private static readonly Type[] BlockedModelTypes = [typeof(WorkItemBase), typeof(WorkItem), typeof(Feature)];

        [Test]
        public void DomainModels_DoNotExposeAComputedIsBlockedMember()
        {
            foreach (var modelType in BlockedModelTypes)
            {
                var isBlockedProperty = modelType.GetProperty("IsBlocked", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                Assert.That(isBlockedProperty, Is.Null,
                    $"{modelType.Name} must not expose a computed IsBlocked member (ADR-067 single rule-based read path). " +
                    "Blocked evaluation belongs to IBlockedItemService, not the domain model. If a model-level " +
                    "blocked member is genuinely required, update ADR-067 first and amend this test.");
            }
        }

        [Test]
        public void OnlyBlockedItemService_ResolvesBlockedEvaluation()
        {
            var implementors = typeof(BlockedItemService).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .Where(type => typeof(IBlockedItemService).IsAssignableFrom(type))
                .ToList();

            Assert.That(implementors, Is.EquivalentTo(new[] { typeof(BlockedItemService) }),
                "Exactly one production type (BlockedItemService) may resolve blocked evaluation (ADR-067). " +
                "A second IBlockedItemService implementation would fork the single read path. Found: " +
                string.Join(", ", implementors.Select(type => type.Name)) + ".");
        }

        [Test]
        public void BlockedItemService_ResolvesBlocked_ThroughThePureRuleEvaluator()
        {
            var dependsOnRuleEvaluator = typeof(BlockedItemService)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => typeof(IRuleEvaluator<WorkItem>).IsAssignableFrom(parameter.ParameterType));

            Assert.That(dependsOnRuleEvaluator, Is.True,
                "BlockedItemService must resolve blocked through the pure IRuleEvaluator<WorkItem> (ADR-012 / ADR-067). " +
                "The evaluator's purity is separately enforced by RuleEvaluatorPurityTest.");
        }
    }
}
