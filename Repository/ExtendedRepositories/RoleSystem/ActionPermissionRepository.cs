using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.ExtendedRepositories
{
    public class PermissionAlreadyAssignedException : Exception { }
    public interface IActionPermissionRepository : ICachedRepository<ActionPermission>
    {
        Task AssignPermissionToAction(string ActionName, string PermissionName);
        Task AssignPermissionToAction(string ActionName, int PermissionId);
        void RemovePermissionFromAction(string ActionName, string PermissionName);
        IQueryable<Permission> GetPermissionsOfAction(string ActionName);
        IQueryable<Permission> GetDerivedPermissionOfAction(string ActionName);
    }
    public class ActionPermissionRepository : CachedRepository<ActionPermission>, IActionPermissionRepository
    {
        private readonly IPermissionsRepository PermissionsRepository;
        private readonly IActionRolesRepository ActionRoles;
        public ActionPermissionRepository(ILogger<ActionPermissionRepository> logger, 
            IPermissionsRepository PermissionsRepository, IActionRolesRepository ActionRoles) : base(logger)
        {
            this.PermissionsRepository = PermissionsRepository;
            this.ActionRoles = ActionRoles;
        }
        public Task AssignPermissionToAction(string ActionName, string PermissionName)
        {
            return AssignPermissionToAction(ActionName, PermissionsRepository.GetPermission(PermissionName).Id);
        }
        public Task AssignPermissionToAction(string ActionName, int PermissionId)
        {
            bool permissionExists = (from permission in GetAll()
                                     where permission.ActionName == ActionName && permission.Id == PermissionId
                                     select permission).Any();
            if (permissionExists) throw new PermissionAlreadyAssignedException();
            return Insert(new ActionPermission
            {
                ActionName = ActionName,
                PermissionId = PermissionId
            });
        }
        public void RemovePermissionFromAction(string actionName, string PermissionName)
        {
            var permissionOfAction = GetAll().Where(x =>
                x.ActionName == actionName && x.Permission.Name == PermissionName).FirstOrDefault();
            SoftDelete(permissionOfAction);
        }
        public IQueryable<Permission> GetPermissionsOfAction(string ActionName)
        {
            return from actionPermission in GetAll()
                   where actionPermission.ActionName == ActionName
                   select actionPermission.Permission;
        }
        public IQueryable<Permission> GetDerivedPermissionOfAction(string ActionName)
        {
            return ActionRoles.GetAll().Where(u => u.ActionName == ActionName)
                                       .SelectMany(u => u.Role.RolePermissions)
                                       .Select(u => u.Permission);
        }
    }
}
