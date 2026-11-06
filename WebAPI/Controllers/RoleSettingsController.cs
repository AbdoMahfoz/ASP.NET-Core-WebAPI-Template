using System.Collections.Generic;


using BusinessLogic.Interfaces;


using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;


using Models;


using Services.DTOs;

using Services.DTOs.Requests;

using Services.RoleSystem;


namespace WebAPI.Controllers;


/// <summary>
///     Role administration: which roles exist, how many accounts sit in each one, and which permissions each role grants. Every endpoint here is guarded by HasAnyRole, so a caller whose account holds none of the listed roles is refused with 403 before the action runs - that guard is the point of this surface. Unlike the permissions registry endpoints elsewhere in the template, nothing here is reachable anonymously. Writes are whole-set writes: updating a role's permissions applies exactly the set you send rather than merging into whatever was already granted.  
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RoleSettingsController(IRolesAndPermissionsManager rolesAndPermissionsManager) : ControllerBase

{


    /// <summary>Every role with the number of accounts that currently carry it.</summary>
    /// <response code="200">Roles retrieved</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = typeof(IEnumerable<RoleUserCountDto>))]
    [HttpGet]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult GetRolesWithUserCount() => Ok(rolesAndPermissionsManager.ListRolesWithUserCount());


    /// <summary>Creates a role.</summary>
    /// <remarks>A name another role already owns is refused with 400.</remarks>
    /// <param name="request">Role name</param>
    /// <response code="200">Role created</response>
    /// <response code="400">Name blank or already taken</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPost]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult CreateRole([FromBody] CreateRoleRequest request) => rolesAndPermissionsManager.InsertRole(request.Name) > 0 ? Ok() : BadRequest();


    /// <summary>Renames a role in place so its existing permission and account assignments survive the rename.</summary>
    /// <param name="id">Role id</param>
    /// <param name="request">New name</param>
    /// <response code="200">Role updated</response>
    /// <response code="400">No such role, or the new name is already taken</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPut("{id:int}")]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult UpdateRole(int id, [FromBody] UpdateRoleRequest request) => rolesAndPermissionsManager.EditRole(id, request.Name) ? Ok() : BadRequest();


    /// <summary>The permission-to-role matrix in one response: every permission with the roles that grant it, plus every role.</summary>
    /// <response code="200">Matrix retrieved</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = typeof(RolesAndPermissionsDto))]
    [HttpGet("permissions-with-roles")]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult GetPermissionsWithRoles() => Ok(rolesAndPermissionsManager.ListPermissionsWithRoles());


    /// <summary>Deletes a role.</summary>
    /// <remarks>The delete is a soft delete, so audit history and action wiring that referenced the role stay readable.</remarks>
    /// <param name="id">Role id</param>
    /// <response code="200">Role deleted</response>
    /// <response code="400">No such role</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = null)]
    [HttpDelete("{id:int}")]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult DeleteRole(int id) => rolesAndPermissionsManager.DeleteRole(id) ? Ok() : BadRequest();


    /// <summary>Sets exactly which permissions a role grants.</summary>
    /// <remarks>The set you send is the resulting set: permissions already granted but absent from PermissionIds are removed, and an empty list clears the role's permissions. Unknown permission ids are rejected by validation with 400 before anything is written, so a partial write cannot happen.</remarks>
    /// <param name="request">Role and its complete permission id list</param>
    /// <response code="200">Role permissions updated</response>
    /// <response code="404">No such role</response>
    /// <response code="403">Caller holds none of the allowed roles</response>
    [ProducesResponseType(200, Type = null)]
    [HttpPut("permissions-with-roles")]
    [HasAnyRole(RoleNames.Admin)]
    public IActionResult UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest request) => rolesAndPermissionsManager.UpdateRolePermissions(request.RoleId, request.PermissionIds) ? Ok() : NotFound();

}
