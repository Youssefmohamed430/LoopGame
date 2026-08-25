namespace LoopGame.Infrastructure.Identity;

/// <summary>
/// Roles table integrating ASP.NET Core Identity (IdentityRole&lt;int&gt;).
/// Pre-populated with: player, admin, super_admin, instructor.
/// Lives in Infrastructure because IdentityRole requires the Identity packages.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
