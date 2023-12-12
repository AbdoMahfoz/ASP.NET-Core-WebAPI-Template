using BusinessLogic.Interfaces;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Repository.ExtendedRepositories.RoleSystem;
using Repository.Tenant.Interfaces;
using Services.DTOs;

namespace BusinessLogic.Implementations;

public class AccountLogic(
    IUserRepository UserRepository,
    IRolesRepository RolesRepository,
    IPasswordManager PasswordManager,
    ITenantManager tenantManager)
    : IAccountLogic
{
    public bool Register(UserAuthenticationRequest request, string Role)
    {
        if (UserRepository.CheckUsernameExists(request.Username)) return false;
        var u = new User
        {
            UserName = request.Username,
            Password = PasswordManager.HashPassword(request.Password),
            TenantId = tenantManager.TenantId
        };
        UserRepository.Insert(u).Wait();
        RolesRepository.AssignRoleToUser(Role, u.Id);
        return true;
    }
}