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
            var rows = workItems
                .Select(item => new RefinementRow(item, votes.GetValueOrDefault(item.ReferenceId, RowVotes.None)))
                .ToList();

            return new RefinementView(true, rows, yardstickResolver.For(team), voterIdentity);
        }

        private Dictionary<string, RowVotes> CurrentVotesOn(int teamId, List<WorkItem> workItems, string? readerKey)
        {
            List<string> references = [.. workItems.Select(item => item.ReferenceId).Distinct(StringComparer.Ordinal)];

            return sizingLog.ReadForTeam(teamId, references)
                .Where(entry => entry.Kind != SizingEntryKind.Comment)
                .GroupBy(entry => entry.WorkItemReferenceId, StringComparer.Ordinal)
                .ToDictionary(entries => entries.Key, entries => Tally(entries, readerKey), StringComparer.Ordinal);
        }

        // Each voter counts once, with whatever they said last; a vote taken back leaves them uncounted.
        private static RowVotes Tally(IEnumerable<SizingLogEntry> entries, string? readerKey)
        {
            var current = entries
                .GroupBy(entry => entry.VoterKey, StringComparer.Ordinal)
                .Select(byVoter => byVoter.MaxBy(entry => entry.Id)!)
                .Where(latest => latest.Kind == SizingEntryKind.Vote)
                .ToList();

            var myVote = current.FirstOrDefault(entry => readerKey is not null && entry.VoterKey == readerKey)?.Answer;

            return new RowVotes(current.Count, myVote);
        }
    }
}
