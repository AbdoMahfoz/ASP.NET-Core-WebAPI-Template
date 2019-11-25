using System.Collections.Generic;
using System.Linq;
using Repository.ExtendedRepositories;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem.Implementations
{
    public class ActionRoleManager : IActionRoleManager
    {
        private readonly IActionPermissionRepository ActionPermissionsRepository;
        private readonly IActionRolesRepository ActionRolesRepository;

        public ActionRoleManager(IActionRolesRepository ActionRolesRepository,
            IActionPermissionRepository ActionPermissionsRepository)
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

        public void RemoveRoleFromAction(string ActionName, string RoleName)
        {
            ActionRolesRepository.RemoveRoleFromAction(ActionName, RoleName);
        }

        public void RemovePermissionFromAction(string ActionName, string PermissionName)
        {
            ActionPermissionsRepository.RemovePermissionFromAction(ActionName, PermissionName);
        }

        public void RegisterRoleToAction(string ActionName, string RoleName)
        {
            ActionRolesRepository.AssignRoleToAction(ActionName, RoleName);
        }
        public IEnumerable<string> GetDerivedPermissionOfAction(string ActionName)
        {
            return ActionPermissionsRepository.GetDerivedPermissionOfAction(ActionName).Select(u => u.Name);
        }
    }
}