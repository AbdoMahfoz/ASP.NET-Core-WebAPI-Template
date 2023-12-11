using Services.Extensions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Interfaces;

namespace BusinessLogic.Implementations;

public class AccountRequirement : IAuthorizationRequirement;

public class LoginHandler(IAuth auth) : AuthorizationHandler<AccountRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountRequirement requirement)
    {
        if (context.User.Identity == null) context.Fail();
        else if (!context.User.Identity.IsAuthenticated) return Task.CompletedTask;
        else if (!auth.Validate(context.User.GetId(), context.User.GetTokenDateIssued())) context.Fail();
        else context.Succeed(requirement);
        return Task.CompletedTask;
    }
}