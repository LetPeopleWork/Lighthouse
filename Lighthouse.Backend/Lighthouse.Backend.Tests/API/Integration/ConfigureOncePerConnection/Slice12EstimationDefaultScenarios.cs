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

        [Test]
        public async Task Every_Team_without_an_estimation_shows_the_default_on_its_next_page_load_and_nothing_updates()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheEstimationDefault(ConnectionDefault.TeamsEstimation, StoryPoints, Points, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave();
            var ledger = await WhatTheEstimationReadsFor(Ledger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ledger, Is.EqualTo(((string?)StoryPoints, (string?)Points, (string?)"ConnectionDefault", (string?)StoryPointsInPoints)));
                Assert.That(ran, Is.Empty, "Estimation is read when the page loads, so nothing needs downloading.");
                Assert.That(await HowManyFollow(ConnectionDefault.TeamsEstimation), Is.EqualTo(4), "The dialog says 4 Teams follow it.");
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Teams' Estimation default").And.Some.Contains("4 Teams follow it"));
            }
        }

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

        // Moving a Team to another connection throws its Work Items away, so a save refused only after that
        // would lose them for nothing.
        [Test]
        public async Task A_Team_moved_to_another_connection_with_an_Estimation_source_Lighthouse_does_not_know_is_refused_and_keeps_its_Work_Items()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheOwnersAdminSendsTheSourceAsTheNumber(FraudSignals, 99, OwnerSetting.Estimation, movingTo: JiraDcTreasury);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsRefusedWith(HttpStatusCode.BadRequest);
                Assert.That(TheRefusal().Code, Is.EqualTo("team-setting-source-unknown"));
                Assert.That(await WhereTheItemOfHangs(FraudSignals), Is.EqualTo(TheTrackersOwnParent), "Its Work Items are still there.");
                Assert.That(await TheConnectionOf(FraudSignals), Is.EqualTo(JiraDcPayments), "Nothing was saved.");
            }
        }
    }
}
