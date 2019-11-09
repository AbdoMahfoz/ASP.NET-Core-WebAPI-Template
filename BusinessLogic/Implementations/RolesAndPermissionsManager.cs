using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Services;

namespace BusinessLogic.Implementations
{
    public class RolesAndPermissionsManager : IRolesAndPermissionsManager
    {
        private readonly IPermissionsRepository _permissionsRepo;
        private readonly IRolesRepository _rolesRepo;
        private readonly IUserRepository _userRepo;

        public RolesAndPermissionsManager(IPermissionsRepository permissionsRepo, IRolesRepository rolesRepo,
            IUserRepository userRepo)
        {
            _permissionsRepo = permissionsRepo;
            _rolesRepo = rolesRepo;
            _userRepo = userRepo;
        }

        public IQueryable<Role> GetAllRoles()
        {
            return _rolesRepo.GetAll();
        }

        public Task InsertRole(Role newRole)
        {
            if (Helpers.HasNullOrEmptyStrings(newRole))
                throw new Exception("The New Role To be added Contains null values");
            return _rolesRepo.Insert(newRole);
        }

        public void DeleteRole(int id)
        {
            if (id <= 0) throw new KeyNotFoundException($"id {id} doesn't exist in the Database");
            var role = _rolesRepo.Get(id);
            _rolesRepo.SoftDelete(role);
        }

        public IQueryable<Permission> GetAllPermissions()
        {
            return _permissionsRepo.GetAll();
        }

        public Task InsertPermission(Permission newPermission)
        {
            if (Helpers.HasNullOrEmptyStrings(newPermission))
                throw new Exception("The New Permission To be added Contains null values");
            return _permissionsRepo.Insert(newPermission);
        }

        public void DeletePermission(int id)
        {
            if (id <= 0) throw new KeyNotFoundException($"id {id} doesn't exist in the Database");
            var permission = _permissionsRepo.Get(id);
            _permissionsRepo.SoftDelete(permission);
        }

        public IQueryable<Permission> GetPermissionsOfRole(string roleName)
        {
            return _permissionsRepo.GetPermissionsOfRole(roleName);
        }

        public void AssignPermissionToRole(string roleName, string permissionName)
        {
            _permissionsRepo.AssignPermissionToRole(permissionName, roleName);
        }

        public void RemovePermissionFromRole(string roleName, string permissionName)
        {

            _permissionsRepo.RemovePermissionFromRole(permissionName, roleName);
        }

        public void AssignRoleToUser(string roleName, int userId)
        {
            _rolesRepo.AssignRoleToUser(roleName, userId);
        }

        public void RemoveRoleFromUser(string roleName, int userId)
        {
            _rolesRepo.RemoveRoleFormUser(roleName, userId);
        }
    }
}