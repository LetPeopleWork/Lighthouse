using System.Reflection;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>
    /// Sets every settable property of a settings DTO to a value that is not its default, saves it onto a
    /// Team or Portfolio, and reads the DTO back. A property added to the DTO but forgotten in the
    /// Sync*With*Settings helper (or in the DTO's constructor) comes back as its default and fails here.
    /// </summary>
    [TestFixture]
    public class SettingsOwnerDtoRoundTripTest
    {
        private static readonly DateOnly Today = new(2026, 5, 1);

        private static readonly Dictionary<string, string> NotARoundTrippedSetting = new Dictionary<string, string>
        {
            [nameof(SettingsOwnerDtoBase.Id)] = "identity assigned by the database, not a setting",
            [nameof(SettingsOwnerDtoBase.ConcurrencyToken)] = "stamped by the database on save, not taken from the payload",
            [nameof(SettingsOwnerDtoBase.DataRetrievalSchema)] = "derived from the work tracking system of the connection",
            [nameof(TeamSettingDto.Refinement)] = "kept only for states that are in the To Do or Doing lists, so an arbitrary value cannot round-trip",
            [nameof(PortfolioSettingDto.InvolvedTeams)] = "derived from the Features of the Portfolio, never stored from the payload",
        };

        [Test]
        public void EverySettingOfATeamSurvivesBeingSavedAndReadBack()
        {
            var sent = Populated(new TeamSettingDto());
            var team = new Team();

            team.SyncTeamWithTeamSettings(sent);
            var received = new TeamSettingDto(team, Today);

            AssertEverySettingCameBack(sent, received);
        }

        [Test]
        public void EverySettingOfAPortfolioSurvivesBeingSavedAndReadBack()
        {
            var owningTeam = new Team { Id = 33, Name = "Owner" };
            var teamRepository = new Mock<IRepository<Team>>();
            teamRepository.Setup(repository => repository.GetById(owningTeam.Id)).Returns(owningTeam);

            var sent = Populated(new PortfolioSettingDto());
            var portfolio = new Portfolio();

            portfolio.SyncWithPortfolioSettings(sent, teamRepository.Object);
            var received = new PortfolioSettingDto(portfolio);

            AssertEverySettingCameBack(sent, received);
        }

        private static T Populated<T>(T dto) where T : SettingsOwnerDtoBase
        {
            var ordinal = 0;
            foreach (var property in SettableProperties(typeof(T)))
            {
                ordinal++;
                property.SetValue(dto, NonDefaultValueFor(property, ordinal));
            }

            return dto;
        }

        private static void AssertEverySettingCameBack(SettingsOwnerDtoBase sent, SettingsOwnerDtoBase received)
        {
            var lost = SettableProperties(sent.GetType())
                .Where(property => Describe(property.GetValue(sent)) != Describe(property.GetValue(received)))
                .Select(property => $"{property.Name} (sent {Describe(property.GetValue(sent))}, read back {Describe(property.GetValue(received))})")
                .ToList();

            Assert.That(
                lost,
                Is.Empty,
                "A setting the DTO carries does not survive a save. Copy it in the Sync*With*Settings helper and in the DTO constructor, " +
                "or, if it is genuinely not a setting taken from the payload, list it in NotARoundTrippedSetting with the reason. Lost: " +
                string.Join("; ", lost));
        }

        private static IEnumerable<PropertyInfo> SettableProperties(Type dtoType)
            => dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Where(property => !NotARoundTrippedSetting.ContainsKey(property.Name))
                .OrderBy(property => property.Name, StringComparer.Ordinal);

        private static object? NonDefaultValueFor(PropertyInfo property, int ordinal)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (type == typeof(int))
            {
                return 10 + ordinal;
            }

            if (type == typeof(bool))
            {
                return true;
            }

            if (type == typeof(string))
            {
                return $"{property.Name}-value";
            }

            if (type == typeof(DateTime))
            {
                return new DateTime(2026, 3, 1 + ordinal, 0, 0, 0, DateTimeKind.Utc);
            }

            if (type == typeof(SettingSource))
            {
                return SettingSource.Own;
            }

            if (type == typeof(List<string>))
            {
                return new List<string> { $"{property.Name}-first", $"{property.Name}-second" };
            }

            if (type == typeof(List<StateMappingDto>))
            {
                return new List<StateMappingDto> { new() { Name = "Mapped", States = ["Review", "Testing"] } };
            }

            if (type == typeof(List<CycleTimeDefinitionDto>))
            {
                return new List<CycleTimeDefinitionDto> { new() { Id = 1, Name = "Lead", StartState = "Review", EndState = "Testing" } };
            }

            if (type == typeof(EntityReferenceDto))
            {
                return new EntityReferenceDto(33, "Owner");
            }

            throw new NotSupportedException(
                $"{property.DeclaringType!.Name}.{property.Name} has the type {property.PropertyType.Name}, which this test cannot fill. " +
                "Teach NonDefaultValueFor the type, or list the property in NotARoundTrippedSetting with the reason.");
        }

        private static string Describe(object? value) => value switch
        {
            List<CycleTimeDefinitionDto> definitions => JsonSerializer.Serialize(
                definitions.Select(definition => new { definition.Id, definition.Name, definition.StartState, definition.EndState })),
            _ => JsonSerializer.Serialize(value),
        };
    }
}
