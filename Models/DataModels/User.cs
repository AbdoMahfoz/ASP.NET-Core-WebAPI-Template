using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.DataModels.RoleSystem;

namespace Models.DataModels;

public class User : BaseModel
{
    [Required] public string UserName { get; set; }

    [Required] public string Password { get; set; }

    /// <summary>
    ///     True while the account still carries a password someone else set for it (a generated temporary password or an admin reset). A first login does not have to prove the old password, and the flag clears as soon as the user sets their own.
    /// </summary>
    public bool IsFirstLogin { get; set; }

    /// <summary>
    ///     Consecutive failed logins for this account. Authentication refuses further attempts once AppSettings.AccountLockThreshold is passed, and a successful login resets the count.
    /// </summary>
    public int LogInAttempts { get; set; }

    /// <summary>
    ///     Deactivated accounts keep their rows and history but can no longer authenticate or be listed as active users.
    /// </summary>
    public bool Deactivated { get; set; }

    public bool LoggedIn { get; set; } = false;
    public DateTime? LastLogOut { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; }
}