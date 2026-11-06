using System;
using System.Linq;


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Filters;

using Microsoft.Extensions.DependencyInjection;


using Models;


using Services.RoleSystem.Interfaces;


namespace Services.RoleSystem;


/// <summary>
///     Base for the action filters that guard endpoints by role or permission. The guard runs its own DI scope to reach the configured IRoleValidator, so a token-claim validator and a database validator can both be plugged in without changing an endpoint. Denial answers 403 rather than 401: the caller proved who they are, the server is refusing what they asked for, and a client should read that as "this role cannot do that" instead of re-authenticating. The reason and what was missing are returned so a client can react to it.  
/// </summary>
public abstract class BaseRoleValidator(string[] roles) : ActionFilterAttribute

{

    protected string[] Roles = roles;

    protected IRoleValidator? RoleValidator;


    protected BaseRoleValidator() : this([])

    {

    }


    protected void InitializeServices(ActionExecutingContext context)

    {

        var scope = context.HttpContext.RequestServices.CreateScope();

        RoleValidator = (IRoleValidator)scope.ServiceProvider.GetService(typeof(IRoleValidator))!;

    }

}


/// <summary>The caller must hold every one of these roles.</summary>  
public class HasRole(params RoleNames[] roles) : BaseRoleValidator(roles.Select(u => u.ToString())
    .ToArray())

{

    public override void OnActionExecuting(ActionExecutingContext context)

    {

        InitializeServices(context);


        if (!RoleValidator!.ValidateRoles(context.HttpContext.User, Roles))
            context.Result = new ObjectResult(new { require = "all", missingRoles = Roles })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

    }

}


/// <summary>
///     The caller must hold at least one of these roles - the guard to use when several roles may reach the same endpoint. A caller holding none of them is refused with 403 even if their token itself is perfectly valid.  
/// </summary>
public class HasAnyRole : BaseRoleValidator

{

    public HasAnyRole(params RoleNames[] roles) : base(roles.Select(role => role.ToString())
        .ToArray())

    {

    }


    public override void OnActionExecuting(ActionExecutingContext context)

    {

        InitializeServices(context);


        if (!RoleValidator!.ValidateAnyRole(context.HttpContext.User, Roles))
            context.Result = new ObjectResult(new { require = "any", missingRoles = Roles })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

    }

}


/// <summary>The caller's role set must grant every one of these permissions.</summary>  
public class HasPermission(params PermissionNames[] permissions) : BaseRoleValidator(permissions
    .Select(u => u.ToString())
    .ToArray())

{

    public override void OnActionExecuting(ActionExecutingContext context)

    {

        InitializeServices(context);


        if (!RoleValidator!.ValidatePermissions(context.HttpContext.User, Roles))
            context.Result = new ObjectResult(new { require = "all", missingPermissions = Roles })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

    }

}


/// <summary>Any one of these permissions is enough.</summary>  
public class HasOnePermission(params PermissionNames[] permissions) : BaseRoleValidator(permissions
    .Select(u => u.ToString())
    .ToArray())

{

    public override void OnActionExecuting(ActionExecutingContext context)

    {

        InitializeServices(context);


        if (!RoleValidator!.ValidateOnePermission(context.HttpContext.User, Roles))
            context.Result = new ObjectResult(new { require = "one", missingPermissions = Roles })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

    }

}


public enum CrudVerb

{

    Create,

    Read,

    Update,

    Delete

}


/// <summary>Guards a generic CRUD action by the permission naming convention this template uses: "Verb ModelName" or the wildcard "Verb *".</summary>  
public class HasCrudPermission(CrudVerb crudVerb, Type? type = null) : BaseRoleValidator

{

    public override void OnActionExecuting(ActionExecutingContext context)

    {

        InitializeServices(context);


        lock (this)

        {

            var modelName = type == null ? context.Controller.GetType().GenericTypeArguments[0].Name : type.Name;


            Roles = [$"{crudVerb} {modelName}", $"{crudVerb} *"];


            if (!RoleValidator!.ValidateOnePermission(context.HttpContext.User, Roles))
                context.Result = new ObjectResult(new { require = "one", missingPermissions = Roles })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

        }

    }

}
