using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
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
    public sealed class EncryptionController : ControllerBase
    {
        private const string KeyStorePathSetting = "Encryption:KeyStorePath";

        private const string DataProtectionKeyStorePathSetting = "Lighthouse:DataProtection:KeyStorePath";

        private const string DatabaseProviderSetting = "Database:Provider";

        private const string DatabaseConnectionStringSetting = "Database:ConnectionString";

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly IConfiguration configuration;

        private readonly IWebHostEnvironment environment;

        public EncryptionController(
            IEncryptionKeyRingHolder keyRingHolder,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            this.keyRingHolder = keyRingHolder ?? throw new ArgumentNullException(nameof(keyRingHolder));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [HttpGet]
        [ProducesResponseType<EncryptionStateDto>(StatusCodes.Status200OK)]
        public ActionResult<EncryptionStateDto> GetEncryptionState()
        {
            return Ok(new EncryptionStateDto(keyRingHolder.Current, WhereTheKeyIsKept().Directory));
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
