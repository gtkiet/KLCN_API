using System.Text;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using KLCN_API.Configurations;
using KLCN_API.Models.Enums;
using KLCN_API.Models.Entities;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Helpers;

public class JwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Tạo JWT access token từ thông tin user.</summary>
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email,          user.Email),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Role,           user.Role?.Name ?? user.RoleId.ToString()),
            new("roleId",                  user.RoleId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Tạo refresh token ngẫu nhiên (opaque token).</summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Lấy ClaimsPrincipal từ access token đã hết hạn.
    /// Dùng khi refresh — token hết hạn nhưng signature vẫn hợp lệ.
    /// </summary>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // cho phép token hết hạn
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(_settings.SecretKey))
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParams, out var securityToken);

        if (securityToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                                    StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Token không hợp lệ.");
        }

        return principal;
    }

    /// <summary>Lấy userId từ claims của expired token (dùng khi refresh).</summary>
    public int GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}

public static class PasswordHelper
{
    private const int WorkFactor = 12;

    /// <summary>Hash password bằng BCrypt với work factor 12.</summary>
    public static string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>Kiểm tra password plaintext so với hash đã lưu.</summary>
    public static bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}


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

public static class StoredProcedureHelper
{
    /// <summary>
    /// Thực thi Stored Procedure không trả về kết quả (INSERT/UPDATE).
    /// Ném SqlException với message từ SP nếu SP THROW lỗi.
    /// </summary>
    public static async Task ExecuteSpAsync(
        DbContext ctx,
        string spName,
        params SqlParameter[] parameters)
    {
        var paramList = BuildParamString(parameters);
        var sql = $"EXEC {spName} {paramList}";
        await ctx.Database.ExecuteSqlRawAsync(sql, parameters.Cast<object>().ToArray());
    }

    /// <summary>
    /// Thực thi SP và đọc kết quả vào List&lt;T&gt; qua DataReader.
    /// </summary>
    public static async Task<List<T>> QuerySpAsync<T>(
        DbContext ctx,
        string spName,
        Func<IDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var results = new List<T>();
        var conn = ctx.Database.GetDbConnection();

        try
        {
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(mapper(reader));
        }
        finally
        {
            // EF quản lý connection — chỉ đóng nếu chúng ta tự mở
            if (conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }

        return results;
    }

    // ── Factory methods cho các SP thường dùng ──────────────────

    /// <summary>sp_HoldSlots — giữ slot, kiểm tra ràng buộc đặt trước.</summary>
    public static Task HoldSlotsAsync(DbContext ctx, string fieldSlotIds, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_HoldSlots",
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmBooking — xác nhận booking + tính tiền + tạo deposit nếu cần.</summary>
    public static Task ConfirmBookingAsync(
        DbContext ctx, int bookingId, string fieldSlotIds,
        bool isFullPayment = true, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmBooking",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@IsFullPayment", isFullPayment),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_CancelBooking — hủy + hoàn tiền theo policy.</summary>
    public static Task CancelBookingAsync(
        DbContext ctx, int bookingId,
        int? userId = null, string? reason = null, bool isAdminOverride = false)
        => ExecuteSpAsync(ctx, "sp_CancelBooking",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@Reason", (object?)reason ?? DBNull.Value),
            new SqlParameter("@IsAdminOverride", isAdminOverride));

    /// <summary>sp_RecordDeposit — ghi nhận thanh toán đặt cọc.</summary>
    public static Task RecordDepositAsync(
        DbContext ctx, int bookingId, decimal amount,
        int methodId, string? transactionCode = null, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RecordDeposit",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@Amount", amount),
            new SqlParameter("@MethodId", methodId),
            new SqlParameter("@TransactionCode", (object?)transactionCode ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_RecordFullPayment — thanh toán phần còn lại.</summary>
    public static Task RecordFullPaymentAsync(
        DbContext ctx, int bookingId, int methodId,
        string? transactionCode = null, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RecordFullPayment",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@MethodId", methodId),
            new SqlParameter("@TransactionCode", (object?)transactionCode ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ApplyPromotion — áp dụng voucher vào booking.</summary>
    public static Task ApplyPromotionAsync(
        DbContext ctx, int bookingId, string code, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ApplyPromotion",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@Code", code),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_RescheduleBooking — đổi lịch một slot trong booking.</summary>
    public static Task RescheduleBookingAsync(
        DbContext ctx, int bookingDetailId, int newFieldSlotId, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RescheduleBooking",
            new SqlParameter("@BookingDetailId", bookingDetailId),
            new SqlParameter("@NewFieldSlotId", newFieldSlotId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmPurchaseOrder — xác nhận đơn nhập kho, cập nhật tồn kho.</summary>
    public static Task ConfirmPurchaseOrderAsync(
        DbContext ctx, int purchaseOrderId, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmPurchaseOrder",
            new SqlParameter("@PurchaseOrderId", purchaseOrderId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_GenerateSlots — sinh FieldSlots cho khoảng ngày.</summary>
    public static Task GenerateSlotsAsync(DbContext ctx, DateOnly startDate, DateOnly endDate)
        => ExecuteSpAsync(ctx, "sp_GenerateSlots",
            new SqlParameter("@StartDate", startDate.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@EndDate", endDate.ToDateTime(TimeOnly.MinValue)));

    /// <summary>sp_ReleaseExpiredSlots — giải phóng slot hết hạn hold + tự hủy deposit quá hạn.</summary>
    public static Task ReleaseExpiredSlotsAsync(DbContext ctx)
        => ExecuteSpAsync(ctx, "sp_ReleaseExpiredSlots");

    /// <summary>sp_UpdateSystemConfig — cập nhật cấu hình hệ thống.</summary>
    public static Task UpdateSystemConfigAsync(
        DbContext ctx, string configKey, string configValue, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_UpdateSystemConfig",
            new SqlParameter("@ConfigKey", configKey),
            new SqlParameter("@ConfigValue", configValue),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    // ── Internal ─────────────────────────────────────────────────

    private static string BuildParamString(SqlParameter[] parameters)
        => string.Join(", ", parameters.Select(p => p.ParameterName));
}

public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Phân trang IQueryable, thực thi 2 query: Count + dữ liệu trang.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Phân trang từ List đã có trong bộ nhớ (dùng khi đã fetch xong).
    /// </summary>
    public static PagedResponse<T> ToPagedFromList<T>(
        IEnumerable<T> source,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var list = source.ToList();
        var totalCount = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}