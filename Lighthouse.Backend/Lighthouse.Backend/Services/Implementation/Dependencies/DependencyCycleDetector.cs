using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Finds the groups of Features that wait on one another in a circle, over the Features the caller has
    /// already loaded and nothing else. It reaches nothing - no repository, no database, no log - which is
    /// what lets a screen showing a warning and anything later acting on the same dependency read one answer.
    /// </summary>
    public sealed class DependencyCycleDetector
    {
        private readonly IReadOnlyCollection<FeatureDependencyFacts> featuresInScope;

        public DependencyCycleDetector(IReadOnlyCollection<FeatureDependencyFacts> featuresInScope)
        {
            this.featuresInScope = featuresInScope;
        }

        /// <summary>
        /// The walk is a loop over an explicit stack rather than a recursive call per hop, and that is not a
        /// style preference. This runs inside the background refresh, where one long chain of Features waiting
        /// on one another would be a stack overflow - and a stack overflow cannot be caught, so it takes the
        /// whole process down rather than failing one refresh. Do not rewrite it recursively.
        /// </summary>
        public DependencyLoops Detect()
        {
            var waitsOn = WhatEachFeatureWaitsOnAmongTheseFeatures(featuresInScope);

            return new DependencyLoops(CirclesIn(waitsOn));
        }

        /// <summary>
        /// A reference to something Lighthouse does not hold is dropped here rather than tested for later: a
        /// Feature in another Portfolio, or a tracker record never imported, is a real edge but not one that
        /// can close a circle among the Features in front of us.
        /// </summary>
        private static Dictionary<string, List<string>> WhatEachFeatureWaitsOnAmongTheseFeatures(
            IReadOnlyCollection<FeatureDependencyFacts> featuresInScope)
        {
            var held = featuresInScope.Select(feature => feature.ReferenceId).ToHashSet();

            return featuresInScope
                .GroupBy(feature => feature.ReferenceId)
                .ToDictionary(
                    byReferenceId => byReferenceId.Key,
                    byReferenceId => byReferenceId
                        .SelectMany(feature => feature.DependsOnReferenceIds)
                        .Where(held.Contains)
                        .Distinct()
                        .ToList());
        }

        private static List<DependencyLoop> CirclesIn(Dictionary<string, List<string>> waitsOn)
        {
            var circles = new List<DependencyLoop>();
            var walk = new CircleWalk(waitsOn);

            foreach (var feature in waitsOn.Keys)
            {
                circles.AddRange(walk.From(feature));
            }

            return circles;
        }

        /// <summary>
        /// Tarjan's strongly connected components, kept as one object so the bookkeeping that has to survive
        /// between starting points - which Features have been seen, and in what order - is not passed around
        /// as six arguments.
        /// </summary>
        private sealed class CircleWalk
        {
            private readonly Dictionary<string, List<string>> waitsOn;
            private readonly Dictionary<string, int> orderSeen = [];
            private readonly Dictionary<string, int> earliestReachable = [];
            private readonly Stack<string> notYetGrouped = new();
            private readonly HashSet<string> awaitingAGroup = [];
            private int seenSoFar;

            public CircleWalk(Dictionary<string, List<string>> waitsOn)
            {
                this.waitsOn = waitsOn;
            }

            public List<DependencyLoop> From(string start)
            {
                var circles = new List<DependencyLoop>();

                if (orderSeen.ContainsKey(start))
                {
                    return circles;
                }

                var toExplore = new Stack<HopInProgress>();
                Arrive(start);
                toExplore.Push(new HopInProgress(start));

                while (toExplore.Count > 0)
                {
                    var hop = toExplore.Pop();
                    var blockers = waitsOn[hop.Feature];

                    if (hop.NextBlocker < blockers.Count)
                    {
                        var blocker = blockers[hop.NextBlocker];
                        toExplore.Push(hop.MovedPastThisBlocker());
                        StepInto(blocker, toExplore);
                        continue;
                    }

                    if (earliestReachable[hop.Feature] == orderSeen[hop.Feature])
                    {
                        var members = GroupClosedBy(hop.Feature);
                        if (IsACircle(members))
                        {
                            circles.Add(new DependencyLoop(members));
                        }
                    }

                    CarryBackTo(toExplore, hop.Feature);
                }

                return circles;
            }

            private void StepInto(string blocker, Stack<HopInProgress> toExplore)
            {
                if (!orderSeen.ContainsKey(blocker))
                {
                    Arrive(blocker);
                    toExplore.Push(new HopInProgress(blocker));
                }
                else if (awaitingAGroup.Contains(blocker))
                {
                    var waiting = toExplore.Peek().Feature;
                    earliestReachable[waiting] = Math.Min(earliestReachable[waiting], orderSeen[blocker]);
                }
            }

            private void Arrive(string feature)
            {
                orderSeen[feature] = seenSoFar;
                earliestReachable[feature] = seenSoFar;
                seenSoFar++;
                notYetGrouped.Push(feature);
                awaitingAGroup.Add(feature);
            }

            private void CarryBackTo(Stack<HopInProgress> toExplore, string finished)
            {
                if (toExplore.Count == 0)
                {
                    return;
                }

                var waiting = toExplore.Peek().Feature;
                earliestReachable[waiting] = Math.Min(earliestReachable[waiting], earliestReachable[finished]);
            }

            private List<string> GroupClosedBy(string feature)
            {
                var members = new List<string>();

                string member;
                do
                {
                    member = notYetGrouped.Pop();
                    awaitingAGroup.Remove(member);
                    members.Add(member);
                }
                while (member != feature);

                return members;
            }

            /// <summary>
            /// A group of one is only a circle if that Feature names itself. Every other Feature forms a group
            /// of one too, simply by being reachable from itself in no hops, and none of those is anything wrong.
            /// </summary>
            private bool IsACircle(List<string> members)
            {
                return members.Count > 1 || waitsOn[members[0]].Contains(members[0]);
            }

            private sealed record HopInProgress(string Feature, int NextBlocker = 0)
            {
                public HopInProgress MovedPastThisBlocker() => this with { NextBlocker = NextBlocker + 1 };
            }
        }
    }

    /// <summary>
    /// Every circle found in one pass, plus the two questions a caller actually has: is this Feature caught in
    /// one, and which other Features is it waiting on the way round.
    /// </summary>
    public sealed class DependencyLoops
    {
        private readonly List<DependencyLoop> loops;
        private readonly Dictionary<string, DependencyLoop> loopEachMemberIsIn;

        public DependencyLoops(IEnumerable<DependencyLoop> loops)
        {
            this.loops = loops.ToList();
            loopEachMemberIsIn = this.loops
                .SelectMany(loop => loop.MemberReferenceIds.Select(member => (Member: member, Loop: loop)))
                .ToDictionary(entry => entry.Member, entry => entry.Loop);
        }

        public IReadOnlyCollection<DependencyLoop> Loops => loops;

        public bool IsInALoop(string referenceId) => loopEachMemberIsIn.ContainsKey(referenceId);

        public IReadOnlyCollection<string> OthersInTheLoopWith(string referenceId)
        {
            if (!loopEachMemberIsIn.TryGetValue(referenceId, out var loop))
            {
                return [];
            }

            return loop.MemberReferenceIds.Where(member => member != referenceId).ToList();
        }
    }

    /// <summary>
    /// One circle. Every Feature named here is waiting, however many hops away, on every other Feature named
    /// here - including the case of a single Feature that names itself.
    /// </summary>
    public sealed class DependencyLoop
    {
        private readonly List<string> memberReferenceIds;

        public DependencyLoop(IEnumerable<string> memberReferenceIds)
        {
            this.memberReferenceIds = memberReferenceIds.ToList();
        }

        public IReadOnlyCollection<string> MemberReferenceIds => memberReferenceIds;
    }
}
