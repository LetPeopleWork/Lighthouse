using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Every Community instance runs without sign-in, and there whoever reaches the instance may do everything:
    /// create, edit and delete reports included. The Read and Write guards are the existing ones, so this pins
    /// that the Reports addresses use them rather than a rule of their own.
    ///
    /// Driving ports: POST /teams/{id}/reports, PUT/DELETE /teams/{id}/reports/{reportId}.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    public class ReportsWithoutSignInScenarios : ReportsAcceptanceTest
    {
        protected override WebApplicationFactory<Program> WithAuthentication(TestHelpers.TestWebApplicationFactory<Program> root) => root;

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice01)]
        public async Task Without_sign_in_anybody_creates_a_report()
        {
            var lightspeed = ATeam(Lightspeed);

            using var created = await CreateAReport(lightspeed, new NewReport());

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created), await created.Content.ReadAsStringAsync());
        }

        // @driving_port @real-io @us-08 @slice-08 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice08)]
        public async Task Without_sign_in_anybody_deletes_a_report()
        {
            var lightspeed = ATeam(Lightspeed);
            using var created = await CreateAReport(lightspeed, new NewReport());
            var reportId = await ThenItWasCreated(created);

            using var deleted = await DeleteTheReport(lightspeed, reportId);

            Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        // @driving_port @real-io @us-10 @slice-10 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice10)]
        public async Task Without_sign_in_anybody_edits_a_report()
        {
            var lightspeed = ATeam(Lightspeed);
            using var created = await CreateAReport(lightspeed, new NewReport());
            var reportId = await ThenItWasCreated(created);

            using var saved = await EditTheReport(lightspeed, reportId, AnEdit(await TheReport(lightspeed, reportId), nowLengthDays: 30));

            Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
        }
    }
}
