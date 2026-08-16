using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    // Which key an instance runs on, and where it keeps it, is not part of the system information
    // response. That one asks only that the caller be signed in, and a viewer who opens Lighthouse inside
    // an embedded frame satisfies exactly that - so putting key state there would hand the security
    // posture of the installation to anyone who can reach the frame. Everything on this controller is
    // System Administrator only, which is why it is a controller of its own rather than a few more
    // properties on that response.
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    // Reading the state of this instance's encryption key and acting on it are one job, not two: an
    // administrator looks at which keys are held to decide whether to move the secrets, and looks again
    // afterwards to see that it worked. Splitting them would put the same System Administrator guard and the
    // same key ring behind two routes with two chances to drift apart.
#pragma warning disable S6960
    public sealed class EncryptionController : ControllerBase
#pragma warning restore S6960
    {
        private const string KeyStorePathSetting = "Encryption:KeyStorePath";

        private const string DataProtectionKeyStorePathSetting = "Lighthouse:DataProtection:KeyStorePath";

        private const string DatabaseProviderSetting = "Database:Provider";

        private const string DatabaseConnectionStringSetting = "Database:ConnectionString";

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly IConfiguration configuration;

        private readonly IWebHostEnvironment environment;

        private readonly ISecretCustodyService custodyService;

        // The check is handed a port with no method that writes, so it cannot move a secret even by
        // mistake. That guarantee is the shape of what this controller holds rather than something a
        // reviewer has to keep noticing, which is why it is a second dependency onto the same object.
        private readonly ISecretCustodyReader secretReader;

        private readonly IPublishedKeySecretCount publishedKeySecrets;

        private readonly IReferencedKeyIds referencedKeys;

        private readonly ILogger<EncryptionController> logger;

        public EncryptionController(
            IEncryptionKeyRingHolder keyRingHolder,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ISecretCustodyService custodyService,
            ISecretCustodyReader secretReader,
            IPublishedKeySecretCount publishedKeySecrets,
            IReferencedKeyIds referencedKeys,
            ILogger<EncryptionController> logger)
        {
            this.keyRingHolder = keyRingHolder ?? throw new ArgumentNullException(nameof(keyRingHolder));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.custodyService = custodyService ?? throw new ArgumentNullException(nameof(custodyService));
            this.secretReader = secretReader ?? throw new ArgumentNullException(nameof(secretReader));
            this.publishedKeySecrets = publishedKeySecrets ?? throw new ArgumentNullException(nameof(publishedKeySecrets));
            this.referencedKeys = referencedKeys ?? throw new ArgumentNullException(nameof(referencedKeys));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType<EncryptionStateDto>(StatusCodes.Status200OK)]
        public async Task<ActionResult<EncryptionStateDto>> GetEncryptionState(CancellationToken cancellationToken)
        {
            return Ok(new EncryptionStateDto(
                keyRingHolder.Current,
                WhereTheKeyIsKept().Directory,
                await publishedKeySecrets.CountAsync(cancellationToken),
                configuration.GetValue<bool>(EncryptionKeyRingBootstrapper.StartAnywaySettingKey),
                await referencedKeys.ReadAsync(cancellationToken),
                WhatTheKeyArrivedIn()));
        }

        // Nothing is recorded about a check, and that is deliberate: nothing changed, and a running record
        // of who read which Connections hold unreadable credentials is the one thing in this feature that
        // would accumulate somewhere nobody is guarding.
        [HttpGet("secrets")]
        [ProducesResponseType<SecretReadabilityReportDto>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SecretReadabilityReportDto>> CheckSecrets(CancellationToken cancellationToken)
        {
            return Ok(new SecretReadabilityReportDto(await secretReader.InspectAsync(cancellationToken)));
        }

        // The refusal is part of the contract rather than a convention of the screen. A rotation started
        // with the screen bypassed entirely has to be turned down for the same reason and in the same words,
        // because the reason is where the key came from and not what was drawn.
        [HttpPost("rotate")]
        [ProducesResponseType<SecretReadabilityReportDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SecretReadabilityReportDto>> RotateKey(CancellationToken cancellationToken)
        {
            SecretReadabilityReport report;

            try
            {
                report = await custodyService.RotateAsync(cancellationToken);
            }
            catch (MintingNotPermittedException refusal)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "This instance cannot make a new encryption key",
                    Detail = refusal.Message,
                    Status = StatusCodes.Status409Conflict,
                });
            }

            logger.LogInformation(
                "encryption.rotation.completed {Actor} {MovedCount} {UnreadableCount} {NewActiveKeyId}",
                WhoAskedForIt(),
                report.MovedCount,
                report.UnreadableCount,
                report.ActiveKeyId);

            return Ok(new SecretReadabilityReportDto(report));
        }

        [HttpPost("reencrypt")]
        [ProducesResponseType<SecretReadabilityReportDto>(StatusCodes.Status200OK)]
        public async Task<ActionResult<SecretReadabilityReportDto>> ReEncrypt(CancellationToken cancellationToken)
        {
            var report = await custodyService.ReEncryptAsync(cancellationToken);

            logger.LogInformation(
                "encryption.reencryption.completed {Actor} {MovedCount} {UnreadableCount} {NewActiveKeyId}",
                WhoAskedForIt(),
                report.MovedCount,
                report.UnreadableCount,
                report.ActiveKeyId);

            return Ok(new SecretReadabilityReportDto(report));
        }

        // An action that rewrites every stored credential in the installation is one somebody has to be
        // answerable for afterwards, and the request is the only place that knows who that was.
        private string WhoAskedForIt()
        {
            return User.FindFirst("sub")?.Value
                ?? User.FindFirst("oid")?.Value
                ?? User.Identity?.Name
                ?? "unknown";
        }

        // The ring decides whether anybody supplied this key at all, and only then is configuration asked
        // which setting they used. Asking configuration on its own would name a setting on an instance
        // running on a key it made for itself - a value can sit in a setting without having won the
        // resolution - and that sends an operator to edit something that is not in force.
        //
        // Within a supplied key it is asked in the order the resolution asks it, configuration before a
        // mounted file, so an instance with both set is told about the one that answered.
        private string? WhatTheKeyArrivedIn()
        {
            if (keyRingHolder.Current.Custody is not (KeyCustody.SuppliedByConfiguration or KeyCustody.SuppliedByExternalSecret))
            {
                return null;
            }

            var configured = ConfiguredKeyRingSource.SettingThatAnswered(
                configuration[ConfiguredKeyRingSource.RingSettingKey],
                configuration[ConfiguredKeyRingSource.SingleKeySettingKey],
                configuration[ConfiguredKeyRingSource.RetiredSingleKeySettingKey]);

            if (configured is not null)
            {
                return configured;
            }

            return string.IsNullOrWhiteSpace(configuration[MountedFileKeyRingSource.PathSettingKey])
                ? null
                : ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(MountedFileKeyRingSource.PathSettingKey);
        }

        // Asked the same way startup asked it, off the same settings, so the path an operator is shown
        // here is the path the key was actually resolved under rather than a second opinion about it.
        private KeyStoreLocation WhereTheKeyIsKept()
        {
            return KeyStoreResolver.Resolve(
                configuration[KeyStorePathSetting],
                configuration[DataProtectionKeyStorePathSetting],
                configuration[DatabaseProviderSetting],
                configuration[DatabaseConnectionStringSetting],
                environment.ContentRootPath);
        }
    }
}
