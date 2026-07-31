using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5574. The imperative shell around the verdict ladder, exercised with a stubbed
    // transport so the ladder's rungs are reachable without a live instance.
    //
    // Layer 3 (real adapter, stubbed transport): sad paths are enumerated one example each, never
    // generated. The exhaustive rung coverage lives in ServiceNowValidationVerdictTest.
    [TestFixture]
    public class ServiceNowWorkTrackingConnectorTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string ProbeResponseWithOneRecord = """{"result":[{"sys_id":"abc","number":"INC0010001"}]}""";
        private const string ProbeResponseWithNothingVisible = """{"result":[]}""";

        // The headline assertion. A permitted-but-unauthorised read and a genuinely empty table
        // are byte-identical, so a connector that infers success from the status code ships the
        // bug this whole slice exists to prevent.
        [Test]
        public async Task AnInstanceThatAnswersSuccessfullyWithNothingVisible_IsNotReportedAsAWorkingConnection()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithNothingVisible));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("no_records_visible"));
            }
        }

        [Test]
        public async Task AnInstanceThatShowsWorkToTheCredential_IsReportedAsAWorkingConnection()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        [Test]
        public async Task AnInstanceThatRejectsTheCredential_IsReportedAsAnAuthenticationFailure()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.Unauthorized, string.Empty));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("authentication_failed"));
            }
        }

        [Test]
        public async Task AnInstanceThatCannotBeReached_IsReportedAsAConnectionFailure()
        {
            var subject = CreateSubject(Failing(new HttpRequestException("No such host is known.")));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        [Test]
        public async Task AnInstanceAddressThatIsNotAnAddress_IsRejectedWithoutContactingAnything()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            var result = await subject.ValidateConnection(CreateConnection(instanceUrl: "dev12345"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("invalid_url"));
            }

            VerifyRequestCount(handler, 0);
        }

        // A connection has one thing to prove and one place to prove it: the work hierarchy every
        // read is rooted at. There is nothing to configure and nothing to get wrong (ADR-116
        // decision 1, withdrawn 2026-07-31).
        [Test]
        public async Task ValidatingAConnection_AsksTheWorkHierarchyForASingleRecord()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            await subject.ValidateConnection(CreateConnection());

            var probe = CapturedRequests(handler)[0];
            var probeUri = probe.RequestUri?.ToString() ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(probeUri, Does.Contain($"/api/now/table/{ServiceNowReadScope.RootTable}"));
                Assert.That(probeUri, Does.Contain("sysparm_limit=1"));
                Assert.That(probeUri, Does.Not.Contain("sysparm_fields"),
                    "Field projection was never measured against ACL filtering, so a validation probe must " +
                    "not rely on it. Slice 02 may add it once it has evidence.");
            }
        }

        // AC5. The connector never touches the stored password itself: decryption and the Basic
        // header are the auth strategy's job, resolved through the same factory every other
        // connector uses.
        [Test]
        public async Task ValidatingAConnection_LeavesTheCredentialHandlingToTheResolvedAuthenticationStrategy()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(AuthenticationMethodKeys.ServiceNowBasic)).Returns(strategy.Object);

            var connection = CreateConnection();
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord), factory.Object);

            await subject.ValidateConnection(connection);

            factory.Verify(f => f.Resolve(AuthenticationMethodKeys.ServiceNowBasic), Times.AtLeastOnce);
            strategy.Verify(
                s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.Is<WorkTrackingSystemConnection>(c => c == connection), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        // DoD 5 / KPI 3: every capability ServiceNow does not deliver says so out loud. A method
        // that quietly returns nothing is the failure mode this epic is trying to avoid, because
        // it reads to the user as "no work found" rather than "not supported". Reading a team's
        // work moved off this list in slice 02 (#5575); the portfolio refusals are permanent.
        [Test]
        public void ReadingWorkFromServiceNow_IsDeclaredUnsupportedRatherThanReturningNothing()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.GetFeaturesForProject(new Portfolio()),
                    Throws.InstanceOf<NotSupportedException>());
                Assert.That(async () => await subject.GetParentFeaturesDetails(new Portfolio(), ["PRJ0001"]),
                    Throws.InstanceOf<NotSupportedException>());
            }
        }

        // D8, permanently out of scope. Linear sets the precedent.
        [Test]
        public void WritingBackToServiceNow_IsDeclaredUnsupported()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            Assert.That(async () => await subject.WriteFieldsToWorkItems(CreateConnection(), []),
                Throws.InstanceOf<NotSupportedException>());
        }

        // Slice 02 (#5575) replaced the blanket team refusal this fixture used to assert with a
        // verdict about the team's own query. Its successors are the ValidatingATeam… tests in
        // ServiceNowTeamSyncTest.

        // Slice 03 is cancelled: ITSM has no rollup Lighthouse can forecast over. The connector
        // declines rather than half-working.
        [Test]
        public async Task PointingAPortfolioAtServiceNow_IsRefusedWithAnActionableReason()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            var result = await subject.ValidatePortfolioSettings(new Portfolio());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("portfolio_not_supported"));
                Assert.That(result.Message, Is.Not.Empty);
            }
        }

        // D6: the history source costs an itil-grade role, so v1 says no rather than guessing.
        [Test]
        public void TimeInStateOnServiceNowWork_IsDeclaredUnavailable()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            Assert.That(subject.SupportsTransitionHistory(CreateConnection()), Is.False);
        }

        [Test]
        public void AServiceNowConnection_BringsNoPredefinedAdditionalFields()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            Assert.That(subject.GetPredefinedAdditionalFields(CreateConnection()), Is.Empty);
        }

        // ServiceNow answers some failures with a structured error envelope carrying a 200. That is
        // still JSON, and still zero rows, so it belongs on the no-visible-rows rung. Reading it as
        // "not data" would send the administrator off to look at single sign-on instead of at rights.
        [Test]
        public async Task AnInstanceThatAnswersWithAStructuredErrorEnvelope_IsReadAsJsonWithNoRows()
        {
            var subject = CreateSubject(RespondingWith(
                HttpStatusCode.OK, """{"error":{"message":"Insufficient rights"},"status":"failure"}"""));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("no_records_visible"));
            }
        }

        // The single-sign-on shape behind the hypothesis rung: a login page wearing a 200. Nothing
        // in it parses, so the connector reports an answer it could not read rather than no rows.
        [Test]
        public async Task AnInstanceThatAnswersWithASignInPage_IsNotMistakenForAnEmptyTable()
        {
            var subject = CreateSubject(RespondingWith(
                HttpStatusCode.OK,
                "<html><head><title>Sign In</title></head><body>Log in to continue</body></html>",
                contentType: "text/html"));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unexpected_response"));
            }
        }

        // Parseable as a URI, but not one the Table API can be reached over. Rejected pre-flight for
        // the same reason an unparseable address is: there is nothing worth sending.
        [Test]
        public async Task AnInstanceAddressThatIsNotAnHttpAddress_IsRejectedWithoutContactingAnything()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            var result = await subject.ValidateConnection(CreateConnection(instanceUrl: "ftp://dev12345.service-now.com"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("invalid_url"));
            }

            VerifyRequestCount(handler, 0);
        }

        // An option nobody ever saved and an option saved blank are the same thing to an
        // administrator, so they have to produce the same answer — neither may substitute an
        // address of its own for the one that is missing.
        [Test]
        public async Task AConnectionWithNoInstanceAddressAtAll_IsRejectedExactlyAsABlankOneIs()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            var withoutTheOption = await subject.ValidateConnection(ConnectionWithoutAnInstanceAddressOption());
            var withABlankOption = await subject.ValidateConnection(CreateConnection(instanceUrl: string.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutTheOption.Code, Is.EqualTo("invalid_url"));
                Assert.That(withoutTheOption.Message, Is.EqualTo(withABlankOption.Message));
            }

            VerifyRequestCount(handler, 0);
        }

        // ~600ms per Table API call was measured during the SPIKE, so a probe that never comes back
        // is a shape this connector will meet. It is an instance Lighthouse could not reach, and it
        // has to read as that rather than escaping as a cancellation.
        [Test]
        public async Task AnInstanceThatNeverAnswers_IsReportedAsAConnectionFailure()
        {
            var subject = CreateSubject(Failing(
                new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout elapsing.")));

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        // A connection saved before ServiceNow recorded an authentication method still has to
        // authenticate. An unset method means the one method slice 01 ships, never "no credential".
        [Test]
        [TestCase("")]
        [TestCase("   ")]
        public async Task AConnectionWithNoAuthenticationMethodRecorded_IsAuthenticatedWithBasic(string storedKey)
        {
            var factory = NoOpAuthStrategyFactory();
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord), factory.Object);

            await subject.ValidateConnection(CreateConnection(authenticationMethodKey: storedKey));

            factory.Verify(f => f.Resolve(AuthenticationMethodKeys.ServiceNowBasic), Times.AtLeastOnce);
        }

        // ...and a connection that does record one is authenticated with that one, so the fallback
        // above cannot quietly become the only path once a second method exists.
        [Test]
        public async Task AConnectionThatRecordsItsAuthenticationMethod_IsAuthenticatedWithThatMethod()
        {
            var factory = NoOpAuthStrategyFactory();
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord), factory.Object);

            await subject.ValidateConnection(CreateConnection(authenticationMethodKey: "servicenow.oauth"));

            factory.Verify(f => f.Resolve("servicenow.oauth"), Times.AtLeastOnce);
        }

        // Story #5577, ADR-118 D5, as re-decided by ADR-123 decision 10. Connection validation no
        // longer reads metric_definition at all: it would ask about `task`, which carries none
        // (measured 0 rows), and answer with advice to activate a definition there — unfollowable,
        // and contradicted by what the connection's teams actually get. So the connection says the
        // one true thing instead, in one request rather than two.
        [Test]
        public async Task AWorkingConnection_SaysHistoryIsDecidedPerTeamRatherThanClaimingWhatItCannotKnow()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.AdvisoryCode, Is.EqualTo("history_determined_per_team"));
                Assert.That(result.Advisory, Is.Not.Null.And.Not.Empty,
                    "Declining to answer and then saying nothing about it would be the silent no-op DoD 5 forbids.");
            }

            VerifyRequestCount(handler, 1);
        }

        // The capability question is only worth answering for a connection that already works.
        // Answering it for one that does not would overwrite the rung the administrator needs — a
        // rejected credential would come back as advice about metric definitions instead.
        [Test]
        public async Task AConnectionThatFailsTheLadder_KeepsItsOwnVerdictAndSaysNothingAboutHistory()
        {
            var handler = RespondingWith(HttpStatusCode.Unauthorized, string.Empty);
            var subject = CreateSubject(handler);

            var result = await subject.ValidateConnection(CreateConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("authentication_failed"));
                Assert.That(result.Advisory, Is.Null);
                Assert.That(result.AdvisoryCode, Is.Null);
            }

            VerifyRequestCount(handler, 1);
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(
            Mock<HttpMessageHandler> handler, IWorkTrackingAuthStrategyFactory? authStrategyFactory = null)
        {
            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                authStrategyFactory ?? NoOpAuthStrategyFactory().Object,
                handler.Object);
        }

        private static Mock<IWorkTrackingAuthStrategyFactory> NoOpAuthStrategyFactory()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return factory;
        }

        private static WorkTrackingSystemConnection CreateConnection(
            string instanceUrl = InstanceUrl,
            string authenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ServiceNow Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = authenticationMethodKey,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = "encrypted-secret", IsSecret = true },
            ]);

            return connection;
        }

        private static WorkTrackingSystemConnection ConnectionWithoutAnInstanceAddressOption()
        {
            var connection = CreateConnection();
            connection.Options.RemoveAll(option => option.Key == ServiceNowWorkTrackingOptionNames.InstanceUrl);

            return connection;
        }

        private static Mock<HttpMessageHandler> RespondingWith(HttpStatusCode statusCode, string body, string contentType = "application/json")
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, Encoding.UTF8, contentType),
                });

            return handler;
        }

        private static Mock<HttpMessageHandler> Failing(Exception exception)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);

            return handler;
        }

        private static List<HttpRequestMessage> CapturedRequests(Mock<HttpMessageHandler> handler)
        {
            return handler.Invocations
                .Where(invocation => invocation.Method.Name == "SendAsync")
                .Select(invocation => (HttpRequestMessage)invocation.Arguments[0])
                .ToList();
        }

        private static void VerifyRequestCount(Mock<HttpMessageHandler> handler, int expected)
        {
            Assert.That(CapturedRequests(handler), Has.Count.EqualTo(expected));
        }
    }
}
