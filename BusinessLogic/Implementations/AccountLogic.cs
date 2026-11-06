using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;

using System.Threading.Tasks;


using BusinessLogic.Interfaces;


using Microsoft.Extensions.Logging;


using Models;

using Models.DataModels;

using Models.DataModels.RoleSystem;


using Repository.ExtendedRepositories;

using Repository.ExtendedRepositories.RoleSystem;

using Repository.Tenant.Interfaces;


using Services.DTOs;

using Services.DTOs.Requests;

using Services.Exceptions;


namespace BusinessLogic.Implementations;


/// <summary>
///     Account administration over the template's own schema. Two adaptations are deliberate: the reference implementation kept one role per account in a role column, while this template models roles as a many-to-many join, so an update that names a role replaces the account's roles instead of re-pointing a column; and there is no audit recorder in this template, so the reference's audit calls are left out rather than invented - who acted is still carried through the method signatures. Scope is a tenant id, never a site id, and passwords only ever leave this class hashed except for a generated temporary password handed back once at registration. Tracing is not wired here yet: it arrives with the observability bundle.
/// </summary>
public class AccountLogic(
    IUserRepository userRepository,
    IRolesRepository rolesRepository,
    IPermissionsRepository permissionsRepository,
    IPasswordManager passwordManager,
    ITenantManager tenantManager,
    IAuth auth,
    ILogger<AccountLogic> logger) : IAccountLogic

