using System.Collections.Generic;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Models.DataModels;
using Repository.ExtendedRepositories;
using Services.DTOs;

namespace WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    [ApiController]
    public class RolesAndPermissionsController : ControllerBase
    {
        private readonly IRolesAndPermissionsManager RolesAndPermissionsManager;
        public RolesAndPermissionsController(IRolesAndPermissionsManager RolesAndPermissionsManager)
        {
            this.RolesAndPermissionsManager = RolesAndPermissionsManager;
        }
        /// <summary>
        /// Get all available roles
        /// </summary>
        [ProducesResponseType(200, Type = typeof(IEnumerable<RoleDTO>))]
        [HttpGet("Roles")]
        public IActionResult GetAllRoles()
        {
            return Ok(RolesAndPermissionsManager.GetAllRoles());
        }
        /// <summary>
        /// Gets the role by it's name or id
        /// </summary>
        /// <remarks>
        /// <b>Only provide one of these parameters</b>
        /// </remarks>
        /// <response code="400">
        /// One of the following happened:
        ///   - You entered an invalid id, or an invalid role name
        ///   - You entered both an id and a role name
        ///   - You didn't enter any data
        /// </response>
        [ProducesResponseType(200, Type = typeof(RoleDTO))]
        [ProducesResponseType(400, Type = null)]
        [HttpGet("Roles/Get")]
        public IActionResult GetRole(int? roleId, string roleName)
        {
            if (roleId != null && !string.IsNullOrEmpty(roleName)) return BadRequest();
            RoleDTO res;
            if (roleId != null) res = RolesAndPermissionsManager.GetRoleById((int)roleId);
            else if (!string.IsNullOrEmpty(roleName)) res = RolesAndPermissionsManager.GetRoleByName(roleName);
            else return BadRequest();
            if (res == null) return BadRequest();
            else return Ok(res);
        }
        /// <summary>
        /// Insert a new role
        /// </summary>
        /// <remarks>
        /// Roles are created without any permissions as a starter
        /// </remarks>
        /// <response code="400">Role name already exists or not supplied</response>
        /// <response code="200">Role created, Id returned</response>
        [ProducesResponseType(200, Type = typeof(int))]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Roles")]
        public IActionResult InsertRole([FromQuery]string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return BadRequest();
            int id = RolesAndPermissionsManager.InsertRole(roleName);
            if (id == -1) return BadRequest();
            return Ok(id);
        }
        /// <summary>
        /// Deletes a role
        /// </summary>
        /// <response code="200">Role deleted successfully</response>
        /// <response code="400">Given Id doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpDelete("Roles")]
        public IActionResult DeleteRole(int roleId)
        {
            if (RolesAndPermissionsManager.DeleteRole(roleId))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Assigns role to user
        /// </summary>
        /// <response code="200">Role assigned successfully</response>
        /// <response code="400">The given role or the given user id doesn't exist, or the given user already has the given role</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Roles/User")]
        public IActionResult AssignRoleToUser(string roleName, int userId)
        {
            if (RolesAndPermissionsManager.AssignRoleToUser(roleName, userId))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Unassigns role of user
        /// </summary>
        /// <response code="200">Role removed successfully</response>
        /// <response code="400">The given role or the given user id doesn't exist, or the given user doesn't have the given role</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpDelete("Roles/User")]
        public IActionResult RemoveRoleFromUser(string roleName, int userId)
        {
            if (RolesAndPermissionsManager.RemoveRoleFromUser(roleName, userId))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Assigns an action to a role
        /// </summary>
        /// <response code="400">Role already registered to this action</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Roles/Action")]
        public IActionResult RegisterRoleToAction(string roleName, string actionName)
        {
            try
            {
                RolesAndPermissionsManager.RegisterRoleToAction(actionName, roleName);
                return Ok();
            }
            catch(RoleAlreadyAssignedException)
            {
                return BadRequest();
            }
        }
        /// <summary>
        /// Gets the roles assigned to an action
        /// </summary>
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        [HttpGet("Roles/Action")]
        public IActionResult GetRolesOfAction(string actionName)
        {
            return Ok(RolesAndPermissionsManager.GetRolesOfAction(actionName));
        }
        /// <summary>
        /// Gets the permissions assigned to an action
        /// </summary>
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        [HttpGet("Permissions/Action")]
        public IActionResult GetPermissionsOfAction(string actionName)
        {
            return Ok(RolesAndPermissionsManager.GetPermissionsOfAction(actionName));
        }
        /// <summary>
        /// Removes role from an action
        /// </summary>
        /// <response code="400">The given action doesn't have the given role</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpDelete("Roles/Action")]
        public IActionResult RemoveRoleFromAction(string roleName, string actionName)
        {
            if (RolesAndPermissionsManager.RemoveRoleFromAction(actionName, roleName))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Get all permissions
        /// </summary>
        [ProducesResponseType(200, Type = typeof(IEnumerable<PermissionDTO>))]
        [HttpGet("Permissions")]
        public IActionResult GetAllPermissions()
        {
            return Ok(RolesAndPermissionsManager.GetAllPermissions());
        }
        /// <summary>
        /// Gets the permission by it's name or id
        /// </summary>
        /// <remarks>
        /// <b>Only provide one of these parameters</b>
        /// </remarks>
        /// <response code="400">
        /// One of the following happened:
        ///   - You entered an invalid id, or an invalid permission name
        ///   - You entered both an id and a permission name
        ///   - You didn't enter any data
        /// </response>
        [ProducesResponseType(200, Type = typeof(PermissionDTO))]
        [ProducesResponseType(400, Type = null)]
        [HttpGet("Permissions/Get")]
        public IActionResult GetPermission(int? permissionId, string permissionName)
        {
            if (permissionId != null && !string.IsNullOrEmpty(permissionName)) return BadRequest();
            PermissionDTO res;
            if (permissionId != null) res = RolesAndPermissionsManager.GetPermissionById((int)permissionId);
            else if (!string.IsNullOrEmpty(permissionName)) res = RolesAndPermissionsManager.GetPermissionByName(permissionName);
            else return BadRequest();
            if (res == null) return BadRequest();
            else return Ok(res);
        }
        /// <summary>
        /// Insert a permission
        /// </summary>
        /// <response code="400">Permission already exists</response>
        [ProducesResponseType(200, Type = typeof(int))]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Permissions")]
        public IActionResult InsertPermission(string permissionName)
        {
            if (permissionName == null) return BadRequest();
            int Id = RolesAndPermissionsManager.InsertPermission(permissionName);
            if (Id == -1)
                return BadRequest();
            return Ok(Id);
        }
        /// <summary>
        /// Deletes a permission
        /// </summary>
        /// <response code="400">The given permission Id doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpDelete("Permissions")]
        public IActionResult DeletePermission(string permission)
        {
            if (RolesAndPermissionsManager.DeletePermission(permission))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Gets the permissions of a role
        /// </summary>
        /// <response code="400">Role doesn't exist</response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<Permission>))]
        [ProducesResponseType(400, Type = null)]
        [HttpGet("Permissions/Role")]
        public IActionResult GetPermissionsOfRole(string roleName)
        {
            var res = RolesAndPermissionsManager.GetPermissionsOfRole(roleName);
            if (res == null) BadRequest();
            return Ok(res);
        }
        /// <summary>
        /// Assigns permission to Role
        /// </summary>
        /// <response code="400">either the role or the permission doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Permissions/Role")]
        public IActionResult AssignPermissionToRole(string roleName, string permissionName)
        {
            if (RolesAndPermissionsManager.AssignPermissionToRole(roleName, permissionName))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Removes permission to Role
        /// </summary>
        /// <response code="400">either the role or the permission doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(200, Type = null)]
        [HttpDelete("Permissions/Role")]
        public IActionResult RemovePermissionFromRole(string roleName, string permissionName)
        {
            if (RolesAndPermissionsManager.RemovePermissionFromRole(roleName, permissionName))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Grants access to an action to a certain permission
        /// </summary>
        /// <response code="400">Permission doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpPost("Permissions/Action")]
        public IActionResult RegisterPermissionToAction(string permissionName, string actionName)
        {
            if (RolesAndPermissionsManager.RegisterPermissionToAction(actionName, permissionName))
                return Ok();
            else
                return BadRequest();
        }
        /// <summary>
        /// Removes access to an action to a certain permission
        /// </summary>
        /// <response code="400">Permission doesn't exist</response>
        [ProducesResponseType(200, Type = null)]
        [ProducesResponseType(400, Type = null)]
        [HttpDelete("Permissions/Action")]
        public IActionResult RemovePermissionFromAction(string permissionName, string actionName)
        {
            if (RolesAndPermissionsManager.RemovePermissionFromAction(actionName, permissionName))
                return Ok();
            else
                return BadRequest();
        }
    }
}