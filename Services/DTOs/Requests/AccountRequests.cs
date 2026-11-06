using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;


using Microsoft.Extensions.DependencyInjection;


using Models.DataModels;

using Models.DataModels.RoleSystem;


using Repository;


using Services.Exceptions;


namespace Services.DTOs.Requests;


/// <summary>
///     Creates an account. Password is optional: leave it blank and the account is given a generated temporary password which is handed back once in CreateUserResult. Every rejection is reported as a machine-readable FailureReasons token in the message so a client can switch on a stable code instead of on prose. The checks run against the caller's own request scope, so the username and role lookups are already tenant scoped.
/// </summary>
public partial class CreateUserRequest : IValidatableObject

{

    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.UsernameRequired))]
    public string UserName { get; set; }


    /// <summary>Optional. When it is absent the account gets a generated temporary password.</summary>
    public string? Password { get; set; }


    /// <summary>The role to assign. When null, the acting user's own role is used and, failing that, the seeded Admin role.</summary>
    public int? RoleId { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)

    {

        if (string.IsNullOrWhiteSpace(UserName)) yield break;


        if (!string.IsNullOrWhiteSpace(Password) && !PasswordRegex().IsMatch(Password))
            yield return new ValidationResult(nameof(FailureReasons.WeakPassword), [nameof(Password)]);


        // Username uniqueness is deliberately left to the account logic rather than checked here: Register answers 409 Conflict with a machine-readable reason when a name is taken, and a validation filter can only ever answer 400. Keeping it in one place avoids the same mistake answering two different statuses.  
        if (RoleId.HasValue)

        {

            var roles = validationContext.GetRequiredService<IRepository<Role>>();


            if (!roles.GetAll().Any(role => role.Id == RoleId.Value))
                yield return new ValidationResult(nameof(FailureReasons.RoleNotFound), [nameof(RoleId)]);

        }

    }


    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]
    private static partial Regex PasswordRegex();

}


/// <summary>
///     Updates an existing account: rename it, reset its password, deactivate it or move it to another role. Rejections again carry FailureReasons tokens. A password reset through this endpoint marks the account as needing its first login, exactly like an administrator-issued temporary password.
/// </summary>
public partial class UpdateUserRequest : IValidatableObject

{

    [Required(ErrorMessage = nameof(FailureReasons.UserNotFound))]
    public int UserId { get; set; }


    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.UsernameRequired))]
    public string UserName { get; set; }


    /// <summary>Optional. Blank means the password is left alone.</summary>
    public string? Password { get; set; }


    /// <summary>Null leaves the account's state untouched; true deactivates it and invalidates its sessions, false reactivates it.</summary>
    public bool? Deactivated { get; set; }


    public int? RoleId { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)

    {

        if (string.IsNullOrWhiteSpace(UserName)) yield break;


        if (!string.IsNullOrEmpty(Password) && !PasswordRegex().IsMatch(Password))
            yield return new ValidationResult(nameof(FailureReasons.WeakPassword), [nameof(Password)]);


        var users = validationContext.GetRequiredService<IRepository<User>>();


        if (users.GetAll().Any(user => user.Id != UserId && user.UserName.ToUpper() == UserName.ToUpper()))
            yield return new ValidationResult(nameof(FailureReasons.UsernameAlreadyTaken), [nameof(UserName)]);


        // Only an active account can be edited here; reactivating a deactivated one is what the activate endpoint is for.  
        if (!users.GetAll().Any(user => user.Id == UserId && !user.Deactivated))
            yield return new ValidationResult(nameof(FailureReasons.UserNotFound), [nameof(UserId)]);


        if (RoleId.HasValue)

        {

            var roles = validationContext.GetRequiredService<IRepository<Role>>();


            if (!roles.GetAll().Any(role => role.Id == RoleId.Value))
                yield return new ValidationResult(nameof(FailureReasons.RoleNotFound), [nameof(RoleId)]);

        }

    }


    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]
    private static partial Regex PasswordRegex();

}


/// <summary>
///     Changes the password of the signed-in account. CurrentPassword is only required once the account has set its own password - while it still carries a temporary one the user cannot be expected to know it.
/// </summary>
public partial class UpdatePasswordRequest : IValidatableObject

{

    /// <summary>Ignored while the account is still on its temporary password, required and verified afterwards.</summary>
    public string? CurrentPassword { get; set; }


    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.PasswordRequired))]
    public string NewPassword { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)

    {

        if (string.IsNullOrWhiteSpace(NewPassword)) yield break;


        if (!PasswordRegex().IsMatch(NewPassword))
            yield return new ValidationResult(nameof(FailureReasons.WeakPassword), [nameof(NewPassword)]);

    }


    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]
    private static partial Regex PasswordRegex();

}


/// <summary>
///     Logout request. Logging out of one device is a client-side decision - dropping the token is enough. IsFromAllDevices is the explicit way to invalidate every token that was issued before this moment.
/// </summary>
public class LogoutRequest

{

    /// <summary>True invalidates every session of the signed-in account by recording a logout instant that older tokens can never be later than.</summary>
    public bool IsFromAllDevices { get; set; }

}
