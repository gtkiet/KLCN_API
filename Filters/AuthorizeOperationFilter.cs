//using Microsoft.AspNetCore.Authorization;
//using Microsoft.OpenApi;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace KLCN_API.Filters;

//public class AuthorizeOperationFilter : IOperationFilter
//{
//    public void Apply(
//        OpenApiOperation operation,
//        OperationFilterContext context)
//    {
//        // ========================================================
//        // CONTROLLER ATTRIBUTES
//        // ========================================================

//        var controllerAttributes =
//            context.MethodInfo
//                .DeclaringType?
//                .GetCustomAttributes(true)
//                .OfType<AuthorizeAttribute>()
//            ?? Enumerable.Empty<AuthorizeAttribute>();

//        // ========================================================
//        // ACTION ATTRIBUTES
//        // ========================================================

//        var actionAttributes =
//            context.MethodInfo
//                .GetCustomAttributes(true)
//                .OfType<AuthorizeAttribute>();

//        // ========================================================
//        // HAS AUTHORIZE
//        // ========================================================

//        var hasAuthorize =
//            controllerAttributes.Any()
//            || actionAttributes.Any();

//        // ========================================================
//        // ALLOW ANONYMOUS
//        // ========================================================

//        var allowAnonymous =
//            context.MethodInfo
//                .GetCustomAttributes(true)
//                .OfType<AllowAnonymousAttribute>()
//                .Any();

//        if (allowAnonymous)
//            return;

//        if (!hasAuthorize)
//            return;

//        // ========================================================
//        // SECURITY
//        // ========================================================

//        operation.Security ??=
//            new List<OpenApiSecurityRequirement>();

//        var securityRequirement =
//            new OpenApiSecurityRequirement();

//        var securityScheme =
//            new OpenApiSecuritySchemeReference(
//                "Bearer");

//        securityRequirement.Add(
//            securityScheme,
//            new List<string>());

//        operation.Security.Add(
//            securityRequirement);

//        // ========================================================
//        // RESPONSES
//        // ========================================================

//        operation.Responses ??=
//            new OpenApiResponses();

//        operation.Responses.TryAdd(
//            "401",
//            new OpenApiResponse
//            {
//                Description = "Unauthorized"
//            });

//        operation.Responses.TryAdd(
//            "403",
//            new OpenApiResponse
//            {
//                Description = "Forbidden"
//            });
//    }
//}