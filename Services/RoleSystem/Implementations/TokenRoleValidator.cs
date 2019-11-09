using System.Collections.Generic;
using System.Security.Claims;
using Services.Extensions;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem.Implementations
{
    public class TokenRoleValidator : IRoleValidator
    {
        public bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions)
        {
            SortedSet<string> UserPermissions = new SortedSet<string>(User.GetPermissions());
            foreach(string Permission in Permissions)
            {
                if(!UserPermissions.Contains(Permission))
                {
                    return false;
                }
            }
            return true;
        }
        public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
        {
            SortedSet<string> UserRoles = new SortedSet<string>(User.GetRoles());
            foreach (string Role in Roles)
            {
                if (!UserRoles.Contains(Role))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
