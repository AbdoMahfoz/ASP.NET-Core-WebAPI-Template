using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem;

public abstract class BaseRoleValidator : ActionFilterAttribute
{
    protected string[] Roles;
    protected IRoleValidator RoleValidator;

    protected BaseRoleValidator(string[] roles)
    {
            Roles = roles;
        }

    protected BaseRoleValidator()
    {
        }

    protected void InitializeServices(ActionExecutingContext context)
    {
            var scope = context.HttpContext.RequestServices.CreateScope();
            RoleValidator = (IRoleValidator)scope.ServiceProvider.GetService(typeof(IRoleValidator));
        }
}

public class HasRole(params RoleNames[] roles) : BaseRoleValidator(roles.Select(u => u.ToString())
    .ToArray())
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
            InitializeServices(context);
            if (!RoleValidator.ValidateRoles(context.HttpContext.User, Roles))
            {
                context.Result = new ObjectResult(new { require = "all", missingRoles = Roles })
                {
                    StatusCode = 401
                };
            }
        }
}

public class HasPermission(params PermissionNames[] permissions) : BaseRoleValidator(permissions
    .Select(u => u.ToString())
    .ToArray())
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
            InitializeServices(context);
            if (!RoleValidator.ValidateOnePermission(context.HttpContext.User, Roles))
            {
                context.Result = new ObjectResult(new { require = "all", missingPermissions = Roles })
                {
                    StatusCode = 401
                };
            }
        }
}

public class HasOnePermission(params PermissionNames[] permissions) : BaseRoleValidator(permissions
    .Select(u => u.ToString())
    .ToArray())
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
            InitializeServices(context);
            if (!RoleValidator.ValidatePermissions(context.HttpContext.User, Roles))
            {
                context.Result = new ObjectResult(new { require = "one", missingPermissions = Roles })
                {
                    StatusCode = 401
                };
            }
        }
}


public enum CrudVerb
{
    Create,
    Read,
    Update,
    Delete
}

public class HasCrudPermission(CrudVerb crudVerb, Type type = null) : BaseRoleValidator
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
            InitializeServices(context);
            lock (this)
            {
                var modelName = type == null ? context.Controller.GetType().GenericTypeArguments[0].Name : type.Name;
                Roles = new[] { $"{crudVerb} {modelName}", $"{crudVerb} *" };
                if (!RoleValidator.ValidateOnePermission(context.HttpContext.User, Roles))
                {
                    context.Result = new ObjectResult(new { require = "one", missingPermissions = Roles })
                    {
                        StatusCode = 401
                    };
                }
            }
        }
}