using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class DeliveryNotesControllerTest
    {
        private const int DeliveryId = 42;
        private const int PortfolioId = 7;

        private static readonly string[] NewestFirst = ["third", "second", "first"];
        private static readonly string[] OnlyThisDeliverysNote = ["belongs here"];

        private DbContextOptions<LighthouseAppContext> options;
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> contextLogger;
        private Mock<IDeliveryRepository> deliveryRepository;
        private Mock<IRbacAdministrationService> rbacAdministrationService;
        private Mock<ICurrentUserProfileService> currentUserProfileService;

        [SetUp]
        public void Setup()
        {
            cryptoService = new Mock<ICryptoService>();
            contextLogger = new Mock<ILogger<LighthouseAppContext>>();

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();

            deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(x => x.GetPortfolioId(DeliveryId)).Returns(PortfolioId);

            rbacAdministrationService = new Mock<IRbacAdministrationService>();
            rbacAdministrationService
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            currentUserProfileService = new Mock<ICurrentUserProfileService>();
            currentUserProfileService
                .Setup(x => x.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);
        }

        [Test]
        public async Task AddNote_StoresTheTextAndReturnsIt()
        {
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "Two Features added after the steering review" });

            var note = ResultOf(response);
            Assert.That(note.Text, Is.EqualTo("Two Features added after the steering review"));

            using var context = CreateContext();
            Assert.That(context.DeliveryNotes.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task AddNote_AttributesTheNoteToTheCallersDisplayName()
        {
            GivenSignedInAs("Anoop Kumar");
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "Vendor slipped a week" });

            Assert.That(ResultOf(response).AuthorDisplayName, Is.EqualTo("Anoop Kumar"));
        }

        [Test]
        public async Task AddNote_WithNobodyToName_StoresTheNoteUnattributed()
        {
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "Scope cut agreed" });

            var note = ResultOf(response);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(note.AuthorDisplayName, Is.Null);
                Assert.That(note.Text, Is.EqualTo("Scope cut agreed"));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n ")]
        public async Task AddNote_WithNothingInIt_IsRefusedAndStoresNothing(string text)
        {
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = text });

            Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

            using var context = CreateContext();
            Assert.That(context.DeliveryNotes.Count(), Is.Zero);
        }

        [Test]
        public async Task AddNote_TrimsTheBlankSpaceAroundWhatSomebodyTyped()
        {
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "  Vendor slipped\n" });

            Assert.That(ResultOf(response).Text, Is.EqualTo("Vendor slipped"));
        }

        [Test]
        public async Task GetNotes_ReturnsTheNewestFirst()
        {
            var subject = CreateSubject();
            await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "first" });
            await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "second" });
            await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "third" });

            var notes = ResultOf(await subject.GetNotes(DeliveryId));

            Assert.That(notes.Select(n => n.Text), Is.EqualTo(NewestFirst));
        }

        [Test]
        public async Task GetNotes_ReturnsOnlyTheNotesOfTheDeliveryAskedFor()
        {
            const int otherDeliveryId = 99;
            deliveryRepository.Setup(x => x.GetPortfolioId(otherDeliveryId)).Returns(PortfolioId);

            var subject = CreateSubject();
            await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "belongs here" });
            await subject.AddNote(otherDeliveryId, new DeliveryNoteRequest { Text = "belongs elsewhere" });

            var notes = ResultOf(await subject.GetNotes(DeliveryId));

            Assert.That(notes.Select(n => n.Text), Is.EqualTo(OnlyThisDeliverysNote));
        }

        [Test]
        public async Task AddNote_WithoutWriteAccess_IsRefusedAndStoresNothing()
        {
            RefuseScope();
            var subject = CreateSubject();

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "should not land" });

            Assert.That(response.Result, Is.InstanceOf<ForbidResult>());

            using var context = CreateContext();
            Assert.That(context.DeliveryNotes.Count(), Is.Zero);
        }

        [Test]
        public async Task GetNotes_WithoutReadAccess_IsRefused()
        {
            RefuseScope();
            var subject = CreateSubject();

            var response = await subject.GetNotes(DeliveryId);

            Assert.That(response.Result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task AddNote_ChecksTheScopeOfTheDeliveryItIsAskedAbout()
        {
            var subject = CreateSubject();

            await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = "anything" });

            rbacAdministrationService.Verify(x => x.CanSatisfyRequirementAsync(
                It.IsAny<ClaimsPrincipal>(),
                RbacGuardRequirement.PortfolioWrite,
                PortfolioId,
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task GetNotes_ForADeliveryThatDoesNotExist_IsNotFound()
        {
            deliveryRepository.Setup(x => x.GetPortfolioId(It.IsAny<int>())).Returns((int?)null);
            var subject = CreateSubject();

            var response = await subject.GetNotes(DeliveryId);

            Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task AddNote_KeepsWhatLooksLikeMarkupExactlyAsItWasTyped()
        {
            var subject = CreateSubject();
            const string markup = "<b>not bold</b> & <script>alert(1)</script>";

            var response = await subject.AddNote(DeliveryId, new DeliveryNoteRequest { Text = markup });

            Assert.That(ResultOf(response).Text, Is.EqualTo(markup));
        }

        private void GivenSignedInAs(string displayName)
        {
            currentUserProfileService
                .Setup(x => x.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 5, DisplayName = displayName });
        }

        private void RefuseScope()
        {
            rbacAdministrationService
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        private static DeliveryNoteDto ResultOf(ActionResult<DeliveryNoteDto> response)
        {
            return (DeliveryNoteDto)((OkObjectResult)response.Result!).Value!;
        }

        private static List<DeliveryNoteDto> ResultOf(ActionResult<IEnumerable<DeliveryNoteDto>> response)
        {
            return (List<DeliveryNoteDto>)((OkObjectResult)response.Result!).Value!;
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoService.Object, contextLogger.Object);
        }

        private DeliveryNotesController CreateSubject()
        {
            return new DeliveryNotesController(
                CreateContext(),
                deliveryRepository.Object,
                rbacAdministrationService.Object,
                currentUserProfileService.Object,
                TestToday.Clock);
        }
    }
}
