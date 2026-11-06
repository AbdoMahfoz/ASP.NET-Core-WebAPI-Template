using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.Linq;


using Microsoft.Extensions.DependencyInjection;


using Models.DataModels.RoleSystem;


using Repository;


using Services.Exceptions;


namespace Services.DTOs.Requests;


/// <summary>Creates a role. A name that another role already uses is rejected with the machine-readable RoleAlreadyExists code.</summary>  
public class CreateRoleRequest

{

    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.InvalidParameter))]
    public string Name { get; set; } = null!;

}


/// <summary>Renames a role. Roles are referenced by name throughout the template's permission wiring, so renaming replaces the role rather than creating a second one.</summary>  
public class UpdateRoleRequest

{

    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.InvalidParameter))]
    public string Name { get; set; } = null!;

}


/// <summary>
///     Sets exactly which permissions one role grants. PermissionIds is the resulting set, not an addition: permissions already granted but missing from the list are taken away, and an empty list clears the role's permissions - deliberately explicit so a role-administration screen cannot half-apply a change. The checks run against the caller's own request scope, so roles and permissions are looked up inside the caller's tenant.  
/// </summary>
public partial class UpdateRolePermissionsRequest : IValidatableObject

{

    [Required(ErrorMessage = nameof(FailureReasons.RoleNotFound))]
    public int RoleId { get; set; }


    public IEnumerable<int> PermissionIds { get; set; } = [];


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)

    {

        var roles = validationContext.GetRequiredService<IRepository<Role>>();


        if (!roles.GetAll().Any(role => role.Id == RoleId))
            yield return new ValidationResult(nameof(FailureReasons.RoleNotFound), [nameof(RoleId)]);


        var permissionIds = (PermissionIds ?? []).ToList();


        if (permissionIds.Count == 0) yield break;


        var permissions = validationContext.GetRequiredService<IRepository<Permission>>();


        var known = permissions.GetAll().Where(permission => permissionIds.Contains(permission.Id))
            .Select(permission => permission.Id)
            .ToList();


        var missing = permissionIds.Except(known).ToList();


        if (missing.Count > 0)
            yield return new ValidationResult(nameof(FailureReasons.NotFound), [nameof(PermissionIds)]);

    }

}
