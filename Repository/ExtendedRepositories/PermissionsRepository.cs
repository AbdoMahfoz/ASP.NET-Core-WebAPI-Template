using Models.DataModels;
using Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Repository.ExtendedRepositories
{
    public interface IPermissionsRepository : IRepository<Permission>
    {
        Permission GetPermission(string Name);
        IQueryable<Permission> GetPermissionsOfRole(string Role);
        IQueryable<Permission> GetPermissionsOfRole(int RoleId);
        IQueryable<Permission> GetPermissionsOfUser(int UserId);
        IQueryable<Permission> GetPermissionsOfUser(string Username);
        void AssignPermissionToRole(string Permission, string Role);
        void AssignPermissionToRole(string Permission, int RoleId);
        bool UserHasPermission(string Username, string Permission);
        bool UserHasPermission(int UserId, string Permission);
    }
    public class PermissionsRepository : Repository<Permission>, IPermissionsRepository
    {
        private readonly IRepository<RolePermission> RolePermissionRepository;
        private readonly IRepository<UserRole> UserRoleRepository;
        private readonly IRolesRepository RolesRepository;
        public PermissionsRepository(ApplicationDbContext db, ILogger<PermissionsRepository> logger,
            IRepository<RolePermission> RolePermissionRepository, IRolesRepository RolesRepository, IRepository<UserRole> UserRoleRepository)
            : base(db, logger)
        {
            this.RolePermissionRepository = RolePermissionRepository;
            this.RolesRepository = RolesRepository;
            this.UserRoleRepository = UserRoleRepository;
        }
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
            });
        }
        public Permission GetPermission(string Name)
        {
            return (from permission in GetAll()
                    where permission.Name == Name
                    select permission).SingleOrDefault();
        }
        public IQueryable<Permission> GetPermissionsOfRole(string Role)
        {
            return from rolePermission in RolePermissionRepository.GetAll()
                   where rolePermission.Role.Name == Role
                   select rolePermission.Permission;
        }
        public IQueryable<Permission> GetPermissionsOfRole(int RoleId)
        {
            return from rolePermission in RolePermissionRepository.GetAll()
                   where rolePermission.RoleId == RoleId
                   select rolePermission.Permission;
        }
        public IQueryable<Permission> GetPermissionsOfUser(int UserId)
        {
            return (from roleUser in UserRoleRepository.GetAll()
                    join permissionRole in RolePermissionRepository.GetAll()
                    on roleUser.RoleId equals permissionRole.RoleId
                    where roleUser.UserId == UserId
                    select permissionRole.Permission).Distinct();
        }
        public IQueryable<Permission> GetPermissionsOfUser(string Username)
        {
            return (from roleUser in UserRoleRepository.GetAll()
                    join permissionRole in RolePermissionRepository.GetAll()
                    on roleUser.RoleId equals permissionRole.RoleId
                    where roleUser.User.UserName == Username
                    select permissionRole.Permission).Distinct();
        }
        public bool UserHasPermission(string Username, string Permission)
        {
            return (from roleUser in UserRoleRepository.GetAll()
                    join permissionRole in RolePermissionRepository.GetAll()
                    on roleUser.RoleId equals permissionRole.RoleId
                    where roleUser.User.UserName == Username && permissionRole.Permission.Name == Permission
                    select permissionRole.Permission).Any();
        }
        public bool UserHasPermission(int UserId, string Permission)
        {
            return (from roleUser in UserRoleRepository.GetAll()
                    join permissionRole in RolePermissionRepository.GetAll()
                    on roleUser.RoleId equals permissionRole.RoleId
                    where roleUser.UserId == UserId && permissionRole.Permission.Name == Permission
                    select permissionRole.Permission).Any();
        }
    }
}
