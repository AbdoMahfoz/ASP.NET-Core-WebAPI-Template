using System;
using System.Collections.Generic;


namespace Services.DTOs;


/// <summary>
///     A user as the account endpoints expose it: identity, owning tenant, when the row was created, whether it can still authenticate, and the role names it carries. Roles are a many-to-many join in this template, so one account can legitimately carry several role names - the reference implementation had a single role column and therefore a single role name.
/// </summary>
public class UserDTO

{

    public int Id { get; set; }

    public string UserName { get; set; }

    public int TenantId { get; set; }

    public DateTime AddedDate { get; set; }

    /// <summary>A deactivated account keeps its row and history but cannot authenticate or appear in the active listing.</summary>
    public bool Deactivated { get; set; }

    /// <summary>True while the account still has to set its own password.</summary>
    public bool IsFirstLogin { get; set; }

    public IEnumerable<string> RoleNames { get; set; } = [];

}


/// <summary>
///     What registering an account hands back: the new id, the generated temporary password only when the caller did not supply one (never echo a password the caller already knows), and whether the account still has a first login to complete.
/// </summary>
public class CreateUserResult

{

    public int UserId { get; set; }

    /// <summary>Populated only for a generated temporary password, and only once - it is not stored anywhere readable afterwards.</summary>
    public string? TempPassword { get; set; }

    public bool IsFirstLogin { get; set; }

}
