using System.Collections.Generic;
using System.Linq;
using Models.DataModels;
using Repository.Tenant.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.ExtendedRepositories;

public interface IUserRepository : IRepository<User>
{
    User GetUser(string username);
    bool CheckUsernameExists(string username);
    bool CheckUserExists(int UserId);

    /// <summary>
    ///     The tenant's users in the requested state, with their role rows loaded so a caller can read role names without running one query per user. Pass active true for accounts that may still authenticate, false for deactivated ones, and a tenantId to look past the current tenant when the caller is allowed to.
    /// </summary>
    IQueryable<User> GetUsers(bool active, int? tenantId = null);

    /// <summary>How many of the tenant's users carry this role.</summary>
    int CountUsersInRole(string roleName);
}

public class UserRepository(ITenantManager tenantManager)
    : Repository<User>(tenantManager), IUserRepository
{
    public User GetUser(string username)
    {
        var result = GetAll().SingleOrDefault(e => e.UserName == username);
        return result is { Id: > 0 }
            ? result
            : throw new KeyNotFoundException($"{nameof(username)} {username} Doesn't exist in {nameof(User)} Table");
    }

    public bool CheckUsernameExists(string username)
    {
        return GetAll().Any(user => user.UserName == username);
    }

    public IQueryable<User> GetUsers(bool active, int? tenantId = null)
    {
        var users = active
            ? GetAll().Where(user => !user.Deactivated)
            : GetAll().Where(user => user.Deactivated);

        users = users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role);

        return tenantId.HasValue
            ? users.Where(user => user.TenantId == tenantId.Value)
            : users;
    }

    public int CountUsersInRole(string roleName)
    {
        return GetAll().Count(user => user.UserRoles.Any(userRole => userRole.Role.Name == roleName));
    }

    public bool CheckUserExists(int UserId)
    {
        return GetAll().Any(user => user.Id == UserId);
    }
}