using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class AuthenticationMethodDtoTest
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void FromSchema_PropagatesIsPremiumFlag(bool isPremium)
        {
            var method = new AuthenticationMethod
            {
                Key = "some.key",
                DisplayName = "Some Method",
                IsPremium = isPremium,
                Options = []
            };

            var dto = AuthenticationMethodDto.FromSchema(method);

            Assert.That(dto.IsPremium, Is.EqualTo(isPremium));
        }

        [Test]
        public void FromSchema_CopiesKeyAndDisplayName()
        {
            var method = new AuthenticationMethod
            {
                Key = "some.key",
                DisplayName = "Some Display",
                Options = []
            };

            var dto = AuthenticationMethodDto.FromSchema(method);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Key, Is.EqualTo("some.key"));
                Assert.That(dto.DisplayName, Is.EqualTo("Some Display"));
            }
        }
    }
}
