using System;
using System.Linq;
using Models;
using Models.DataModels;

namespace Repository.ExtendedRepositories
{
    public interface IUserRepository : IRepository<User>
    {
        User GetUser(string username);
        bool CheckUsernameExists(string username);
    }
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext _context) : base(_context) { }

        public User GetUser(string username)
        {
            return entities.Where(e => e.IsDeleted == false).SingleOrDefault(u => u.UserName == username);
        }
        public bool CheckUsernameExists(string username)
        {
            return (from user in entities where user.UserName == username && user.IsDeleted == false select user.Id).Any();
        }
    }
}   
