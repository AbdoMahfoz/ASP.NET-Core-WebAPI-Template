using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Services;
using Services.DTOs;
using Services.RoleSystem.Interfaces;

namespace BusinessLogic.Implementations
{
    public class RolesAndPermissionsManager : IRolesAndPermissionsManager
    {
        private readonly IActionRoleManager ActionRoleManager;
        private readonly IPermissionsRepository PermissionsRepository;
        private readonly IRolesRepository RolesRepository;

        public RolesAndPermissionsManager(IPermissionsRepository PermissionsRepository, IRolesRepository RolesRepository,
            IActionRoleManager ActionRoleManager)
        {
            this.PermissionsRepository = PermissionsRepository;
            this.RolesRepository = RolesRepository;
            this.ActionRoleManager = ActionRoleManager;
        }

        public IQueryable<RoleDTO> GetAllRoles()
        {
            return RolesRepository.GetAll().Select(u => Helpers.MapTo<RoleDTO>(u));
        }

        public RoleDTO GetRoleById(int roleId)
        {
            return Helpers.MapTo<RoleDTO>(RolesRepository.Get(roleId));
        }

        public RoleDTO GetRoleByName(string roleName)
        {
            return Helpers.MapTo<RoleDTO>(RolesRepository.GetRole(roleName));
        }

        public int InsertRole(string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole))
                return -1;
            if (RolesRepository.CheckRoleExists(newRole))
                return -1;
            var role = new Role { Name = newRole };
            RolesRepository.Insert(role).Wait();
            return role.Id;
        }

        public bool DeleteRole(int id)
        {
            var role = RolesRepository.Get(id);
            if (role == null) return false;
            RolesRepository.SoftDelete(role);
            return true;
        }

        public void RegisterRoleToAction(string actionName, string roleName)
        {
            ActionRoleManager.RegisterRoleToAction(actionName, roleName);
        }

        public void RemoveRoleFromAction(string actionName, string roleName)
        {
            ActionRoleManager.RemoveRoleFromAction(actionName, roleName);
        }

        public void AssignRoleToUser(string roleName, int userId)
        {
            RolesRepository.AssignRoleToUser(roleName, userId);
        }

        public void RemoveRoleFromUser(string roleName, int userId)
        {
            RolesRepository.RemoveRoleFormUser(roleName, userId);
        }

        public IQueryable<PermissionDTO> GetAllPermissions()
        {
            return PermissionsRepository.GetAll().Select(x => Helpers.MapTo<PermissionDTO>(x));
        }

        public PermissionDTO GetPermissionById(int permissionId)
        {
            return Helpers.MapTo<PermissionDTO>(PermissionsRepository.Get(permissionId));
        }

        public PermissionDTO GetPermissionByName(string permissionName)
        {
            return Helpers.MapTo<PermissionDTO>(PermissionsRepository.GetPermission(permissionName));
        }

        public int InsertPermission(string newPermission)
        {
            if (string.IsNullOrWhiteSpace(newPermission))
                return -1;
            if (PermissionsRepository.CheckPermissionExists(newPermission))
                return -1;
            var permission = new Permission { Name = newPermission };
            PermissionsRepository.Insert(permission).Wait();
            return permission.Id;
        }

        public bool DeletePermission(int id)
        {
            var permission = PermissionsRepository.Get(id);
            if (permission == null)
                return false;
            PermissionsRepository.SoftDelete(permission);
            return true;
        }

        public void RegisterPermissionToAction(string actionName, string permissionName)
        {
            ActionRoleManager.RegisterPermissionToAction(actionName, permissionName);
        }

        public void RemovePermissionFromAction(string ActionName, string permissionName)
        {
            ActionRoleManager.RemovePermissionFromAction(ActionName, permissionName);
        }

        public IQueryable<PermissionDTO> GetPermissionsOfRole(string roleName)
        {
            return PermissionsRepository.GetPermissionsOfRole(roleName).Select(x => Helpers.MapTo<PermissionDTO>(x));
        }

        public void AssignPermissionToRole(string roleName, string permissionName)
        {
            PermissionsRepository.AssignPermissionToRole(permissionName, roleName);
        }

        public void RemovePermissionFromRole(string roleName, string permissionName)
        {
            PermissionsRepository.RemovePermissionFromRole(permissionName, roleName);
        }

        public IEnumerable<string> GetRolesOfAction(string actionName)
        {
            return ActionRoleManager.GetRolesOfAction(actionName);
        }

        public IEnumerable<string> GetPermissionsOfAction(string actionName)
        {
            return ActionRoleManager.GetPermissionOfAction(actionName)
                                    .Concat(ActionRoleManager.GetDerivedPermissionOfAction(actionName))
                                    .Distinct();
        }
    }
}