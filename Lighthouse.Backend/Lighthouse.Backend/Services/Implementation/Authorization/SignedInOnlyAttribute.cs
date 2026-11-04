namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SignedInOnlyAttribute : Attribute
    {
        public SignedInOnlyAttribute(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
