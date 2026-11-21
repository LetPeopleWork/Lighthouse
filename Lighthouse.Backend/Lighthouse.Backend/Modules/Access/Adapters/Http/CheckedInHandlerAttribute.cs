namespace Lighthouse.Backend.Modules.Access.Adapters.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CheckedInHandlerAttribute(string reason) : AccessMarkerAttribute(reason);
}
