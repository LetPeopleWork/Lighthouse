namespace Lighthouse.Backend.Models
{
    /// <summary>A shape of stored To Do, Doing and Done lists that older versions read in their own way.</summary>
    public enum LegacyListShape
    {
        /// <summary>A mapping has no states (null, empty or only blank ones), so its name cannot be replaced by states.</summary>
        EmptyGroup,

        /// <summary>A mapping's name is listed beside one of that mapping's own states, in the same list.</summary>
        Shadowing,

        /// <summary>A listed mapping's name stands for states that include the name of another mapping.</summary>
        Chained,
    }
}
