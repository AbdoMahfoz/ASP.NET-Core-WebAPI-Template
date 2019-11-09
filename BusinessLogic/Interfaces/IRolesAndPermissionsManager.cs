using System.Linq;
using System.Threading.Tasks;
using Models.DataModels;

namespace BusinessLogic.Interfaces
{
    public interface IRolesAndPermissionsManager
    {
        IQueryable<Role> GetAllRoles();
        Task InsertRole(Role newRole);
        void DeleteRole(int id);

        IQueryable<Permission> GetAllPermissions();
        Task InsertPermission(Permission newPermission);
        void DeletePermission(int id);

        IQueryable<Permission> GetPermissionsOfRole(string roleName);

        void AssignPermissionToRole(string roleName, string permissionName);
        void RemovePermissionFromRole(string roleName, string permissionName);

        void AssignRoleToUser(string roleName, int userId);
        void RemoveRoleFromUser(string roleName, int userId);
    }
}