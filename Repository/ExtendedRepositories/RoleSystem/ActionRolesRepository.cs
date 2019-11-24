using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;

namespace Repository.ExtendedRepositories
{
    public class RoleAlreadyAssignedException : Exception { }
    public interface IActionRolesRepository : ICachedRepository<ActionRole>
    {
        Task AssignRoleToAction(string ActionName, string RoleName);
        Task AssignRoleToAction(string ActionName, int RoleId);
        void RemoveRoleFromAction(string ActionName, string RoleName);
        IQueryable<Role> GetRolesOfAction(string ActionName);
    }

    public class ActionRolesRepository : CachedRepository<ActionRole>, IActionRolesRepository
    {
        private readonly IRolesRepository RolesRepository;

        public ActionRolesRepository(ILogger<ActionRolesRepository> logger, IRolesRepository RolesRepository) : base(logger)
        {
            this.RolesRepository = RolesRepository;
        }

        public Task AssignRoleToAction(string ActionName, string RoleName)
        {
            return AssignRoleToAction(ActionName, RolesRepository.GetRole(RoleName).Id);
        }

        public Task AssignRoleToAction(string ActionName, int RoleId)
        {
            bool roleExists = (from role in GetAll()
                               where role.ActionName == ActionName && role.Id == RoleId
                               select role).Any();
            if (roleExists) throw new RoleAlreadyAssignedException();
            return Insert(new ActionRole
            {
                ActionName = ActionName,
                RoleId = RoleId
            });
        }

        public void RemoveRoleFromAction(string actionName, string RoleName)
        {
            var permissionOfAction = GetAll().Where(x =>
                x.ActionName == actionName && x.Role.Name == RoleName).FirstOrDefault();
            SoftDelete(permissionOfAction);
        }

        public IQueryable<Role> GetRolesOfAction(string ActionName)
        {
            return from actionRole in GetAll()
                where actionRole.ActionName == ActionName
                select actionRole.Role;
        }
    }
}