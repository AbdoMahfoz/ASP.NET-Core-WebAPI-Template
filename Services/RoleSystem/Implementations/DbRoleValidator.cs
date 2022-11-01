using System.Collections.Generic;
using System.Security.Claims;
using Services.Extensions;
using System.Linq;
using Repository.ExtendedRepositories;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem.Implementations
{
    public class DbRoleValidator : IRoleValidator
    {
        private readonly IRolesRepository _rolesRepository;
        private readonly IPermissionsRepository _permissionsRepository;

        public DbRoleValidator(IRolesRepository RolesRepository, IPermissionsRepository PermissionsRepository)
        {
            _rolesRepository = RolesRepository;
            _permissionsRepository = PermissionsRepository;
        }

        public bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions)
        {
            var userPermissions =
                new SortedSet<string>(_permissionsRepository.GetPermissionsOfUser(User.GetId()).Select(u => u.Name));
            return Permissions.All(Permission => userPermissions.Contains(Permission));
        }

        public bool ValidateOnePermission(ClaimsPrincipal User, string[] Permissions)
        {
            var userPermissions =
                new SortedSet<string>(_permissionsRepository.GetPermissionsOfUser(User.GetId()).Select(u => u.Name));
            return Permissions.Any(Permission => userPermissions.Contains(Permission));
        }

        public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
        {
            var userRoles = new SortedSet<string>(_rolesRepository.GetRolesOfUser(User.GetId()).Select(u => u.Name));
            return Roles.All(Role => userRoles.Contains(Role));
        }
    }
}