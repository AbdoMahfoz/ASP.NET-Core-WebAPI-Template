using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.ExtendedRepositories
{
    public interface IActionPermissionRepository : ICachedRepository<ActionPermission>
    {
        Task AssignPermissionToAction(string ActionName, string PermissionName);
        Task AssignPermissionToAction(string ActionName, int PermissionId);
        void RemovePermissionFromAction(string ActionName, string PermissionName);
        IQueryable<Permission> GetPermissionsOfAction(string ActionName);
    }
    public class ActionPermissionRepository : CachedRepository<ActionPermission>, IActionPermissionRepository
    {
        private readonly IPermissionsRepository PermissionsRepository;
        public ActionPermissionRepository(ILogger<ActionPermissionRepository> logger, IPermissionsRepository PermissionsRepository) : base(logger)
        {
            this.PermissionsRepository = PermissionsRepository;
        }
        public Task AssignPermissionToAction(string ActionName, string PermissionName)
        {
            return AssignPermissionToAction(ActionName, PermissionsRepository.GetPermission(PermissionName).Id);
        }
        public Task AssignPermissionToAction(string ActionName, int PermissionId)
        {
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
    }
}
