using Models.DataModels;
using System.Linq;
using Models;
using Microsoft.Extensions.Logging;

namespace Repository.ExtendedRepositories
{
    public interface IRolesRepository : IRepository<Role>
    {
        Role GetRole(string Name);
        IQueryable<Role> GetRolesOfUser(int UserId);
        IQueryable<Role> GetRolesOfUser(string Username);
        void AssignRoleToUser(string Role, string Username);
        void AssignRoleToUser(string Role, int UserId);
        bool UserHasRole(string Username, string Role);
        bool UserHasRole(int UserId, string Role);
    }
    public class RolesRepository : Repository<Role>, IRolesRepository
    {
        private readonly IUserRepository UserRepository;
        private readonly IRepository<UserRole> UserRoleRepository;
        public RolesRepository(ApplicationDbContext db, ILogger<RolesRepository> logger, 
            IUserRepository UserRepository, IRepository<UserRole> UserRoleRepository) : base(db, logger)
        {
            this.UserRepository = UserRepository;
            this.UserRoleRepository = UserRoleRepository;
        }
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
    }
}
