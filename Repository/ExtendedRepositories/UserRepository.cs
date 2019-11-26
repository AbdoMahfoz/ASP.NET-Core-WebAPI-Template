using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;

namespace Repository.ExtendedRepositories
{
    public interface IUserRepository : IRepository<User>
    {
        User GetUser(string username);
        bool CheckUsernameExists(string username);
        bool CheckUserExists(int UserId);
    }
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger) : base(context, logger) { }

        public User GetUser(string username)
        {
            var result = GetAll().Where(e => e.UserName == username).AsQueryable().FirstOrDefault();
            return result != null && result.Id > 0 ? result : throw new KeyNotFoundException($"{nameof(username)} {username} Doesn't exist in {nameof(User)} Table");
        }
        public bool CheckUsernameExists(string username)
        {
            return (from user in GetAll()
                    where user.UserName == username
                    select user).Any();
        }

        public bool CheckUserExists(int UserId)
        {
            return (from user in GetAll()
                    where user.Id == UserId
                    select user).Any();
        }
    }
}
