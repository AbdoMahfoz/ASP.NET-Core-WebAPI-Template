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
using System.Threading.Tasks;

namespace BusinessLogic.Implementations
{
    public class JwtAuthorization : IAuth
    {
        private readonly IOptions<AppSettings> options;
        private readonly IUserRepository UserRepository;
        private readonly IRolesRepository RolesRepository;
        private readonly IPermissionsRepository PermissionsRepository;
        private readonly IPasswordManager PasswordManager;
        public JwtAuthorization(IOptions<AppSettings> options, 
            IUserRepository UserRepository, IPasswordManager PasswordManager, 
            IRolesRepository RolesRepository, IPermissionsRepository PermissionsRepository)
        {
            this.options = options;
            this.PasswordManager = PasswordManager;
            this.UserRepository = UserRepository;
            this.RolesRepository = RolesRepository;
            this.PermissionsRepository = PermissionsRepository;
        }
        public User GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(options.Value.Secret);
            List<Claim> Claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("Id", user.Id.ToString()),
                new Claim("DateIssued", DateTime.UtcNow.ToString())
            };
            if(options.Value.ValidateRolesFromToken)
            {
                List<Claim> RoleClaims = null, PermissionClaims = null;
                Task.WaitAll(
                    Task.Run(() =>
                    {
                        RoleClaims = new List<Claim>(from role in RolesRepository.GetRolesOfUser(user.Id)
                                                     select new Claim(ClaimTypes.Role, role.Name));
                    }),
                    Task.Run(() => 
                    {
                        PermissionClaims = new List<Claim>(from permission in PermissionsRepository.GetPermissionsOfUser(user.Id)
                                                           select new Claim("Permission", permission.Name));
                    })
                );
                Claims.AddRange(RoleClaims);
                Claims.AddRange(PermissionClaims);
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = DateTime.UtcNow.AddMinutes(options.Value.TokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = null;
            return user;
        }
        public User GenerateToken(int UserId)
        {
            return GenerateToken(UserRepository.Get(UserId));
        }
        public User Authenticate(UserAuthenticationRequest request)
        {
            User user = UserRepository.GetUser(request.Username);
            if (user == null || !PasswordManager.ComparePassword(request.Password, user.Password)) return null;
            if(!user.LoggedIn)
            {
                user.LoggedIn = true;
                UserRepository.Update(user);
            }
            return GenerateToken(user);
        }
        public void Logout(int UserId)
        {
            User user = UserRepository.Get(UserId);
            user.LoggedIn = false;
            user.LastLogOut = DateTime.UtcNow;
            UserRepository.Update(user);
        }
        public bool Validate(int UserId, DateTime TokenIssuedDate)
        {
            User user = UserRepository.Get(UserId);
            if(user.LoggedIn && (user.LastLogOut == null || TokenIssuedDate > user.LastLogOut))
            {
                return true;
            }
            return false;
        }
    }
}
