using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private const string EncryptionNamespacePrefix = "Lighthouse.Backend.Services.Implementation.Encryption.";

        private const string LoggingNamespace = "Microsoft.Extensions.Logging";

        private const string StoredCredential = "a stored credential";

        private const string KeyOnTheRing = "key-on-the-ring";

        private const string KeyNotOnTheRing = "key-not-on-the-ring";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private static readonly byte[] MaterialOnTheRing = RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength);

        private static readonly byte[] MaterialOffTheRing = RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength);

        private static readonly char[] SettingValueSeparators = [',', ';', ' '];

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
            "encryption",
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

        /// <summary>
        /// Resolving a key deliberately puts nothing back into configuration, and this is what makes that
        /// structural rather than a habit reviewers have to remember. Every value in configuration is
        /// readable from its debug view and from anything that enumerates a section, so a key written back
        /// would be one support request away from being pasted into an issue.
        /// </summary>
        [Test]
        public void NoValueLeftInConfigurationAfterTheKeyIsResolved_IsAKeyOnTheRing()
        {
            var keyStore = Directory.CreateTempSubdirectory("SecretCustodySeam_");

            try
            {
                var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

                Backend.Program.EnsureEncryptionKeyRing(
                    builder, new KeyStoreLocation(keyStore.FullName, KeyStoreCase.ExplicitKeyStorePath));

                Assert.That(
                    KeyMaterialReadableFromConfiguration(builder),
                    Is.Empty,
                    "A key the instance resolved is readable straight out of its own settings. Nothing puts " +
                    "it there except code that writes what it resolved back, and there is no way to take it " +
                    "out again once something has read the debug view.");
            }
            finally
            {
                keyStore.Delete(recursive: true);
            }
        }

        /// <summary>
        /// The cheapest way to guarantee that no log line carries key material is for the code that handles
        /// key material to have no way of writing one. Every source, the store and the bootstrapper raise
        /// what goes wrong instead, so the sentence an operator reads is written once, where the caller
        /// decides what to do about it.
        ///
        /// Two types have to both handle keys and say something, and both live outside this namespace for
        /// that reason: CryptoService, which is pinned by the test below, and KeyRingFileWatcher, which runs
        /// on a timer and so has no caller to hand a sentence to - what it writes is pinned by its own tests.
        /// </summary>
        [Test]
        public void NothingThatResolvesOrKeepsAKey_CanWriteToALogAtAll()
        {
            var ableToLog = Architecture.Types
                .Where(type => type.FullName.StartsWith(EncryptionNamespacePrefix, StringComparison.Ordinal))
                .Where(type => type.Dependencies.Any(
                    dependency => dependency.Target.FullName.StartsWith(LoggingNamespace, StringComparison.Ordinal)))
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(ableToLog, Is.Empty,
                "A type that handles key material can now write to a log, so whether a key ends up in one is " +
                "decided line by line from here on. Raise what went wrong instead and let the caller say it. " +
                "Found: " + string.Join(", ", ableToLog));
        }

        [Test]
        public void TheOneTypeThatBothHoldsKeysAndLogs_PutsNoKeyMaterialInAnyStructuredProperty()
        {
            var logger = new Mock<ILogger<CryptoService>>();
            var ring = new EncryptionKeyRing(new EncryptionKey(KeyOnTheRing, MaterialOnTheRing));

            new CryptoService(new EncryptionKeyRingHolder(ring), logger.Object)
                .Read(SecretEnvelope.Protect(StoredCredential, KeyNotOnTheRing, MaterialOffTheRing).Format());

            var logged = EverythingHandedToTheLoggingPipeline(logger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logged, Is.Not.Empty,
                    "Nothing was logged at all, so this test would pass no matter what the log carried. A " +
                    "secret naming a key the ring does not hold is what makes this type write a line.");
                Assert.That(logged.Where(HoldsKeyMaterial).ToList(), Is.Empty,
                    "Key material reached the logging pipeline. A structured property is the way it gets " +
                    "there unnoticed: the rendered sentence looks harmless while the property beside it " +
                    "carries the key into every sink the log is shipped to.");
            }
        }

        private static bool HoldsKeyMaterial(string logged)
        {
            return EveryWayKeyMaterialCouldBeWrittenDown().Exists(
                rendering => logged.Contains(rendering, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> EveryWayKeyMaterialCouldBeWrittenDown()
        {
            return
            [
                Convert.ToBase64String(MaterialOnTheRing),
                Convert.ToHexString(MaterialOnTheRing),
                Convert.ToBase64String(MaterialOffTheRing),
                Convert.ToHexString(MaterialOffTheRing),
            ];
        }

        // The rendered line and each structured property separately: key material in a property would never
        // show up in a test that only read the sentence.
        private static List<string> EverythingHandedToTheLoggingPipeline(Mock<ILogger<CryptoService>> logger)
        {
            return [.. logger.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .SelectMany(invocation => Rendered(invocation.Arguments[2]))];
        }

        private static List<string> Rendered(object? state)
        {
            var written = new List<string> { state?.ToString() ?? string.Empty };

            if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
            {
                written.AddRange(properties.Select(property => $"{property.Key}={property.Value}"));
            }

            return written;
        }

        // Every setting is broken back into the pieces a key ring is spelled in, so a key hidden inside a
        // longer value is found too rather than only one stored on its own.
        private static List<string> KeyMaterialReadableFromConfiguration(WebApplicationBuilder builder)
        {
            var ringMaterial = KeysOn(RingResolvedInto(builder));

            return [.. builder.Configuration.AsEnumerable()
                .Select(setting => setting.Value)
                .OfType<string>()
                .SelectMany(value => value.Split(SettingValueSeparators, StringSplitOptions.RemoveEmptyEntries))
                .Where(piece => ringMaterial.Contains(piece.Trim(), StringComparer.Ordinal))];
        }

        private static List<string> KeysOn(EncryptionKeyRing ring)
        {
            return [.. ring.RetiredKeys
                .Prepend(ring.ActiveKey)
                .Select(key => Convert.ToBase64String(key.Material.Span))];
        }

        private static EncryptionKeyRing RingResolvedInto(WebApplicationBuilder builder)
        {
            var holder = (IEncryptionKeyRingHolder)builder.Services
                .Single(descriptor => descriptor.ServiceType == typeof(IEncryptionKeyRingHolder))
                .ImplementationInstance!;

            return holder.Current;
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
        ///
        /// Read off the type rather than off a serialised example. An example only shows the fields whose
        /// values it happened to set, and a field that is left out of the response when it is empty is
        /// invisible in one - which is exactly how the encryption line, the field this rule exists for, sat
        /// outside the compared set from the day it was added.
        /// </summary>
        [Test]
        public void SystemInfo_DisclosesExactlyThisPropertySetAndNothingAboutKeys()
        {
            var declared = EveryPropertySystemInfoDeclares();

            Assert.That(declared, Is.EquivalentTo(EverythingSystemInfoDiscloses),
                "The system information endpoint has grown or lost a field. Anything added here is readable by " +
                "every signed-in caller, so a field naming an encryption key, its state or its origin does not " +
                "belong. If the new field is genuinely safe to publish, add it to this list deliberately. " +
                $"Declared: {string.Join(", ", declared)}");
        }

        /// <summary>
        /// The rule above is only worth anything if it sees a field that carries no value. Two of the fields
        /// on this response are written that way, and one of them is the encryption line.
        /// </summary>
        [Test]
        public void SystemInfo_AFieldLeftOffTheResponseWhenEmpty_IsStillInsideTheRule()
        {
            var nothingIsSetOnIt = new SystemInfo(
                string.Empty, string.Empty, string.Empty, 0, string.Empty, null, null, false, false, [], string.Empty, null);

            var serialised = JsonSerializer.Serialize(nothingIsSetOnIt, JsonSerializerOptions.Web);
            using var document = JsonDocument.Parse(serialised);
            var onTheWire = document.RootElement.EnumerateObject().Select(property => property.Name).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(onTheWire, Does.Not.Contain("encryption"),
                    "This response no longer leaves the encryption line off when it is empty, so the example " +
                    "this rule used to be written against would now see it and the regression is no longer " +
                    "expressible. Rewrite this test rather than deleting it.");

                Assert.That(EveryPropertySystemInfoDeclares(), Contains.Item("encryption"),
                    "The rule is reading a serialised example again. A field omitted when empty is invisible " +
                    "in one, which is how key state got onto this response without the rule noticing.");
            }
        }

        // The name each property answers to on the wire, so the compared set is the set a caller receives
        // rather than the set the C# happens to spell.
        private static List<string> EveryPropertySystemInfoDeclares()
        {
            return [.. typeof(SystemInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always)
                .Select(NameOnTheWire)
                .OrderBy(name => name, StringComparer.Ordinal)];
        }

        private static string NameOnTheWire(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        }
    }
}
