using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem
{
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

    public class HasRole : BaseRoleValidator
    {
        public HasRole(params RoleNames[] roles) : base(roles.Select(u => u.ToString())
            .ToArray())
        {
        }

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

    public class HasPermission : BaseRoleValidator
    {
        public HasPermission(params PermissionNames[] permissions) : base(permissions.Select(u => u.ToString())
            .ToArray())
        {
        }

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

    public class HasOnePermission : BaseRoleValidator
    {
        public HasOnePermission(params PermissionNames[] permissions) : base(permissions.Select(u => u.ToString())
            .ToArray())
        {
        }

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

    public class HasCrudPermission : BaseRoleValidator
    {
        private readonly CrudVerb _crudVerb;
        private readonly Type _type;

        public HasCrudPermission(CrudVerb crudVerb, Type type = null)
        {
            _crudVerb = crudVerb;
            _type = type;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            InitializeServices(context);
            lock (this)
            {
                var modelName = _type == null ? context.Controller.GetType().GenericTypeArguments[0].Name : _type.Name;
                Roles = new[] { $"{_crudVerb} {modelName}", $"{_crudVerb} *" };
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
}