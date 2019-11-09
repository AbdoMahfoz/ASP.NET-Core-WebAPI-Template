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
        IQueryable<Permission> GetPermissionsOfAction(string ActionName);
    }
    public class ActionPermissionRepository : CachedRepository<ActionPermission>, IActionPermissionRepository
    {
        private readonly IPermissionsRepository PermissionsRepository;
        public ActionPermissionRepository(ApplicationDbContext db, ILogger<ActionPermissionRepository> logger, 
            IPermissionsRepository PermissionsRepository) : base(db, logger) 
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
        public IQueryable<Permission> GetPermissionsOfAction(string ActionName)
        {
            return from actionPermission in GetAll()
                   where actionPermission.ActionName == ActionName
                   select actionPermission.Permission;
        }
    }
}
