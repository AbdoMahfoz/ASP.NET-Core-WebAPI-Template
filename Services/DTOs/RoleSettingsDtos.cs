using System.Collections.Generic;


namespace Services.DTOs;


/// <summary>One role and how many accounts currently carry it - the read model behind a role-administration screen that shows where people actually sit.</summary>  
public class RoleUserCountDto

{

    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int UsersCount { get; set; }

}


/// <summary>A permission and the roles that grant it.</summary>  
public class PermissionRolesDto

{

    public int PermissionId { get; set; }

    public string PermissionName { get; set; } = null!;

    public IEnumerable<RoleDto> Roles { get; set; } = [];

}


/// <summary>The full permission-to-role matrix in one response: every permission with the roles holding it, plus every role so a client can render both axes without further calls.</summary>  
public class RolesAndPermissionsDto

{

    public IEnumerable<PermissionRolesDto> Permissions { get; set; } = null!;

    public IEnumerable<RoleDto> Roles { get; set; } = null!;

}


/// <summary>One row of a role-permission write: a permission and the complete set of role ids that should hold it afterwards.</summary>  
public class UpdateRolePermissionsDto

{

    public int PermissionId { get; set; }

    /// <summary>The exact roles that should grant this permission after the update - roles not listed lose it.</summary>  
    public IEnumerable<int> Roles { get; set; } = [];

}
