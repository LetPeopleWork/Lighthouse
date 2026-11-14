using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Estimation is set once per grain on the connection: field, unit, non-numeric flag and categories as one
    /// bundle. It is read when an owner's page loads, so changing it updates nothing; a following owner's page
    /// shows "Connection default (Story Points, Points)". Clearing it leaves the followers without an
    /// estimation.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class Slice12EstimationDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private const string StoryPointsInPoints = "Story Points, Points";

        // Only a System Admin can read the connection, so a Team admin's page names the default from what the
        // Team's settings carry, and the unit is what tells two defaults on the same field apart.
        [Test]
        public async Task A_Team_admin_without_connection_rights_sees_the_Estimation_default_with_its_unit()
        {
            await GivenTheTeamsEstimationDefaultIs(StoryPoints, Points);

            var seen = await WhatTheTeamAdminSees(OpsTooling, OwnerSetting.Estimation);

            Assert.That(seen, Is.EqualTo(((string?)"ConnectionDefault", (string?)StoryPointsInPoints, (string?)"Jira")),
                "The page shows \"Connection default (Story Points, Points)\" and \"Set on the Jira connection.\"");
        }

        [TestCase(FraudSignals, "team-setting-source-unknown")]
        public async Task An_owner_saved_with_an_Estimation_source_Lighthouse_does_not_know_is_refused(string owner, string code)
        {
            await WhenTheOwnersAdminSendsTheSourceAsTheNumber(owner, 99, OwnerSetting.Estimation);
            var estimation = await WhatTheEstimationReadsFor(owner);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.BadRequest);
                Assert.That(TheRefusal().Code, Is.EqualTo(code));
                Assert.That(TheRefusal().Message, Is.EqualTo("The Estimation Field source must be one of: ConnectionDefault, Own. Nothing was saved."));
                Assert.That(estimation.Source, Is.EqualTo("ConnectionDefault"), "Nothing was saved.");
            }
        }
    }
}
