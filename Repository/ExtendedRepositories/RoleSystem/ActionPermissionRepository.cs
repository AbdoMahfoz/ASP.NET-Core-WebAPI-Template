using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels.RoleSystem;

namespace Repository.ExtendedRepositories.RoleSystem;

public class PermissionAlreadyAssignedException : Exception;

public interface IActionPermissionRepository : IRepository<ActionPermission>
{
    Task AssignPermissionToAction(string ActionName, string PermissionName);
    Task AssignPermissionToAction(string ActionName, int PermissionId);
    void RemovePermissionFromAction(string ActionName, string PermissionName);
    IQueryable<Permission> GetPermissionsOfAction(string ActionName);
    IQueryable<Permission> GetDerivedPermissionOfAction(string ActionName);
}

public class ActionPermissionRepository(
    ApplicationDbContext db,
    ILogger<ActionPermissionRepository> logger,
    IPermissionsRepository PermissionsRepository,
    IActionRolesRepository ActionRoles)
    : Repository<ActionPermission>(db, logger), IActionPermissionRepository
{
    public Task AssignPermissionToAction(string ActionName, string PermissionName)
    {
        return AssignPermissionToAction(ActionName, PermissionsRepository.GetPermission(PermissionName).Id);
    }

    public Task AssignPermissionToAction(string ActionName, int PermissionId)
    {
        var permissionExists = GetAll()
            .Any(permission => permission.ActionName == ActionName && permission.Id == PermissionId);
        if (permissionExists) throw new PermissionAlreadyAssignedException();
        return Insert(new ActionPermission
        {
            ActionName = ActionName,
            PermissionId = PermissionId
        });
    }

    public void RemovePermissionFromAction(string actionName, string PermissionName)
    {
        var permissionOfAction =
            GetAll().FirstOrDefault(x => x.ActionName == actionName && x.Permission.Name == PermissionName);
        SoftDelete(permissionOfAction).Wait();
    }

    public IQueryable<Permission> GetPermissionsOfAction(string ActionName)
    {
        return GetAll()
            .Where(actionPermission => actionPermission.ActionName == ActionName)
            .Select(actionPermission => actionPermission.Permission);
    }

    public IQueryable<Permission> GetDerivedPermissionOfAction(string ActionName)
    {
        return ActionRoles.GetAll().Where(u => u.ActionName == ActionName)
            .SelectMany(u => u.Role.RolePermissions)
            .Select(u => u.Permission);
    }
}