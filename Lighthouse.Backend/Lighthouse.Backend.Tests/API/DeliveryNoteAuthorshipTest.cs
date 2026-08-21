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
    /// <summary>
    /// The whole risk of this feature is one predicate, so it gets a fixture of its own rather than
    /// a couple of cases buried among the CRUD tests.
    /// </summary>
    [TestFixture]
    public class DeliveryNoteAuthorshipTest
    {
        private const int DeliveryId = 42;
        private const int PortfolioId = 7;
        private const int AnoopId = 5;
        private const int SomebodyElseId = 6;

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
            SignedInAsNobody();
        }

        [Test]
        public async Task TheAuthorMayCorrectTheirOwnNote()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("corrected"));

            Assert.That(ResultOf(response).Text, Is.EqualTo("corrected"));
        }

        [Test]
        public async Task SomebodyElseMayNotCorrectAnAuthoredNote()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(SomebodyElseId);

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("not yours"));

            Assert.That(response.Result, Is.InstanceOf<ForbidResult>());
            AssertTextUnchanged(noteId);
        }

        [Test]
        public async Task SomebodyElseMayNotWithdrawAnAuthoredNote()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(SomebodyElseId);

            var response = await CreateSubject().DeleteNote(DeliveryId, noteId);

            Assert.That(response, Is.InstanceOf<ForbidResult>());

            using var context = CreateContext();
            Assert.That(context.DeliveryNotes.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task ACallerWithNoProfileMayNotCorrectAnAuthoredNote()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAsNobody();

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("not yours"));

            Assert.That(response.Result, Is.InstanceOf<ForbidResult>());
            AssertTextUnchanged(noteId);
        }

        [Test]
        public async Task AnUnsignedNoteMayBeCorrectedByAnybodyWhoMayWrite()
        {
            var noteId = GivenNote(authorId: null);
            SignedInAsNobody();

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("corrected"));

            Assert.That(ResultOf(response).Text, Is.EqualTo("corrected"));
        }

        [Test]
        public async Task AnUnsignedNoteStaysCorrectableOnceTheInstanceTurnsAuthenticationOn()
        {
            // The case that strands notes if the rule is written as a plain id comparison: a note
            // written while nobody was signed in, on an instance that has since started signing
            // people in. Nobody owns it, so anybody who may write may still fix it.
            var noteId = GivenNote(authorId: null);
            SignedInAs(AnoopId);

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("corrected"));

            Assert.That(ResultOf(response).Text, Is.EqualTo("corrected"));
        }

        [Test]
        public async Task CorrectingANoteMarksItEditedWithoutMovingItsCreationDay()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text("corrected"));

            var note = ResultOf(response);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(note.LastEditedOn, Is.Not.Null);
                Assert.That(note.CreatedOn, Is.EqualTo(TestToday.Clock.ToInstanceDay(note.CreatedAt)));
            }
        }

        [Test]
        public async Task AnUneditedNoteCarriesNoEditMarker()
        {
            GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var notes = ResultOf(await CreateSubject().GetNotes(DeliveryId));

            Assert.That(notes[0].LastEditedOn, Is.Null);
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task ACorrectionThatEmptiesTheNoteIsRefusedAndLeavesItAlone(string text)
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var response = await CreateSubject().UpdateNote(DeliveryId, noteId, Text(text));

            Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());
            AssertTextUnchanged(noteId);
        }

        [Test]
        public async Task TheAuthorMayWithdrawTheirOwnNote()
        {
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var response = await CreateSubject().DeleteNote(DeliveryId, noteId);

            Assert.That(response, Is.InstanceOf<NoContentResult>());

            using var context = CreateContext();
            Assert.That(context.DeliveryNotes.Count(), Is.Zero);
        }

        [Test]
        public async Task ANoteCannotBeReachedThroughADeliveryItDoesNotBelongTo()
        {
            const int otherDeliveryId = 99;
            deliveryRepository.Setup(x => x.GetPortfolioId(otherDeliveryId)).Returns(PortfolioId);
            var noteId = GivenNote(authorId: AnoopId);
            SignedInAs(AnoopId);

            var response = await CreateSubject().UpdateNote(otherDeliveryId, noteId, Text("elsewhere"));

            Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
            AssertTextUnchanged(noteId);
        }

        [Test]
        public async Task TheListTellsTheCallerWhichNotesTheyMayChange()
        {
            GivenNote(authorId: AnoopId, text: "mine");
            GivenNote(authorId: SomebodyElseId, text: "theirs");
            GivenNote(authorId: null, text: "nobody's");
            SignedInAs(AnoopId);

            var notes = ResultOf(await CreateSubject().GetNotes(DeliveryId));

            var verdicts = notes.ToDictionary(n => n.Text, n => n.CanModify);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdicts["mine"], Is.True);
                Assert.That(verdicts["theirs"], Is.False);
                Assert.That(verdicts["nobody's"], Is.True);
            }
        }

        private static DeliveryNoteRequest Text(string text) => new() { Text = text };

        private void AssertTextUnchanged(int noteId)
        {
            using var context = CreateContext();
            var stored = context.DeliveryNotes.Single(n => n.Id == noteId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Text, Is.EqualTo("as it was"));
                Assert.That(stored.LastEditedAt, Is.Null);
            }
        }

        private int GivenNote(int? authorId, string text = "as it was")
        {
            using var context = CreateContext();
            var note = new DeliveryNote
            {
                DeliveryId = DeliveryId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                AuthorUserProfileId = authorId,
                AuthorDisplayName = authorId is null ? null : $"User {authorId}",
            };
            context.DeliveryNotes.Add(note);
            context.SaveChanges();
            return note.Id;
        }

        private void SignedInAs(int profileId)
        {
            currentUserProfileService
                .Setup(x => x.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = profileId, DisplayName = $"User {profileId}" });
        }

        private void SignedInAsNobody()
        {
            currentUserProfileService
                .Setup(x => x.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);
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
