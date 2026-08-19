namespace SigurnaDob.Api.Security;

public static class AuthorizationPolicies
{
    public const string Staff = "Staff";
    public const string AdminOnly = "AdminOnly";
    public const string CoordinatorOrAdmin = "CoordinatorOrAdmin";
}
