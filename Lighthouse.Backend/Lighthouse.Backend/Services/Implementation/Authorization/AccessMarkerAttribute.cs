namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    // Checks nothing at run time. It records, right beside an action, why that action needs no RbacGuard, so
    // a reviewer can weigh the reason where the access decision is read. An architecture test makes every
    // action carry a guard, one of these markers or AllowAnonymous, and refuses a reason too short to say
    // anything.
    public abstract class AccessMarkerAttribute(string reason) : Attribute
    {
        public string Reason { get; } = reason;
    }
}
