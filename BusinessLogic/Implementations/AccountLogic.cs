using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Repository.ExtendedRepositories.RoleSystem;
using Services.DTOs;

namespace BusinessLogic.Implementations;

public class AccountLogic(
    IUserRepository UserRepository,
    IRolesRepository RolesRepository,
    IPasswordManager PasswordManager)
    : IAccountLogic
{
    public bool Register(UserAuthenticationRequest request, string Role)
    {
        if (UserRepository.CheckUsernameExists(request.Username)) return false;
        var u = new User
        {
            UserName = request.Username,
            Password = PasswordManager.HashPassword(request.Password)
        };
        UserRepository.Insert(u).Wait();
        RolesRepository.AssignRoleToUser(Role, u.Id);
        return true;
    }
}