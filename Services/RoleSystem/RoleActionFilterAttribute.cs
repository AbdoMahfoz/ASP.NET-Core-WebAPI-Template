using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Services.RoleSystem.Interfaces;

namespace Services.RoleSystem
{
    public abstract class BaseRoleValidator : ActionFilterAttribute
    {
        protected readonly string[] Roles;
        protected IRoleValidator RoleValidator;
        public BaseRoleValidator(string[] Roles)
        {
            this.Roles = Roles;
        }
        protected void InitializeServices(ActionExecutingContext context)
        {
            RoleValidator = (IRoleValidator)context.HttpContext.RequestServices.GetService(typeof(IRoleValidator));
        }
    }
    public class HasRole : BaseRoleValidator
    {
        public HasRole(params string[] Roles) : base(Roles) { }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            InitializeServices(context);
            if(!RoleValidator.ValidateRoles(context.HttpContext.User, Roles))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
    public class HasPermission : BaseRoleValidator
    {
        public HasPermission(params string[] Roles) : base(Roles) { }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            InitializeServices(context);
            if (!RoleValidator.ValidatePermissions(context.HttpContext.User, Roles))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
