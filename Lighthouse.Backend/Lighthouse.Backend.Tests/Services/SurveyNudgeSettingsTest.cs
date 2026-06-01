using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services
{
    public class SurveyNudgeSettingsTest
    {
        private static readonly DateTimeOffset FirstRunInstant = new(2026, 6, 1, 9, 30, 0, TimeSpan.Zero);

        private Mock<IRepository<AppSetting>> repositoryMock;
        private List<AppSetting> store;

        [SetUp]
        public void Setup()
        {
            store = [];
            repositoryMock = new Mock<IRepository<AppSetting>>();

            repositoryMock
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<AppSetting, bool>>()))
                .Returns<Func<AppSetting, bool>>(predicate => store.FirstOrDefault(predicate));

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<AppSetting>()))
                .Callback<AppSetting>(setting => store.Add(setting));
        }

        [Test]
        public async Task EnsureInstallTimestamp_FirstRun_StoresCurrentUtcInstant()
        {
            var timeProvider = new FakeTimeProvider(FirstRunInstant);
            var service = CreateService(timeProvider);

            await service.EnsureInstallTimestamp();

            Assert.That(service.GetInstallTimestamp(), Is.EqualTo(FirstRunInstant));
        }

        [Test]
        public async Task EnsureInstallTimestamp_CalledTwice_IsWriteOnceAndKeepsFirstInstant()
        {
            var timeProvider = new FakeTimeProvider(FirstRunInstant);
            var service = CreateService(timeProvider);

            await service.EnsureInstallTimestamp();
            timeProvider.Advance(TimeSpan.FromDays(14));
            await service.EnsureInstallTimestamp();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.GetInstallTimestamp(), Is.EqualTo(FirstRunInstant));
                repositoryMock.Verify(repository => repository.Add(It.IsAny<AppSetting>()), Times.Once);
            }
        }

        [Test]
        public async Task SystemInfo_AfterInstallTimestampWritten_ExposesRoundTrippedUtcInstant()
        {
            var timeProvider = new FakeTimeProvider(FirstRunInstant);
            var appSettingService = CreateService(timeProvider);
            await appSettingService.EnsureInstallTimestamp();

            var result = CreateSystemInfoService(appSettingService).GetSystemInfo();

            Assert.That(
                DateTimeOffset.Parse(result.InstallTimestamp!, null, System.Globalization.DateTimeStyles.RoundtripKind),
                Is.EqualTo(FirstRunInstant));
        }

        [Test]
        public void SystemInfo_InstallTimestampAbsent_ExposesNullForFailClosed()
        {
            var appSettingService = CreateService(new FakeTimeProvider(FirstRunInstant));

            var result = CreateSystemInfoService(appSettingService).GetSystemInfo();

            Assert.That(result.InstallTimestamp, Is.Null);
        }

        [Test]
        public void GetInstallTimestamp_StoredValueUnparseable_ReturnsNullForFailClosed()
        {
            store.Add(new AppSetting { Key = AppSettingKeys.InstallTimestamp, Value = "not-a-timestamp" });
            var service = CreateService(new FakeTimeProvider(FirstRunInstant));

            Assert.That(service.GetInstallTimestamp(), Is.Null);
        }

        private AppSettingService CreateService(TimeProvider timeProvider)
        {
            return new AppSettingService(repositoryMock.Object, timeProvider);
        }

        private SystemInfoService CreateSystemInfoService(IAppSettingService appSettingService)
        {
            var configuration = new Mock<IConfiguration>();
            var emptySection = new Mock<IConfigurationSection>();
            emptySection.Setup(section => section.Value).Returns((string?)null);
            configuration.Setup(config => config.GetSection(It.IsAny<string>())).Returns(emptySection.Object);

            var logConfiguration = new Mock<ILogConfiguration>();
            var serviceConfig = new Mock<IServiceConfig>();
            serviceConfig.Setup(config => config.BaseUrl).Returns(string.Empty);

            var services = new ServiceCollection();
            services.AddScoped(_ => appSettingService);
            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            return new SystemInfoService(configuration.Object, logConfiguration.Object, serviceConfig.Object, scopeFactory, NullLogger<SystemInfoService>.Instance);
        }
    }
}
