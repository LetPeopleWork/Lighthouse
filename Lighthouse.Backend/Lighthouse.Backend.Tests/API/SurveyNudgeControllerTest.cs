using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class SurveyNudgeControllerTest
    {
        private static readonly DateTimeOffset NextEligibleInstant = new(2026, 12, 1, 9, 30, 0, TimeSpan.Zero);

        private Mock<IAppSettingService> appSettingServiceMock;

        [SetUp]
        public void Setup()
        {
            appSettingServiceMock = new Mock<IAppSettingService>();
        }

        [Test]
        public void SurveyNudgeController_IsAuthenticatedButNotRbacGuarded()
        {
            var authorizeAttribute = typeof(SurveyNudgeController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            var hasRbacGuard = typeof(SurveyNudgeController)
                .GetCustomAttributes(inherit: true)
                .Any(attribute => attribute.GetType().Name.Contains("RbacGuard"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authorizeAttribute, Is.Not.Null);
                Assert.That(hasRbacGuard, Is.False);
            }
        }

        [Test]
        public void GetState_NeverActedUpon_ReturnsNullNextEligibleInstant()
        {
            appSettingServiceMock.Setup(service => service.GetSurveyNudgeNextEligibleAt()).Returns((DateTimeOffset?)null);

            var response = CreateSubject().GetState();

            var state = (response.Result as OkObjectResult)?.Value as SurveyNudgeState;
            Assert.That(state!.NextEligibleAt, Is.Null);
        }

        [Test]
        public void GetState_AfterAChoice_ReturnsRoundTrippedNextEligibleInstant()
        {
            appSettingServiceMock.Setup(service => service.GetSurveyNudgeNextEligibleAt()).Returns(NextEligibleInstant);

            var response = CreateSubject().GetState();

            var state = (response.Result as OkObjectResult)?.Value as SurveyNudgeState;
            Assert.That(state!.NextEligibleAt, Is.EqualTo(NextEligibleInstant));
        }

        [Test]
        public async Task RecordAction_DelegatesTheChosenActionToTheService()
        {
            var subject = CreateSubject();

            var response = await subject.RecordAction(new SurveyNudgeActionRequest { Action = SurveyNudgeAction.RemindLater });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<NoContentResult>());
                appSettingServiceMock.Verify(service => service.RecordSurveyNudgeAction(SurveyNudgeAction.RemindLater), Times.Once);
            }
        }

        [Test]
        public async Task RecordAction_WithoutAnAction_ReturnsBadRequestAndDoesNotRecord()
        {
            var subject = CreateSubject();

            var response = await subject.RecordAction(new SurveyNudgeActionRequest());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<BadRequestResult>());
                appSettingServiceMock.Verify(service => service.RecordSurveyNudgeAction(It.IsAny<SurveyNudgeAction>()), Times.Never);
            }
        }

        private SurveyNudgeController CreateSubject()
        {
            return new SurveyNudgeController(appSettingServiceMock.Object);
        }
    }
}
