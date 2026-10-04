namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>The three answers to "can we do this within our SLE?". The numbers are stored, so they never change.</summary>
    public enum SizingAnswer
    {
        Yes = 0,
        YesBut = 1,
        No = 2,
    }
}
