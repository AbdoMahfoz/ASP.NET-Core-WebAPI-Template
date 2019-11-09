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
            if(!RoleValidator.ValidateRoles(context.HttpContext.User, 
                ActionManager.GetRolesOfAction(context.ActionDescriptor.DisplayName).ToArray()))
            {
                context.Result = new UnauthorizedResult();
            }
            if (!RoleValidator.ValidatePermissions(context.HttpContext.User, 
                ActionManager.GetPermissionOfAction(context.ActionDescriptor.DisplayName).ToArray()))
            {
                context.Result = new UnauthorizedResult();
            }
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            return;
        }
    }
}
