using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels.RoleSystem;

namespace Repository.ExtendedRepositories.RoleSystem;

public class RoleAlreadyAssignedException : Exception;

public interface IActionRolesRepository : IRepository<ActionRole>
{
    Task AssignRoleToAction(string ActionName, string RoleName);
    Task AssignRoleToAction(string ActionName, int RoleId);
    void RemoveRoleFromAction(string ActionName, string RoleName);
    IQueryable<Role> GetRolesOfAction(string ActionName);
}

public class ActionRolesRepository(
    ApplicationDbContext db,
    IRolesRepository RolesRepository)
    : Repository<ActionRole>(db), IActionRolesRepository
{
    public Task AssignRoleToAction(string ActionName, string RoleName)
    {
        return AssignRoleToAction(ActionName, RolesRepository.GetRole(RoleName).Id);
    }

    public Task AssignRoleToAction(string ActionName, int RoleId)
    {
        var roleExists = GetAll().Any(role => role.ActionName == ActionName && role.Id == RoleId);
        if (roleExists) throw new RoleAlreadyAssignedException();
        return Insert(new ActionRole
        {
            ActionName = ActionName,
            RoleId = RoleId
        });
    }

    public void RemoveRoleFromAction(string actionName, string RoleName)
    {
        var permissionOfAction = GetAll().FirstOrDefault(x => x.ActionName == actionName && x.Role.Name == RoleName);
        SoftDelete(permissionOfAction).Wait();
    }

    public IQueryable<Role> GetRolesOfAction(string ActionName)
    {
        return GetAll().Where(actionRole => actionRole.ActionName == ActionName).Select(actionRole => actionRole.Role);
    }
}