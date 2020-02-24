using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Services.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        private static void Verify(ClaimsPrincipal principal)
        {
            if (!principal.Claims.Any()) throw new UnauthorizedAccessException();
        }

        public static int GetId(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return (from claim in principal.Claims where claim.Type == "Id" select int.Parse(claim.Value)).Single();
        }

        public static DateTime GetTokenDateIssued(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return (from claim in principal.Claims where claim.Type == "DateIssued" select DateTime.Parse(claim.Value))
                .Single();
        }

        public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return from claim in principal.Claims where claim.Type == ClaimTypes.Role select claim.Value;
        }

        public static IEnumerable<string> GetPermissions(this ClaimsPrincipal principal)
        {
            Verify(principal);
            return from claim in principal.Claims where claim.Type == "Permission" select claim.Value;
        }
    }
}