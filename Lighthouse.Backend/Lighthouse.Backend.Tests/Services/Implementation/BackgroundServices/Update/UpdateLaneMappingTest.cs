using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Every kind of update knows which of the three lanes it runs in, and the answer lives in one place.
    ///
    /// Two lanes worth of care sit in the delete cases. A delete runs in the same lane as a refresh of the
    /// same kind of entity, because the two would otherwise be writing one entity at the same time; and a
    /// delete is awaited by a caller holding an HTTP response open, so it must not queue behind unrelated
    /// work either.
    /// </summary>
    [TestFixture]
    public class UpdateLaneMappingTest
    {
        private static readonly UpdateType[] EveryUpdateType = Enum.GetValues<UpdateType>();

        [TestCaseSource(nameof(EveryUpdateType))]
        public void EveryKindOfUpdate_IsAssignedALane(UpdateType updateType)
        {
            var lane = UpdateLaneMapping.LaneOf(updateType);

            Assert.That(Enum.IsDefined(lane), Is.True,
                $"{updateType} was not given one of the three lanes - a new update type has to be added to the lane mapping.");
        }

        [Test]
        public void RefreshingWork_RunsInTheLaneOfTheThingItRefreshes()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(UpdateLaneMapping.LaneOf(UpdateType.Team), Is.EqualTo(UpdateLane.Team));
                Assert.That(UpdateLaneMapping.LaneOf(UpdateType.Features), Is.EqualTo(UpdateLane.Portfolio));
                Assert.That(UpdateLaneMapping.LaneOf(UpdateType.Forecasts), Is.EqualTo(UpdateLane.Forecast));
            }
        }

        [Test]
        public void DeletingATeam_RunsInTheTeamLane()
        {
            Assert.That(UpdateLaneMapping.LaneOf(UpdateType.TeamDelete), Is.EqualTo(UpdateLane.Team));
        }

        [Test]
        public void DeletingAPortfolio_RunsInThePortfolioLane()
        {
            Assert.That(UpdateLaneMapping.LaneOf(UpdateType.PortfolioDelete), Is.EqualTo(UpdateLane.Portfolio));
        }
    }
}
