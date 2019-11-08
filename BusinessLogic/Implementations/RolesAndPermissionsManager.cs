using System;
using System.Collections.Generic;
using System.Linq;
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

        public Role InsertRole(Role newRole)
        {
            if (Helpers.HasNullOrEmptyStrings(newRole))
                throw new Exception("The New Role To be added Contains null values");
            _rolesRepo.Insert(newRole).Wait();
            return newRole;
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

        public Permission InsertPermission(Permission newPermission)
        {
            if (Helpers.HasNullOrEmptyStrings(newPermission))
                throw new Exception("The New Permission To be added Contains null values");
            _permissionsRepo.Insert(newPermission).Wait();
            return newPermission;
        }

        public void DeletePermission(int id)
        {
            if (id <= 0) throw new KeyNotFoundException($"id {id} doesn't exist in the Database");
            var permission = _permissionsRepo.Get(id);
            _permissionsRepo.SoftDelete(permission);
        }

        public IQueryable<Permission> GetPermissionsOfRole(string roleName)
        {
            var role = _rolesRepo.GetRole(roleName);
            return role != null
                ? _permissionsRepo.GetPermissionsOfRole(role.Id)
                : throw new Exception($"Role {roleName} Not Found");
        }

        public void AssignPermissionToRole(string roleName, string permissionName)
        {
            var role = _rolesRepo.GetRole(roleName);
            if (role == null)
                throw new NullReferenceException($"Role {roleName} Doesn't Exist to assign Permissions to.");

            var permission = _permissionsRepo.GetPermission(permissionName);
            if (permission == null)
                throw new NullReferenceException($"Permission {permissionName} Doesn't Exist to be assigned to a Role");

            _permissionsRepo.AssignPermissionToRole(permissionName, role.Id);
        }

        public void RemovePermissionFromRole(string roleName, string permissionName)
        {
            var role = _rolesRepo.GetRole(roleName);
            if (role == null)
                throw new NullReferenceException($"Role {roleName} Doesn't Exist to remove its Permissions");

            var permission = _permissionsRepo.GetPermission(permissionName);
            if (permission == null)
                throw new NullReferenceException(
                    $"Permission {permissionName} Doesn't Exist to be removed from a Role");

            _permissionsRepo.RemovePermissionFromRole(permissionName, role.Id);
        }

        public void AssignRoleToUser(string roleName, int userId)
        {
            var role = _rolesRepo.GetRole(roleName);
            if (role == null)
                throw new NullReferenceException($"Role {roleName} Doesn't Exist to assign Permissions to.");

            var user = _userRepo.Get(userId);
            if (user == null) throw new NullReferenceException("User Doesn't Exist to be assigned a Role");

            _rolesRepo.AssignRoleToUser(roleName, userId);
        }

        public void RemoveRoleFromUser(string roleName, int userId)
        {
            var role = _rolesRepo.GetRole(roleName);
            if (role == null)
                throw new NullReferenceException($"Role {roleName} Doesn't Exist to assign Permissions to.");

            var user = _userRepo.Get(userId);
            if (user == null) throw new NullReferenceException("User Doesn't Exist to be assigned a Role");
            _rolesRepo.RemoveRoleFormUser(roleName, userId);
        }
    }
}