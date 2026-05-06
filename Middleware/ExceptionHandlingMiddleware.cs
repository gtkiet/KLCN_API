using System.Text.Json;
using Microsoft.Data.SqlClient;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Middleware;

// ── Custom Exceptions ────────────────────────────────────────────

public class BusinessException : Exception
{
    public int StatusCode { get; }

    public BusinessException(string message, int statusCode = 422)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : BusinessException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} với id={id} không tồn tại.", 404) { }

    public NotFoundException(string message)
        : base(message, 404) { }
}

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message = "Bạn chưa đăng nhập hoặc token không hợp lệ.")
        : base(message, 401) { }
}

public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
        : base(message, 403) { }
}

public class ConflictException : BusinessException
{
    public ConflictException(string message)
        : base(message, 409) { }
}

// ── Middleware ───────────────────────────────────────────────────

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message, errors) = ex switch
        {
            // Custom business exceptions
            BusinessException be => (be.StatusCode, be.Message, (List<string>?)null),

            // SQL Server errors từ SP THROW — parse error number
            SqlException sqlex => HandleSqlException(sqlex),

            // Validation — thường từ FluentValidation nếu dùng sau
            ArgumentException ae => (400, ae.Message, (List<string>?)null),

            // Default
            _ => (500, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", (List<string>?)null)
        };

        // Log tất cả lỗi 5xx, log warning cho 4xx
        if (statusCode >= 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            _logger.LogWarning("Handled exception [{Status}]: {Message}", statusCode, ex.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Map SQL error number sang HTTP status code.
    /// Các SP dùng THROW với error number 5xxxx để phân loại.
    /// </summary>
    private static (int statusCode, string message, List<string>? errors) HandleSqlException(
        SqlException ex)
    {
        // SP THROW 50001..50099 = business rule violations → 422
        // SP THROW 50001 = slot không còn khả dụng → 409 Conflict
        return ex.Number switch
        {
            50001 => (409, ex.Message, null),   // Slot bị đặt bởi người khác
            50002 => (409, ex.Message, null),   // Hold hết hạn
            50003 => (422, ex.Message, null),
            50004 => (422, ex.Message, null),
            >= 50000 and < 60000 => (422, ex.Message, null), // Business logic
            2627 or 2601 => (409, "Dữ liệu bị trùng lặp.", null), // Unique constraint
            547 => (409, "Vi phạm ràng buộc dữ liệu.", null), // FK
            _ => (500, "Lỗi cơ sở dữ liệu.", null)
        };
    }
}