using System.Linq;
using Services.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IRolesAndPermissionsManager
    {
        IQueryable<RoleDTO> GetAllRoles();
        RoleDTO GetRoleById(int roleId);
        RoleDTO GetRoleByName(string roleName);
        int InsertRole(RoleDTO newRole);
        void DeleteRole(int id);
        void RegisterRoleToAction(string actionName, string roleName);
        void RemoveRoleFromAction(string actionName, string roleName);

        void AssignRoleToUser(string roleName, int userId);
        void RemoveRoleFromUser(string roleName, int userId);

        IQueryable<PermissionDTO> GetAllPermissions();
        PermissionDTO GetPermissionById(int permissionId);
        PermissionDTO GetPermissionByName(string permissionName);
        int InsertPermission(PermissionDTO newPermission);
        void DeletePermission(int id);
        void RegisterPermissionToAction(string actionName, string permissionName);
        void RemovePermissionFromAction(string actionName, string permissionName);

        IQueryable<PermissionDTO> GetPermissionsOfRole(string roleName);

        void AssignPermissionToRole(string roleName, string permissionName);
        void RemovePermissionFromRole(string roleName, string permissionName);
    }
}