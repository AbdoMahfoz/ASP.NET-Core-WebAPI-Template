using System.Collections.Generic;
using System.Security.Claims;
using Services.Extensions;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem.Implementations;

public class TokenRoleValidator : IRoleValidator
{
    public bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions)
    {
            var userPermissions = new SortedSet<string>(User.GetPermissions());
            foreach(var permission in Permissions)
            {
                if(!userPermissions.Contains(permission))
                {
                    return false;
                }
            }
            return true;
        }
    public bool ValidateOnePermission(ClaimsPrincipal User, string[] Permissions)
    {
            var userPermissions = new SortedSet<string>(User.GetPermissions());
            foreach(var permission in Permissions)
            {
                if(userPermissions.Contains(permission))
                {
                    return true;
                }
            }
            return false;
        }
    public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
    {
            var userRoles = new SortedSet<string>(User.GetRoles());
            foreach (var role in Roles)
            {
                if (!userRoles.Contains(role))
                {
                    return false;
                }
            }
            return true;
        }
}