using System;
using System.Collections.Generic;

using System.Threading.Tasks;


using BusinessLogic.Interfaces;


using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Options;


using Models;

using Models.Helpers;


using Services.DTOs;

using Services.DTOs.Requests;

using Services.Extensions;
using Services.Exceptions;

using Services.RoleSystem;


namespace WebAPI.Controllers;


/// <summary>
///     Accounts and sessions. Token is the anonymous entry point; everything else needs a live session, which means a token that is still valid for an account that is logged in and has not been logged out since the token was issued. Administration endpoints (register, listing, edit, deactivate, activate) are guarded by the HasAnyRole filter so a role that is not in the caller's list produces 403 rather than an unauthenticated 401. Scope comes from the caller's own TenantId claim - this template has no site concept - and passwords are only ever accepted in a request body, never returned in a read model except for a generated temporary password at registration.
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController(
    IOptions<AppSettings> options,

    IAuth auth,

    IAccountLogic accountLogic) : ControllerBase

{


    /// <summary>
    ///     The only official way to get an access token for this API.
    /// </summary>
    /// <remarks>
    ///     For security reasons the response does not distinguish a wrong username from a wrong password: both answer 404. Once an account has passed AppSettings.AccountLockThreshold consecutive failed logins it answers 403 it answers 403 until a successful sign-in clears the counter. A locked account is answered 403 so a client can tell "slow down / contact an administrator" from "wrong password".  
    /// </remarks>
    /// <param name="request">User credentials</param>
    /// <response code="404">Invalid username or password</response>
    /// <response code="403">Account locked after too many login attempts</response>
    /// <response code="202">Login successful</response>
    [ProducesResponseType(202, Type = typeof(UserAuthenticationResult))]
    [ProducesResponseType(403, Type = null)]
    [ProducesResponseType(404, Type = null)]
    [HttpPost("Token")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] UserAuthenticationRequest request)

    {

        try

        {

            var res = auth.Authenticate(request);


            if (res.token == null) return res.userId < 0 ? Forbid() : NotFound();


            return StatusCode(StatusCodes.Status202Accepted, BuildAuthenticationResult(res.userId, res.token));

        }

        catch (Exception e) when (e is KeyNotFoundException or ArgumentException)

        {

            // GetUser throws for an unknown username; both that and a malformed credential answer like a wrong password.  
            return NotFound();

        }

    }


    // Any negative user id from the auth stack means "refuse this account" - locked or deactivated - so the caller gets 403 instead of a wrong-password 404.  


    /// <summary>
    ///     Issues a new token for the signed-in account without asking for its password again.
    /// </summary>
    /// <remarks>
    ///     Use this instead of storing the username and password on the client. The session is still validated server-side, so a refreshed token is refused once the account has been logged out or deactivated.
    /// </remarks>
    /// <response code="200">New token</response>
    [ProducesResponseType(200, Type = typeof(UserAuthenticationResult))]
    [HttpPost("RefreshToken")]
    public IActionResult RefreshToken() => Ok(BuildAuthenticationResult(User.GetId(), auth.GenerateToken(User.GetId()).token));


    /// <summary>
    ///     Registers an account.
    /// </summary>
    /// <remarks>
    ///     Leave Password blank to have a strong temporary password generated and returned once in the response body. RoleId picks the role; when it is omitted the caller's own role is inherited and, failing that, the seeded Admin role. Only an Admin role holder may register accounts.
    /// </remarks>
    /// <param name="request">Account details</param>
    /// <response code="200">Account created</response>
    /// <response code="400">Request failed validation (weak password, unknown role)</response>
    /// <response code="403">Caller's role is not allowed to create accounts</response>
    /// <response code="409">Username already exists</response>
    [ProducesResponseType(200, Type = typeof(CreateUserResult))]
    [ProducesResponseType(409, Type = null)]
    [HttpPost("Register")]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)

    {

        var created = await accountLogic.Register(request, request.RoleId, User.GetId());


        // The reason travels in the body so a client can react to a taken username without scraping message text.  
        return created == null
            ? Conflict(new { reason = nameof(FailureReasons.UsernameAlreadyTaken) })
            : Ok(created);

    }


    /// <summary>
    ///     Changes the password of the signed-in account.
    /// </summary>
    /// <remarks>
    ///     While the account is still on a temporary or administrator-issued password CurrentPassword may be omitted. Afterwards it must be supplied and must match. A first login clears itself when this call succeeds.
    /// </remarks>
    /// <param name="request">Current and new password</param>
    /// <response code="200">Password updated</response>
    /// <response code="400">New password rejected by the strength rule</response>
    /// <response code="403">Current password is missing or invalid</response>
    [ProducesResponseType(200, Type = null)]
    [ProducesResponseType(403, Type = null)]
    [HttpPost("reset-password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request) => await accountLogic.UpdatePassword(User.GetId(), request.CurrentPassword, request.NewPassword) ? Ok() : Forbid();


    /// <summary>
    ///     Logs the signed-in account out, optionally from every device at once.
    /// </summary>
    /// <remarks>
    ///     Logging out of one device is a client-side decision - discarding the token is enough, so IsFromAllDevices false returns success without touching server-side sessions. IsFromAllDevices true records a logout instant that every earlier token is now older than, which denies all of them at once; each device then has to sign in again. Deactivating an account does the same thing automatically.  
    /// </remarks>
    /// <param name="request">Whether every session should be denied</param>
    /// <response code="200">Logged out</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPost("Logout")]
    public IActionResult Logout([FromBody] LogoutRequest? request)

    {

        if (request?.IsFromAllDevices != true) return Ok();


        auth.Logout(User.GetId());


        return Ok();

    }


    /// <summary>All active accounts in the caller's tenant.</summary>
    /// <response code="200">Users retrieved</response>
    /// <response code="403">Caller's role is not allowed to read accounts</response>
    [ProducesResponseType(200, Type = typeof(IEnumerable<UserDTO>))]
    [HttpGet]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> All() => Ok(await accountLogic.All(User.GetTenantId()));


    /// <summary>A single active account by id.</summary>
    /// <param name="id">Account id</param>
    /// <response code="200">User retrieved</response>
    /// <response code="404">No such account in the caller's tenant</response>
    [ProducesResponseType(200, Type = typeof(UserDTO))]
    [HttpGet("{id:int}")]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> GetById(int id) => await accountLogic.GetById(id, User.GetTenantId()) is { } user ? Ok(user) : NotFound();


    /// <summary>
    ///     Updates an account: rename it, reset its password (which puts it back into first-login state), deactivate it - invalidating every session it holds - or move it to another role, which replaces its roles rather than adding to them.
    /// </summary>
    /// <param name="request">The update to apply</param>
    /// <response code="200">User updated</response>
    /// <response code="404">No such active account in the caller's tenant</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPut]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)

    {

        await accountLogic.Update(request, User.GetId());


        return Ok();

    }


    /// <summary>All deactivated accounts in the caller's tenant.</summary>
    /// <response code="200">Deactivated users retrieved</response>
    [ProducesResponseType(200, Type = typeof(IEnumerable<UserDTO>))]
    [HttpGet("deactivated")]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> Deactivated() => Ok(await accountLogic.AllDeactivated(User.GetTenantId()));


    /// <summary>Returns a deactivated account to service.</summary>
    /// <param name="id">Account id</param>
    /// <response code="200">User activated</response>
    /// <response code="404">No deactivated account with that id in the caller's tenant</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPut("{id:int}/activate")]
    [HasAnyRole(RoleNames.Admin)]
    public async Task<IActionResult> Activate(int id) => await accountLogic.ActivateUser(id, User.GetId(), User.GetTenantId()) ? Ok() : NotFound();


    private UserAuthenticationResult BuildAuthenticationResult(int userId, string token) => new(userId, token, options.Value.TokenExpirationMinutes);

}
