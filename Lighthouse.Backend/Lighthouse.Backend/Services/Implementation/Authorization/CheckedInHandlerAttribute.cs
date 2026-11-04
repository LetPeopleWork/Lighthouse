namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CheckedInHandlerAttribute(string reason) : AccessMarkerAttribute(reason);
}
