using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI
{
    public class ValidatorActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.ModelState.IsValid)
            {
                List<string> messages = filterContext.ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage).ToList();
                filterContext.Result = new UnprocessableEntityObjectResult(messages);
            }
        }
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            return;
        }
    }
}