{


    public async Task<CreateUserResult> Register(CreateUserRequest request, int? roleId = null, int? createdById = null)

    {

        if (string.IsNullOrWhiteSpace(request?.UserName))
            throw new ValidationException(FailureReasons.UsernameRequired);


        // A taken username is an ordinary outcome, not an exception: the endpoint answers 409 from the null result.  
        if (userRepository.GetAll().Any(user => user.UserName.ToUpper() == request.UserName.ToUpper()))
            return null;


        var generatedPassword = string.IsNullOrWhiteSpace(request.Password);

        var password = generatedPassword ? GenerateTempPassword() : request.Password!;


        var user = new User

        {

            UserName = request.UserName,

            Password = passwordManager.HashPassword(password),

            TenantId = tenantManager.TenantId,

            IsFirstLogin = true,

        };


        // The tracked instance comes back with its generated id already assigned, so the role assignment can use it immediately.  
        var created = await userRepository.InsertAndReturnTrackedEntity(user);


        var role = await ResolveRoleForNewUser(roleId, createdById);

        rolesRepository.AssignRoleToUser(role.Name, created.Id);


        logger.LogInformation("Registered account {UserId} with role {RoleName}", created.Id, role.Name);


        return new CreateUserResult

        {

            UserId = created.Id,

            IsFirstLogin = created.IsFirstLogin,

            // Only a password this class generated is ever returned; a password the caller supplied is not echoed back.  
            TempPassword = generatedPassword ? password : null,

        };

    }


    public Task<IEnumerable<string>> GetUserPermissions(int userId) => Task.FromResult<IEnumerable<string>>(
        permissionsRepository.GetPermissionsOfUser(userId)
            .Select(permission => permission.Name)
            .ToList());


    public async Task<bool> IsFirstLogin(int userId) => (await userRepository.Get(userId))?.IsFirstLogin ?? false;


    public async Task<bool> UpdatePassword(int userId, string? currentPassword, string newPassword)

    {

        var user = await userRepository.Get(userId);

        if (user == null) return false;


        // While the account still carries a password somebody else set for it the user cannot be asked to prove it; afterwards the old password must match.  
        if (!user.IsFirstLogin)

        {

            if (string.IsNullOrWhiteSpace(currentPassword)) return false;

            if (!passwordManager.ComparePassword(currentPassword, user.Password)) return false;

        }


        user.Password = passwordManager.HashPassword(newPassword);

        user.IsFirstLogin = false;


        await userRepository.Update(user);


        logger.LogInformation("Password updated for account {UserId}", userId);


        return true;

    }


    public async Task Update(UpdateUserRequest request, int updatedBy)

    {

        var user = await userRepository.Get(request.UserId);

        if (user == null)
            throw new NotFoundException(FailureReasons.UserNotFound, $"No account with id {request.UserId}");


        user.UserName = request.UserName;


        // An administrator-set password puts the account back into first-login state so the owner has to replace it.  
        if (!string.IsNullOrEmpty(request.Password))

        {

            user.Password = passwordManager.HashPassword(request.Password);

            user.IsFirstLogin = true;

        }


        if (request.Deactivated.HasValue)

        {

            user.Deactivated = request.Deactivated.Value;


            // Deactivating an account invalidates every token issued for it, so a stolen or stale session cannot outlive the account.  
            if (request.Deactivated.Value) auth.Logout(request.UserId);

        }


        if (request.RoleId.HasValue) await ReplaceRolesWith(user, request.RoleId.Value);


        await userRepository.Update(user);


        logger.LogInformation("Account {UserId} updated by {UpdatedBy}", request.UserId, updatedBy);

    }


    public Task<IEnumerable<UserDTO>> All(int? tenantId = null) => Task.FromResult(
        userRepository.GetUsers(true, tenantId)
            .ToList()
            .Select(ToDto));


    public Task<UserDTO> GetById(int id, int? tenantId = null) => Task.FromResult(
        userRepository.GetUsers(true, tenantId).SingleOrDefault(user => user.Id == id) is { } found ? ToDto(found) : null);


    public Task<IEnumerable<UserDTO>> AllDeactivated(int? tenantId = null) => Task.FromResult(
        userRepository.GetUsers(false, tenantId)
            .ToList()
            .Select(ToDto));


    public async Task<bool> ActivateUser(int userId, int activatedBy, int? tenantId = null)

    {

        var user = userRepository.GetUsers(false, tenantId).SingleOrDefault(account => account.Id == userId);

        if (user == null) return false;


        user.Deactivated = false;

        await userRepository.Update(user);


        logger.LogInformation("Account {UserId} reactivated by {ActivatedBy}", userId, activatedBy);


        return true;

    }


    /// <summary>Resolves which role a new account gets: the requested one, else the acting user's own role, else the seeded Admin role.</summary>  
    private async Task<Role> ResolveRoleForNewUser(int? roleId, int? createdById)

    {

        if (roleId.HasValue)

        {

            var requested = await rolesRepository.Get(roleId.Value);

            if (requested == null)
                throw new NotFoundException(FailureReasons.RoleNotFound, $"No role with id {roleId.Value}");


            return requested;

        }


        if (createdById.HasValue)

        {

            var inherited = rolesRepository.GetRolesOfUser(createdById.Value).FirstOrDefault();

            if (inherited != null) return inherited;

        }


        var seededAdmin = rolesRepository.GetRole(RoleNames.Admin.ToString());

        if (seededAdmin == null)
            throw new NotFoundException(
                FailureReasons.RoleNotFound,
                $"No role was requested and the seeded {RoleNames.Admin} role is missing, so a new account cannot be given one");


        return seededAdmin;

    }


    /// <summary>Because roles are a join in this schema, naming a role in an update replaces the account's roles instead of adding to them.</summary>  
    private async Task ReplaceRolesWith(User user, int roleId)

    {

        var role = await rolesRepository.Get(roleId);

        if (role == null)
            throw new NotFoundException(FailureReasons.RoleNotFound, $"No role with id {roleId}");


        // Names are read out first: removing rows while iterating the navigation collection would modify the query mid-enumeration.  
        var currentRoleNames = rolesRepository.GetRolesOfUser(user.Id).Select(r => r.Name).ToList();


        foreach (var roleName in currentRoleNames) rolesRepository.RemoveRoleFormUser(roleName, user.Id);


        rolesRepository.AssignRoleToUser(role.Name, user.Id);

    }


    private static UserDTO ToDto(User user) => new()

    {

        Id = user.Id,

        UserName = user.UserName,

        TenantId = user.TenantId,

        AddedDate = user.AddedDate,

        Deactivated = user.Deactivated,

        IsFirstLogin = user.IsFirstLogin,

        RoleNames = user.UserRoles?.Select(userRole => userRole.Role.Name).ToList() ?? [],

    };


    /// <summary>Builds a temporary password that satisfies the same strength rule the request DTOs validate: upper, lower, digit and special character at 12 characters.</summary>  
    private static string GenerateTempPassword(int length = 12)

    {

        const string lower = "abcdefghijklmnopqrstuvwxyz";

        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        const string digits = "0123456789";

        const string special = "!@#$%^&*()-_=+[]{};:,.?";


        var all = lower + upper + digits + special;


        if (length < 8) length = 8;


        var chars = new List<char>(length)
            {
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)],
            };


        while (chars.Count < length) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);


        // Fisher-Yates shuffle with a cryptographically strong source so guaranteed character classes land in unpredictable positions.  
        for (var i = chars.Count - 1; i > 0; i--)

        {

            var j = RandomNumberGenerator.GetInt32(i + 1);

            (chars[i], chars[j]) = (chars[j], chars[i]);

        }


        return new string(chars.ToArray());

    }

}
