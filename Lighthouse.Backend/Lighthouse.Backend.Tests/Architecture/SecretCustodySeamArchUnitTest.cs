using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using System.Text.Json;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class SecretCustodySeamArchUnitTest
    {
        private const string CryptoServiceFullName = "Lighthouse.Backend.Services.Implementation.CryptoService";

        private const string UnreadableSecretExceptionFullName = "Lighthouse.Backend.Services.Implementation.Encryption.UnreadableSecretException";

        private const string SecretStateFullName = "Lighthouse.Backend.Models.Encryption.SecretState";

        private const string AuthStrategyNamespacePattern = @"^Lighthouse\.Backend\.Services\.Implementation\.WorkTrackingConnectors\.Auth($|\..*)";

        private const string ConfigurationNamespace = "Microsoft.Extensions.Configuration.";

        private const string PersistenceNamespace = "Lighthouse.Backend.Data.";

        private const string HttpNamespace = "System.Net.Http.";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private static readonly string[] EverythingSystemInfoDiscloses =
        [
            "os",
            "runtime",
            "architecture",
            "processId",
            "databaseProvider",
            "databaseConnection",
            "logPath",
            "authenticationEnabled",
            "authorizationEnabled",
            "emergencyAdminSubjects",
            "baseUrl",
            "installTimestamp",
        ];

        [Test]
        public void TheCryptoService_NeverReadsSettingsToDecideWhichKeyToUse()
        {
            var settings = WhatTheCryptoServiceDependsOn()
                .Where(target => target.StartsWith(ConfigurationNamespace, StringComparison.Ordinal))
                .ToList();

            Assert.That(settings, Is.Empty,
                "A key read straight out of configuration is how every installation ended up sharing one " +
                "published default key while an operator who had set their own believed it had taken effect. " +
                "The key ring is the only thing that decides which key is in force; the moment this class can " +
                "see settings, a second answer to that question exists and the two can disagree silently. " +
                "Found: " + string.Join(", ", settings));
        }

        [Test]
        public void NoAuthenticationStrategy_KnowsThatCryptographyExists()
        {
            Types().That().ResideInNamespaceMatching(AuthStrategyNamespacePattern)
                .Should().NotDependOnAny(Types().That().HaveFullName(UnreadableSecretExceptionFullName)
                    .Or().HaveFullName(SecretStateFullName))
                .Because(
                    "An auth strategy turns a credential into a header and knows nothing else. If one of them " +
                    "starts handling an unreadable secret, it has to decide what to send instead - and sending " +
                    "anything at all is what made a wrong encryption key look like a work tracking system " +
                    "rejecting an expired token. The failure has to reach the caller, not be absorbed here.")
                .Check(Architecture);
        }

        [Test]
        public void TheCryptoService_TouchesNoDatabaseAndNoNetwork()
        {
            var reachesOut = WhatTheCryptoServiceDependsOn()
                .Where(target => target.StartsWith(PersistenceNamespace, StringComparison.Ordinal)
                    || target.StartsWith(HttpNamespace, StringComparison.Ordinal)
                    || target.Contains("Repository", StringComparison.Ordinal))
                .ToList();

            Assert.That(reachesOut, Is.Empty,
                "Encrypting and decrypting are decisions about the bytes in hand. A lookup or a call out would " +
                "make the answer depend on something none of the rules are written in terms of, and would make " +
                "every test of those rules need a database or a transport stub to run at all. Found: " +
                string.Join(", ", reachesOut));
        }

        // Read off the dependency graph rather than asked as a NotDependOnAny rule, because only the
        // Lighthouse assembly is loaded: a rule phrased against a type from another assembly selects nothing
        // to forbid and passes no matter what the code does. The graph itself does record those targets.
        private static List<string> WhatTheCryptoServiceDependsOn()
        {
            var cryptoService = Architecture.Types.SingleOrDefault(type => type.FullName == CryptoServiceFullName);

            Assert.That(cryptoService, Is.Not.Null,
                $"{CryptoServiceFullName} was renamed or moved, so the rules that name it have quietly stopped guarding anything.");

            return cryptoService!.Dependencies
                .Select(dependency => dependency.Target.FullName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// The endpoint is reachable by anyone signed in, including a viewer who arrives through an embedded
        /// frame, so the safe property set is the one that is already public knowledge. This asserts the whole
        /// set rather than the absence of a key field, because the way key state would arrive is somebody
        /// adding a convenient "which key is active" line for support - and a test that lists only what is
        /// forbidden cannot see a field nobody thought to forbid.
        /// </summary>
        [Test]
        public void SystemInfo_DisclosesExactlyThisPropertySetAndNothingAboutKeys()
        {
            var systemInfo = new SystemInfo(
                "linux",
                "net10.0",
                "x64",
                1234,
                "sqlite",
                "Data Source=lighthouse.db",
                "/logs/lighthouse.log",
                true,
                true,
                [],
                "https://lighthouse.example",
                "2026-01-01T00:00:00Z");

            var serialised = JsonSerializer.Serialize(systemInfo, JsonSerializerOptions.Web);

            using var document = JsonDocument.Parse(serialised);
            var propertyNames = document.RootElement.EnumerateObject().Select(property => property.Name).ToList();

            Assert.That(propertyNames, Is.EquivalentTo(EverythingSystemInfoDiscloses),
                "The system information endpoint has grown or lost a field. Anything added here is readable by " +
                "every signed-in caller, so a field naming an encryption key, its state or its origin does not " +
                "belong. If the new field is genuinely safe to publish, add it to this list deliberately. " +
                $"Serialised response was: {serialised}");
        }
    }
}
