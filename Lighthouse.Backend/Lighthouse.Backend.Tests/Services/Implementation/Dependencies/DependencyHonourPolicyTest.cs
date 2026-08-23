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
            NotHonouredReason.NotLicensed,
        ];

        private static readonly int[] TheSamePortfolio = [1];

        private static readonly int[] TheSameTeam = [7];

        private static readonly int[] AnotherTeam = [8];

        private static readonly int[] SeveralTeams = [7, 8, 9];

        private static readonly int[] NoTeamAtAll = [];

        private static readonly int[] AnotherPortfolio = [2];

        private static readonly int[] BothPortfolios = [1, 2];

        private static readonly int[] NobodyIgnoresTheirs = [];

        private static readonly int[] NoPortfolioAtAll = [];

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

        /// <summary>
        /// Where something sits is only worth saying about a dependency Lighthouse could otherwise act on.
        /// Reporting the order of a Feature this Portfolio cannot see would be telling the reader to
        /// re-order around something they have no way of finding.
        /// </summary>
        [Test]
        public void AFeatureWaitingOnOneOutsideThePortfolio_IsToldNothingAboutWhereThatOneSits()
        {
            var honoured = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: AnotherPortfolio));

            var verdict = TheVerdictFor(honoured, "F-1", "F-2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Reason, Is.EqualTo(NotHonouredReason.OutsideThisPortfolio));
                Assert.That(verdict.BlockerPositionedBelow, Is.False,
                    "F-2 does sit lower down, and saying so would point the reader at a Feature they cannot reach.");
            }
        }

        /// <summary>
        /// A Portfolio holding only the waiting end has no say: the dependency has no consequence there,
        /// because Lighthouse never acts on one whose other end it cannot see. Letting it vote would leave
        /// a Portfolio unable to set aside its own dependencies while any other Portfolio holds one end.
        /// </summary>
        [Test]
        public void OnlyThePortfoliosHoldingBothEnds_DecideWhetherADependencyIsSetAside()
        {
            var honoured = DecideWhile(
                TheSamePortfolio,
                AFeature("F-1", position: 1, portfolioIds: BothPortfolios, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(honoured, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.IgnoredByPortfolio),
                "Only the Portfolio holding both of them can act on this, and it has set its dependencies aside.");
        }

        /// <summary>
        /// A Feature in no Portfolio at all is nobody's to set aside. Reading an empty set of deciders as
        /// unanimous agreement would report it as a deliberate choice somebody made, when nobody made one.
        /// </summary>
        [Test]
        public void AFeatureInNoPortfolio_IsNotSetAsideByAnybody()
        {
            var honoured = DecideWhile(
                TheSamePortfolio,
                AFeature("F-1", position: 1, portfolioIds: NoPortfolioAtAll, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(honoured, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.OutsideThisPortfolio));
        }

        /// <summary>
        /// Every Team is now forecast on one clock, so a run does have a moment at which it can see another
        /// Team finish something. A wait between two Teams is a wait like any other and nothing stands
        /// against it - which is the whole of what this Epic set out to deliver.
        /// </summary>
        [Test]
        public void ABlockerAnotherTeamIsWorking_IsAWaitLikeAnyOther()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: TheSameTeam),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, teamIds: AnotherTeam));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").IsHonoured, Is.True);
        }

        /// <summary>
        /// A Feature several Teams are still working is finished when the last of them is done, not the
        /// first. On one clock that is something a run can see, so the wait is acted on rather than left
        /// out; that it is the last and not the first is what the simulation itself has to get right.
        /// </summary>
        [Test]
        public void ABlockerSeveralTeamsAreWorking_IsStillAWaitLighthouseActsOn()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: TheSameTeam),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, teamIds: SeveralTeams));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").IsHonoured, Is.True);
        }

        [Test]
        public void AFeatureSeveralTeamsAreWorking_WaitsOnSomethingLighthouseActsOn()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: SeveralTeams),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, teamIds: TheSameTeam));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").IsHonoured, Is.True);
        }

        /// <summary>
        /// Nobody is working it, so there is nothing in the run to wait for and the wait holds nothing up.
        /// </summary>
        [Test]
        public void ABlockerNoTeamIsWorking_HoldsNothingUpAndIsLeftAlone()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: TheSameTeam),
                AFeature("F-2", position: 0, portfolioIds: TheSamePortfolio, teamIds: NoTeamAtAll));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").HasNothingWrongWithIt, Is.True);
        }

        [Test]
        public void TwoFeaturesNoTeamIsWorking_HaveNothingWrongBetweenThem()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: NoTeamAtAll),
                AFeature("F-2", position: 0, portfolioIds: TheSamePortfolio, teamIds: NoTeamAtAll));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").HasNothingWrongWithIt, Is.True);
        }

        /// <summary>
        /// A circle between two Teams is still a circle. Nothing about the Teams involved changes what is
        /// wrong with it or what the user has to go and fix.
        /// </summary>
        [Test]
        public void ACircleBetweenTwoTeams_IsStillReportedAsACircle()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: TheSameTeam),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst, teamIds: AnotherTeam));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.InALoop));
        }

        /// <summary>
        /// The cheapest circle there is, and the one that costs most if it gets through: a forecast told to
        /// wait for a Feature to finish before that same Feature may be worked on has nothing it can ever
        /// start. Everything downstream of this decision assumes the waits it is given lead somewhere, so
        /// this is where that assumption is actually paid for.
        /// </summary>
        [Test]
        public void AFeatureRecordedAsWaitingOnItself_IsNeverOneLighthouseActsOn()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheFirst));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheVerdictFor(verdicts, "F-1", "F-1").IsHonoured, Is.False);
                Assert.That(TheVerdictFor(verdicts, "F-1", "F-1").Reason, Is.EqualTo(NotHonouredReason.InALoop));
            }
        }

        [Test]
        public void ADependencyAnUnlicensedInstanceCouldOtherwiseHaveActedOn_SaysThatIsWhatIsMissing()
        {
            var verdicts = DecideUnlicensed(
                AFeature("F-1", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 1, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.NotLicensed));
        }

        /// <summary>
        /// A licence would not have accounted for this wait either. Naming the licence here sells a reader a
        /// date that would not move, which is worse than saying nothing.
        /// </summary>
        [TestCase(NotHonouredReason.InALoop)]
        [TestCase(NotHonouredReason.BlockerCannotBeForecast)]
        public void ADependencyNoLicenceWouldHaveAccountedFor_SaysWhatIsActuallyWrongWithIt(
            NotHonouredReason whatIsActuallyWrong)
        {
            var verdicts = DecideUnlicensed(TheTwoFeaturesThatAre(whatIsActuallyWrong));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(whatIsActuallyWrong));
        }

        /// <summary>
        /// Somebody chose to set these aside, and that choice outlives what the instance has paid for. Told
        /// the licence was what was missing, they would go and buy back a thing they switched off.
        /// </summary>
        [Test]
        public void ADependencySetAsideOnAnUnlicensedInstance_IsStillJustSetAside()
        {
            var verdicts = Decide(
                hasPremiumLicence: false,
                TheSamePortfolio,
                AFeature("F-1", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 1, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").Reason, Is.EqualTo(NotHonouredReason.IgnoredByPortfolio));
        }

        [Test]
        public void ADependencyHeldBackOnlyByTheLicence_IsStillWorthTellingTheReaderAbout()
        {
            var verdicts = DecideUnlicensed(
                AFeature("F-1", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 1, portfolioIds: TheSamePortfolio));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").IsWorthWarningAbout, Is.True);
        }

        private static FeatureDependencyFacts[] TheTwoFeaturesThatAre(NotHonouredReason whatIsActuallyWrong)
        {
            if (whatIsActuallyWrong == NotHonouredReason.InALoop)
            {
                return
                [
                    AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                    AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheFirst),
                ];
            }

            return
            [
                AFeature("F-1", position: 1, portfolioIds: TheSamePortfolio, waitingOn: TheSecond),
                AFeature("F-2", position: 2, portfolioIds: TheSamePortfolio, canBeForecast: false),
            ];
        }

        [Test]
        public void ABlockerTheSameOneTeamIsWorking_IsOneLighthouseActsOn()
        {
            var verdicts = Decide(
                AFeature("F-1", position: 2, portfolioIds: TheSamePortfolio, waitingOn: TheSecond, teamIds: TheSameTeam),
                AFeature("F-2", position: 1, portfolioIds: TheSamePortfolio, teamIds: TheSameTeam));

            Assert.That(TheVerdictFor(verdicts, "F-1", "F-2").IsHonoured, Is.True);
        }

        private static DependencyVerdict AVerdict(NotHonouredReason? reason, bool blockerPositionedBelow)
        {
            return new DependencyVerdict("F-1", "F-2", reason, blockerPositionedBelow);
        }

        /// <summary>
        /// Licensed, unless a test is about what an instance has paid for. Unlicensed is not a neutral
        /// starting point any more: it is a reason in its own right, and every scenario here that is about
        /// something else would end up reading it instead of the thing it set up.
        /// </summary>
        private static HonouredDependencies Decide(params FeatureDependencyFacts[] featuresInScope)
        {
            return DecideWhile(NobodyIgnoresTheirs, featuresInScope);
        }

        private static HonouredDependencies DecideWhile(
            int[] portfoliosSettingTheirDependenciesAside, params FeatureDependencyFacts[] featuresInScope)
        {
            return Decide(hasPremiumLicence: true, portfoliosSettingTheirDependenciesAside, featuresInScope);
        }

        private static HonouredDependencies DecideUnlicensed(params FeatureDependencyFacts[] featuresInScope)
        {
            return Decide(hasPremiumLicence: false, NobodyIgnoresTheirs, featuresInScope);
        }

        private static HonouredDependencies Decide(
            bool hasPremiumLicence,
            int[] portfoliosSettingTheirDependenciesAside,
            params FeatureDependencyFacts[] featuresInScope)
        {
            return new DependencyHonourPolicy().Evaluate(
                new DependencyHonourInput(featuresInScope, hasPremiumLicence, portfoliosSettingTheirDependenciesAside));
        }

        private static FeatureDependencyFacts AFeature(
            string referenceId,
            int? position,
            int[] portfolioIds,
            bool canBeForecast = true,
            string[]? waitingOn = null,
            int[]? teamIds = null)
        {
            return new FeatureDependencyFacts(
                referenceId, portfolioIds, teamIds ?? TheSameTeam, position, canBeForecast, waitingOn ?? NothingWaitedOn);
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
