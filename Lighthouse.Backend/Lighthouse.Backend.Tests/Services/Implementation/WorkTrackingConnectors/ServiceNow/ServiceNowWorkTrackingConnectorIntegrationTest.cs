using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Live tests against a real ServiceNow instance. Turns the SPIKE's one-off hand measurements
    /// (Q8 role matrix, spike/findings.md) into a standing guard: the unit tests pin the verdict
    /// ladder against a stubbed transport, and these pin that the instance still behaves the way
    /// the ladder assumes.
    ///
    /// Path-scoped via Category("ServiceNowIntegration") — see Scripts/test-selection/path-classifier.sh.
    /// Slice 02 extends this fixture with work-item reads rather than adding a second one.
    /// </summary>
    [Category("Integration")]
    [Category("ServiceNowIntegration")]
    public class ServiceNowWorkTrackingConnectorIntegrationTest
    {
        // PDIs are reclaimed after ~10 days idle, so the instance moves. Override without a code
        // change when it does.
        private const string DefaultInstanceUrl = "https://dev191338.service-now.com";

        private const string AdminUser = "admin";

        // Created during the SPIKE with no roles at all. The account that proves the headline bug:
        // it authenticates, and every ITSM read comes back 200 with zero rows.
        private const string NoRolesUser = "lh_probe_none";

        private const string MetricsTable = "metric_definition";

        [Test]
        public async Task ACredentialThatCanSeeIncidents_ValidatesSuccessfully()
        {
            var connection = CreateConnection(AdminUser);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        /// <summary>
        /// The headline bug, against a real instance. SPIKE Q8 measured that a permitted-but-
        /// unauthorised read returns 200 with zero rows — indistinguishable from an empty table —
        /// so a naive connector reports "connected, 0 work items found" and sends the customer
        /// hunting for a query bug that is actually a permissions problem.
        /// </summary>
        [Test]
        public async Task ACredentialWithNoRoles_IsNeverReportedAsValid()
        {
            var connection = CreateConnection(NoRolesUser);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("no_records_visible"));
                Assert.That(result.Message, Is.Not.Empty);
            }
        }

        [Test]
        public async Task ACredentialTheInstanceRejects_IsReportedAsAnAuthenticationFailure()
        {
            var connection = CreateConnection(AdminUser, password: "not-the-password");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("authentication_failed"));
            }
        }

        /// <summary>
        /// SPIKE Q8: metric_definition returns 403 for every read-only role and opens only at
        /// itil-grade. This is the rung that proves ServiceNow does sometimes deny honestly —
        /// without it, "everything is a silent 200" would be indistinguishable from a bug in the ladder.
        /// </summary>
        [Test]
        public async Task ATableTheCredentialMayNotTouch_IsReportedAsInsufficientPermissions()
        {
            var connection = CreateConnection(NoRolesUser, table: MetricsTable);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public async Task ATableTheInstanceDoesNotHave_IsReportedAsAnUnknownTable()
        {
            var connection = CreateConnection(AdminUser, table: "lighthouse_no_such_table");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
            }
        }

        [Test]
        public async Task AnInstanceThatIsNotThere_IsReportedAsAConnectionFailure()
        {
            var connection = CreateConnection(AdminUser, instanceUrl: "https://127.0.0.1:1");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        private static WorkTrackingSystemConnection CreateConnection(
            string username,
            string? password = null,
            string? instanceUrl = null,
            string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ServiceNow Integration Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instanceUrl ?? InstanceUrl() },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = username },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = password ?? Password(), IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table, IsOptional = true },
            ]);

            return connection;
        }

        // The probe accounts created during the SPIKE share the admin password.
        private static string Password()
        {
            return Environment.GetEnvironmentVariable("ServiceNowLighthouseIntegrationTestToken")
                ?? throw new NotSupportedException("Can run test only if Environment Variable 'ServiceNowLighthouseIntegrationTestToken' is set!");
        }

        private static string InstanceUrl()
        {
            return Environment.GetEnvironmentVariable("ServiceNowLighthouseIntegrationTestInstance") ?? DefaultInstanceUrl;
        }

        private static ServiceNowWorkTrackingConnector CreateSubject()
        {
            var cryptoService = new FakeCryptoService();

            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(cryptoService));
        }
    }
}
