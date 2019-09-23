using System.Security.Claims;
using System.Linq;
using System;

namespace Services.Extensions
{
    static public class ClaimsPrincipalExtensions
    {
        static void Verify(ClaimsPrincipal principal)
        {
            if(!principal.Claims.Any())
            {
                throw new UnauthorizedAccessException();
            }
        }
        static public int GetId(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return (from claim in principal.Claims where claim.Type == "Id" select int.Parse(claim.Value)).Single();
        }
        static public DateTime GetTokenDateIssued(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return (from claim in principal.Claims where claim.Type == "DateIssued" select DateTime.Parse(claim.Value)).Single();
        }
    }
}
