using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// Rewriting every stored credential in an installation is something somebody has to be answerable for
    /// afterwards. What is written down is who asked, how many moved and how many could not be read - and
    /// nothing else, because a structured property is exactly how key material reaches every sink a log is
    /// shipped to while the sentence beside it still looks harmless.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class EncryptionRecordTests
    {
        private const string Actor = "an-administrator-subject";

        private static readonly EncryptionKey ActiveKey = new("k-2026-08-16-01", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private Mock<ILogger<EncryptionController>> logger = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new Mock<ILogger<EncryptionController>>();
        }

        [Test]
        public async Task ACompletedRotation_SaysWhoAskedHowManyMovedAndWhichKeyIsNowInForce()
        {
            await ControllerOver(AReportOf(moved: 47, unreadable: 0)).RotateKey(CancellationToken.None);

            var written = EverythingHandedToTheLoggingPipeline();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Has.Some.Contains("encryption.rotation.completed"));
                Assert.That(written, Has.One.Contains($"Actor={Actor}"));
                Assert.That(written, Has.One.Contains("MovedCount=47"));
                Assert.That(written, Has.One.Contains("UnreadableCount=0"));
                Assert.That(written, Has.One.Contains($"NewActiveKeyId={ActiveKey.Id}"));
            }
        }

        [Test]
        public async Task ACompletedMove_SaysTheSameThingAboutTheKeyAlreadyInForce()
        {
            await ControllerOver(AReportOf(moved: 3, unreadable: 1)).ReEncrypt(CancellationToken.None);

            var written = EverythingHandedToTheLoggingPipeline();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Has.Some.Contains("encryption.reencryption.completed"));
                Assert.That(written, Has.One.Contains("MovedCount=3"));
                Assert.That(written, Has.One.Contains("UnreadableCount=1"));
            }
        }

        [Test]
        public async Task NoStructuredProperty_CarriesKeyMaterialInAnyEncoding()
        {
            await ControllerOver(AReportOf(moved: 1, unreadable: 0)).RotateKey(CancellationToken.None);

            var renderings = new[]
            {
                Convert.ToBase64String(ActiveKey.Material.Span),
                Convert.ToHexString(ActiveKey.Material.Span),
            };

            foreach (var written in EverythingHandedToTheLoggingPipeline())
            {
                foreach (var rendering in renderings)
                {
                    Assert.That(written, Does.Not.Contain(rendering).IgnoreCase,
                        "the rendered sentence looks harmless while the property beside it carries the key");
                }
            }
        }

        [Test]
        public async Task ARefusedRotation_IsNotRecordedAsOne()
        {
            var refused = await ControllerOver(new AServiceThatRefusesToMint()).RotateKey(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Result, Is.InstanceOf<ConflictObjectResult>());
                Assert.That(EverythingHandedToTheLoggingPipeline(), Is.Empty,
                    "nothing was moved, so a record saying a rotation completed would be untrue");
            }
        }

        private EncryptionController ControllerOver(ISecretCustodyService custodyService)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(host => host.ContentRootPath).Returns(Path.GetTempPath());

            var controller = new EncryptionController(
                new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, ActiveKey)),
                configuration,
                environment.Object,
                custodyService,
                logger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Actor)], "test")),
                    },
                },
            };

            return controller;
        }

        private static AServiceThatReports AReportOf(int moved, int unreadable)
        {
            var secrets = Enumerable
                .Repeat(new StoredSecretRecord(1, "Contoso Board", "PersonalAccessToken", ActiveKey.Id, SecretState.Envelope, SecretMoveOutcome.Moved), moved)
                .Concat(Enumerable.Repeat(
                    new StoredSecretRecord(1, "Contoso Board", "ClientSecret", "k-lost", SecretState.Unreadable, SecretMoveOutcome.CouldNotBeRead), unreadable));

            return new AServiceThatReports(new SecretReadabilityReport(ActiveKey.Id, secrets));
        }

        private List<string> EverythingHandedToTheLoggingPipeline()
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

        private sealed class AServiceThatReports : ISecretCustodyService
        {
            private readonly SecretReadabilityReport report;

            public AServiceThatReports(SecretReadabilityReport report)
            {
                this.report = report;
            }

            public Task<SecretReadabilityReport> InspectAsync(CancellationToken cancellationToken = default) => Task.FromResult(report);

            public Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken cancellationToken = default) => Task.FromResult(report);

            public Task<SecretReadabilityReport> RotateAsync(CancellationToken cancellationToken = default) => Task.FromResult(report);
        }

        private sealed class AServiceThatRefusesToMint : ISecretCustodyService
        {
            public Task<SecretReadabilityReport> InspectAsync(CancellationToken cancellationToken = default) =>
                throw new MintingNotPermittedException(KeyCustody.SuppliedByConfiguration);

            public Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken cancellationToken = default) =>
                throw new MintingNotPermittedException(KeyCustody.SuppliedByConfiguration);

            public Task<SecretReadabilityReport> RotateAsync(CancellationToken cancellationToken = default) =>
                throw new MintingNotPermittedException(KeyCustody.SuppliedByConfiguration);
        }
    }
}
