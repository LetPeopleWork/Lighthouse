using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Epic 5733 - the promise that no free text can travel, checked as a property of the shape
    /// rather than as a habit at each call site.
    ///
    /// The usage data page tells people that what their browser posts has no field capable of
    /// carrying free text, and says that this is true of its shape rather than because whoever
    /// wrote the call site was careful. That sentence is only worth its ink while something checks
    /// it, and the moment worth checking is when the vocabulary grows: adding a field to carry one
    /// more fact about one more event is exactly how a page address, a name somebody picked or a
    /// sentence somebody typed gets a way out.
    ///
    /// So every part of what a browser sends has to be a choice from a closed list or a whole
    /// number. A string of any kind fails here, including one that happens to be well behaved
    /// today, because the next person to reach for it will find it already there.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataPayloadPurityTest
    {
        private static readonly Type[] EverythingABrowserSends =
        [
            typeof(UsageDataEventDto),
            typeof(UsageDataEventReported),
        ];

        [Test]
        [TestCaseSource(nameof(EverythingABrowserSends))]
        public void Nothing_a_browser_sends_can_carry_text(Type shape)
        {
            ArgumentNullException.ThrowIfNull(shape);

            var carryingText = shape.GetProperties()
                .Where(property => IsText(property.PropertyType))
                .Select(property => $"{shape.Name}.{property.Name}")
                .ToList();

            Assert.That(carryingText, Is.Empty,
                "the usage data page promises that what a browser posts has no field capable of "
                + "carrying free text, and that this holds because of the shape rather than because "
                + "every call site remembered. These are fields in which a page address, a name "
                + "somebody picked or a sentence somebody typed could now travel");
        }

        [Test]
        [TestCaseSource(nameof(EverythingABrowserSends))]
        public void Everything_a_browser_sends_is_a_choice_from_a_closed_list_or_a_whole_number(Type shape)
        {
            ArgumentNullException.ThrowIfNull(shape);

            var somethingElse = shape.GetProperties()
                .Where(property => !IsAChoiceOrANumber(property.PropertyType))
                .Select(property => $"{shape.Name}.{property.Name} is {property.PropertyType.Name}")
                .ToList();

            Assert.That(somethingElse, Is.Empty,
                "something a browser sends is neither a member of a list this product published nor "
                + "a whole number. Both of those can be read off this repository by anyone deciding "
                + "whether to agree; anything else cannot");
        }

        private static bool IsText(Type type) => Underlying(type) == typeof(string);

        private static bool IsAChoiceOrANumber(Type type)
        {
            var actual = Underlying(type);

            return actual.IsEnum || actual == typeof(int) || actual == typeof(bool);
        }

        private static Type Underlying(Type type) => Nullable.GetUnderlyingType(type) ?? type;
    }
}
