using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels.RoleSystem;
using Repository.Tenant.Interfaces;

namespace Repository.ExtendedRepositories.RoleSystem;

public interface IPermissionsRepository : IRepository<Permission>
{
    Permission GetPermission(string Name);
    bool CheckPermissionExists(string Name);

    IQueryable<Permission> GetPermissionsOfRole(string Role);
    IQueryable<Permission> GetPermissionsOfRole(int RoleId);

    IQueryable<Permission> GetPermissionsOfUser(int UserId);
    IQueryable<Permission> GetPermissionsOfUser(string Username);

    void AssignPermissionToRole(string Permission, string Role);
    void AssignPermissionToRole(string Permission, int RoleId);

    void RemovePermissionFromRole(string Permission, int RoleId);
    void RemovePermissionFromRole(string Permission, string roleName);
    void AssignPermissionToRole(int PermissionId, int RoleId);

    bool UserHasPermission(string Username, string Permission);
    bool UserHasPermission(int UserId, string Permission);
    bool UserHasPermission(int UserId, int PermissionId);
}

public class PermissionsRepository(
    ITenantManager tenantManager,
    IRepository<RolePermission> RolePermissionRepository,
    IRolesRepository RolesRepository,
    IRepository<UserRole> UserRoleRepository)
    : Repository<Permission>(tenantManager), IPermissionsRepository
{
    public void AssignPermissionToRole(string Permission, string Role)
    {
        AssignPermissionToRole(Permission, RolesRepository.GetRole(Role).Id);
    }

    public void AssignPermissionToRole(string Permission, int RoleId)
    {
        RolePermissionRepository.Insert(new RolePermission
        {
            RoleId = RoleId,
            PermissionId = GetPermission(Permission).Id
        }).Wait();
    }

    public void RemovePermissionFromRole(string Permission, int RoleId)
    {
        var userRole = RolePermissionRepository
            .GetAll().FirstOrDefault(x => x.Permission.Name == Permission && x.Role.Id == RoleId);
        RolePermissionRepository.SoftDelete(userRole).Wait();
    }

    public void AssignPermissionToRole(int PermissionId, int RoleId)
    {
        RolePermissionRepository.Insert(new RolePermission
        {
            RoleId = RoleId,
            PermissionId = PermissionId
        }).Wait();
    }

    public Permission GetPermission(string Name)
    {
        return GetAll().SingleOrDefault(permission => permission.Name == Name);
    }

    public IQueryable<Permission> GetPermissionsOfRole(string Role)
    {
        return RolePermissionRepository.GetAll()
            .Where(u => u.Role.Name == Role)
            .Select(u => u.Permission);
    }

    public IQueryable<Permission> GetPermissionsOfRole(int RoleId)
    {
        return RolePermissionRepository.GetAll()
            .Where(u => u.RoleId == RoleId)
            .Select(u => u.Permission);
    }

    public IQueryable<Permission> GetPermissionsOfUser(int UserId)
    {
        return UserRoleRepository.GetAll().Where(u => u.UserId == UserId)
            .SelectMany(u => u.Role.RolePermissions.Select(x => x.Permission))
            .Distinct();
    }

    public IQueryable<Permission> GetPermissionsOfUser(string Username)
    {
        return UserRoleRepository.GetAll().Where(u => u.User.UserName == Username)
            .SelectMany(u => u.Role.RolePermissions.Select(x => x.Permission))
            .Distinct();
    }

    public bool UserHasPermission(string Username, string Permission)
    {
        return GetPermissionsOfUser(Username).Any(u => u.Name == Permission);
    }

    public bool UserHasPermission(int UserId, string Permission)
    {
        return GetPermissionsOfUser(UserId).Any(u => u.Name == Permission);
    }

    public void RemovePermissionFromRole(string Permission, string roleName)
    {
        RolePermissionRepository.GetAll()
            .Where(x => x.Permission.Name == Permission && x.Role.Name == roleName)
            .ExecuteDelete();
    }

    public bool UserHasPermission(int UserId, int PermissionId)
    {
        return GetPermissionsOfUser(UserId).Any(u => u.Id == PermissionId);
    }

    public bool CheckPermissionExists(string Name)
    {
        return GetAll().Any(permission => permission.Name == Name);
    }
}