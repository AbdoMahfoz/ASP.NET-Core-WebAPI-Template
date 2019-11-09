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
        private readonly IRolesRepository RolesRepository;
        private readonly IPermissionsRepository PermissionsRepository;
        public DbRoleValidator(IRolesRepository RolesRepository, IPermissionsRepository PermissionsRepository)
        {
            this.RolesRepository = RolesRepository;
            this.PermissionsRepository = PermissionsRepository;
        }
        public bool ValidatePermissions(ClaimsPrincipal User, string[] Permissions)
        {
            SortedSet<string> UserPermissions = null;
            UserPermissions = new SortedSet<string>(PermissionsRepository.GetPermissionsOfUser(User.GetId()).Select(u => u.Name));
            foreach (string Permission in Permissions)
            {
                if (!UserPermissions.Contains(Permission))
                {
                    return false;
                }
            }
            return true;
        }
        public bool ValidateRoles(ClaimsPrincipal User, string[] Roles)
        {
            SortedSet<string> UserRoles = null;
            UserRoles = new SortedSet<string>(RolesRepository.GetRolesOfUser(User.GetId()).Select(u => u.Name));
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
