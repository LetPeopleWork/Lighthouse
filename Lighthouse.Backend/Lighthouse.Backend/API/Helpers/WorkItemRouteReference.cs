namespace Lighthouse.Backend.API.Helpers
{
    public static class WorkItemRouteReference
    {
        // The server has already unescaped every part of the path except an escaped slash, which it leaves
        // as it came so that it cannot pass for a path separator. A reference holding a slash therefore
        // arrives with "%2F" in it. Only that escape is restored: unescaping everything a second time would
        // turn a reference that merely contains "%25" into a different one.
        public static string From(string routeValue)
            => routeValue.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
    }
}
