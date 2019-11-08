using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public RolesAndPermissionsManager(IPermissionsRepository permissionsRepo, IRolesRepository rolesRepo)
        {
            _permissionsRepo = permissionsRepo;
            _rolesRepo = rolesRepo;
        }
        public IQueryable<Role> GetAllRoles()
        {
            return _rolesRepo.GetAll();
        }

        public void InsertRole(Role newRole)
        {
            if (Helpers.HasNullOrEmptyStrings(newRole)) throw new Exception("The New Role To be added Contains null values");
            _rolesRepo.Insert(newRole).Wait();
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

        public void InsertPermission(Permission newPermission)
        {
            if (Helpers.HasNullOrEmptyStrings(newPermission)) throw new Exception("The New Permission To be added Contains null values");
            _permissionsRepo.Insert(newPermission).Wait();
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
            if (role == null) throw new NullReferenceException($"Role {roleName} Doesn't Exist to assign Permissions to.");

            var permission = _permissionsRepo.GetPermission(permissionName);
            if (permission == null) throw new NullReferenceException($"Permission {permissionName} Doesn't Exist to be assigned to a Role");

            _permissionsRepo.AssignPermissionToRole(permissionName, role.Id);
        }

        public void RemovePermissionFromRole(string roleName, string permissionName)
        {
            throw new NotImplementedException();
        }

        public void AssignRoleToUser(string roleName, int userId)
        {
            throw new NotImplementedException();
        }

        public void RemoveRoleFromUser(string roleName, int userId)
        {
            throw new NotImplementedException();
        }
    }
}
