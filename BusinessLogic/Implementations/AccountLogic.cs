using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Services.DTOs;

namespace BusinessLogic.Implementations
{
    public class AccountLogic : IAccountLogic
    {
        private readonly IUserRepository UserRepository;
        private readonly IRolesRepository RolesRepository;
        private readonly IPasswordManager PasswordManager;
        public AccountLogic(IUserRepository UserRepository, IRolesRepository RolesRepository, IPasswordManager PasswordManager)
        {
            this.UserRepository = UserRepository;
            this.RolesRepository = RolesRepository;
            this.PasswordManager = PasswordManager;
        }
        public bool Register(UserAuthenticationRequest request, string Role)
        {
            if (UserRepository.CheckUsernameExists(request.Username)) return false;
            User u = new User
            {
                UserName = request.Username,
                Password = PasswordManager.HashPassword(request.Password)
            };
            UserRepository.Insert(u).Wait();
            RolesRepository.AssignRoleToUser(Role, u.Id);
            return true;
        }
    }
}
