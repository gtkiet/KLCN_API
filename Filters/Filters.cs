using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KLCN_API.Filters;

/// <summary>
/// Tự động trả 400 với danh sách lỗi validation thay vì để controller tự xử lý.
/// Đăng ký toàn cục trong Program.cs qua AddControllers(o => o.Filters.Add<ValidationFilter>()).
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            context.Result = new BadRequestObjectResult(
                ApiResponse.Fail("Dữ liệu không hợp lệ.", errors));

            return;
        }

        await next();
    }
}

/// <summary>
/// Giới hạn endpoint theo role. Dùng thay [Authorize(Roles = "...")] để tránh
/// đặt chuỗi hardcode phân tán khắp controller.
///
/// Lưu ý: giá trị Role trong JWT claim phải khớp với RoleEnum.ToString(),
/// tức là "Admin", "Staff", "Customer" — đúng với seed data trong DB.
///
/// Ví dụ:
///   [AuthorizeRoles(RoleEnum.Admin)]
///   [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(params RoleEnum[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}