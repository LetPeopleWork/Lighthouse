using System.Security.Claims;
using Lighthouse.Backend.Modules.Access.Domain;

namespace Lighthouse.Backend.Tests.Modules.Access.Domain
{
    [TestFixture]
    public class GroupClaimParserTest
    {
        private const string GroupsClaim = "groups";

        private static ClaimsPrincipal PrincipalWith(params string[] values) =>
            new(new ClaimsIdentity(values.Select(value => new Claim(GroupsClaim, value)), "test"));

        [Test]
        public void Plain_values_are_trimmed_and_collected_one_per_claim()
        {
            GroupClaimParser.TryGetGroupValues(PrincipalWith(" admins ", "devs"), GroupsClaim, out var groups, out var unsupported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groups, Is.EquivalentTo(["admins", "devs"]));
                Assert.That(unsupported, Is.False);
            }
        }

        [Test]
        public void A_blank_claim_adds_no_group()
        {
            GroupClaimParser.TryGetGroupValues(PrincipalWith("   ", "devs"), GroupsClaim, out var groups, out var unsupported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groups, Is.EquivalentTo(["devs"]));
                Assert.That(unsupported, Is.False);
            }
        }

        [Test]
        public void A_json_array_claim_adds_its_values_and_not_its_own_text()
        {
            GroupClaimParser.TryGetGroupValues(PrincipalWith("[\"admins\", \" devs \"]"), GroupsClaim, out var groups, out var unsupported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groups, Is.EquivalentTo(["admins", "devs"]));
                Assert.That(unsupported, Is.False);
            }
        }

        [Test]
        public void Blank_entries_inside_a_json_array_are_skipped()
        {
            GroupClaimParser.TryGetGroupValues(PrincipalWith("[\"a\", \" \", \"b\"]"), GroupsClaim, out var groups, out var unsupported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groups, Is.EquivalentTo(["a", "b"]));
                Assert.That(unsupported, Is.False);
            }
        }

        [TestCase("[1, 2]")]
        [TestCase("[\"a\", 2]")]
        [TestCase("[\"a\"")]
        [TestCase("{\"a\": 1}")]
        public void A_claim_that_is_not_a_list_of_strings_is_reported_as_unsupported(string claimValue)
        {
            GroupClaimParser.TryGetGroupValues(PrincipalWith(claimValue), GroupsClaim, out _, out var unsupported);

            Assert.That(unsupported, Is.True);
        }
    }
}
