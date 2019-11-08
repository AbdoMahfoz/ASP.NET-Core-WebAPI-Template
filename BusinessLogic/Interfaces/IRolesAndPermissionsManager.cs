using Models.DataModels;
using System.Linq;

namespace BusinessLogic.Interfaces
{
    public interface IRolesAndPermissionsManager
    {
        IQueryable<Role> GetAllRoles();
        void InsertRole(Role newRole);
        void DeleteRole(int id);

        IQueryable<Permission> GetAllPermissions();
        void InsertPermission(Permission newPermission);
        void DeletePermission(int id);

        IQueryable<Permission> GetPermissionsOfRole(string roleName);

        void AssignPermissionToRole(string roleName, string permissionName);
        void RemovePermissionFromRole(string roleName, string permissionName);

        void AssignRoleToUser(string roleName, int userId);
        void RemoveRoleFromUser(string roleName, int userId);

    }
}
