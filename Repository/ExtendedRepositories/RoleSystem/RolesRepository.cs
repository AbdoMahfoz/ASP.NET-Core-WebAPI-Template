using System.Linq;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels.RoleSystem;
using Repository.Tenant.Interfaces;

namespace Repository.ExtendedRepositories.RoleSystem;

public interface IRolesRepository : IRepository<Role>
{
    Role GetRole(string Name);
    bool CheckRoleExists(string Name);
    IQueryable<Role> GetRolesOfUser(int UserId);
    IQueryable<Role> GetRolesOfUser(string Username);
    void AssignRoleToUser(string Role, string Username);
    void AssignRoleToUser(string Role, int UserId);
    bool UserHasRole(string Username, string Role);
    bool UserHasRole(int UserId, string Role);
    void RemoveRoleFormUser(string Role, int UserId);
}

public class RolesRepository(
    ITenantManager tenantManager,
    IUserRepository UserRepository,
    IRepository<UserRole> UserRoleRepository)
    : Repository<Role>(tenantManager), IRolesRepository
{
    public void AssignRoleToUser(string Role, string Username)
    {
        AssignRoleToUser(Role, UserRepository.GetUser(Username).Id);
    }

    public void AssignRoleToUser(string Role, int UserId)
    {
        UserRoleRepository.Insert(new UserRole
        {
            UserId = UserId,
            RoleId = GetRole(Role).Id
        });
    }

    public void RemoveRoleFormUser(string Role, int UserId)
    {
        var userRole = UserRoleRepository.GetAll().FirstOrDefault(x => x.UserId == UserId && x.Role.Name == Role);
        UserRoleRepository.SoftDelete(userRole);
    }

    public Role GetRole(string Name)
    {
        return (from role in GetAll()
            where role.Name == Name
            select role).SingleOrDefault();
    }

    public IQueryable<Role> GetRolesOfUser(int UserId)
    {
        return from userRole in UserRoleRepository.GetAll()
            where userRole.UserId == UserId
            select userRole.Role;
    }

    public IQueryable<Role> GetRolesOfUser(string Username)
    {
        return from userRole in UserRoleRepository.GetAll()
            where userRole.User.UserName == Username
            select userRole.Role;
    }

    public bool UserHasRole(string Username, string Role)
    {
        return (from userRole in UserRoleRepository.GetAll()
            where userRole.User.UserName == Username && userRole.Role.Name == Role
            select userRole).Any();
    }

    public bool UserHasRole(int UserId, string Role)
    {
        return (from userRole in UserRoleRepository.GetAll()
            where userRole.UserId == UserId && userRole.Role.Name == Role
            select userRole).Any();
    }

    public bool CheckRoleExists(string Name)
    {
        return (from role in GetAll()
            where role.Name == Name
            select role).Any();
    }
}