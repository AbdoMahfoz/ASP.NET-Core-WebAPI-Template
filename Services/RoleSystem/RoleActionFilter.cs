using Microsoft.AspNetCore.Mvc.Filters;
using Services.RoleSystem.Interfaces;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Services.RoleSystem;

public class RoleActionFilter(IActionRoleManager ActionManager, IRoleValidator RoleValidator)
    : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
            string path = context.HttpContext.Request.Path;
            var roles = ActionManager.GetRolesOfAction(path).ToArray();
            var permissions = ActionManager.GetPermissionOfAction(path).ToArray();
            if (!context.HttpContext.User.Claims.Any())
            {
                if (roles.Length > 0 || permissions.Length > 0)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                if (!RoleValidator.ValidateRoles(context.HttpContext.User, roles))
                {
                    context.Result = new UnauthorizedResult();
                }
                if (!RoleValidator.ValidatePermissions(context.HttpContext.User, permissions))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}