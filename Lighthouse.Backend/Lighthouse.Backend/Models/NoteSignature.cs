namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Who signed a note: a profile, or nobody on an instance that ran without authentication.
    /// </summary>
    public readonly record struct NoteSignature(int? SignerProfileId)
    {
        /// <summary>
        /// Deliberately two branches rather than comparing the two ids directly. Comparing them
        /// treats a note nobody signed as belonging to nobody, so on an instance that ran without
        /// authentication and later switched it on, every note written in between becomes permanently
        /// uneditable - visible to everyone and fixable by no one.
        /// </summary>
        public bool MayBeChangedBy(int? callerProfileId)
        {
            if (SignerProfileId is null)
            {
                // Nobody signed it, so nobody owns it. Write access is the whole rule.
                return true;
            }

            return callerProfileId is not null && SignerProfileId == callerProfileId;
        }
    }
}
