using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5574 / ADR-114 — the verdict ladder. ServiceNow answers a denial with a success
    // (200 + zero rows), so "did the call succeed" is not a usable definition of a valid
    // connection. Each test below is one rung of the L1-L7 lie catalogue from the DESIGN wave.
    //
    // Layer 1 (pure function, no IO). Example-based rather than generated: the input space IS the
    // enumerated rung set, so there is nothing for a generator to explore.
    [TestFixture]
    public class ServiceNowValidationVerdictTest
    {
        private const string ConfiguredTable = "incident";

        [Test]
        public void AnInstanceAddressThatIsNotAnAddress_IsRejectedBeforeAnythingIsSent()
        {
            var verdict = ServiceNowValidationVerdict.FromInvalidInstanceAddress("not-an-address");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("invalid_url"));
                Assert.That(verdict.Message, Is.Not.Empty);
            }
        }

        [Test]
        public void AnInstanceThatCannotBeReached_IsReportedAsAConnectionFailure()
        {
            var verdict = ServiceNowValidationVerdict.FromUnreachableInstance("No such host is known.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("connection_failed"));
                Assert.That(verdict.Message, Is.Not.Empty);
            }
        }

        [Test]
        public void ACredentialTheInstanceRejects_IsReportedAsAnAuthenticationFailure()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.Unauthorized, rowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("authentication_failed"));
            }
        }

        // ADR-115. Lighthouse cannot see whether the instance enforces the inbound basic-auth
        // restriction — measured invisible to the account a customer would grant it. So the hint
        // is worded as a possibility, never as a finding, and detection is forbidden.
        [Test]
        public void ARejectedCredential_NamesTheBasicAuthRoleWithoutClaimingToKnowItIsTheCause()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.Unauthorized, rowCount: 0);

            var hint = verdict.TechnicalDetails ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hint, Does.Contain("snc_basic_auth_api_access"));
                Assert.That(hint, Does.Contain("If this instance enforces"),
                    "The hint must stay conditional. Lighthouse cannot read the restriction properties " +
                    "with a least-privilege credential, so it must not assert the restriction is active.");
                Assert.That(hint, Does.Contain("cannot check this for you"));
            }
        }

        [Test]
        public void AnInstanceThatRefusesTheReadOutright_IsReportedAsInsufficientPermissions()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.Forbidden, rowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public void ATableTheInstanceDoesNotHave_IsReportedAsAnUnknownTable()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.BadRequest, rowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("unknown_table"));
                Assert.That(verdict.Message, Does.Contain(ConfiguredTable),
                    "Naming the table the administrator configured is what makes the message actionable.");
            }
        }

        // HYPOTHESIS, NOT MEASURED. The SPIKE never saw an SSO-fronted instance answer a Table API
        // call with a login page. This rung is a defensive guess and is tagged as such everywhere
        // it appears so a later reader does not mistake it for a finding.
        [Test]
        public void Hypothesis_ALoginPageWearingASuccessStatus_IsNotMistakenForData()
        {
            var verdict = ServiceNowValidationVerdict.FromResponse(
                HttpStatusCode.OK, responseIsJson: false, rowCount: 0, table: ConfiguredTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("unexpected_response"));
            }
        }

        [Test]
        public void AnInstanceThatAnswersSuccessfullyWithNothingVisible_IsNeverReportedAsValid()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.OK, rowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "This is the headline bug the slice exists to prevent: a permitted-but-unauthorised " +
                    "read is byte-identical to an empty table, so success must not be inferred from status alone.");
                Assert.That(verdict.Code, Is.EqualTo("no_records_visible"));
            }
        }

        // US-01 AC4 as amended and accepted 2026-07-29. ServiceNow gives no way to tell a rights
        // problem from an empty table, so the message names both causes and the role to grant
        // rather than asserting a certainty the platform cannot supply.
        [Test]
        public void NothingVisible_NamesBothPossibleCausesAndTheRoleToGrant()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.OK, rowCount: 0);

            var explanation = $"{verdict.Message} {verdict.TechnicalDetails}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(explanation, Does.Contain("read access"),
                    "First possible cause: the account cannot see the table.");
                Assert.That(explanation, Does.Contain("empty"),
                    "Second possible cause: the table genuinely holds nothing.");
                Assert.That(explanation, Does.Contain("sn_incident_read"),
                    "The role to grant is the actionable part of the message.");
                Assert.That(explanation, Does.Contain("snc_read_only"),
                    "snc_read_only grants no read access at all; its name invites exactly the wrong guess.");
                Assert.That(explanation, Does.Contain(ConfiguredTable));
            }
        }

        [Test]
        public void AnInstanceThatShowsWorkToTheCredential_IsReportedAsValid()
        {
            var verdict = WhenTheInstanceAnswers(HttpStatusCode.OK, rowCount: 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True);
                Assert.That(verdict.Code, Is.EqualTo("valid"));
            }
        }

        // The whole ladder in one table. This is where the mutants land: flipping any rung's code
        // or its IsValid flag fails here.
        [Test]
        [TestCase(HttpStatusCode.Unauthorized, true, 0, "authentication_failed", false)]
        [TestCase(HttpStatusCode.Forbidden, true, 0, "insufficient_permissions", false)]
        [TestCase(HttpStatusCode.BadRequest, true, 0, "unknown_table", false)]
        [TestCase(HttpStatusCode.OK, false, 0, "unexpected_response", false)]
        [TestCase(HttpStatusCode.OK, true, 0, "no_records_visible", false)]
        [TestCase(HttpStatusCode.OK, true, 1, "valid", true)]
        [TestCase(HttpStatusCode.OK, true, 42, "valid", true)]
        public void EveryRungOfTheLadder_ProducesItsOwnVerdict(
            HttpStatusCode statusCode, bool responseIsJson, int rowCount, string expectedCode, bool expectedIsValid)
        {
            var verdict = ServiceNowValidationVerdict.FromResponse(statusCode, responseIsJson, rowCount, ConfiguredTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo(expectedCode));
                Assert.That(verdict.IsValid, Is.EqualTo(expectedIsValid));
                Assert.That(verdict.Message, Is.Not.Empty,
                    "KPI 3: every outcome is user-visible. A verdict with no message is a silent no-op.");
            }
        }

        // US-01 AC4's safety property, stated directly rather than inferred from the rows above.
        [Test]
        public void TheThreeFailuresAnAdministratorWillMeet_AreToldApart()
        {
            var unreachable = ServiceNowValidationVerdict.FromUnreachableInstance("Connection refused");
            var badCredential = WhenTheInstanceAnswers(HttpStatusCode.Unauthorized, rowCount: 0);
            var nothingVisible = WhenTheInstanceAnswers(HttpStatusCode.OK, rowCount: 0);

            var codes = new[] { unreachable.Code, badCredential.Code, nothingVisible.Code };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(codes, Is.Unique);
                Assert.That(new[] { unreachable.IsValid, badCredential.IsValid, nothingVisible.IsValid },
                    Is.All.False);
                Assert.That(nothingVisible.Code, Is.Not.EqualTo(unreachable.Code),
                    "A rights problem must never be reported as a connection problem.");
            }
        }

        [Test]
        public void ARightsProblem_IsNeverDressedUpAsAReachabilityProblem()
        {
            var reachabilityCodes = new[] { "connection_failed", "invalid_url" };

            var deniedOutright = WhenTheInstanceAnswers(HttpStatusCode.Forbidden, rowCount: 0);
            var deniedSilently = WhenTheInstanceAnswers(HttpStatusCode.OK, rowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reachabilityCodes, Does.Not.Contain(deniedOutright.Code));
                Assert.That(reachabilityCodes, Does.Not.Contain(deniedSilently.Code));
            }
        }

        private static Lighthouse.Backend.Models.Validation.ConnectionValidationResult WhenTheInstanceAnswers(
            HttpStatusCode statusCode, int rowCount)
        {
            return ServiceNowValidationVerdict.FromResponse(statusCode, responseIsJson: true, rowCount, ConfiguredTable);
        }
    }
}
