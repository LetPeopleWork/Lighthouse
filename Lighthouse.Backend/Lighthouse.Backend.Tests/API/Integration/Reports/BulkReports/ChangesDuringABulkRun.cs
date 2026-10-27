using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// What changes on the instance while one bulk request is running, at the moment the run reaches a given
    /// owner. No real clock can make a deletion, a lost Admin right or a lapsing licence land between two owners
    /// of one request, so the scenario says what happens when the run asks whether the caller may write to that
    /// owner, or right after the run has looked that owner up.
    ///
    /// Each change happens once. A scenario that arms nothing leaves the host exactly as production wires it.
    /// </summary>
    public sealed class ChangesDuringABulkRun
    {
        private readonly ConcurrentDictionary<(ReportOwnerKind Kind, int Id), Func<bool?>> whenAskedToWrite = new();

        private readonly ConcurrentDictionary<(ReportOwnerKind Kind, int Id), Action> rightAfterTheLookup = new();

        public void Forget()
        {
            whenAskedToWrite.Clear();
            rightAfterTheLookup.Clear();
        }

        /// <summary>
        /// When the run asks whether the caller may write to the owner, <paramref name="change"/> runs first. It
        /// answers in the caller's place (false: no longer Admin), or null to let the real answer stand.
        /// </summary>
        public void WhenTheRunReaches(ReportOwnerKind kind, int id, Func<bool?> change) => whenAskedToWrite[(kind, id)] = change;

        /// <summary>Right after the run found the owner, so the owner is gone by the time its report is saved.</summary>
        public void RightAfterTheRunLooksUp(ReportOwnerKind kind, int id, Action change) => rightAfterTheLookup[(kind, id)] = change;

        internal bool? Reached(ReportOwnerKind kind, int id)
            => whenAskedToWrite.TryRemove((kind, id), out var change) ? change() : null;

        internal void LookedUp(ReportOwnerKind kind, int id)
        {
            if (rightAfterTheLookup.TryRemove((kind, id), out var change))
            {
                change();
            }
        }
    }

    /// <summary>
    /// The instance's RBAC answers as the bulk run sees them: every question goes to the real service, except a
    /// write question about an owner the scenario has armed a change for.
    /// </summary>
    public class RbacSeenByABulkRun : DispatchProxy
    {
        private IRbacAdministrationService inner = null!;
        private ChangesDuringABulkRun changes = null!;

        public static IRbacAdministrationService Around(IRbacAdministrationService inner, ChangesDuringABulkRun changes)
        {
            var proxy = Create<IRbacAdministrationService, RbacSeenByABulkRun>();
            var seen = (RbacSeenByABulkRun)(object)proxy;
            seen.inner = inner;
            seen.changes = changes;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            var owner = targetMethod.Name switch
            {
                nameof(IRbacAdministrationService.CanWriteTeamAsync) => (ReportOwnerKind.Team, (int)args![1]!),
                nameof(IRbacAdministrationService.CanWritePortfolioAsync) => (ReportOwnerKind.Portfolio, (int)args![1]!),
                _ => ((ReportOwnerKind, int)?)null,
            };

            if (owner is var (kind, id) && changes.Reached(kind, id) is bool answer)
            {
                return Task.FromResult(answer);
            }

            return Forward(targetMethod, inner, args);
        }

        internal static object? Forward(MethodInfo targetMethod, object inner, object?[]? args)
        {
            try
            {
                return targetMethod.Invoke(inner, args);
            }
            catch (TargetInvocationException thrown) when (thrown.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(thrown.InnerException).Throw();
                throw;
            }
        }
    }

    /// <summary>
    /// The Reports module's owner lookup as the bulk run sees it: the real lookup, followed by whatever the
    /// scenario armed for that owner, so an owner can disappear between being found and its report being saved.
    /// </summary>
    public class OwnerLookupSeenByABulkRun : DispatchProxy
    {
        private IReportOwnerLookup inner = null!;
        private ChangesDuringABulkRun changes = null!;

        public static IReportOwnerLookup Around(IReportOwnerLookup inner, ChangesDuringABulkRun changes)
        {
            var proxy = Create<IReportOwnerLookup, OwnerLookupSeenByABulkRun>();
            var seen = (OwnerLookupSeenByABulkRun)(object)proxy;
            seen.inner = inner;
            seen.changes = changes;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            var answer = RbacSeenByABulkRun.Forward(targetMethod, inner, args);

            if (targetMethod.Name == nameof(IReportOwnerLookup.Find) && answer is not null && args?[0] is ReportOwner owner)
            {
                changes.LookedUp(owner.Kind, owner.Id);
            }

            return answer;
        }
    }
}
