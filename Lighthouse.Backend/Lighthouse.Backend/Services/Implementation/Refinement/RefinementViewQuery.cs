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
        VoterIdentityResolver voterIdentityResolver,
        StageRuleMatcher stageRuleMatcher,
        RefinementNeedCalculator needCalculator) : IRefinementViewQuery
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
                return new RefinementView(false, [], Yardstick.None, voterIdentity, needCalculator.For(team, 0).Need);
            }

            var workItems = refinementList.For(team);
            var logs = LogsOn(team.Id, workItems);
            var readerKey = voterIdentityResolver.ReaderKeyFrom(presentedVoterKey);
            var readiness = team.RefinementSettings?.Readiness ?? new ReadinessSetting();
            var stages = stageRuleMatcher.MatchFor(team, workItems);
            var rows = workItems
                .Select(item => RowFor(item, logs.GetValueOrDefault(item.ReferenceId, []), readerKey, readiness, stages))
                .ToList();

            var outlook = needCalculator.For(team, RefinementResolution.ReadyCountOf(stages.StagesConfigured, rows));

            return new RefinementView(true, rows, yardstickResolver.For(team), voterIdentity, outlook.Need, stages.StagesConfigured, outlook.Calendar);
        }

        public SizingLog? LogOf(int teamId, string workItemReference, string? presentedVoterKey)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null || !team.HasRefinementStates || !IsListed(team, workItemReference))
            {
                return null;
            }

            var readerKey = voterIdentityResolver.ReaderKeyFrom(presentedVoterKey);
            var log = sizingLog.ReadForTeam(team.Id, [workItemReference]).OrderBy(entry => entry.Id).ToList();
            var openQuestions = RefinementResolution.OpenQuestionsIn(log);

            return new SizingLog(
                [.. log.Select(entry => LineFor(entry, readerKey, openQuestions.Contains(entry.Id)))],
                RefinementResolution.VotersOn(log));
        }

        private bool IsListed(Team team, string workItemReference)
            => refinementList.For(team).Exists(item => string.Equals(item.ReferenceId, workItemReference, StringComparison.Ordinal));

        private static SizingLogLine LineFor(SizingLogEntry entry, string? readerKey, bool isOpenQuestion) => new(
            entry.Kind,
            entry.Answer,
            entry.Comment,
            entry.VoterDisplayName,
            entry.Channel,
            DateTime.SpecifyKind(entry.RecordedAt, DateTimeKind.Utc),
            readerKey is not null && string.Equals(entry.VoterKey, readerKey, StringComparison.Ordinal),
            isOpenQuestion);

        private static RefinementRow RowFor(WorkItem item, List<SizingLogEntry> log, string? readerKey, ReadinessSetting readiness, StageMatches stages)
        {
            var votes = RefinementResolution.VotesOn(log, readerKey);
            var standing = RefinementResolution.StandingOf(votes.Split, readiness);
            var stage = stages.StageOf(item);

            return new(item, votes, standing, stage, RefinementResolution.SignalsDisagree(stage, votes, standing))
            {
                Conversation = RefinementResolution.ConversationOn(log),
            };
        }

        private Dictionary<string, List<SizingLogEntry>> LogsOn(int teamId, List<WorkItem> workItems)
        {
            List<string> references = [.. workItems.Select(item => item.ReferenceId).Distinct(StringComparer.Ordinal)];

            return sizingLog.ReadForTeam(teamId, references)
                .GroupBy(entry => entry.WorkItemReferenceId, StringComparer.Ordinal)
                .ToDictionary(entries => entries.Key, entries => entries.ToList(), StringComparer.Ordinal);
        }
    }
}
