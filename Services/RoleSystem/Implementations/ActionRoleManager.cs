using Repository.ExtendedRepositories;
using Services.RoleSystem.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Services.RoleSystem.Implementations
{
    public class ActionRoleManager : IActionRoleManager
    {
        private readonly IActionRolesRepository ActionRolesRepository;
        private readonly IActionPermissionRepository ActionPermissionsRepository;
        public ActionRoleManager(IActionRolesRepository ActionRolesRepository, IActionPermissionRepository ActionPermissionsRepository)
        {
            this.ActionRolesRepository = ActionRolesRepository;
            this.ActionPermissionsRepository = ActionPermissionsRepository;
        }
        public IEnumerable<string> GetPermissionOfAction(string ActionName)
        {
            return ActionPermissionsRepository.GetPermissionsOfAction(ActionName).Select(u => u.Name);
        }
        public IEnumerable<string> GetRolesOfAction(string ActionName)
        {
            return ActionRolesRepository.GetRolesOfAction(ActionName).Select(u => u.Name);
        }
        public void RegisterPermissionToAction(string ActionName, string PermissionName)
        {
            ActionPermissionsRepository.AssignPermissionToAction(ActionName, PermissionName);
        }
        public void RegisterRoleToAction(string ActionName, string RoleName)
        {
            ActionRolesRepository.AssignRoleToAction(ActionName, RoleName);
        }
    }
}
