using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Services;
using Services.DTOs;

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

        public IQueryable<RoleDTO> GetAllRoles()
        {
            return _rolesRepo.GetAll().Select(u => Helpers.MapTo<RoleDTO>(u));
        }

        public int InsertRole(RoleDTO newRole)
        {
            if (Helpers.HasNullOrEmptyStrings(newRole))
                throw new Exception("The New Role To be added Contains null values");
            Role role = Helpers.MapTo<Role>(newRole);
            _rolesRepo.Insert(role).Wait();
            return role.Id;
        }

        public void DeleteRole(int id)
        {
            if (id <= 0) throw new KeyNotFoundException($"id {id} doesn't exist in the Database");
            var role = _rolesRepo.Get(id);
            _rolesRepo.SoftDelete(role);
        }

        public IQueryable<PermissionDTO> GetAllPermissions()
        {
            return _permissionsRepo.GetAll().Select(x => Helpers.MapTo<PermissionDTO>(x));
        }

        public int InsertPermission(PermissionDTO newPermission)
        {
            if (Helpers.HasNullOrEmptyStrings(newPermission))
                throw new Exception("The New Permission To be added Contains null values");
            Permission permission = Helpers.MapTo<Permission>(newPermission);
            _permissionsRepo.Insert(permission).Wait();
            return permission.Id;
        }

        public void DeletePermission(int id)
        {
            if (id <= 0) throw new KeyNotFoundException($"id {id} doesn't exist in the Database");
            var permission = _permissionsRepo.Get(id);
            _permissionsRepo.SoftDelete(permission);
        }

        public IQueryable<PermissionDTO> GetPermissionsOfRole(string roleName)
        {
            return _permissionsRepo.GetPermissionsOfRole(roleName).Select(x => Helpers.MapTo<PermissionDTO>(x));
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

        public RoleDTO GetRoleById(int roleId)
        {
            return Helpers.MapTo<RoleDTO>(_rolesRepo.Get(roleId));
        }

        public RoleDTO GetRoleByName(string roleName)
        {
            return Helpers.MapTo<RoleDTO>(_rolesRepo.GetRole(roleName));
        }

        public PermissionDTO GetPermissionById(int permissionId)
        {
            return Helpers.MapTo<PermissionDTO>(_permissionsRepo.Get(permissionId));
        }

        public PermissionDTO GetPermissionByName(string permissionName)
        {
            return Helpers.MapTo<PermissionDTO>(_permissionsRepo.GetPermission(permissionName));
        }
    }
}