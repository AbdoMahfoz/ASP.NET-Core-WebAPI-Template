using System.Collections.Generic;
using System.Linq;
using Services.DTOs;

namespace BusinessLogic.Interfaces;

public interface IRolesAndPermissionsManager
{
    IQueryable<RoleDto> GetAllRoles();
    RoleDto GetRoleById(int roleId);
    RoleDto GetRoleByName(string roleName);
    int InsertRole(string newRole);
    bool DeleteRole(int id);
    void RegisterRoleToAction(string actionName, string roleName);
    bool RemoveRoleFromAction(string actionName, string roleName);
    IEnumerable<string> GetRolesOfAction(string actionName);

    bool AssignRoleToUser(string roleName, int userId);
    bool RemoveRoleFromUser(string roleName, int userId);

    IQueryable<PermissionDto> GetAllPermissions();
    PermissionDto GetPermissionById(int permissionId);
    PermissionDto GetPermissionByName(string permissionName);
    int InsertPermission(string newPermission);
    bool DeletePermission(string permission);
    bool RegisterPermissionToAction(string actionName, string permissionName);
    bool RemovePermissionFromAction(string actionName, string permissionName);
    IEnumerable<string> GetPermissionsOfAction(string actionName);

    IQueryable<PermissionDto> GetPermissionsOfRole(string roleName);

    bool AssignPermissionToRole(string roleName, string permissionName);
    bool RemovePermissionFromRole(string roleName, string permissionName);
}