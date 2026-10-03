using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public static class SleYardstickResolver
    {
        public static Yardstick For(Team team)
        {
            // A probability without a number of days, or days without a probability, promises nothing.
            if (team.ServiceLevelExpectationProbability > 0 && team.ServiceLevelExpectationRange > 0)
            {
                return new Yardstick(YardstickSource.Sle, team.ServiceLevelExpectationRange, team.ServiceLevelExpectationProbability);
            }

            return Yardstick.None;
        }
    }
}
