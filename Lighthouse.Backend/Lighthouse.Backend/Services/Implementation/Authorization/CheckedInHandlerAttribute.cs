namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CheckedInHandlerAttribute(string reason) : AccessMarkerAttribute(reason);
}
