namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>The entries of a To Do, Doing or Done list as a settings save sent them, before they are read against the mappings.</summary>
    internal static class StateListEntries
    {
        /// <summary>The list without blank entries, each entry trimmed.</summary>
        public static List<string> Trimmed(List<string> list)
            => [.. list.Where(state => !string.IsNullOrWhiteSpace(state)).Select(state => state.Trim())];
    }
}
