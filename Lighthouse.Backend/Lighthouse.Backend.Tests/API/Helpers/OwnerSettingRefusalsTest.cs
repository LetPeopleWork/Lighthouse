using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    /// <summary>The words a Team or Portfolio save is refused with when its Parent sources cannot be kept.</summary>
    [TestFixture]
    public class OwnerSettingRefusalsTest
    {
        private const int TheConnection = 4;

        private const int TheOtherConnection = 5;

        private const int AFieldOfTheConnection = 7;

        private const int AnotherFieldOfTheConnection = 8;

        private const int AFieldOfTheOtherConnection = 21;

        private const int AFieldNobodyHolds = 99;

        private const string NothingWasSaved = " Nothing was saved.";

        private static readonly ParentSourceEntryDto OwnParent = new() { Kind = ParentSourceKind.TrackerParent };

        private static ParentSourceEntryDto Field(int id) => new() { Kind = ParentSourceKind.AdditionalField, AdditionalFieldDefinitionId = id };

        private static WorkTrackingSystemConnection AConnection(int id, params (int Id, string Name)[] fields)
        {
            var connection = new WorkTrackingSystemConnection { Id = id };
            connection.AdditionalFieldDefinitions.AddRange(fields.Select(field => new AdditionalFieldDefinition { Id = field.Id, DisplayName = field.Name }));
            return connection;
        }

        private static Team ATeam() => new()
        {
            WorkTrackingSystemConnectionId = TheConnection,
            WorkTrackingSystemConnection = AConnection(TheConnection, (AFieldOfTheConnection, "Epic Link"), (AnotherFieldOfTheConnection, "Program")),
        };

        private static TeamSettingDto ASaveOf(params ParentSourceEntryDto?[] list) => new()
        {
            WorkTrackingSystemConnectionId = TheConnection,
            ParentSources = [.. list],
            ParentOverrideSource = SettingSource.Own,
        };

        private static ProblemDetails RefusalOf(SettingsOwnerDtoBase save, WorkTrackingSystemOptionsOwner owner, SettingsOwnerKind kind)
        {
            var connections = new Mock<IRepository<WorkTrackingSystemConnection>>();
            connections.Setup(repository => repository.GetById(TheOtherConnection))
                .Returns(AConnection(TheOtherConnection, (AFieldOfTheOtherConnection, "Initiative")));
            var terminology = new Mock<ITerminologyService>();
            terminology.Setup(service => service.GetAll()).Returns([]);

            var refusal = new OwnerSettingRefusals(connections.Object, terminology.Object).RefusalOf(new AController(), kind, owner, save);

            Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            return (ProblemDetails)refusal!.Value!;
        }

        private static IEnumerable<TestCaseData> ListsThatCannotBeKept()
        {
            yield return new TestCaseData((object)Array.Empty<ParentSourceEntryDto?>(), "A list needs at least one source")
                .SetName("An empty list");
            yield return new TestCaseData((object)new ParentSourceEntryDto?[] { new() { Kind = ParentSourceKind.AdditionalField } }, "A source is the own Parent or one Additional Field")
                .SetName("An entry naming no field");
            yield return new TestCaseData(
                    (object)new ParentSourceEntryDto?[] { OwnParent, Field(1), Field(2), Field(3), Field(4), Field(5) },
                    "A list holds at most 5 sources")
                .SetName("Six entries");
            yield return new TestCaseData((object)new ParentSourceEntryDto?[] { OwnParent, Field(AFieldOfTheConnection), OwnParent }, "Parent is already in the list")
                .SetName("The own parent twice");
            yield return new TestCaseData((object)new ParentSourceEntryDto?[] { Field(AnotherFieldOfTheConnection), Field(AFieldOfTheConnection), Field(AFieldOfTheConnection) }, "Epic Link is already in the list")
                .SetName("A field twice");
            yield return new TestCaseData((object)new ParentSourceEntryDto?[] { Field(AFieldNobodyHolds), Field(AFieldNobodyHolds) }, $"Unknown field (id {AFieldNobodyHolds}) is already in the list")
                .SetName("A field nobody holds twice");
        }

        [TestCaseSource(nameof(ListsThatCannotBeKept))]
        public void A_list_that_cannot_be_kept_is_refused_and_says_why(ParentSourceEntryDto?[] list, string why)
        {
            var problem = RefusalOf(ASaveOf(list), ATeam(), SettingsOwnerKind.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo("team-parent-sources-refused"));
                Assert.That(problem.Title, Is.EqualTo("The Team names parent sources that cannot be kept."));
                Assert.That(problem.Detail, Is.EqualTo(why + "." + NothingWasSaved));
            }
        }

        [Test]
        public void A_repeated_field_is_named_as_the_connection_the_save_moves_to_calls_it()
        {
            var save = ASaveOf(Field(AFieldOfTheOtherConnection), Field(AFieldOfTheOtherConnection));
            save.WorkTrackingSystemConnectionId = TheOtherConnection;

            var problem = RefusalOf(save, ATeam(), SettingsOwnerKind.Team);

            Assert.That(problem.Detail, Is.EqualTo("Initiative is already in the list." + NothingWasSaved));
        }

        [Test]
        public void A_portfolios_refusal_carries_its_own_code_and_noun()
        {
            var portfolio = new Portfolio { WorkTrackingSystemConnectionId = TheConnection, WorkTrackingSystemConnection = AConnection(TheConnection) };
            var save = new PortfolioSettingDto { WorkTrackingSystemConnectionId = TheConnection, ParentSources = [], ParentOverrideSource = SettingSource.Own };

            var problem = RefusalOf(save, portfolio, SettingsOwnerKind.Portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo("portfolio-parent-sources-refused"));
                Assert.That(problem.Title, Is.EqualTo("The Portfolio names parent sources that cannot be kept."));
            }
        }

        [Test]
        public void One_field_the_connection_lacks_refuses_a_list_whose_other_fields_it_holds()
        {
            var problem = RefusalOf(ASaveOf(Field(AFieldOfTheConnection), Field(AFieldNobodyHolds)), ATeam(), SettingsOwnerKind.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo("team-field-not-on-connection"));
                Assert.That(problem.Title, Is.EqualTo("The Team names a field its connection does not have."));
                Assert.That(problem.Detail, Is.EqualTo("One of the Parent sources is not an Additional Field of this Team's connection." + NothingWasSaved));
            }
        }

        [Test]
        public void A_source_this_version_does_not_know_is_refused_as_the_source_of_the_list()
        {
            var save = ASaveOf(OwnParent);
            save.ParentOverrideSource = (SettingSource)99;

            var problem = RefusalOf(save, ATeam(), SettingsOwnerKind.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo("team-setting-source-unknown"));
                Assert.That(problem.Title, Is.EqualTo("The Team names a source this version of Lighthouse does not know."));
                Assert.That(problem.Detail, Does.StartWith("The source of the Parent sources must be one of: "));
            }
        }

        private sealed class AController : ControllerBase
        {
            public AController()
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
                ProblemDetailsFactory = new PlainProblemDetailsFactory();
            }
        }

        private sealed class PlainProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

            public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
        }
    }
}
