using System.Security.Claims;

namespace Services.RoleSystem.Interfaces;

public interface IRoleValidator
{
    bool ValidateRoles(ClaimsPrincipal User, string[] Roles);
    bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions);
    bool ValidateOnePermission(ClaimsPrincipal User, string[] Permissions);

    /// <summary>
    ///     True when the caller holds at least one of these roles. An empty list is refused: asking for "any of no roles" cannot be satisfied. 
    /// </summary>
    bool ValidateAnyRole(ClaimsPrincipal User, string[] Roles);
}