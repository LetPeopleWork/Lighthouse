using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// The four facts attached to every event that leaves. Nobody outside this class can see them
    /// being worked out: the browser is never asked for any of them, and an instance that gets one
    /// wrong is not wrong once - the same answer rides every event it ever sends, so a mistake here
    /// is a whole column of the shared figures reading something that was never true.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataInstancePropertiesTests
    {
        private const string WhatAVersionNobodyPublishedIsCalledInstead = "unreleased";

        /// <summary>
        /// A version is what a support cut-off is planned against, so a release has to arrive as
        /// itself. What everyone on a release shares says nothing about any of them; a number built
        /// from somebody's working tree is close to unique, and the few people running one are the
        /// people most easily recognised from it - so that one is replaced rather than sent.
        /// </summary>
        [TestCase("v26.9.13", "v26.9.13", true, TestName = "TheVersionReported(a release)")]
        [TestCase("v26.12.31.4", "v26.12.31.4", true, TestName = "TheVersionReported(a release with a build number)")]
        [TestCase("DEV", WhatAVersionNobodyPublishedIsCalledInstead, false, TestName = "TheVersionReported(a development build)")]
        [TestCase("v26.9.13-dirty", WhatAVersionNobodyPublishedIsCalledInstead, false, TestName = "TheVersionReported(a working tree)")]
        [TestCase("1.2.3", WhatAVersionNobodyPublishedIsCalledInstead, false, TestName = "TheVersionReported(not a date at all)")]
        public void TheVersionThisInstanceReports_IsSentOnlyWhenItIsOneEverybodyShares(
            string whatThisBuildCallsItself, string expected, bool published)
        {
            var facts = Describing(version: whatThisBuildCallsItself);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(facts.Version, Is.EqualTo(expected),
                    "either a version that identifies whoever built it went out as it was, or a "
                    + "version everybody on the release shares was thrown away and the support "
                    + "cut-off nobody can now plan is what that costs");
                Assert.That(facts.IsPublishedRelease, Is.EqualTo(published),
                    "whether this is a release also decides whether the built-in collector is "
                    + "written to at all, so getting it wrong either silences every release or puts "
                    + "working trees into the shared figures");
            }
        }

        /// <summary>
        /// Which tier an instance is on is the one fact here a commercial decision gets read off.
        /// Both answers have to be pinned, because a value that is constant is indistinguishable
        /// from a value that is correct in the only case anybody happened to look at.
        /// </summary>
        [TestCase(true, "Premium")]
        [TestCase(false, "Community")]
        public void TheTierThisInstanceReports_IsTheOneItsLicenceActuallyBuys(bool premium, string expected)
        {
            Assert.That(Describing(premium: premium).LicenceTier, Is.EqualTo(expected),
                "the tier reported is not the tier in force, so every event from this instance is "
                + "counted against the wrong side of the only commercial split in these figures");
        }

        /// <summary>
        /// Only a working sign-in makes "one browser is roughly one person" plausible. With
        /// authentication off every caller is the same subject, and an instance that cannot let
        /// anybody in is not one people have their own logins on either - so those three arrive as
        /// the same answer, and it is the answer that says do not read these as people.
        /// </summary>
        [TestCase(AuthMode.Enabled, true)]
        [TestCase(AuthMode.Disabled, false)]
        [TestCase(AuthMode.Misconfigured, false)]
        [TestCase(AuthMode.Blocked, false)]
        public void WhetherAnybodySignsIn_IsReportedTrueOnlyWhenSigningInActuallyWorks(
            AuthMode mode, bool expected)
        {
            Assert.That(Describing(authentication: mode).AuthenticationEnabled, Is.EqualTo(expected),
                "an instance where nobody can sign in reported that they can, or the other way "
                + "round - and this is the flag that says whether counting browsers is anything "
                + "like counting people");
        }

        [Test]
        public void WhatShapeThisInstanceRunsIn_IsWhateverTheRestOfTheApplicationAlreadySaysItIs()
        {
            Assert.That(Describing(deployment: UsageDataDeploymentMode.Kubernetes).DeploymentMode,
                Is.EqualTo(UsageDataDeploymentMode.Kubernetes),
                "the deployment shape was decided here instead of being taken from the part of the "
                + "application that already works it out, so the two can now disagree");
        }

        private static UsageDataInstanceFacts Describing(
            string version = "v26.9.13",
            bool premium = false,
            AuthMode authentication = AuthMode.Disabled,
            UsageDataDeploymentMode deployment = UsageDataDeploymentMode.Docker)
        {
            var releases = new Mock<ILighthouseReleaseService>();
            releases.Setup(asked => asked.GetCurrentVersion()).Returns(version);

            var licences = new Mock<ILicenseService>();
            licences.Setup(asked => asked.CanUsePremiumFeatures()).Returns(premium);

            var signingIn = new Mock<IAuthModeResolver>();
            signingIn.Setup(asked => asked.Resolve()).Returns(new RuntimeAuthStatus { Mode = authentication });

            var shape = new Mock<IUsageDataDeploymentModeResolver>();
            shape.Setup(asked => asked.Resolve()).Returns(deployment);

            // Three of the four are per-request services and what is being described runs on a
            // background loop that has no request, so a scope factory is the seam rather than a
            // shortcut taken here.
            var services = new ServiceCollection();
            services.AddScoped(_ => releases.Object);
            services.AddScoped(_ => licences.Object);
            services.AddScoped(_ => signingIn.Object);

            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            return new UsageDataInstanceProperties(scopeFactory, shape.Object).Describe();
        }
    }
}
