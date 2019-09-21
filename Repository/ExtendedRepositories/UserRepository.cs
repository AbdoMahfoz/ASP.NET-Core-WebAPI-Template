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
            return (from user in GetAll()
                    where user.UserName == username
                    select user).SingleOrDefault();
        }
        public bool CheckUsernameExists(string username)
        {
            return (from user in GetAll()
                    where user.UserName == username
                    select user).Any();
        }
    }
}   
