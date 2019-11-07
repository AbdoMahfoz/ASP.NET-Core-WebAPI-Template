using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI
{
    public class HasRole : ActionFilterAttribute
    {
        private readonly string[] Roles;
        public HasRole(params string[] Roles)
        {
            this.Roles = Roles;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            
        }
    }
    public class HasPermission : ActionFilterAttribute
    {
        private readonly string[] Permissions;
        public HasPermission(params string[] Permissions)
        {
            this.Permissions = Permissions;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {

        }
    }
}
