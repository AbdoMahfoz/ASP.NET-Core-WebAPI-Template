using System;
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
    /// <summary>
    ///     Claim-backed counterpart of the database validator: when the application trusts role claims it must still refuse a caller whose claims hold none of the requested roles, and an empty request list is refused rather than accepted vacuously. 
    /// </summary>
    public bool ValidateAnyRole(ClaimsPrincipal User, string[] Roles)
    {
            if (Roles.Length == 0) return false;

            var userRoles = new SortedSet<string>(User.GetRoles(), StringComparer.OrdinalIgnoreCase);

            foreach (var role in Roles)
            {
                if (userRoles.Contains(role))
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