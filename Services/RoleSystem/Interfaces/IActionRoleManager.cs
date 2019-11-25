using System.Collections.Generic;

namespace Services.RoleSystem.Interfaces
{
    public interface IActionRoleManager
    {
        IEnumerable<string> GetPermissionOfAction(string ActionName);
        IEnumerable<string> GetDerivedPermissionOfAction(string ActionName);
        IEnumerable<string> GetRolesOfAction(string ActionName);

        void RegisterRoleToAction(string ActionName, string RoleName);
        void RegisterPermissionToAction(string ActionName, string PermissionName);

        void RemoveRoleFromAction(string ActionName, string RoleName);
        void RemovePermissionFromAction(string ActionName, string PermissionName);
    }
}
