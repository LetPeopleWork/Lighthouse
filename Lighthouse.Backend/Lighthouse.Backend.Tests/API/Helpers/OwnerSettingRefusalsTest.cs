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
            var refusal = TheRefusalIfAny(save, owner, kind);

            Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            return (ProblemDetails)refusal!.Value!;
        }

        private static ObjectResult? TheRefusalIfAny(SettingsOwnerDtoBase save, WorkTrackingSystemOptionsOwner? owner, SettingsOwnerKind kind)
        {
            var connections = new Mock<IRepository<WorkTrackingSystemConnection>>();
            connections.Setup(repository => repository.GetById(TheOtherConnection))
                .Returns(AConnection(TheOtherConnection, (AFieldOfTheOtherConnection, "Initiative")));
            var terminology = new Mock<ITerminologyService>();
            terminology.Setup(service => service.GetAll()).Returns([]);

            return new OwnerSettingRefusals(connections.Object, terminology.Object).RefusalOf(new AController(), kind, owner, save);
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
        public void A_list_whose_every_field_the_connection_holds_is_not_refused()
        {
            var refusal = TheRefusalIfAny(ASaveOf(Field(AFieldOfTheConnection), Field(AnotherFieldOfTheConnection)), ATeam(), SettingsOwnerKind.Team);

            Assert.That(refusal, Is.Null, "Every field of the list is one the Team's connection holds, so there is nothing to refuse.");
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

        private const string TheEstimationSourceWords = "The Estimation Field source must be one of: ConnectionDefault, Own." + NothingWasSaved;

        private static WorkTrackingSystemOptionsOwner AnOwnerOf(SettingsOwnerKind kind)
            => kind == SettingsOwnerKind.Team
                ? ATeam()
                : new Portfolio { WorkTrackingSystemConnectionId = TheConnection, WorkTrackingSystemConnection = AConnection(TheConnection, (AFieldOfTheConnection, "Epic Link")) };

        private static SettingsOwnerDtoBase AnEstimationSaveFor(SettingsOwnerKind kind, SettingSource? source, int? field = null)
        {
            SettingsOwnerDtoBase save = kind == SettingsOwnerKind.Team ? new TeamSettingDto() : new PortfolioSettingDto();
            save.WorkTrackingSystemConnectionId = TheConnection;
            save.EstimationSource = source;
            save.EstimationAdditionalFieldDefinitionId = field;
            return save;
        }

        private static TestCaseData[] BothKinds(string team, string portfolio) =>
        [
            new TestCaseData(SettingsOwnerKind.Team, team).SetArgDisplayNames("Team"),
            new TestCaseData(SettingsOwnerKind.Portfolio, portfolio).SetArgDisplayNames("Portfolio"),
        ];

        private static TestCaseData[] TheKindsAndTheirCodesForAnUnknownSource() => BothKinds("team-setting-source-unknown", "portfolio-setting-source-unknown");

        private static TestCaseData[] TheKindsAndTheirCodesForAMissingField() => BothKinds("team-field-not-on-connection", "portfolio-field-not-on-connection");

        [TestCaseSource(nameof(TheKindsAndTheirCodesForAnUnknownSource))]
        public void An_owner_saved_with_an_Estimation_source_this_version_does_not_know_is_refused_with_its_own_code_and_words(SettingsOwnerKind kind, string code)
        {
            var problem = RefusalOf(AnEstimationSaveFor(kind, (SettingSource)99), AnOwnerOf(kind), kind);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo(code));
                Assert.That(problem.Title, Is.EqualTo($"The {(kind == SettingsOwnerKind.Team ? "Team" : "Portfolio")} names a source this version of Lighthouse does not know."));
                Assert.That(problem.Detail, Is.EqualTo(TheEstimationSourceWords));
            }
        }

        [Test]
        public void A_Team_moved_to_another_connection_with_an_Estimation_source_this_version_does_not_know_is_refused()
        {
            var save = AnEstimationSaveFor(SettingsOwnerKind.Team, (SettingSource)99);
            save.WorkTrackingSystemConnectionId = TheOtherConnection;

            var problem = RefusalOf(save, ATeam(), SettingsOwnerKind.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo("team-setting-source-unknown"));
                Assert.That(problem.Detail, Is.EqualTo(TheEstimationSourceWords));
            }
        }

        [TestCaseSource(nameof(TheKindsAndTheirCodesForAnUnknownSource))]
        public void A_new_owner_created_with_an_Estimation_source_this_version_does_not_know_is_refused(SettingsOwnerKind kind, string code)
        {
            var refusal = TheRefusalIfAny(AnEstimationSaveFor(kind, (SettingSource)99), null, kind);
            Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            var problem = (ProblemDetails)refusal!.Value!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo(code));
                Assert.That(problem.Detail, Is.EqualTo(TheEstimationSourceWords));
            }
        }

        [TestCaseSource(nameof(TheKindsAndTheirCodesForAMissingField))]
        public void An_owner_moved_to_another_connection_while_owning_an_Estimation_Field_of_the_old_one_is_refused(SettingsOwnerKind kind, string code)
        {
            var owner = AnOwnerOf(kind);
            owner.OwnEstimation(new EstimationBundle(AFieldOfTheConnection, "Points", false, []));
            var save = AnEstimationSaveFor(kind, SettingSource.Own, AFieldOfTheConnection);
            save.WorkTrackingSystemConnectionId = TheOtherConnection;

            var problem = RefusalOf(save, owner, kind);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem.Extensions["code"], Is.EqualTo(code));
                Assert.That(problem.Title, Is.EqualTo($"The {(kind == SettingsOwnerKind.Team ? "Team" : "Portfolio")} names a field its connection does not have."));
                Assert.That(problem.Detail, Is.EqualTo($"The Estimation Field is not an Additional Field of this {(kind == SettingsOwnerKind.Team ? "Team" : "Portfolio")}'s connection." + NothingWasSaved));
            }
        }

        [Test]
        public void An_owner_following_the_Estimation_default_that_still_names_a_field_of_the_old_connection_is_not_refused()
        {
            var save = AnEstimationSaveFor(SettingsOwnerKind.Team, SettingSource.ConnectionDefault, AFieldOfTheConnection);
            save.WorkTrackingSystemConnectionId = TheOtherConnection;

            Assert.That(TheRefusalIfAny(save, ATeam(), SettingsOwnerKind.Team), Is.Null, "A field sent along with following is not what the owner reads, so it is not judged.");
        }

        [Test]
        public void An_owner_owning_an_Estimation_Field_the_connection_holds_is_not_refused()
        {
            var owner = ATeam();
            owner.OwnEstimation(new EstimationBundle(AFieldOfTheConnection, "Points", false, []));

            Assert.That(TheRefusalIfAny(AnEstimationSaveFor(SettingsOwnerKind.Team, SettingSource.Own, AFieldOfTheConnection), owner, SettingsOwnerKind.Team), Is.Null);
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
