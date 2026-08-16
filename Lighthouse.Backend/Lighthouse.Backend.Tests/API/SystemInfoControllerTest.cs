using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class SystemInfoControllerTest
    {
        private Mock<ISystemInfoService> systemInfoServiceMock;
        private Mock<IRefreshLogService> refreshLogServiceMock;
        private Mock<IRbacAdministrationService> rbacMock;

        private static readonly string[] OneEmergencyAdmin = ["alice@example.com"];

        private static readonly string[] TwoEmergencyAdmins = ["alice@example.com", "bob@example.com"];

        private static readonly string[] WhatOnlyAnAdministratorMaySee =
            [nameof(SystemInfo.EmergencyAdminSubjects), nameof(SystemInfo.Encryption)];

        [SetUp]
        public void Setup()
        {
            systemInfoServiceMock = new Mock<ISystemInfoService>();
            refreshLogServiceMock = new Mock<IRefreshLogService>();
            rbacMock = new Mock<IRbacAdministrationService>();

            AnsweringAsA(systemAdministrator: true);
        }

        private void AnsweringAsA(bool systemAdministrator)
        {
            rbacMock
                .Setup(rbac => rbac.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.SystemAdmin,
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(systemAdministrator);
        }

        [Test]
        public void SystemInfoController_HasAuthorizeAttribute()
        {
            var attribute = typeof(SystemInfoController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public async Task GetSystemInfo_ReturnsSystemInfoFromService()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Linux 5.15.0",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 12345,
                DatabaseProvider: "sqlite",
                DatabaseConnection: "/data/lighthouse.db",
                LogPath: "/var/log/lighthouse",
                IsAuthenticationEnabled: false,
                IsAuthorizationEnabled: false,
                EmergencyAdminSubjects: Array.Empty<string>(),
                BaseUrl: string.Empty,
                InstallTimestamp: null);

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = await subject.GetSystemInfo(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));

                var actual = okResult.Value as SystemInfo;
                Assert.That(actual, Is.EqualTo(expectedSystemInfo));
            }
        }

        [Test]
        public async Task GetSystemInfo_WithoutLogPath_ReturnsSystemInfoWithNullLogPath()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Windows 11",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 99,
                DatabaseProvider: "postgresql",
                DatabaseConnection: "Host=myhost;Port=5432;Database=mydb",
                LogPath: null,
                IsAuthenticationEnabled: false,
                IsAuthorizationEnabled: false,
                EmergencyAdminSubjects: Array.Empty<string>(),
                BaseUrl: string.Empty,
                InstallTimestamp: null);

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = await subject.GetSystemInfo(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                var actual = okResult!.Value as SystemInfo;
                Assert.That(actual!.LogPath, Is.Null);
            }
        }

        [Test]
        public async Task GetSystemInfo_PropagatesBaseUrlFromService()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Linux 5.15.0",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 12345,
                DatabaseProvider: "sqlite",
                DatabaseConnection: "/data/lighthouse.db",
                LogPath: "/var/log/lighthouse",
                IsAuthenticationEnabled: false,
                IsAuthorizationEnabled: false,
                EmergencyAdminSubjects: Array.Empty<string>(),
                BaseUrl: "https://lighthouse.example.com",
                InstallTimestamp: null);

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = await subject.GetSystemInfo(CancellationToken.None);

            var okResult = response.Result as OkObjectResult;
            var actual = okResult!.Value as SystemInfo;
            Assert.That(actual!.BaseUrl, Is.EqualTo("https://lighthouse.example.com"));
        }

        [Test]
        public async Task GetSystemInfo_PropagatesAuthPostureFieldsFromService()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Linux 5.15.0",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 12345,
                DatabaseProvider: "sqlite",
                DatabaseConnection: "/data/lighthouse.db",
                LogPath: "/var/log/lighthouse",
                IsAuthenticationEnabled: true,
                IsAuthorizationEnabled: true,
                EmergencyAdminSubjects: ["alice@example.com", "bob@example.com"],
                BaseUrl: string.Empty,
                InstallTimestamp: null);

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = await subject.GetSystemInfo(CancellationToken.None);

            var okResult = response.Result as OkObjectResult;
            var actual = okResult!.Value as SystemInfo;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual!.IsAuthenticationEnabled, Is.True);
                Assert.That(actual.IsAuthorizationEnabled, Is.True);
                Assert.That(actual.EmergencyAdminSubjects, Is.EqualTo(["alice@example.com", "bob@example.com"]));
            }
        }

        /// <summary>
        /// This response answers before anybody is authorised, because the shell needs the version and
        /// the authentication posture to render — and a viewer who opens Lighthouse inside an embedded
        /// frame satisfies "signed in". The emergency administrators are not a category: they are the
        /// names of real people who can administer this installation.
        /// </summary>
        [Test]
        public async Task GetSystemInfo_ACallerWhoIsOnlySignedIn_IsToldNothingOnlyAnAdministratorMaySee()
        {
            AnsweringAsA(systemAdministrator: false);
            systemInfoServiceMock.Setup(service => service.GetSystemInfo()).Returns(AnInstanceWith(
                emergencyAdmins: TwoEmergencyAdmins,
                encryption: "instance · /app/data/keys"));

            var answered = await Answered();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.EmergencyAdminSubjects, Is.Empty);
                Assert.That(answered.Encryption, Is.Null);
            }
        }

        [Test]
        public async Task GetSystemInfo_ASystemAdministrator_IsToldBoth()
        {
            systemInfoServiceMock.Setup(service => service.GetSystemInfo()).Returns(AnInstanceWith(
                emergencyAdmins: OneEmergencyAdmin,
                encryption: "instance · /app/data/keys"));

            var answered = await Answered();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.EmergencyAdminSubjects, Is.EqualTo(OneEmergencyAdmin).AsCollection);
                Assert.That(answered.Encryption, Is.EqualTo("instance · /app/data/keys"));
            }
        }

        // Everything the application shell needs to draw itself, which is why this route asks only that
        // the caller be somebody. Withholding any of it would break the page for every viewer.
        [Test]
        public async Task GetSystemInfo_ACallerWhoIsOnlySignedIn_IsStillToldEverythingTheShellNeeds()
        {
            AnsweringAsA(systemAdministrator: false);
            var whole = AnInstanceWith(emergencyAdmins: OneEmergencyAdmin, encryption: "configured · /app/keys");
            systemInfoServiceMock.Setup(service => service.GetSystemInfo()).Returns(whole);

            var answered = await Answered();

            Assert.That(
                answered,
                Is.EqualTo(whole with { EmergencyAdminSubjects = [], Encryption = null }),
                "exactly two fields are withheld; anything else going missing breaks a page every viewer loads");
        }

        /// <summary>
        /// The reason the narrowing lives on the record rather than at the call site. A field added later
        /// without a decision about who may see it would otherwise ship unguarded — which is exactly how
        /// the emergency administrators came to be on an unguarded response in the first place.
        /// </summary>
        [Test]
        public void WhatOnlyAnAdministratorMaySee_IsNamedInOnePlace()
        {
            var whole = AnInstanceWith(emergencyAdmins: OneEmergencyAdmin, encryption: "instance · /app/keys");

            var narrowed = whole.WithoutWhatOnlyAnAdministratorMaySee();

            var changed = typeof(SystemInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => !Equals(property.GetValue(whole), property.GetValue(narrowed)))
                .Select(property => property.Name)
                .ToList();

            Assert.That(
                changed,
                Is.EquivalentTo(WhatOnlyAnAdministratorMaySee),
                "a field added to this response later is withheld by the same sentence or by nothing at all");
        }

        // The startup line is read from a console by whoever runs the process; this page is read by an
        // administrator months later, and is the only one of the two a standalone operator ever sees.
        // Two copies of the same sentence that agree today are two copies that disagree later, so there
        // is one sentence.
        [TestCase(KeyCustody.GeneratedForThisInstance)]
        [TestCase(KeyCustody.SuppliedByConfiguration)]
        [TestCase(KeyCustody.SuppliedByExternalSecret)]
        [TestCase(KeyCustody.NoDurableStore)]
        public async Task GetSystemInfo_TheCustodyItReports_ReadsTheSameAsTheStartupLine(KeyCustody custody)
        {
            var keyStore = new KeyStoreLocation("/app/data/keys", KeyStoreCase.BesideTheDatabaseFile);
            var described = WhoseKeyThisIs.AndWhereItIsKept(custody, keyStore.Directory);

            systemInfoServiceMock.Setup(service => service.GetSystemInfo())
                .Returns(AnInstanceWith(OneEmergencyAdmin, described));

            var bannerLine = StartupBanner.BuildEncryptionCustodyLines(
                new EncryptionKeyRing(custody, new EncryptionKey("k-2026-08-16-01", new byte[EncryptionKey.MaterialLength])),
                keyStore,
                keyCameFromTheRetiredSetting: false)[0];

            var answered = await Answered();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Encryption, Is.EqualTo(described));
                Assert.That(bannerLine, Does.Contain(described),
                    "the banner and this page render the same sentence, so neither can be changed without the other");
            }
        }

        [Test]
        public async Task GetSystemInfo_WhatItSaysAboutTheKey_IsNeitherTheKeyNorItsName()
        {
            var described = WhoseKeyThisIs.AndWhereItIsKept(
                KeyCustody.GeneratedForThisInstance, "/app/data/keys");

            systemInfoServiceMock.Setup(service => service.GetSystemInfo())
                .Returns(AnInstanceWith(OneEmergencyAdmin, described));

            var answered = await Answered();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Encryption, Does.Not.Contain("k-"),
                    "a key id is not on this page: the moment it is worth having is a start that stopped, and the refusal names it there");
                Assert.That(answered.Encryption, Does.Contain("instance"));
                Assert.That(answered.Encryption, Does.Contain("/app/data/keys"),
                    "the directory an operator has to back up alongside the database");
            }
        }

        /// <summary>
        /// Withholding is the right answer to a question that could not be asked. It is also the answer a
        /// genuine fault in the permission check would produce, and an authorisation bug whose only
        /// symptom is a missing row on a settings page is one nobody ever reports.
        /// </summary>
        [Test]
        public async Task GetSystemInfo_TheAdministratorCheckCouldNotBeMade_WithholdsAndSaysSo()
        {
            var logger = new Mock<ILogger<SystemInfoController>>();

            rbacMock
                .Setup(rbac => rbac.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.SystemAdmin,
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("no database to ask"));

            systemInfoServiceMock.Setup(service => service.GetSystemInfo())
                .Returns(AnInstanceWith(OneEmergencyAdmin, "instance · /app/data/keys"));

            var response = await CreateSubject(logger).GetSystemInfo(CancellationToken.None);
            var answered = (SystemInfo)((OkObjectResult)response.Result!).Value!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Encryption, Is.Null,
                    "a question that cannot be answered is not a yes");
                Assert.That(answered.EmergencyAdminSubjects, Is.Empty);
                Assert.That(answered.Os, Is.Not.Empty,
                    "and the request still succeeds - this is what the application shell fetches before it can draw anything at all");

                logger.Verify(
                    written => written.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                    Times.Once,
                    "the safe behaviour must not also be a silent one");
            }
        }

        private async Task<SystemInfo> Answered()
        {
            var response = await CreateSubject().GetSystemInfo(CancellationToken.None);

            return (SystemInfo)((OkObjectResult)response.Result!).Value!;
        }

        private static SystemInfo AnInstanceWith(IReadOnlyList<string> emergencyAdmins, string? encryption)
        {
            return new SystemInfo(
                Os: "Linux 5.15.0",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 12345,
                DatabaseProvider: "sqlite",
                DatabaseConnection: "/data/lighthouse.db",
                LogPath: "/var/log/lighthouse",
                IsAuthenticationEnabled: true,
                IsAuthorizationEnabled: true,
                EmergencyAdminSubjects: emergencyAdmins,
                BaseUrl: "https://lighthouse.example.com",
                InstallTimestamp: null,
                Encryption: encryption);
        }

        private SystemInfoController CreateSubject(Mock<ILogger<SystemInfoController>>? logger = null)
        {
            return new SystemInfoController(
                systemInfoServiceMock.Object,
                refreshLogServiceMock.Object,
                rbacMock.Object,
                logger?.Object ?? NullLogger<SystemInfoController>.Instance)
            {
                // Somebody who signed in. A caller who did not is settled without asking anybody, so a
                // controller built without a principal would answer every one of these as a stranger.
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "somebody")], "test")),
                    },
                },
            };
        }

        [Test]
        public void GetRefreshLog_ReturnsLogsFromService()
        {
            var logs = new List<RefreshLog>
            {
                new RefreshLog { Id = 1, Type = RefreshType.Team, EntityId = 1, EntityName = "Team A", ItemCount = 10, DurationMs = 500, ExecutedAt = DateTime.UtcNow, Success = true }
            };
            refreshLogServiceMock.Setup(x => x.GetRefreshLogs()).Returns(logs);

            var subject = CreateSubject();

            var response = subject.GetRefreshLog();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(logs));
            }
        }
    }
}
