using System.Linq;
using Models.DataModels;

namespace BusinessLogic.Interfaces
{
    public interface IRolesAndPermissionsManager
    {
        IQueryable<Role> GetAllRoles();
        Role InsertRole(Role newRole);
        void DeleteRole(int id);

        IQueryable<Permission> GetAllPermissions();
        Permission InsertPermission(Permission newPermission);
        void DeletePermission(int id);

        IQueryable<Permission> GetPermissionsOfRole(string roleName);

        void AssignPermissionToRole(string roleName, string permissionName);
        void RemovePermissionFromRole(string roleName, string permissionName);

        void AssignRoleToUser(string roleName, int userId);
        void RemoveRoleFromUser(string roleName, int userId);
    }
}