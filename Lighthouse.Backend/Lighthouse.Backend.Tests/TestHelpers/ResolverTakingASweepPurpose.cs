using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Stands in for the sweep purpose each caller will hand the resolver, which the resolver cannot take yet. A caller
    /// test writes its verify against the four-argument call it expects; until the resolver takes the purpose itself,
    /// that call binds to the method below, and the verify fails as an assertion that names what is missing. Once the
    /// resolver takes the purpose, the compiler picks its own method over this one, the same verify starts reading the
    /// real call, and this class has nothing left to do.
    /// </summary>
    internal static class ResolverTakingASweepPurpose
    {
        public static Task<IReadOnlyDictionary<string, PortfolioSourcePreview>> ResolveForPortfolio(
            this IDeliverySourceResolver resolver,
            Portfolio portfolio,
            string sourceKey,
            IReadOnlyList<string> sourceReferences,
            DeliverySourceSweepPurpose purpose)
            => throw new AssertionException(
                $"{resolver.GetType().Name} cannot be told yet that resolving {sourceReferences.Count} reference(s) of " +
                $"{sourceKey} for Portfolio {portfolio.Id} is for the {purpose}");

        /// <summary>
        /// Verifies the caller asked the resolver exactly once with the purpose in the expected call. Moq refuses a call
        /// it cannot intercept, as the stand-in above is, with a <see cref="NotSupportedException"/>; that refusal is
        /// what "the resolver cannot be told the purpose yet" looks like today, so it is reported as the assertion it is.
        /// </summary>
        public static void VerifyThePurposeIn(
            this Mock<IDeliverySourceResolver> resolver,
            Expression<Func<IDeliverySourceResolver, Task<IReadOnlyDictionary<string, PortfolioSourcePreview>>>> expectedCall)
        {
            try
            {
                resolver.Verify(expectedCall, Times.Once());
            }
            catch (NotSupportedException refused)
            {
                throw new AssertionException($"The resolver cannot be told the sweep's purpose yet: {refused.Message}");
            }
        }
    }
}
