using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;

namespace KLCN_API.Filters;

// ── Validation Filter ────────────────────────────────────────────

/// <summary>
/// Tự động validate ModelState trước khi vào action.
/// Trả về 400 + danh sách lỗi nếu DTO không hợp lệ.
/// Đăng ký global: builder.Services.AddControllers(o => o.Filters.Add&lt;ValidationFilter&gt;())
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
            .ToList();

        context.Result = new BadRequestObjectResult(
            ApiResponse.Fail("Dữ liệu không hợp lệ.", errors));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

// ── AuthorizeRoles Attribute ─────────────────────────────────────

/// <summary>
/// Kết hợp [Authorize] + kiểm tra role cụ thể.
/// Dùng: [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(params RoleEnum[] roles)
    {
        // Chuyển roles thành policy name theo chuẩn ASP.NET Core
        // Roles property của AuthorizeAttribute nhận string CSV
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}