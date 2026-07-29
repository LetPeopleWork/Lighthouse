using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
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

        // ~600ms per Table API call was measured during the SPIKE, so validation asks the instance
        // exactly one question and asks for exactly one row.
        [Test]
        public async Task ValidatingAConnection_AsksTheConfiguredTableForASingleRecordAndNothingElse()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            await subject.ValidateConnection(CreateConnection(table: "change_request"));

            var probe = CapturedRequests(handler).Single();
            var probeUri = probe.RequestUri?.ToString() ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(probe.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(probeUri, Does.Contain("/api/now/table/change_request"));
                Assert.That(probeUri, Does.Contain("sysparm_limit=1"));
                Assert.That(probeUri, Does.Not.Contain("sysparm_fields"),
                    "Field projection was never measured against ACL filtering, so a validation probe must " +
                    "not rely on it. Slice 02 may add it once it has evidence.");
            }
        }

        [Test]
        public async Task AConnectionWithNoTableChosen_IsProbedAgainstTheIncidentTable()
        {
            var handler = RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord);
            var subject = CreateSubject(handler);

            await subject.ValidateConnection(CreateConnection(table: null));

            var probeUri = CapturedRequests(handler).Single().RequestUri?.ToString() ?? string.Empty;

            Assert.That(probeUri, Does.Contain($"/api/now/table/{ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable}"));
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

        // DoD 5 / KPI 3: every capability slice 01 does not deliver says so out loud. A method
        // that quietly returns nothing is the failure mode this epic is trying to avoid, because
        // it reads to the user as "no work found" rather than "not built yet".
        [Test]
        public void ReadingWorkFromServiceNow_IsDeclaredUnsupportedRatherThanReturningNothing()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.GetWorkItemsForTeam(new Team()),
                    Throws.InstanceOf<NotSupportedException>());
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

        [Test]
        public async Task PointingATeamAtServiceNow_IsRefusedWithAnActionableReason()
        {
            var subject = CreateSubject(RespondingWith(HttpStatusCode.OK, ProbeResponseWithOneRecord));

            var result = await subject.ValidateTeamSettings(new Team());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("team_settings_not_supported"));
                Assert.That(result.Message, Is.Not.Empty);
            }
        }

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

        private static ServiceNowWorkTrackingConnector CreateSubject(
            Mock<HttpMessageHandler> handler, IWorkTrackingAuthStrategyFactory? authStrategyFactory = null)
        {
            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                authStrategyFactory ?? NoOpAuthStrategyFactory(),
                handler.Object);
        }

        private static IWorkTrackingAuthStrategyFactory NoOpAuthStrategyFactory()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return factory.Object;
        }

        private static WorkTrackingSystemConnection CreateConnection(
            string instanceUrl = InstanceUrl, string? table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ServiceNow Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = "encrypted-secret", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table ?? string.Empty, IsOptional = true },
            ]);

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
