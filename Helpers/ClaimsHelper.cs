using KLCN_API.Models.Enums;
using System.Security.Claims;

namespace KLCN_API.Helpers;

public static class ClaimsHelper
{
    /// <summary>Lấy UserId từ JWT claims. Trả về 0 nếu không tìm thấy.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>Lấy tên role (Admin / Staff / Customer).</summary>
    public static string GetRole(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    /// <summary>Lấy RoleId dạng int.</summary>
    public static int GetRoleId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("roleId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>Lấy email từ claims.</summary>
    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    /// <summary>Lấy tên đầy đủ từ claims.</summary>
    public static string GetFullName(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    // ── Role checks ──────────────────────────────────────────────

    public static bool IsAdmin(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Admin;

    public static bool IsStaff(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Staff;

    public static bool IsCustomer(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Customer;

    public static bool IsAdminOrStaff(this ClaimsPrincipal principal)
        => principal.IsAdmin() || principal.IsStaff();
}