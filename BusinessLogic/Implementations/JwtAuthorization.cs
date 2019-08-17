using System.Security.Claims;
using System.Text;
using System;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Options;
using Models.DataModels;
using Models.Helpers;
using Services.DTOs;
using Repository.ExtendedRepositories;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLogic.Implementations
{
    public class JwtAuthorization : IAuth
    {
        private readonly IOptions<AppSettings> options;
        private readonly IUserRepository UserRepository;
        private readonly IPasswordManager PasswordManager;
        public JwtAuthorization(IOptions<AppSettings> options, IUserRepository UserRepository, IPasswordManager PasswordManager)
        {
            this.options = options;
            this.PasswordManager = PasswordManager;
            this.UserRepository = UserRepository;
        }
        public User GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(options.Value.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("Id", user.Id.ToString()),
                    new Claim("DateIssued", DateTime.UtcNow.ToString())
                }),
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
        public bool Register(UserAuthenticationRequest request)
        {
            if (UserRepository.CheckUsernameExists(request.Username)) return false;
            UserRepository.Insert(new User
            {
                UserName = request.Username,
                Password = PasswordManager.HashPassword(request.Password)
            }).Wait();
            return true;
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
