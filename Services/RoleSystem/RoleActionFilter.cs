using Microsoft.AspNetCore.Mvc.Filters;
using Services.RoleSystem.Interfaces;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Services.RoleSystem
{
    public class RoleActionFilter : IActionFilter
    {
        private readonly IActionRoleManager ActionManager;
        private readonly IRoleValidator RoleValidator;
        public RoleActionFilter(IActionRoleManager ActionManager, IRoleValidator RoleValidator)
        {
            this.ActionManager = ActionManager;
            this.RoleValidator = RoleValidator;
        }
        public void OnActionExecuting(ActionExecutingContext context)
        {
            string path = context.HttpContext.Request.Path;
            string[] Roles = ActionManager.GetRolesOfAction(path).ToArray();
            string[] Permissions = ActionManager.GetPermissionOfAction(path).ToArray();
            if (!context.HttpContext.User.Claims.Any())
            {
                if (Roles.Length > 0 || Permissions.Length > 0)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                if (!RoleValidator.ValidateRoles(context.HttpContext.User, Roles))
                {
                    context.Result = new UnauthorizedResult();
                }
                if (!RoleValidator.ValidatePermissions(context.HttpContext.User, Permissions))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            return;
        }
    }
}
