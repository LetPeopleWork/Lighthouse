namespace Lighthouse.Backend.Modules.Access.Adapters.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SignedInOnlyAttribute(string reason) : AccessMarkerAttribute(reason);
}
