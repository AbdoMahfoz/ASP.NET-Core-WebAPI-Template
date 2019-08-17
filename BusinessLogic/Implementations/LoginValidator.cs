using Services.Extensions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Interfaces;

namespace BusinessLogic.Implementations
{
    public class AccountRequirement : IAuthorizationRequirement { }
    public class LoginHandler : AuthorizationHandler<AccountRequirement>
    {
        private readonly IAuth auth;
        public LoginHandler(IAuth auth)
        {
            this.auth = auth;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountRequirement requirement)
        {
            if(!context.User.Identity.IsAuthenticated) return Task.CompletedTask;
            if (!auth.Validate(context.User.GetId(), context.User.GetTokenDateIssued())) context.Fail();
            else context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
