using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthSchemaExtensionsTest
    {
        [Test]
        public void TwoInstances_HoldDisjointExtraKeys_WithoutBleed()
        {
            var first = new OAuthSchemaExtensions(new[] { "first.oauth" });
            var second = new OAuthSchemaExtensions(new[] { "second.oauth" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ExtraOAuthKeys, Is.EquivalentTo(new[] { "first.oauth" }));
                Assert.That(second.ExtraOAuthKeys, Is.EquivalentTo(new[] { "second.oauth" }));
            }
        }

        [Test]
        public void ParameterlessConstructor_YieldsEmptyExtras()
        {
            var extensions = new OAuthSchemaExtensions();

            Assert.That(extensions.ExtraOAuthKeys, Is.Empty);
        }

        [Test]
        public void Constructor_NullKeys_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OAuthSchemaExtensions(null!));
        }
    }
}
