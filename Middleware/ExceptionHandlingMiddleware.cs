using System.Text.Json;
using Microsoft.Data.SqlClient;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Middleware;

// ── Custom exceptions ────────────────────────────────────────────

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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            BusinessException be => (be.StatusCode, be.Message, (List<string>?)null),
            SqlException sqlex => HandleSqlException(sqlex),
            ArgumentException ae => (400, ae.Message, (List<string>?)null),
            _ => (500, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", (List<string>?)null)
        };

        if (statusCode >= 500)
            _logger.LogError(ex, "Unhandled exception at {Path}: {Message}",
                context.Request.Path, ex.Message);
        else
            _logger.LogWarning("Handled exception [{Status}] at {Path}: {Message}",
                statusCode, context.Request.Path, ex.Message);

        // Nếu response đã bắt đầu ghi (streaming) thì không thể ghi thêm
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response đã started, không thể ghi error response.");
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(ApiResponse.Fail(message, errors), JsonOptions));
    }

    /// <summary>
    /// Map SQL Server error number sang HTTP status.
    /// SP trong DB dùng THROW 5xxxx để phân loại lỗi business.
    /// </summary>
    private static (int statusCode, string message, List<string>? errors) HandleSqlException(
        SqlException ex)
    {
        return ex.Number switch
        {
            // Slot / booking conflicts
            50001 => (409, ex.Message, null),   // Slot bị đặt bởi người khác
            50002 => (409, ex.Message, null),   // Hold đã hết hạn

            // Business rule violations từ SP
            >= 50000 and < 60000 => (422, ex.Message, null),

            // DB constraint violations
            2627 or 2601 => (409, "Dữ liệu bị trùng lặp.", null),
            547 => (409, "Vi phạm ràng buộc dữ liệu.", null),

            _ => (500, "Lỗi cơ sở dữ liệu.", null)
        };
    }
}