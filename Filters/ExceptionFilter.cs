// Filters/ExceptionFilter.cs — file mới

using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KLCN_API.Filters;

public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not BusinessException ex)
            return;

        context.Result = new ObjectResult(ApiResponse.Fail(ex.Message))
        {
            StatusCode = ex.StatusCode
        };

        context.ExceptionHandled = true;
    }
}