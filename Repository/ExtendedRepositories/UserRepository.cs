using System.Collections.Generic;
using System.Linq;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace Repository.ExtendedRepositories;

public interface IUserRepository : IRepository<User>
{
    User GetUser(string username);
    bool CheckUsernameExists(string username);
    bool CheckUserExists(int UserId);
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

    public bool CheckUserExists(int UserId)
    {
        return GetAll().Any(user => user.Id == UserId);
    }
}