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


/// <summary>
///     Issues and validates the bearer tokens the template's endpoints accept. Claims are limited to identity, tenant and role/permission names - there is no site claim because this template has no site concept. Authentication counts failed passwords on the account row itself, so an account lock survives a restart and is not tied to one client address, and a successful sign-in clears the counter. Session validity is decided by Validate: an account must be logged in and its token must have been issued after its last logout instant, which is what makes logout-from-all-devices and account deactivation deny every existing token at once. Passwords are never logged or placed in a claim.  
/// </summary>
public class JwtAuthorization(
    IOptions<AppSettings> options,
    IUserRepository UserRepository,
    IPasswordManager PasswordManager,
    IRolesRepository RolesRepository,
    IPermissionsRepository PermissionsRepository)
    : IAuth

{


    /// <summary>Authenticate returns this instead of a user id when the account has passed its failed-login threshold, so the endpoint can answer 403 instead of pretending the credentials were merely wrong.</summary>  
    public const int LockedAccount = -1;


    /// <summary>Authenticate returns this when the account exists but has been deactivated.</summary>  
    public const int DeactivatedAccount = -2;


    /// <summary>The ordinary wrong-credentials result: no token, no lock implied.</summary>  
    public const int FailedCredentials = 0;


    private readonly int _accountLockThreshold = options.Value.AccountLockThreshold <= 0 ? 5 : options.Value.AccountLockThreshold;


    private (string token, int userId) GenerateToken(User user)

    {

        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(options.Value.Secret);


        // HS256 requires a 256-bit key. Saying so here, with the length that was configured, beats an opaque IDX10720 raised deep inside the token handler on the first login attempt.  
        if (key.Length < 32)
            throw new InvalidOperationException(
                $"AppSettings.Secret must be at least 32 characters to sign HS256 tokens, but it is {key.Length} characters long.");


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


    public (string token, int userId) GenerateToken(int UserId) => GenerateToken(UserRepository.Get(UserId).Result);


    public (string token, int userId) Authenticate(UserAuthenticationRequest request)

    {

        var user = UserRepository.GetUser(request.Username);


        if (user.Deactivated) return (null, DeactivatedAccount);


        // The counter lives on the account, so the lock is not per-client and cannot be reset by changing address or restarting the process.  
        if (user.LogInAttempts >= _accountLockThreshold) return (null, LockedAccount);


        if (!PasswordManager.ComparePassword(request.Password, user.Password))

        {

            user.LogInAttempts += 1;


            // Persisted before answering: an unawaited update would let a failing client keep hammering without ever reaching the threshold.  
            UserRepository.Update(user).GetAwaiter().GetResult();


            return (null, FailedCredentials);

        }


        // A successful sign-in is also what releases the lock, and it re-arms the session for Logout invalidation.  
        if (!user.LoggedIn || user.LogInAttempts != 0)

        {

            user.LoggedIn = true;

            user.LogInAttempts = 0;


            UserRepository.Update(user).GetAwaiter().GetResult();

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


    /// <summary>
    ///     True while the session behind a token is still live: the account must exist, must not be deactivated, must be logged in, and the token must have been issued after the account's last logout instant. That last condition is what invalidates every previously issued token when the user logs out from all devices or an administrator deactivates the account.  
    /// </summary>
    public bool Validate(int UserId, DateTime TokenIssuedDate)

    {

        var user = UserRepository.Get(UserId).Result;


        if (user == null || user.Deactivated) return false;


        return user.LoggedIn && (user.LastLogOut == null || TokenIssuedDate > user.LastLogOut);
    }

}
