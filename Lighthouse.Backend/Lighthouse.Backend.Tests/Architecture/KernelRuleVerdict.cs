namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// What a rule looked at, and which of those types broke it. <paramref name="Breaks"/> says how an offender
    /// relates to the names listed after it, so a failure reads as a sentence.
    /// </summary>
    internal sealed record KernelRuleVerdict(IReadOnlyList<string> Examined, IReadOnlyList<IlDependent> Offenders, string Rule, string Breaks = "depends on")
    {
        /// <summary>The types the rule was told to look at by name but could not find in the module.</summary>
        public IReadOnlyList<string> MissingSubjects { get; init; } = [];

        public void Check()
        {
            if (Offenders.Count == 0 && MissingSubjects.Count == 0)
            {
                return;
            }

            var offenders = Offenders.Select(offender => $"{offender.TypeName} {Breaks} {string.Join(", ", offender.Targets)}");
            var missing = MissingSubjects.Select(subject =>
                $"{subject} is named by the rule but does not exist. A named type that was renamed or moved leaves the rule " +
                "checking fewer types, possibly none, while it stays green. Put the type's current full name in the rule's list.");

            Assert.Fail($"{Rule}{Environment.NewLine}{string.Join(Environment.NewLine, offenders.Concat(missing))}");
        }
    }
}
