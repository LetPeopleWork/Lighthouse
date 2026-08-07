using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Epic 5375 slice 02 widened several constructors with <see cref="IFeatureOrdering"/>. Tests written
    /// before it want the ordering they always had - the tracker's own value, ties broken on Id - so they
    /// take the real seam over a policy that says the tracker still owns the order. A bare
    /// <c>Mock.Of&lt;IFeatureOrdering&gt;()</c> would hand back an empty sequence and quietly gut them.
    /// </summary>
    public static class FeatureOrderingTestHelper
    {
        public static IFeatureOrdering FollowingTheTracker()
        {
            var policyProvider = new Mock<IFeatureOrderingPolicyProvider>();
            policyProvider.Setup(provider => provider.GetPolicy()).Returns(FeatureOrderingPolicy.SourceOrder);

            return new FeatureOrdering(policyProvider.Object);
        }
    }
}
