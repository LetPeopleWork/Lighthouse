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
    /// <summary>
    /// Epic #5511 slice 04, AC-04.2 - the number, for ServiceNow, and the last of the four connectors.
    ///
    /// This one hid its cancellation the deepest. Every ServiceNow round trip goes through one private
    /// <c>Read</c>, five methods below the public surface, and that method did not merely fail to pass the
    /// token on - it wrote <c>CancellationToken.None</c> out by hand when applying the credential, and gave
    /// <c>SendAsync</c> no token at all. Nothing about the signature above it said so.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public class ServiceNowCancellationGranularityTest
    {
        private const string InstanceUrl = "https://example.service-now.com";

        private const int ThePageTheCancelArrivesOn = 3;

        /// <summary>
        /// The instance stops offering pages here rather than never. An endless supply would hang a
        /// connector that ignores the token instead of failing it, and a test that hangs teaches nothing.
        /// </summary>
        private const int PagesTheInstanceWillOffer = 40;

        private const int RecordsPerPage = 2;

        [Test]
        public async Task GetWorkItemsForTeam_CancelledBeforeItStarts_NeverAsksTheInstanceAnything()
        {
            var instance = new AnInstanceThatAlwaysHasAnotherPage();
            var subject = AServiceNowReading(instance);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(ATeamOnServiceNow(), alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(instance.Reads, Is.Zero,
                    "A refresh cancelled before it began must not spend the instance's quota an operator "
                    + "cancelled to protect. The first page is already too many.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_CancelledWhileItPages_StopsWithinOnePageRoundTrip()
        {
            using var stopAfterTheThirdPage = new CancellationTokenSource();

            var instance = new AnInstanceThatAlwaysHasAnotherPage();
            instance.OnRead = () =>
            {
                if (instance.Reads == ThePageTheCancelArrivesOn)
                {
                    stopAfterTheThirdPage.Cancel();
                }
            };

            var subject = AServiceNowReading(instance);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(ATeamOnServiceNow(), stopAfterTheThirdPage.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(instance.Reads, Is.EqualTo(3),
                    "This is the number AC-04.2 asserts against: a refresh told to stop mid-walk stops after "
                    + "the page it is already reading, not after the whole table. A fourth page means the "
                    + "token never reached the request, with thirty-seven pages still on offer.");
            }
        }

        private static ServiceNowWorkTrackingConnector AServiceNowReading(AnInstanceThatAlwaysHasAnotherPage instance)
            => new(Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(), ANoOpAuthStrategyFactory(), instance.Handler);

        private static IWorkTrackingAuthStrategyFactory ANoOpAuthStrategyFactory()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return factory.Object;
        }

        private static Team ATeamOnServiceNow()
        {
            var team = new Team
            {
                Name = "Demo Team",
                DataRetrievalValue = "active=true",
                WorkTrackingSystemConnection = AServiceNowConnection(),
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("change_request");
            team.ToDoStates.Clear();
            team.ToDoStates.Add("New");
            team.DoingStates.Clear();
            team.DoingStates.Add("Work in Progress");
            team.DoneStates.Clear();
            team.DoneStates.Add("Closed");

            return team;
        }

        private static WorkTrackingSystemConnection AServiceNowConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                Name = "ServiceNow Connection",
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = ServiceNowWorkTrackingOptionNames.InstanceUrl,
                Value = InstanceUrl,
            });

            return connection;
        }

        /// <summary>
        /// A ServiceNow instance whose table always has another page. Every record carries its own sys_id,
        /// because the connector aborts a read that sees the same identity twice - so a fake that repeats
        /// itself would fail the walk for a reason that has nothing to do with cancellation.
        /// </summary>
        private sealed class AnInstanceThatAlwaysHasAnotherPage
        {
            private int recordsHandedOut;

            public AnInstanceThatAlwaysHasAnotherPage()
            {
                var mock = new Mock<HttpMessageHandler>();
                mock.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                mock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                    {
                        // Counted when it is asked for and only then refused, exactly as a socket would:
                        // a walk that issues its next request and has it refused has still spent the quota.
                        Reads++;
                        token.ThrowIfCancellationRequested();
                        OnRead?.Invoke();

                        return Task.FromResult(APageOfRecords(request));
                    });

                Handler = mock.Object;
            }

            public HttpMessageHandler Handler { get; }

            public int Reads { get; private set; }

            public Action? OnRead { get; set; }

            private HttpResponseMessage APageOfRecords(HttpRequestMessage request)
            {
                var records = new StringBuilder();

                for (var record = 0; record < RecordsPerPage; record++)
                {
                    recordsHandedOut++;

                    if (records.Length > 0)
                    {
                        records.Append(',');
                    }

                    records.Append(ARecord(recordsHandedOut));
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($@"{{ ""result"": [ {records} ] }}", Encoding.UTF8, "application/json"),
                };

                // The count has to agree with the Link headers. The connector also pages by offset when
                // the instance stops linking, so a count that outruns the pages on offer walks on to the
                // connector's own thousand-page ceiling and dies there instead of finishing.
                response.Headers.Add("X-Total-Count", $"{PagesTheInstanceWillOffer * RecordsPerPage}");

                if (Reads < PagesTheInstanceWillOffer)
                {
                    var nextPage = request.RequestUri ?? new Uri(InstanceUrl);
                    response.Headers.Add("Link", $"<{nextPage}&sysparm_offset={recordsHandedOut}>;rel=\"next\"");
                }

                return response;
            }

            private static string ARecord(int number) => $@"{{
                ""sys_id"": {{ ""display_value"": ""id-{number}"", ""value"": ""id-{number}"" }},
                ""number"": {{ ""display_value"": ""CHG{number:D7}"", ""value"": ""CHG{number:D7}"" }},
                ""sys_class_name"": {{ ""display_value"": ""Change Request"", ""value"": ""change_request"" }},
                ""short_description"": {{ ""display_value"": ""Record {number}"", ""value"": ""Record {number}"" }},
                ""state"": {{ ""display_value"": ""New"", ""value"": ""-5"" }},
                ""sys_created_on"": {{ ""display_value"": ""2026-01-01 08:00:00"", ""value"": ""2026-01-01 08:00:00"" }}
            }}";
        }
    }
}
