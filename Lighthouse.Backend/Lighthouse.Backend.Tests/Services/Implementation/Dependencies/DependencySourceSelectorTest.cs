using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    /// <summary>
    /// Where a Feature's dependencies were read from. The answer belongs to the Portfolio rather than to
    /// the tracker asking, which is why every tracker asks the same question here instead of each keeping
    /// its own copy of the rule.
    /// </summary>
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencySourceSelectorTest
    {
        private const int TheFieldTheyNamed = 7;

        private static readonly string[] WhatTheTrackerLinkedTo = ["LGHTHSDMO-7", "LGHTHSDMO-9"];

        private static readonly string[] WhatTheFieldNames = ["LGHTHSDMO-11", "LGHTHSDMO-12"];

        private static readonly string[] WrittenWordForWord =
            ["LGHTHSDMO-7", "lghthsdmo-9", "https://example.net/browse/LGHTHSDMO-11"];

        [Test]
        public void TheDependenciesOf_APortfolioNamingNoField_ReadsTheTrackersOwnLink()
        {
            var portfolio = new Portfolio();
            var workItem = AWorkItemWhoseFieldReads("LGHTHSDMO-404");

            var dependencies = DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies.ConvertAll(d => d.ReferenceId), Is.EqualTo(WhatTheTrackerLinkedTo));
                Assert.That(dependencies.TrueForAll(d => d.Source == DependencySource.TrackerLink), Is.True);
            }
        }

        [Test]
        public void TheDependenciesOf_APortfolioNamingAField_ReadsThatFieldAndNotTheLink()
        {
            var portfolio = APortfolioReadingItsOwnField();
            var workItem = AWorkItemWhoseFieldReads("LGHTHSDMO-11;LGHTHSDMO-12");

            var dependencies = DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies.ConvertAll(d => d.ReferenceId), Is.EqualTo(WhatTheFieldNames));
                Assert.That(dependencies.TrueForAll(d => d.Source == DependencySource.PortfolioField), Is.True);
            }
        }

        /// <summary>
        /// Naming a field is a declaration that the field is authoritative. A Portfolio that named one and
        /// left it empty is waiting on nothing - falling back to the links would quietly make the setting
        /// mean "both", which is the one thing it was decided not to mean.
        /// </summary>
        [Test]
        public void TheDependenciesOf_APortfolioNamingAnEmptyField_WaitsOnNothingRatherThanFallingBack()
        {
            var portfolio = APortfolioReadingItsOwnField();
            var workItem = AWorkItemWhoseFieldReads(string.Empty);

            var dependencies = DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo);

            Assert.That(dependencies, Is.Empty);
        }

        /// <summary>
        /// The field is typed by hand and on Jira the letters before the dash are part of the name. Nothing
        /// is corrected on the way in: an entry nobody can resolve is left out where it can be seen missing,
        /// which is easier to notice than a Feature quietly waiting on something nobody chose.
        /// </summary>
        [Test]
        public void TheDependenciesOf_AFieldWithNonsenseInIt_KeepsWhatWasWrittenWordForWord()
        {
            var portfolio = APortfolioReadingItsOwnField();
            var workItem = AWorkItemWhoseFieldReads("LGHTHSDMO-7;lghthsdmo-9;https://example.net/browse/LGHTHSDMO-11");

            var dependencies = DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo);

            Assert.That(dependencies.ConvertAll(d => d.ReferenceId), Is.EqualTo(WrittenWordForWord));
        }

        [Test]
        public void ReadsTheTrackersOwnLink_TurnsOnWhetherThePortfolioNamedAField()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DependencySourceSelector.ReadsTheTrackersOwnLink(new Portfolio()), Is.True);
                Assert.That(DependencySourceSelector.ReadsTheTrackersOwnLink(APortfolioReadingItsOwnField()), Is.False);
            }
        }

        /// <summary>
        /// A tracker with no fields of its own to point at keeps reading its links, whatever the Portfolio
        /// has typed into a setting it cannot serve. Answering otherwise would hand it an empty field and
        /// drop every link it does have.
        /// </summary>
        [Test]
        public void TheTrackersOwnLinksOnly_IgnoresWhateverThePortfolioNamed()
        {
            var dependencies = DependencySourceSelector.TheTrackersOwnLinksOnly(WhatTheTrackerLinkedTo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies.ConvertAll(d => d.ReferenceId), Is.EqualTo(WhatTheTrackerLinkedTo));
                Assert.That(dependencies.TrueForAll(d => d.Source == DependencySource.TrackerLink), Is.True);
            }
        }

        public enum WhatThePortfolioNamed
        {
            Nothing,
            AnOrdinaryField,
            ALinkType,
        }

        public enum TheTrackerAsking
        {
            OneThatCannotServeANamedField,
            OneWhoseFieldMayNameALinkType,
            OneThatServesOnlyFields,
        }

        /// <summary>
        /// Every tracker asks with what it read off its own links, and the field holds keys of its own, so
        /// each answer shows which of the three it came from. A link type the Portfolio named replaces the
        /// tracker's own link outright: adding the two together would leave nobody able to say where an
        /// edge came from. A tracker that serves only fields reads a name as a field, whatever it names.
        /// </summary>
        [TestCaseSource(nameof(EverySettingOnEveryTracker))]
        public void TheDependenciesOf_ReadsOneSourceDecidedByThePortfolioAndTheTracker(
            WhatThePortfolioNamed named, TheTrackerAsking tracker, string[] expectedReferences, DependencySource expectedSource)
        {
            var portfolio = named == WhatThePortfolioNamed.Nothing ? new Portfolio() : APortfolioReadingItsOwnField();
            var workItem = AWorkItemWhoseFieldReads("LGHTHSDMO-11;LGHTHSDMO-12");

            var dependencies = AskedBy(tracker, portfolio, workItem, named);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies.ConvertAll(d => d.ReferenceId), Is.EqualTo(expectedReferences));
                Assert.That(dependencies.TrueForAll(d => d.Source == expectedSource), Is.True);
            }
        }

        /// <summary>
        /// A named link type that no link on this Feature carries means the Feature waits on nothing.
        /// Falling back to "is blocked by" would quietly make the setting mean "both".
        /// </summary>
        [Test]
        public void TheDependenciesOf_ANamedLinkTypeNoLinkCarries_WaitsOnNothingRatherThanFallingBack()
        {
            var dependencies = DependencySourceSelector.TheDependenciesOf(
                APortfolioReadingItsOwnField(), AWorkItemWhoseFieldReads("LGHTHSDMO-11"), WhatTheTrackerLinkedTo, NoLinkOfTheNamedType);

            Assert.That(dependencies, Is.Empty);
        }

        private static readonly string[] WhatTheNamedLinkTypePointsAt = ["LGHTHSDMO-98"];

        private static readonly string[] NoLinkOfTheNamedType = [];

        private static IEnumerable<TestCaseData> EverySettingOnEveryTracker()
        {
            const TheTrackerAsking OptsOut = TheTrackerAsking.OneThatCannotServeANamedField;
            const TheTrackerAsking LinkAware = TheTrackerAsking.OneWhoseFieldMayNameALinkType;
            const TheTrackerAsking FieldsOnly = TheTrackerAsking.OneThatServesOnlyFields;

            yield return new TestCaseData(WhatThePortfolioNamed.Nothing, OptsOut, WhatTheTrackerLinkedTo, DependencySource.TrackerLink);
            yield return new TestCaseData(WhatThePortfolioNamed.AnOrdinaryField, OptsOut, WhatTheTrackerLinkedTo, DependencySource.TrackerLink);
            yield return new TestCaseData(WhatThePortfolioNamed.ALinkType, OptsOut, WhatTheTrackerLinkedTo, DependencySource.TrackerLink);

            yield return new TestCaseData(WhatThePortfolioNamed.Nothing, LinkAware, WhatTheTrackerLinkedTo, DependencySource.TrackerLink);
            yield return new TestCaseData(WhatThePortfolioNamed.AnOrdinaryField, LinkAware, WhatTheFieldNames, DependencySource.PortfolioField);
            yield return new TestCaseData(WhatThePortfolioNamed.ALinkType, LinkAware, WhatTheNamedLinkTypePointsAt, DependencySource.TrackerLink);

            yield return new TestCaseData(WhatThePortfolioNamed.Nothing, FieldsOnly, WhatTheTrackerLinkedTo, DependencySource.TrackerLink);
            yield return new TestCaseData(WhatThePortfolioNamed.AnOrdinaryField, FieldsOnly, WhatTheFieldNames, DependencySource.PortfolioField);
            yield return new TestCaseData(WhatThePortfolioNamed.ALinkType, FieldsOnly, WhatTheFieldNames, DependencySource.PortfolioField);
        }

        private static List<FeatureDependencyReference> AskedBy(
            TheTrackerAsking tracker, Portfolio portfolio, WorkItemBase workItem, WhatThePortfolioNamed named)
        {
            var fromTheNamedLinkType = named == WhatThePortfolioNamed.ALinkType ? WhatTheNamedLinkTypePointsAt : null;

            return tracker switch
            {
                TheTrackerAsking.OneThatCannotServeANamedField => DependencySourceSelector.TheTrackersOwnLinksOnly(WhatTheTrackerLinkedTo),
                TheTrackerAsking.OneWhoseFieldMayNameALinkType => DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo, fromTheNamedLinkType),
                TheTrackerAsking.OneThatServesOnlyFields => DependencySourceSelector.TheDependenciesOf(portfolio, workItem, WhatTheTrackerLinkedTo),
                _ => throw new ArgumentOutOfRangeException(nameof(tracker)),
            };
        }

        private static Portfolio APortfolioReadingItsOwnField()
            => new() { DependencyOverrideAdditionalFieldDefinitionId = TheFieldTheyNamed };

        private static WorkItemBase AWorkItemWhoseFieldReads(string value)
        {
            var workItem = new WorkItemBase();
            workItem.AdditionalFieldValues[TheFieldTheyNamed] = value;

            return workItem;
        }
    }
}
