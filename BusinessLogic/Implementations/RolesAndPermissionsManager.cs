﻿using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Models.DataModels;
using Models.DataModels.RoleSystem;
using Repository.ExtendedRepositories;
using Repository.ExtendedRepositories.RoleSystem;
using Services.DTOs;
using Services.Helpers;
using Services.RoleSystem.Interfaces;

namespace BusinessLogic.Implementations;

public class RolesAndPermissionsManager(
    IPermissionsRepository PermissionsRepository,
    IRolesRepository RolesRepository,
    IActionRoleManager ActionRoleManager,
    IUserRepository UserRepository)
    : IRolesAndPermissionsManager
{
    public IQueryable<RoleDto> GetAllRoles()
    {
        return RolesRepository.GetAll().Select(u => ObjectHelpers.MapTo<RoleDto>(u));
    }

    public RoleDto GetRoleById(int roleId)
    {
        return ObjectHelpers.MapTo<RoleDto>(RolesRepository.Get(roleId));
    }

    public RoleDto GetRoleByName(string roleName)
    {
        return ObjectHelpers.MapTo<RoleDto>(RolesRepository.GetRole(roleName));
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
        var role = RolesRepository.Get(id).Result;
        if (role == null) return false;
        RolesRepository.SoftDelete(role);
        return true;
    }

    public void RegisterRoleToAction(string actionName, string roleName)
    {
        ActionRoleManager.RegisterRoleToAction(actionName, roleName);
    }

    public bool RemoveRoleFromAction(string actionName, string roleName)
    {
        if (!ActionRoleManager.GetRolesOfAction(actionName).Contains(roleName))
            return false;
        ActionRoleManager.RemoveRoleFromAction(actionName, roleName);
        return true;
    }

    public bool AssignRoleToUser(string roleName, int userId)
    {
        if (!RolesRepository.CheckRoleExists(roleName) || !UserRepository.CheckUserExists(userId) ||
            RolesRepository.UserHasRole(userId, roleName))
            return false;
        RolesRepository.AssignRoleToUser(roleName, userId);
        return true;
    }

    public bool RemoveRoleFromUser(string roleName, int userId)
    {
        if (!RolesRepository.CheckRoleExists(roleName) || !UserRepository.CheckUserExists(userId) ||
            !RolesRepository.UserHasRole(userId, roleName))
            return false;
        RolesRepository.RemoveRoleFormUser(roleName, userId);
        return true;
    }

    public IQueryable<PermissionDto> GetAllPermissions()
    {
        return PermissionsRepository.GetAll().Select(x => ObjectHelpers.MapTo<PermissionDto>(x));
    }

    public PermissionDto GetPermissionById(int permissionId)
    {
        return ObjectHelpers.MapTo<PermissionDto>(PermissionsRepository.Get(permissionId));
    }

    public PermissionDto GetPermissionByName(string permissionName)
    {
        return ObjectHelpers.MapTo<PermissionDto>(PermissionsRepository.GetPermission(permissionName));
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

    public bool DeletePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;
        var res = PermissionsRepository.GetPermission(permission);
        if (res == null)
            return false;
        PermissionsRepository.SoftDelete(res);
        return true;
    }

    public bool RegisterPermissionToAction(string actionName, string permissionName)
    {
        if (!PermissionsRepository.CheckPermissionExists(permissionName))
            return false;
        ActionRoleManager.RegisterPermissionToAction(actionName, permissionName);
        return true;
    }

    public bool RemovePermissionFromAction(string ActionName, string permissionName)
    {
        if (!PermissionsRepository.CheckPermissionExists(permissionName))
            return false;
        ActionRoleManager.RemovePermissionFromAction(ActionName, permissionName);
        return true;
    }

    public IQueryable<PermissionDto> GetPermissionsOfRole(string roleName)
    {
        if (!RolesRepository.CheckRoleExists(roleName))
            return null;
        return PermissionsRepository.GetPermissionsOfRole(roleName).Select(x => ObjectHelpers.MapTo<PermissionDto>(x));
    }

    public bool AssignPermissionToRole(string roleName, string permissionName)
    {
        if (!RolesRepository.CheckRoleExists(roleName) || !PermissionsRepository.CheckPermissionExists(permissionName))
            return false;
        PermissionsRepository.AssignPermissionToRole(permissionName, roleName);
        return true;
    }

    public bool RemovePermissionFromRole(string roleName, string permissionName)
    {
        if (!RolesRepository.CheckRoleExists(roleName) || !PermissionsRepository.CheckPermissionExists(permissionName))
            return false;
        PermissionsRepository.RemovePermissionFromRole(permissionName, roleName);
        return true;
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