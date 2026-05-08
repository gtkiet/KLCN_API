using KLCN_API.Models.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KLCN_API.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(
        ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x =>
                x.Value!.Errors.Select(e => e.ErrorMessage))
            .ToList();

        context.Result =
            new BadRequestObjectResult(
                ApiResponse.Fail(
                    "Du lieu khong hop le.",
                    errors));
    }

    public void OnActionExecuted(
        ActionExecutedContext context)
    {
    }
}