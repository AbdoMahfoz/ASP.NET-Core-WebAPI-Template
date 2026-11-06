using System;
using System.Collections.Generic;
using System.Security.Claims;
using Services.Extensions;
using System.Linq;
using Repository.ExtendedRepositories;
using Repository.ExtendedRepositories.RoleSystem;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem.Implementations;

public class DbRoleValidator(IRolesRepository RolesRepository, IPermissionsRepository PermissionsRepository)
    : IRoleValidator
{
    public bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions)
    {
            var userPermissions =
                new SortedSet<string>(PermissionsRepository.GetPermissionsOfUser(User.GetId()).Select(u => u.Name));
            return Permissions.All(Permission => userPermissions.Contains(Permission));
        }

    public bool ValidateOnePermission(ClaimsPrincipal User, string[] Permissions)
    {
            var userPermissions =
                new SortedSet<string>(PermissionsRepository.GetPermissionsOfUser(User.GetId()).Select(u => u.Name));
            return Permissions.Any(Permission => userPermissions.Contains(Permission));
        }

    /// <summary>
    ///     Reads the account's role rows rather than its claims, so this guard keeps working when AppSettings.ValidateRolesFromToken is false and tokens carry no role claim at all - which is the configuration this template ships with. Comparison is case-insensitive because role names are stored as text. An empty request list is refused: "any of no roles" cannot be satisfied. 
    /// </summary>
    public bool ValidateAnyRole(ClaimsPrincipal User, string[] Roles)
    {
            if (Roles.Length == 0) return false;

            var userRoles = new SortedSet<string>(
                RolesRepository.GetRolesOfUser(User.GetId()).Select(u => u.Name),
                StringComparer.OrdinalIgnoreCase);

            return Roles.Any(Role => userRoles.Contains(Role));
        }

    public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
    {
            var userRoles = new SortedSet<string>(RolesRepository.GetRolesOfUser(User.GetId()).Select(u => u.Name));
            return Roles.All(Role => userRoles.Contains(Role));
        }
}