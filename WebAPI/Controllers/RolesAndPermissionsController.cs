using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.DataModels;
using Services.DTOs;

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
        ///     Get All Available Roles
        /// </summary>
        /// <response code="200"> Available Roles</response>
        /// <response code="403">UnAuthorized Access </response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<Role>))]
        [ProducesResponseType(403, Type = null)]
        [HttpGet("GetAllRoles")]
        public IActionResult GetAllRoles()
        {
            return StatusCode(StatusCodes.Status201Created, _rolesAndPermissionsManager.GetAllRoles().ToList());
        }

        /// <summary>
        ///     Gets the Role By Id
        /// </summary>
        /// <remarks>
        ///     Id must be greater than 0
        /// </remarks>
        /// <param name="roleId">Role Id</param>
        /// <response code="500">Role Id is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Role</response>
        [ProducesResponseType(200, Type = typeof(Role))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetRoleById")]
        public IActionResult GetRoleById(int roleId)
        {
            if (roleId <= 0) return BadRequest();
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetRoleById(roleId));
        }

        /// <summary>
        ///     Gets the Role By Role's Name
        /// </summary>
        /// <param name="roleName">Role Id</param>
        /// <response code="500">Role Name is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Role</response>
        [ProducesResponseType(200, Type = typeof(Role))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetRoleByName")]
        public IActionResult GetRoleByName(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return BadRequest();
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetRoleByName(roleName));
        }

        /// <summary>
        ///     Gets the Role By Id
        /// </summary>
        /// <remarks>
        ///     Id must be greater than 0
        /// </remarks>
        /// <param name="roleId">Role Id</param>
        /// <response code="500">Role Id is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Role</response>
        [ProducesResponseType(200, Type = typeof(Role))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetRoleById")]
        public IActionResult GetPermissionsOfRole(int roleId)
        {
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetRoleById(roleId));
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
        public IActionResult InsertRole([FromBody] RoleDTO role)
        {
            if (role == null) return BadRequest();
            return StatusCode(StatusCodes.Status201Created, _rolesAndPermissionsManager.InsertRole(role));
        }

        /// <summary>
        ///     removes a role from the DB
        /// </summary>
        /// <param name="roleId">Role's Id to be deleted </param>
        /// <response code="500"></response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="202">Role Deleted</response>
        [ProducesResponseType(202, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpDelete("DeleteRole")]
        public IActionResult DeleteRole(int roleId)
        {
            if (roleId <= 0) return BadRequest();
            _rolesAndPermissionsManager.DeleteRole(roleId);
            return StatusCode(StatusCodes.Status202Accepted);
        }

        /// <summary>
        ///     Gets the Permissions Of Role
        /// </summary>
        /// <remarks>
        ///     Please Make sure to spell the rolename correctly
        /// </remarks>
        /// <param name="roleName">Role Name</param>
        /// <response code="500">Role name is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Role's Permission</response>
        [ProducesResponseType(201, Type = typeof(IEnumerable<Permission>))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetPermissionsOfRole")]
        public IActionResult GetPermissionsOfRole(string roleName)
        {
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetPermissionsOfRole(roleName).ToList());
        }

        /// <summary>
        ///     Assignes permission to Role
        /// </summary>
        /// <remarks>
        ///     Please Make sure to spell the rolename and permission correctly
        /// </remarks>
        /// <param name="roleName">Role Name</param>
        /// <param name="permissionName">Permission Name</param>
        /// <response code="500">Role or Permission name is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200"></response>
        [ProducesResponseType(201, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("AssignPermissionToRole")]
        public IActionResult AssignPermissionToRole(string roleName, string permissionName)
        {
            _rolesAndPermissionsManager.AssignPermissionToRole(roleName, permissionName);
            return StatusCode(StatusCodes.Status200OK);
        }

        /// <summary>
        ///     Removes permission to Role
        /// </summary>
        /// <remarks>
        ///     Please Make sure to spell the rolename and permission correctly
        /// </remarks>
        /// <param name="roleName">Role Name</param>
        /// <param name="permissionName">Permission Name</param>
        /// <response code="500">Role or Permission name is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200"></response>
        [ProducesResponseType(201, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("RemovePermissionFromRole")]
        public IActionResult RemovePermissionFromRole(string roleName, string permissionName)
        {
            _rolesAndPermissionsManager.RemovePermissionFromRole(roleName, permissionName);
            return StatusCode(StatusCodes.Status200OK);
        }

        /// <summary>
        ///     Get All Available Permissions
        /// </summary>
        /// <response code="200"> Available Permissions</response>
        /// <response code="403">UnAuthorized Access </response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<Permission>))]
        [ProducesResponseType(403, Type = null)]
        [HttpGet("GetAllPermissions")]
        public IActionResult GetAllPermissions()
        {
            return StatusCode(StatusCodes.Status201Created, _rolesAndPermissionsManager.GetAllPermissions().ToList());
        }

        /// <summary>
        ///     Gets the Permission By Id
        /// </summary>
        /// <remarks>
        ///     Id must be greater than 0
        /// </remarks>
        /// <param name="permissionId">Permission Id</param>
        /// <response code="500">Permission Id is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Permission</response>
        [ProducesResponseType(200, Type = typeof(Permission))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetPermissionById")]
        public IActionResult GetPermissionById(int permissionId)
        {
            if (permissionId <= 0) return BadRequest();
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetPermissionById(permissionId));
        }

        /// <summary>
        ///     Gets the Permission By Name
        /// </summary>
        /// <param name="permissionName">Permission Name</param>
        /// <response code="500">Permission Name is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200">Permission</response>
        [ProducesResponseType(200, Type = typeof(Permission))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("GetPermissionByName")]
        public IActionResult GetPermissionByName(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName)) return BadRequest();
            return StatusCode(StatusCodes.Status200OK, _rolesAndPermissionsManager.GetPermissionByName(permissionName));
        }

        /// <summary>
        ///     Insert a new permission in the DB
        /// </summary>
        /// <param name="permission">permission to be Inserted</param>
        /// <response code="500">permission's data is not correct or Already Exists</response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="201">permission Created</response>
        [ProducesResponseType(201, Type = typeof(Permission))]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpPost("InsertPermission")]
        public IActionResult InsertPermission([FromBody] PermissionDTO permission)
        {
            if (permission == null) return BadRequest();
            return StatusCode(StatusCodes.Status201Created, _rolesAndPermissionsManager.InsertPermission(permission));
        }

        /// <summary>
        ///     removes a Permission from the DB
        /// </summary>
        /// <param name="permissionId">permission's Id to be deleted</param>
        /// <response code="500"></response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="202">Permission Deleted</response>
        [ProducesResponseType(202, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpDelete("DeletePermission")]
        public IActionResult DeletePermission(int permissionId)
        {
            if (permissionId <= 0) return BadRequest();
            _rolesAndPermissionsManager.DeleteRole(permissionId);
            return StatusCode(StatusCodes.Status202Accepted);
        }

        /// <summary>
        ///     Assignes Role to User
        /// </summary>
        /// <remarks>
        ///     Please Make sure to spell the rolename and userId correctly
        /// </remarks>
        /// <param name="roleName">Role Name</param>
        /// <param name="userId">User's Id</param>
        /// <response code="500">RoleName or UserId is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200"></response>
        [ProducesResponseType(201, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("AssignRoleToUser")]
        public IActionResult AssignRoleToUser(string roleName, int userId)
        {
            _rolesAndPermissionsManager.AssignRoleToUser(roleName, userId);
            return StatusCode(StatusCodes.Status200OK);
        }

        /// <summary>
        ///     removes Role from User
        /// </summary>
        /// <remarks>
        ///     Please Make sure to spell the rolename and userId correctly
        /// </remarks>
        /// <param name="roleName">Role Name</param>
        /// <param name="userId">User's Id</param>
        /// <response code="500">RoleName or UserId is not correct </response>
        /// <response code="403">UnAuthorized Access </response>
        /// <response code="200"></response>
        [ProducesResponseType(201, Type = null)]
        [ProducesResponseType(403, Type = null)]
        [ProducesResponseType(500, Type = null)]
        [HttpGet("RemoveRoleFromUser")]
        public IActionResult RemoveRoleFromUser(string roleName, int userId)
        {
            _rolesAndPermissionsManager.RemoveRoleFromUser(roleName, userId);
            return StatusCode(StatusCodes.Status200OK);
        }
    }
}