using KLCN_API.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace KLCN_API.Filters;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class AuthorizeRolesAttribute
    : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(
        params RoleEnum[] roles)
    {
        Roles = string.Join(
            ",",
            roles.Select(r => r.ToString()));
    }
}