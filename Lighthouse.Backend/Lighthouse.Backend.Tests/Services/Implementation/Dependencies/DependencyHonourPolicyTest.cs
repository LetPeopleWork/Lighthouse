using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencyHonourPolicyTest
    {
        private static readonly NotHonouredReason[] TheOnlyReasonsThereAre =
        [
            NotHonouredReason.OutsideThisPortfolio,
            NotHonouredReason.InALoop,
            NotHonouredReason.BlockerCannotBeForecast,
            NotHonouredReason.IgnoredByPortfolio,
        ];

        private static readonly int[] TheSamePortfolio = [1];

        private static readonly int[] AnotherPortfolio = [2];

        private static readonly int[] BothPortfolios = [1, 2];

        private static readonly int[] NobodyIgnoresTheirs = [];

        private static readonly string[] NothingWaitedOn = [];

        private static readonly string[] TheFirst = ["F-1"];

        private static readonly string[] TheSecond = ["F-2"];

        private static readonly string[] TheOtherThree = ["F-2", "F-3", "F-4"];

        private static readonly string[] SomethingNotHeldHere = ["F-99"];

        [Test]
        public void TheReasonsNotToActOnADependency_AreOnlyEverThese()
        {
            Assert.That(Enum.GetValues<NotHonouredReason>(), Is.EqualTo(TheOnlyReasonsThereAre),
                "A reason was added or removed. Every screen that explains why Lighthouse left a dependency " +
                "out says one of these and nothing else, so a new one needs its wording and its warning " +
                "decided with it rather than appearing on its own.");
        }

        [Test]
        public void ADependencyWithNothingWrongWithIt_CarriesNoReasonAndNothingAboutWhereItSits()
        {
            var verdict = AVerdict(reason: null, blockerPositionedBelow: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.HasNothingWrongWithIt, Is.True);
                Assert.That(verdict.IsHonoured, Is.True);
            }
        }

        [Test]
        public void AFeatureWaitingOnOnePositionedBelowIt_IsWorthSayingAndIsNoReasonToLeaveItOut()
        {
            var verdict = AVerdict(reason: null, blockerPositionedBelow: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsHonoured, Is.True);
                Assert.That(verdict.HasNothingWrongWithIt, Is.False);
            }
        }

        [TestCase(NotHonouredReason.OutsideThisPortfolio)]
        [TestCase(NotHonouredReason.InALoop)]
        [TestCase(NotHonouredReason.BlockerCannotBeForecast)]
        public void ADependencyWithAReasonAgainstIt_IsNotOneLighthouseActsOn(NotHonouredReason reason)
        {
            var verdict = AVerdict(reason, blockerPositionedBelow: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsHonoured, Is.False);
                Assert.That(verdict.Reason, Is.EqualTo(reason));
            }
        }

        [Test]
        public void WhatWasDecided_StaysDecidedWhenTheListItWasBuiltFromIsEmptiedAfterwards()
        {
            var asDecided = new List<DependencyVerdict> { AVerdict(NotHonouredReason.InALoop, blockerPositionedBelow: false) };

            var honoured = new HonouredDependencies(asDecided);
            asDecided.Clear();

            Assert.That(honoured.Verdicts, Has.Count.EqualTo(1));
        }

        [Test]
        public void ABlockerSharingNoPortfolioWithWhatWaitsOnIt_IsOutsideThisPortfolio()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: AnotherPortfolio));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason,
                Is.EqualTo(NotHonouredReason.OutsideThisPortfolio));
        }

        [Test]
        public void ABlockerLighthouseIsNotLookingAt_IsOutsideThisPortfolioToo()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: SomethingNotHeldHere));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-99").Reason,
                Is.EqualTo(NotHonouredReason.OutsideThisPortfolio));
        }

        [Test]
        public void ABlockerWhoseTeamHasNothingDeliveredToMeasure_IsOneThatCannotBeForecast()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, canBeForecast: false));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason,
                Is.EqualTo(NotHonouredReason.BlockerCannotBeForecast));
        }

        [Test]
        public void WhenTwoFeaturesWaitOnEachOther_BothWaysRoundAreJudgedPartOfTheSameLoop()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.InALoop));
                Assert.That(TheVerdictFor(verdicts, "F-2", "F-1").Reason, Is.EqualTo(NotHonouredReason.InALoop));
            }
        }

        [Test]
        public void AFeatureInALoopThatAlsoCannotBeForecast_StillReadsAsTheLoop()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, canBeForecast: false, waitingOn: TheFirst));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.InALoop));
        }

        [Test]
        public void ADependencyWithNothingWrongWithIt_RaisesNoWarningAtAll()
        {
            var verdicts = Decide(
                AFeature("F-2", position: 9, portfolioIds: TheSamePortfolio, waitingOn: TheFirst),
                AFeature("F-1", position: 3, portfolioIds: TheSamePortfolio));

            var verdict = TheVerdictFor(verdicts, "F-2", "F-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Reason, Is.Null);
                Assert.That(verdict.BlockerPositionedBelow, Is.False);
                Assert.That(verdict.HasNothingWrongWithIt, Is.True);
            }
        }

        [Test]
        public void AFeatureWaitingOnOneTheUserPutBelowIt_IsSaidSoAndIsStillActedOn()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 3, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 9, portfolioIds: TheSamePortfolio));

            var verdict = TheVerdictFor(verdicts, "F-1", "F-2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.BlockerPositionedBelow, Is.True);
                Assert.That(verdict.IsHonoured, Is.True);
                Assert.That(verdict.HasNothingWrongWithIt, Is.False);
            }
        }

        [Test]
        public void TwoFeaturesSideBySideInTheOrder_AreNeitherOfThemBelowTheOther()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 4, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 4, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").BlockerPositionedBelow, Is.False);
        }

        // Not knowing where something sits is not the same as knowing it sits below. A read path that
        // never numbers the Features would otherwise turn every dependency on it into an ordering warning,
        // and the reader would go looking for a mess that is not there.
        [TestCase(null, 4, TestName = "AFeatureWithNoPlaceOfItsOwn_IsNotSaidToBeWaitingOnOneBelowIt")]
        [TestCase(4, null, TestName = "AFeatureWaitingOnOneWithNoPlaceAtAll_IsToldNothingAboutTheOrder")]
        public void WhereNeitherPlaceIsKnown_NothingIsSaidAboutTheOrder(int? dependentPosition, int? blockerPosition)
        {
            var verdicts = Decide(
                AFeature("F-1", dependentPosition, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", blockerPosition, portfolioIds: TheSamePortfolio));

            var verdict = TheVerdictFor(verdicts, "F-1", "F-2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.BlockerPositionedBelow, Is.False);
                Assert.That(verdict.HasNothingWrongWithIt, Is.True);
            }
        }

        [Test]
        public void AskingTheSameQuestionTwice_AnswersItTheSameWayAndLeavesNothingBehind()
        {
            var featuresInScope = new List<FeatureDependencyFacts>
            {
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheOtherThree),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst),
                AFeature("F-3", position: 3, portfolioIds: AnotherPortfolio),
                AFeature("F-4", position: 4, portfolioIds: TheSamePortfolio, canBeForecast: false),
            };
            var input = new DependencyHonourInput(featuresInScope, HasPremiumLicence: false, NobodyIgnoresTheirs);
            var policy = new DependencyHonourPolicy();

            var asDecided = AsRead(policy.Evaluate(input));
            var asDecidedAgain = AsRead(policy.Evaluate(input));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asDecidedAgain, Is.EqualTo(asDecided));
                Assert.That(featuresInScope, Has.Count.EqualTo(4));
            }
        }

        /// <summary>
        /// The cheapest form of the guarantee the whole design rests on: two answers to "can Lighthouse act on
        /// this dependency" is worse than either answer alone, because the screen then becomes evidence for
        /// something untrue.
        /// </summary>
        [Test]
        public void OneTypeAndOnlyOne_DecidesWhetherADependencyCanBeActedOn()
        {
            var deciders = typeof(IDependencyHonourPolicy).Assembly
                .GetTypes()
                .Where(candidate => candidate.IsClass
                    && !candidate.IsAbstract
                    && typeof(IDependencyHonourPolicy).IsAssignableFrom(candidate))
                .Select(candidate => candidate.FullName)
                .ToList();

            Assert.That(deciders, Has.Count.EqualTo(1),
                "Found: " + string.Join(", ", deciders));
        }

        [Test]
        public void APortfolioThatSetsItsDependenciesAside_ActsOnNoneOfThemAndStillHasEveryOne()
        {
            var honoured = DecideWhile(
                TheSamePortfolio,
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(honoured.Verdicts, Has.Count.EqualTo(1));
                Assert.That(honoured.Verdicts.Single().Reason, Is.EqualTo(NotHonouredReason.IgnoredByPortfolio));
            }
        }

        /// <summary>
        /// The reason for the whole switch: a choice somebody made is not a broken link, and a warning on
        /// every Feature in the Portfolio would teach the reader to stop looking at the column.
        /// </summary>
        [Test]
        public void ADependencySetAside_IsTheOneReasonNobodyIsWarnedAbout()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(AVerdict(NotHonouredReason.IgnoredByPortfolio, blockerPositionedBelow: false).IsWorthWarningAbout, Is.False);
                Assert.That(AVerdict(NotHonouredReason.IgnoredByPortfolio, blockerPositionedBelow: true).IsWorthWarningAbout, Is.False);
                Assert.That(AVerdict(NotHonouredReason.InALoop, blockerPositionedBelow: false).IsWorthWarningAbout, Is.True);
                Assert.That(AVerdict(NotHonouredReason.OutsideThisPortfolio, blockerPositionedBelow: false).IsWorthWarningAbout, Is.True);
                Assert.That(AVerdict(NotHonouredReason.BlockerCannotBeForecast, blockerPositionedBelow: false).IsWorthWarningAbout, Is.True);
                Assert.That(AVerdict(reason: null, blockerPositionedBelow: true).IsWorthWarningAbout, Is.True);
                Assert.That(AVerdict(reason: null, blockerPositionedBelow: false).IsWorthWarningAbout, Is.False);
            }
        }

        [Test]
        public void SettingDependenciesAside_QuietensTheWarningAboutOneWaitedOnFromOutsideThePortfolio()
        {
            var honoured = DecideWhile(
                TheSamePortfolio,
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: SomethingNotHeldHere));

            Assert.That(TheVerdictFor(honoured, "F-1", "F-99").Reason, Is.EqualTo(NotHonouredReason.IgnoredByPortfolio));
        }

        /// <summary>
        /// A Feature can belong to several Portfolios. One Portfolio trying out a different order must not
        /// decide what another Portfolio is allowed to see.
        /// </summary>
        [Test]
        public void ADependencyAnotherPortfolioStillHonours_KeepsTheVerdictItHad()
        {
            var honoured = DecideWhile(
                TheSamePortfolio,
                AFeature("F-1", position: 1, portfolioIds: BothPortfolios, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: BothPortfolios));

            Assert.That(TheVerdictFor(honoured, "F-1", "F-2").IsHonoured, Is.True);
        }

        [Test]
        public void ADependencyEveryPortfolioHoldingBothEndsHasSetAside_ReadsAsSetAside()
        {
            var honoured = DecideWhile(
                BothPortfolios,
                AFeature("F-1", position: 1, portfolioIds: BothPortfolios, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: BothPortfolios));

            Assert.That(TheVerdictFor(honoured, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.IgnoredByPortfolio));
        }

        /// <summary>
        /// The loop check is what stops a forecast running forever, so setting dependencies aside switches
        /// off what is acted on and never what is looked for. The verdict a Feature carries the moment the
        /// switch goes back off has to be the one it would have had all along.
        /// </summary>
        [Test]
        public void ALoopIsStillFoundWhileTheDependenciesAreSetAside_AndReadsAsALoopAgainAsSoonAsTheyAreNot()
        {
            FeatureDependencyFacts[] twoWaitingOnEachOther =
            [
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst),
            ];

            var whileSetAside = DecideWhile(TheSamePortfolio, twoWaitingOnEachOther);
            var onceHonouredAgain = DecideWhile(NobodyIgnoresTheirs, twoWaitingOnEachOther);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileSetAside.Verdicts.Select(verdict => verdict.Reason),
                    Has.All.EqualTo(NotHonouredReason.IgnoredByPortfolio));
                Assert.That(onceHonouredAgain.Verdicts.Select(verdict => verdict.Reason),
                    Has.All.EqualTo(NotHonouredReason.InALoop));
            }
        }

        [Test]
        public void PuttingTheSwitchBack_RestoresEveryVerdictItWouldHaveHad()
        {
            FeatureDependencyFacts[] aPortfolioWithOneOfEverything =
            [
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheOtherThree),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst),
                AFeature("F-3", position: 3, portfolioIds: AnotherPortfolio),
                AFeature("F-4", position: 4, portfolioIds: TheSamePortfolio, canBeForecast: false),
            ];

            var beforeTheSwitch = AsRead(DecideWhile(NobodyIgnoresTheirs, aPortfolioWithOneOfEverything));
            var whileSetAside = AsRead(DecideWhile(TheSamePortfolio, aPortfolioWithOneOfEverything));
            var afterTheSwitchGoesBack = AsRead(DecideWhile(NobodyIgnoresTheirs, aPortfolioWithOneOfEverything));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheSwitchGoesBack, Is.EqualTo(beforeTheSwitch));
                Assert.That(whileSetAside, Is.Not.EqualTo(beforeTheSwitch));
            }
        }

        private static DependencyVerdict AVerdict(NotHonouredReason? reason, bool blockerPositionedBelow)
        {
            return new DependencyVerdict("F-1", "F-2", reason, blockerPositionedBelow);
        }

        private static HonouredDependencies Decide(params FeatureDependencyFacts[] featuresInScope)
        {
            return DecideWhile(NobodyIgnoresTheirs, featuresInScope);
        }

        private static HonouredDependencies DecideWhile(
            int[] portfoliosSettingTheirDependenciesAside, params FeatureDependencyFacts[] featuresInScope)
        {
            return new DependencyHonourPolicy().Evaluate(
                new DependencyHonourInput(featuresInScope, HasPremiumLicence: false, portfoliosSettingTheirDependenciesAside));
        }

        private static FeatureDependencyFacts AFeature(
            string referenceId,
            int? position,
            int[] portfolioIds,
            bool canBeForecast = true,
            string[]? waitingOn = null)
        {
            return new FeatureDependencyFacts(referenceId, portfolioIds, position, canBeForecast, waitingOn ?? NothingWaitedOn);
        }

        private static DependencyVerdict TheVerdictFor(
            HonouredDependencies honoured,
            string dependentReferenceId,
            string blockerReferenceId)
        {
            return honoured.Verdicts.Single(verdict =>
                verdict.DependentReferenceId == dependentReferenceId
                && verdict.BlockerReferenceId == blockerReferenceId);
        }

        private static List<string> AsRead(HonouredDependencies honoured)
        {
            return honoured.Verdicts
                .Select(verdict => string.Join(
                    "|",
                    verdict.DependentReferenceId,
                    verdict.BlockerReferenceId,
                    verdict.Reason?.ToString() ?? "nothing wrong",
                    verdict.BlockerPositionedBelow))
                .ToList();
        }
    }
}
