using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KLCN_API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // ── Validation ───────────────────────────────────────────
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

        // ── Execute action & catch exception ─────────────────────
        var executed = await next();

        if (executed.Exception is BusinessException ex && !executed.ExceptionHandled)
        {
            executed.Result = new ObjectResult(ApiResponse.Fail(ex.Message))
            {
                StatusCode = ex.StatusCode
            };

            executed.ExceptionHandled = true;
        }
    }
}