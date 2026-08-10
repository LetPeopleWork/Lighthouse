using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Which work items a refresh handed to storage to be written, by reference id and in order.
    ///
    /// Comparing an issue's stored values before and after a cycle cannot tell "it was never written"
    /// from "it was rewritten with what it already said": for an issue that did not move on the tracker,
    /// every write on the refresh path re-applies the stored truth, so the comparison holds either way.
    /// The two are only distinguishable at the write path itself, which is why this records the calls
    /// rather than their effect (Epic #5687, AC-2.4).
    /// </summary>
    public sealed class CapturedWorkItemWrites
    {
        private readonly List<string> written = [];
        private readonly Lock gate = new();

        public void Record(string referenceId)
        {
            lock (gate)
            {
                written.Add(referenceId);
            }
        }

        /// <summary>
        /// Forgets the calls so far. Seeding writes through the same port, and every promise in this epic
        /// is about what ONE cycle wrote - a chained scenario runs more than one.
        /// </summary>
        public void Clear()
        {
            lock (gate)
            {
                written.Clear();
            }
        }

        public List<string> ReferenceIds
        {
            get
            {
                lock (gate)
                {
                    return [.. written];
                }
            }
        }
    }

    /// <summary>
    /// Records every work item handed to <see cref="IWorkItemRepository"/> to be added or updated, and
    /// delegates everything - including the write itself - to the real repository. Registered in place of
    /// the production registration but wrapping it, so the adapter under observation is still the real
    /// one: this is an observation seam on a driven port, not a fake.
    /// </summary>
    public sealed class WriteRecordingWorkItemRepository(IWorkItemRepository inner, CapturedWorkItemWrites captured)
        : IWorkItemRepository
    {
        public void Add(WorkItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            captured.Record(item.ReferenceId);
            inner.Add(item);
        }

        public void Update(WorkItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            captured.Record(item.ReferenceId);
            inner.Update(item);
        }

        public void ApplyConcurrencyTokenForEdit(IConcurrencyTokenEntity entity, Guid clientToken)
            => inner.ApplyConcurrencyTokenForEdit(entity, clientToken);

        public IEnumerable<WorkItem> GetAll() => inner.GetAll();

        public IQueryable<WorkItem> GetAllByPredicate(Expression<Func<WorkItem, bool>> predicate)
            => inner.GetAllByPredicate(predicate);

        public WorkItem? GetById(int id) => inner.GetById(id);

        public WorkItem? GetByPredicate(Func<WorkItem, bool> predicate) => inner.GetByPredicate(predicate);

        public void Remove(int id) => inner.Remove(id);

        public void Remove(WorkItem? item) => inner.Remove(item);

        public void RemoveWorkItemsForTeam(int teamId) => inner.RemoveWorkItemsForTeam(teamId);

        public bool Exists(int id) => inner.Exists(id);

        public bool Exists(Func<WorkItem, bool> predicate) => inner.Exists(predicate);

        public Task Save() => inner.Save();
    }
}
