using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.DataModels;

namespace WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesAndPermissionsController : ControllerBase
    {
        private readonly IRolesAndPermissionsManager _rolesAndPermissionsManager;

        public RolesAndPermissionsController(IRolesAndPermissionsManager rolesAndPermissionsManager)
        {
            _rolesAndPermissionsManager = rolesAndPermissionsManager;
        }

        /// <summary>
        ///     Insert a new role in the DB
        /// </summary>
        /// <remarks>
        ///     Roles are created without any permissions as a starter
        /// </remarks>
        /// <param name="role">Role to be Inserted</param>
        /// <response code="500">Role data is not correct or Already Exists</response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="201">Role Created</response>
        [ProducesResponseType(201, Type = typeof(Role))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpPost("InsertRole")]
        public IActionResult InsertRole([FromBody] Role role)
        {
            if (role == null) return BadRequest();
            var resRole = _rolesAndPermissionsManager.InsertRole(role);
            return StatusCode(StatusCodes.Status201Created, resRole.Id);
        }
    }
}