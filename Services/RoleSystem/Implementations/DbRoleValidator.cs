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

    public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
    {
            var userRoles = new SortedSet<string>(RolesRepository.GetRolesOfUser(User.GetId()).Select(u => u.Name));
            return Roles.All(Role => userRoles.Contains(Role));
        }
}