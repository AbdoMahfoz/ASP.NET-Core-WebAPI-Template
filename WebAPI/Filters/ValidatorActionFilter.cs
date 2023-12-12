using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebAPI.Filters;

public class ValidatorActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!filterContext.ModelState.IsValid)
        {
            var messages = filterContext.ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage).ToList();
            filterContext.Result = new UnprocessableEntityObjectResult(messages);
        }
    }

    public void OnActionExecuted(ActionExecutedContext filterContext)
    {
    }
}