using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Services.DTOs;

using Services.DTOs.Requests;


namespace BusinessLogic.Interfaces;


/// <summary>
///     Account administration for the template: registration, listing, editing, deactivation/reactivation and password changes. Scope is expressed as a tenant id because this template has no site concept - every method takes an optional tenantId so an administrator acting across tenants can ask for one explicitly and null means the tenant of the current request. Register returns null when the username is taken so the endpoint can answer 409 without an exception being used for flow control. Passwords never appear in a returned DTO.
/// </summary>
public interface IAccountLogic

{

    /// <summary>
    ///     Creates an account. roleId picks the role to assign; when it is null the acting user's own role is inherited and, failing that, the seeded Admin role. createdById is whoever performed the registration. Returns null when the username already exists.
    /// </summary>
    Task<CreateUserResult> Register(CreateUserRequest request, int? roleId = null, int? createdById = null);


    /// <summary>The permission names the account holds through its roles - names rather than enum values because permissions are stored as rows in this template.</summary>
    Task<IEnumerable<string>> GetUserPermissions(int userId);


    /// <summary>True while the account still has to set its own password.</summary>
    Task<bool> IsFirstLogin(int userId);


    /// <summary>
    ///     Sets a new password for an account. While the account is still on its first login currentPassword may be null; afterwards it must be supplied and must match. Returns false when the account does not exist or the current password does not match.
    /// </summary>
    Task<bool> UpdatePassword(int userId, string? currentPassword, string newPassword);


    /// <summary>
    ///     Applies an update to an account: rename, reset its password (which puts it back into first-login state), deactivate it - which also invalidates its sessions - or move it to another role. updatedBy is whoever performed the change. Throws NotFoundException when no such account exists in scope.
    /// </summary>
    Task Update(UpdateUserRequest request, int updatedBy);


    Task<IEnumerable<UserDTO>> All(int? tenantId = null);


    Task<UserDTO> GetById(int id, int? tenantId = null);


    Task<IEnumerable<UserDTO>> AllDeactivated(int? tenantId = null);


    /// <summary>Returns an deactivated account to service; false when no deactivated account has that id in scope.</summary>
    Task<bool> ActivateUser(int userId, int activatedBy, int? tenantId = null);

}
