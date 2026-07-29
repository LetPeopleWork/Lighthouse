using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5574, US-01 AC1 and AC2. Nothing here is behaviour: it is the configuration surface
    // that makes ServiceNow selectable and makes its connection form render from the schema
    // instead of from a bespoke screen.
    [TestFixture]
    public class ServiceNowConnectionConfigurationTest
    {
        // Bug guard, not a style preference: the work tracking system is persisted as an int, so
        // a member inserted above ServiceNow silently repoints every stored connection to a
        // different system.
        [Test]
        public void ServiceNow_SitsAtTheEndOfTheStoredWorkTrackingSystemOrder()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)WorkTrackingSystems.ServiceNow, Is.EqualTo(4));
                Assert.That((int)WorkTrackingSystems.AzureDevOps, Is.Zero);
                Assert.That((int)WorkTrackingSystems.Jira, Is.EqualTo(1));
                Assert.That((int)WorkTrackingSystems.Linear, Is.EqualTo(2));
                Assert.That((int)WorkTrackingSystems.Csv, Is.EqualTo(3));
            }
        }

        [Test]
        public void AShopThatTracksWorkInServiceNow_FindsItAmongTheSystemsTheyCanConnect()
        {
            Assert.That(Enum.GetValues<WorkTrackingSystems>(), Contains.Item(WorkTrackingSystems.ServiceNow));
        }

        [Test]
        public void ANewServiceNowConnection_StartsOutUsingUsernameAndPassword()
        {
            var connection = CreateDefaultServiceNowConnection();

            Assert.That(connection.AuthenticationMethodKey, Is.EqualTo(AuthenticationMethodKeys.ServiceNowBasic));
        }

        // AC2. The form is schema-driven, so this list IS the connection form the administrator
        // sees. ADR-116 puts the work item table here, at connection scope, because
        // ValidateConnection needs something concrete to probe read access against.
        [Test]
        public void TheServiceNowConnectionForm_AsksForAnInstanceAddressAUsernameAndAPassword()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(
                WorkTrackingSystems.ServiceNow, AuthenticationMethodKeys.ServiceNowBasic);

            Assert.That(method, Is.Not.Null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method.Options, Has.Count.EqualTo(3));

                var instanceUrl = method.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.InstanceUrl);
                Assert.That(instanceUrl.DisplayName, Is.EqualTo("ServiceNow Instance URL"));
                Assert.That(instanceUrl.IsSecret, Is.False);

                var username = method.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.Username);
                Assert.That(username.DisplayName, Is.EqualTo("Username"));
                Assert.That(username.IsSecret, Is.False);

                var password = method.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.Password);
                Assert.That(password.DisplayName, Is.EqualTo("Password"));
                Assert.That(password.IsSecret, Is.True);
            }
        }

        [Test]
        public void TheServiceNowConnectionForm_OffersExactlyOneWayToAuthenticate()
        {
            var methods = AuthenticationMethodSchema.GetMethodsForSystem(WorkTrackingSystems.ServiceNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(methods, Has.Count.EqualTo(1));
                Assert.That(methods.Single().DisplayName, Is.EqualTo("Username and Password"));
                Assert.That(methods.Single().IsPremium, Is.False,
                    "A whole work tracking system behind a premium gate would contradict every other connector " +
                    "and would suppress the adoption signal this epic exists to collect.");
            }
        }

        // ADR-116. The table is a connection option rather than something discovered: table and
        // field discovery both measured unavailable to a least-privilege account, so a wizard
        // that enumerates tables would work for an admin and show a customer an empty list.
        [Test]
        public void ANewServiceNowConnection_ComesPreFilledWithTheIncidentTable()
        {
            var connection = CreateDefaultServiceNowConnection();

            var table = connection.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.WorkItemTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(table.Value, Is.EqualTo("incident"));
                Assert.That(table.IsOptional, Is.True,
                    "ITSM-first, but an Agile Development 2.0 shop must be able to type their own table.");
                Assert.That(table.IsSecret, Is.False);
            }
        }

        [Test]
        public void ANewServiceNowConnection_KeepsThePasswordAsASecret()
        {
            var connection = CreateDefaultServiceNowConnection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Has.Count.EqualTo(4));

                var password = connection.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.Password);
                Assert.That(password.IsSecret, Is.True,
                    "The existing EncryptSecrets change tracker hook is what satisfies AC5, and it keys off this flag.");

                var instanceUrl = connection.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.InstanceUrl);
                Assert.That(instanceUrl.IsOptional, Is.False);

                var username = connection.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.Username);
                Assert.That(username.IsOptional, Is.False);
            }
        }

        private static WorkTrackingSystemConnection CreateDefaultServiceNowConnection()
        {
            var factory = new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>());

            return factory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.ServiceNow);
        }
    }
}
