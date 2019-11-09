using System.Security.Claims;

namespace Services.RoleSystem.Interfaces
{
    public interface IRoleValidator
    {
        bool ValidateRoles(ClaimsPrincipal User, string[] Roles);
        bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions);
    }
}
