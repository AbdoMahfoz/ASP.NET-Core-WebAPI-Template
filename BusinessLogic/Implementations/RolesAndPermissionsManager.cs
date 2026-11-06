using System.Collections.Generic;
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

    /// <summary>
    ///     Roles with how many accounts carry them. The counting is done by the user repository so this stays one query per role instead of loading every account into memory.  
    /// </summary>
    public IEnumerable<RoleUserCountDto> ListRolesWithUserCount()
    {
        return RolesRepository.GetAll().ToList()
            .Select(role => new RoleUserCountDto
            {
                Id = role.Id,
                Name = role.Name,
                UsersCount = UserRepository.CountUsersInRole(role.Name)
            })
            .ToList();
    }

    /// <summary>
    ///     Renames a role in place instead of replacing it, because the permission wiring in this template refers to roles by name and existing assignments must survive the rename. Refuses a name another role already owns.  
    /// </summary>
    public bool EditRole(int roleId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return false;

        var role = RolesRepository.Get(roleId).Result;
        if (role == null) return false;

        // Asking for the name it already has is a no-op success, not a collision with itself.  
        if (role.Name == newName) return true;

        if (RolesRepository.CheckRoleExists(newName)) return false;

        role.Name = newName;
        RolesRepository.Update(role).Wait();
        return true;
    }

    /// <summary>
    ///     Assembles the permission-to-role matrix: each role's granted permission ids are read once and the matrix is built in memory, rather than running one query per permission.  
    /// </summary>
    public RolesAndPermissionsDto ListPermissionsWithRoles()
    {
        var roles = RolesRepository.GetAll().ToList();

        var grantedPermissionIdsByRole = roles.ToDictionary(
            role => role.Id,
            role => PermissionsRepository.GetPermissionsOfRole(role.Name).Select(p => p.Id).ToList());

        var permissions = PermissionsRepository.GetAll().ToList();

        return new RolesAndPermissionsDto
        {
            Roles = roles.Select(role => ObjectHelpers.MapTo<RoleDto>(role)).ToList(),
            Permissions = permissions.Select(permission => new PermissionRolesDto
            {
                PermissionId = permission.Id,
                PermissionName = permission.Name,
                Roles = roles.Where(role => grantedPermissionIdsByRole[role.Id].Contains(permission.Id))
                    .Select(role => ObjectHelpers.MapTo<RoleDto>(role))
                    .ToList()
            }).ToList()
        };
    }

    /// <summary>
    ///     Makes a role's permission set exactly the requested one: grants missing from permissionIds are removed and requested ones are added. The pairs are read from the join directly so neither direction needs a lookup per id.  
    /// </summary>
    public bool UpdateRolePermissions(int roleId, IEnumerable<int> permissionIds)
    {
        var role = RolesRepository.Get(roleId).Result;
        if (role == null) return false;

        var desired = (permissionIds ?? []).Distinct().ToList();

        var currentGrants = PermissionsRepository.GetPermissionsOfRole(role.Name)
            .Select(p => new { p.Id, p.Name })
            .ToList();

        var desiredGrants = PermissionsRepository.GetAll()
            .Where(p => desired.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToList();

        foreach (var grant in currentGrants.Where(grant => !desired.Contains(grant.Id)))
            PermissionsRepository.RemovePermissionFromRole(grant.Name, roleId);

        var currentIds = currentGrants.Select(grant => grant.Id).ToList();

        foreach (var grant in desiredGrants.Where(grant => !currentIds.Contains(grant.Id)))
            PermissionsRepository.AssignPermissionToRole(grant.Name, roleId);

        return true;
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
        RolesRepository.SoftDelete(role).Wait();
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
        PermissionsRepository.SoftDelete(res).Wait();
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