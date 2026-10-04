using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementViewQuery(
        IRepository<Team> teamRepository,
        RefinementList refinementList,
        SleYardstickResolver yardstickResolver,
        ISizingLogRepository sizingLog,
        VoterIdentityResolver voterIdentityResolver) : IRefinementViewQuery
    {
        public RefinementView? ForTeam(int teamId, string? presentedVoterKey)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return null;
            }

            var voterIdentity = voterIdentityResolver.Kind;
            if (!team.HasRefinementStates)
            {
                return new RefinementView(false, [], Yardstick.None, voterIdentity);
            }

            var workItems = refinementList.For(team);
            var votes = CurrentVotesOn(team.Id, workItems, voterIdentityResolver.ReaderKeyFrom(presentedVoterKey));
            var readiness = team.RefinementSettings?.Readiness ?? new ReadinessSetting();
            var rows = workItems
                .Select(item => RowFor(item, votes.GetValueOrDefault(item.ReferenceId, RowVotes.None), readiness))
                .ToList();

            return new RefinementView(true, rows, yardstickResolver.For(team), voterIdentity);
        }

        private static RefinementRow RowFor(WorkItem item, RowVotes votes, ReadinessSetting readiness)
            => new(item, votes, RefinementResolution.StandingOf(votes.Split, readiness));

        private Dictionary<string, RowVotes> CurrentVotesOn(int teamId, List<WorkItem> workItems, string? readerKey)
        {
            List<string> references = [.. workItems.Select(item => item.ReferenceId).Distinct(StringComparer.Ordinal)];

            return sizingLog.ReadForTeam(teamId, references)
                .GroupBy(entry => entry.WorkItemReferenceId, StringComparer.Ordinal)
                .ToDictionary(entries => entries.Key, entries => RefinementResolution.VotesOn(entries, readerKey), StringComparer.Ordinal);
        }
    }
}
