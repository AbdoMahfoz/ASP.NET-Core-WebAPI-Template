using System.Security.Claims;
using System.Text;
using System;
using System.Linq;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Options;
using Models.DataModels;
using Models.Helpers;
using Services.DTOs;
using Repository.ExtendedRepositories;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Globalization;
using Repository.ExtendedRepositories.RoleSystem;

namespace BusinessLogic.Implementations;

public class JwtAuthorization(
    IOptions<AppSettings> options,
    IUserRepository UserRepository,
    IPasswordManager PasswordManager,
    IRolesRepository RolesRepository,
    IPermissionsRepository PermissionsRepository)
    : IAuth
{
    private (string token, int userId) GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(options.Value.Secret);
        List<Claim> claims =
        [
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("Id", user.Id.ToString()),
            new Claim("TenantId", user.TenantId.ToString()),
            new Claim("DateIssued", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))
        ];
        if (options.Value.ValidateRolesFromToken)
        {
            claims.AddRange(RolesRepository.GetRolesOfUser(user.Id)
                .Select(u => new Claim(ClaimTypes.Role, u.Name)));
            claims.AddRange(PermissionsRepository.GetPermissionsOfUser(user.Id)
                .Select(u => new Claim("Permission", u.Name)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(options.Value.TokenExpirationMinutes),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), user.Id);
    }

    public (string token, int userId) GenerateToken(int UserId)
    {
        return GenerateToken(UserRepository.Get(UserId).Result);
    }

    public (string token, int userId) Authenticate(UserAuthenticationRequest request)
    {
        var user = UserRepository.GetUser(request.Username);
        if (user == null || !PasswordManager.ComparePassword(request.Password, user.Password))
        {
            return (null, 0);
        }

        if (!user.LoggedIn)
        {
            user.LoggedIn = true;
            UserRepository.Update(user);
        }

        return GenerateToken(user);
    }

    public void Logout(int UserId)
    {
        var user = UserRepository.Get(UserId).Result;
        user.LoggedIn = false;
        user.LastLogOut = DateTime.UtcNow;
        UserRepository.Update(user).Wait();
    }

    public bool Validate(int UserId, DateTime TokenIssuedDate)
    {
        var user = UserRepository.Get(UserId).Result;
        if (user.LoggedIn && (user.LastLogOut == null || TokenIssuedDate > user.LastLogOut))
        {
            return true;
        }

        return false;
    }
}