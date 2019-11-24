using System;
using System.Collections.Generic;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models.DataModels;
using Models.Helpers;
using Services.DTOs;
using Services.Extensions;

namespace WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly IAuth Auth;
        private readonly IAccountLogic AccountLogic;
        private readonly IOptions<AppSettings> options;
        public AccountController(IOptions<AppSettings> options, IAuth Auth, IAccountLogic AccountLogic)
        {
            this.Auth = Auth;
            this.options = options;
            this.AccountLogic = AccountLogic;
        }
        /// <summary>
        /// The only official way to get an access token for this API
        /// </summary>
        /// <remarks>
        /// For security reasons, the response doesn't confirm the username existence if the password is wrong.
        /// The Username and Password combination must be valid
        /// </remarks>
        /// <param name="request">User credentials</param>
        /// <response code="404">Invalid username or password</response>
        /// <response code="202">Login successful</response>
        [ProducesResponseType(202, Type = typeof(UserAuthenticationResult))]
        [ProducesResponseType(404, Type = null)]
        [HttpPost("Token")]
        public IActionResult Login([FromBody]UserAuthenticationRequest request)
        {
            try
            {
                User user = Auth.Authenticate(request);
                if (user == null)
                    return NotFound();
                return StatusCode(StatusCodes.Status202Accepted, new UserAuthenticationResult(user.Id, user.Token, options.Value.TokenExpirationMinutes));
            }
            catch(KeyNotFoundException)
            {
                return NotFound();
            }
        }
        /// <summary>
        /// Creates a new token with the same credientials of the exisiting one
        /// </summary>
        /// <remarks>
        /// Use this to bypass the expiration date of the token without requiring the user to re-enter their passwords or dangerously save the username and password locally
        /// </remarks>
        /// <response code="200">New token</response>
        [Authorize]
        [ProducesResponseType(200, Type = typeof(UserAuthenticationResult))]
        [HttpPost("RefreshToken")]
        public IActionResult RefreshToken()
        {
            User user = Auth.GenerateToken(User.GetId());
            return Ok(new UserAuthenticationResult(user.Id, user.Token, options.Value.TokenExpirationMinutes));
        }
        /// <summary>
        /// Registers the user into the database
        /// </summary>
        /// <param name="request">User credentials</param>
        /// <response code="200">Operation successful</response>
        /// <response code="409">Username already exists</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(409, Type = null)]
        [HttpPost("Register")]
        public IActionResult Register([FromBody]UserAuthenticationRequest request)
        {
            if (AccountLogic.Register(request, "User"))
                return Ok();
            return StatusCode(StatusCodes.Status409Conflict);
        }
        /// <summary>
        /// Logs-out user from all devices
        /// </summary>
        /// <remarks>
        /// <b style="color:red">WARNING</b>: This will deny access to all current tokens; user will be logged-out from <b>ALL</b> devices and each device will have to log-in again
        /// if you want to log out the user from a single client, just get rid of the token that your client stores
        /// </remarks>
        /// <response code="200">User logged-out successfully</response>
        [Authorize]
        [ProducesResponseType(200, Type = null)]
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Auth.Logout(User.GetId());
            return Ok();
        }
    }
}
