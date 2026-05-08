//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.OpenApi;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace KLCN_API.Filters;

//public class ValidationFilter : IActionFilter
//{
//    public void OnActionExecuting(ActionExecutingContext context)
//    {
//        if (context.ModelState.IsValid) return;

//        var errors = context.ModelState
//            .Where(x => x.Value?.Errors.Count > 0)
//            .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
//            .ToList();

//        context.Result = new BadRequestObjectResult(
//            ApiResponse.Fail("Du lieu khong hop le.", errors));
//    }

//    public void OnActionExecuted(ActionExecutedContext context) { }
//}

//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
//public class AuthorizeRolesAttribute : AuthorizeAttribute
//{
//    public AuthorizeRolesAttribute(params RoleEnum[] roles)
//    {
//        Roles = string.Join(",", roles.Select(r => r.ToString()));
//    }
//}

//public class AuthorizeOperationFilter : IOperationFilter
//{
//    public void Apply(OpenApiOperation operation, OperationFilterContext context)
//    {
//        var hasAllowAnonymous =
//            context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
//            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
//                    .OfType<AllowAnonymousAttribute>().Any() ?? false);

//        if (hasAllowAnonymous)
//        {
//            operation.Security?.Clear();
//            return;
//        }

//        var hasAuthorize =
//            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
//            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
//                    .OfType<AuthorizeAttribute>().Any() ?? false);

//        if (!hasAuthorize) return;

//        operation.Security ??= new List<OpenApiSecurityRequirement>();

//        // Swashbuckle 10 / OpenApi 2.x dùng OpenApiSecuritySchemeReference
//        var schemeRef = new OpenApiSecuritySchemeReference("Bearer");

//        if (!operation.Security.Any(r => r.ContainsKey(schemeRef)))
//        {
//            operation.Security.Add(new OpenApiSecurityRequirement
//            {
//                { schemeRef, new List<string>() }
//            });
//        }

//        operation.Responses?.TryAdd("401", new OpenApiResponse
//        {
//            Description = "Unauthorized - Token không hợp lệ hoặc hết hạn"
//        });
//        operation.Responses?.TryAdd("403", new OpenApiResponse
//        {
//            Description = "Forbidden - Không đủ quyền truy cập"
//        });
//    }
//}