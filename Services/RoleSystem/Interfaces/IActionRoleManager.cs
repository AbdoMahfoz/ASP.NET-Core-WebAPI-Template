using System.Collections.Generic;

namespace Services.RoleSystem.Interfaces
{
    public interface IActionRoleManager
    {
        void RegisterRoleToAction(string ActionName, string RoleName);
        void RegisterPermissionToAction(string ActionName, string PermissionName);
        IEnumerable<string> GetPermissionOfAction(string ActionName);
        IEnumerable<string> GetRolesOfAction(string ActionName);
    }
}
