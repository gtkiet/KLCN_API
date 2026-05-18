using KLCN_API.Configurations;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KLCN_API.Helpers;

// ── JWT ───────────────────────────────────────────────────────────

public class JwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings) => _settings = settings;

    /// <summary>
    /// Tạo JWT access token từ thông tin user.
    /// Caller phải đảm bảo user.Role đã được load (Include hoặc eager load),
    /// nếu không claim role sẽ sai và [AuthorizeRoles] sẽ không hoạt động.
    /// </summary>
    public string GenerateAccessToken(Models.Entities.User user)
    {
        if (user.Role is null)
            throw new InvalidOperationException(
                $"User {user.UserId} chua load navigation Role. " +
                "Them Include(u => u.Role) truoc khi goi GenerateAccessToken.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier,   user.UserId.ToString()),
            new(ClaimTypes.Email,            user.Email),
            new(ClaimTypes.Name,             user.FullName),
            new(ClaimTypes.Role,             user.Role.Name),
            new("roleId",                    user.RoleId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Tạo refresh token ngẫu nhiên (opaque token, 64 bytes).</summary>
    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>
    /// Lấy ClaimsPrincipal từ access token đã hết hạn.
    /// Dùng khi refresh — token hết hạn nhưng signature vẫn phải hợp lệ.
    /// Throw SecurityTokenException nếu token bị giả mạo hoặc sai algorithm.
    /// </summary>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(_settings.SecretKey))
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParams, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenException("Token khong hop le.");

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Token khong the xac thuc.", ex);
        }
    }

    /// <summary>Lấy userId từ claims của expired token (dùng khi refresh).</summary>
    public int GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}

// ── Password ──────────────────────────────────────────────────────

public static class PasswordHelper
{
    private const int WorkFactor = 12;

    /// <summary>Hash password bằng BCrypt với work factor 12 (~300–400ms).</summary>
    public static string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>So sánh password plaintext với hash đã lưu.</summary>
    public static bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}

// ── Claims ────────────────────────────────────────────────────────

public static class ClaimsHelper
{
    /// <summary>
    /// Lấy UserId từ JWT claims.
    /// Trả về 0 nếu claim không tồn tại hoặc không parse được.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>Lấy tên role ("Admin" / "Staff" / "Customer").</summary>
    public static string GetRole(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    /// <summary>Lấy RoleId dạng int.</summary>
    public static int GetRoleId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("roleId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public static string GetFullName(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public static bool IsAdmin(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Admin;

    public static bool IsStaff(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Staff;

    public static bool IsCustomer(this ClaimsPrincipal principal)
        => principal.GetRoleId() == (int)RoleEnum.Customer;

    public static bool IsAdminOrStaff(this ClaimsPrincipal principal)
        => principal.IsAdmin() || principal.IsStaff();
}

// ── Stored Procedures ─────────────────────────────────────────────

public static class StoredProcedureHelper
{
    /// <summary>
    /// Thực thi Stored Procedure không trả về kết quả (INSERT / UPDATE).
    /// Throw SqlException với message từ SP nếu SP THROW lỗi.
    /// </summary>
    public static async Task ExecuteSpAsync(
        DbContext ctx, string spName, params SqlParameter[] parameters)
    {
        var paramList = BuildParamString(parameters);
        var sql = string.IsNullOrEmpty(paramList)
            ? $"EXEC {spName}"
            : $"EXEC {spName} {paramList}";

        await ctx.Database.ExecuteSqlRawAsync(sql, parameters.Cast<object>().ToArray());
    }

    /// <summary>
    /// Thực thi SP và đọc kết quả vào List&lt;T&gt; qua DataReader.
    /// Chỉ tự mở / đóng connection nếu EF chưa mở — tránh đóng connection
    /// đang được EF quản lý bên trong transaction.
    /// </summary>
    public static async Task<List<T>> QuerySpAsync<T>(
        DbContext ctx, string spName,
        Func<IDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var results = new List<T>();
        var conn = ctx.Database.GetDbConnection();
        var wasOpen = conn.State == ConnectionState.Open;

        try
        {
            if (!wasOpen) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;

            if (ctx.Database.CurrentTransaction is { } efTx)
                cmd.Transaction = efTx.GetDbTransaction();

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(mapper(reader));
        }
        finally
        {
            if (!wasOpen && conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }

        return results;
    }

    // ── Factory methods ───────────────────────────────────────────

    /// <summary>sp_HoldSlots — giữ slot, kiểm tra ràng buộc đặt trước.</summary>
    public static Task HoldSlotsAsync(
        DbContext ctx, IEnumerable<int> fieldSlotIds, int? userId = null)
        => HoldSlotsAsync(ctx, string.Join(",", fieldSlotIds), userId);

    public static Task HoldSlotsAsync(
        DbContext ctx, string fieldSlotIds, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_HoldSlots",
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmBooking — xác nhận booking, tính tiền, tạo deposit.</summary>
    public static Task ConfirmBookingAsync(
        DbContext ctx, int bookingId, string fieldSlotIds,
        bool isFullPayment = false, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmBooking",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@IsFullPayment", isFullPayment),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));


    public static Task ConfirmAdminWalkInAsync(
    DbContext ctx, int bookingId, string fieldSlotIds,
    bool isFullPayment = false, int? userId = null)
    => ExecuteSpAsync(ctx, "sp_ConfirmAdminWalkIn",
        new SqlParameter("@BookingId", bookingId),
        new SqlParameter("@FieldSlotIds", fieldSlotIds),
        new SqlParameter("@IsFullPayment", isFullPayment),
        new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_CancelBooking — hủy booking và hoàn tiền theo policy.</summary>
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

    /// <summary>sp_RecordFullPayment — thanh toán phần còn lại sau khi đã cọc.</summary>
    public static Task RecordFullPaymentAsync(
        DbContext ctx, int bookingId, int methodId,
        string? transactionCode = null, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RecordFullPayment",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@MethodId", methodId),
            new SqlParameter("@TransactionCode", (object?)transactionCode ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ApplyPromotion — áp dụng mã voucher vào booking.</summary>
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

    /// <summary>sp_ConfirmPurchaseOrder — xác nhận nhập kho, cộng tồn kho.</summary>
    public static Task ConfirmPurchaseOrderAsync(
        DbContext ctx, int purchaseOrderId, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmPurchaseOrder",
            new SqlParameter("@PurchaseOrderId", purchaseOrderId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_GenerateSlots — sinh FieldSlots cho khoảng ngày chỉ định.</summary>
    public static Task GenerateSlotsAsync(DbContext ctx, DateOnly startDate, DateOnly endDate)
        => ExecuteSpAsync(ctx, "sp_GenerateSlots",
            new SqlParameter("@StartDate", startDate.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@EndDate", endDate.ToDateTime(TimeOnly.MinValue)));

    /// <summary>sp_ReleaseExpiredSlots — giải phóng slot hết hạn hold, hủy deposit quá hạn.</summary>
    public static Task ReleaseExpiredSlotsAsync(DbContext ctx)
        => ExecuteSpAsync(ctx, "sp_ReleaseExpiredSlots");

    /// <summary>sp_UpdateSystemConfig — cập nhật một mục cấu hình hệ thống.</summary>
    public static Task UpdateSystemConfigAsync(
        DbContext ctx, string configKey, string configValue, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_UpdateSystemConfig",
            new SqlParameter("@ConfigKey", configKey),
            new SqlParameter("@ConfigValue", configValue),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    private static string BuildParamString(SqlParameter[] parameters)
        => string.Join(", ", parameters.Select(p => p.ParameterName));
}

// ── Pagination ────────────────────────────────────────────────────

public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Phân trang IQueryable — thực thi 2 query: Count và dữ liệu trang.</summary>
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        IQueryable<T> query, int page, int pageSize)
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

    /// <summary>Phân trang từ List đã có trong bộ nhớ (dùng khi đã fetch xong).</summary>
    public static PagedResponse<T> ToPagedFromList<T>(
        IEnumerable<T> source, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var list = source.ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = list.Count,
            Page = page,
            PageSize = pageSize
        };
    }
}

// ── Image Upload ──────────────────────────────────────────────────

/// <summary>
/// Helper upload ảnh dùng chung cho mọi entity (Field, Service, Profile...).
/// Tên file = GUID — không bao giờ trùng, không path traversal.
/// Lưu vào Uploads/{subfolder}/, trả về relative URL để lưu DB.
/// Frontend tự ghép host khi hiển thị.
/// </summary>
public static class ImageUploadHelper
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Lưu file ảnh vào Uploads/{subfolder}/.
    /// Trả về relative URL "/Uploads/{subfolder}/{guid}.ext".
    /// </summary>
    public static async Task<string> SaveAsync(
        IFormFile file, string contentRootPath, string subfolder)
    {
        if (file is null || file.Length == 0)
            throw new BusinessException("File không được rỗng.", 400);

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new BusinessException(
                "Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .webp.", 400);

        if (file.Length > MaxFileSizeBytes)
            throw new BusinessException("File ảnh không được vượt quá 5MB.", 400);

        var folder = Path.Combine(contentRootPath, "Uploads", subfolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);

        return $"/Uploads/{subfolder}/{fileName}";
    }

    /// <summary>
    /// Xóa file ảnh cũ nếu tồn tại. Silent fail nếu file không có.
    /// Bảo vệ path traversal: chỉ xóa file trong thư mục Uploads/.
    /// </summary>
    public static void DeleteIfExists(string? oldRelativeUrl, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(oldRelativeUrl)) return;

        var relativePath = oldRelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, relativePath));
        var uploadRoot = Path.GetFullPath(Path.Combine(contentRootPath, "Uploads"));

        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}


// ── VNPay ─────────────────────────────────────────────────────────

public class VNPayHelper
{
    private readonly VNPaySettings _settings;

    public VNPayHelper(VNPaySettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay.
    /// Quy tắc đúng:
    /// - Ký trên dữ liệu đã URL encode.
    /// - URL cuối cùng dùng đúng chuỗi đã ký.
    /// </summary>
    public string CreatePaymentUrl(
        int bookingId,
        decimal amount,
        string orderInfo,
        string ipAddress)
    {
        var txnRef = $"{bookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var vnTime = DateTime.UtcNow.AddHours(7);

        var vnpay = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = vnTime.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = vnTime.AddMinutes(15).ToString("yyyyMMddHHmmss")
        };

        // Chuỗi dữ liệu đã encode (được dùng để ký)
        var signData = string.Join("&",
            vnpay.Select(kv =>
                $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

        // Tạo chữ ký
        var secureHash = HmacSha512(_settings.HashSecret, signData);

        // URL thanh toán
        return $"{_settings.BaseUrl}?{signData}&vnp_SecureHash={secureHash}";
    }

    /// <summary>
    /// Xác minh chữ ký từ Return URL hoặc IPN.
    /// QueryCollection đã decode sẵn nên phải encode lại trước khi hash.
    /// </summary>
    public bool ValidateSignature(
        IQueryCollection query,
        out string txnRef,
        out bool isSuccess)
    {
        txnRef = query["vnp_TxnRef"].ToString();

        isSuccess =
            query["vnp_ResponseCode"] == "00" &&
            query["vnp_TransactionStatus"] == "00";

        var receivedHash = query["vnp_SecureHash"].ToString();

        // Tạo lại chuỗi dữ liệu đúng thứ tự và encode lại
        var signData = string.Join("&",
            query
                .Where(kv =>
                    kv.Key.StartsWith("vnp_") &&
                    kv.Key != "vnp_SecureHash" &&
                    kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv =>
                    $"{kv.Key}={WebUtility.UrlEncode(kv.Value.ToString())}"));

        // Tính chữ ký
        var expectedHash = HmacSha512(_settings.HashSecret, signData);

        // Log debug
        Console.WriteLine("===== VNPAY SIGNATURE CHECK =====");
        Console.WriteLine("SIGN DATA: " + signData);
        Console.WriteLine("EXPECTED : " + expectedHash);
        Console.WriteLine("RECEIVED : " + receivedHash);
        Console.WriteLine("MATCH    : " +
            string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase));

        return string.Equals(
            expectedHash,
            receivedHash,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Xử lý IPN từ VNPay.
    /// Chỉ gọi ValidateSignature(), không cần logic riêng.
    /// </summary>
    public bool ValidateIpn(
        IQueryCollection query,
        out string txnRef,
        out bool isSuccess)
    {
        return ValidateSignature(query, out txnRef, out isSuccess);
    }

    /// <summary>
    /// Tạo HMAC SHA512, trả về hex chữ thường.
    /// </summary>
    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
//public class VNPayHelper
//{
//    private readonly VNPaySettings _settings;

//    public VNPayHelper(VNPaySettings settings)
//    {
//        _settings = settings;
//    }

//    /// <summary>
//    /// Tạo URL thanh toán VNPay.
//    /// Lưu ý:
//    /// - Ký trên dữ liệu chưa URL encode.
//    /// - Chỉ URL encode khi ghép URL cuối.
//    /// </summary>
//    public string CreatePaymentUrl(
//    int bookingId,
//    decimal amount,
//    string orderInfo,
//    string ipAddress)
//    {
//        var txnRef = $"{bookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
//        var vnTime = DateTime.UtcNow.AddHours(7);

//        var vnpay = new SortedDictionary<string, string>
//        {
//            ["vnp_Version"] = "2.1.0",
//            ["vnp_Command"] = "pay",
//            ["vnp_TmnCode"] = _settings.TmnCode,
//            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
//            ["vnp_CurrCode"] = "VND",
//            ["vnp_TxnRef"] = txnRef,
//            ["vnp_OrderInfo"] = orderInfo,
//            ["vnp_OrderType"] = "other",
//            ["vnp_Locale"] = "vn",
//            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
//            ["vnp_IpAddr"] = ipAddress,
//            ["vnp_CreateDate"] = vnTime.ToString("yyyyMMddHHmmss"),
//            ["vnp_ExpireDate"] = vnTime.AddMinutes(15).ToString("yyyyMMddHHmmss")
//        };

//        // Encode trước
//        var encodedData = string.Join("&",
//            vnpay.Select(kv =>
//                $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

//        // Ký trên dữ liệu đã encode
//        var secureHash = HmacSha512(_settings.HashSecret, encodedData);

//        // URL cuối cùng
//        return $"{_settings.BaseUrl}?{encodedData}&vnp_SecureHash={secureHash}";
//    }

//    /// <summary>
//    /// Xác minh chữ ký từ VNPay Return/IPN.
//    /// IQueryCollection đã tự decode value, nên dùng raw value để hash,
//    /// KHÔNG encode lại.
//    /// </summary>
//    public bool ValidateSignature(
//        IQueryCollection query,
//        out string txnRef,
//        out bool isSuccess)
//    {
//        txnRef = query["vnp_TxnRef"].ToString();

//        isSuccess =
//            query["vnp_ResponseCode"] == "00" &&
//            query["vnp_TransactionStatus"] == "00";

//        var receivedHash = query["vnp_SecureHash"].ToString();

//        // Encode value giống hệt CreatePaymentUrl()
//        var signData = string.Join("&",
//            query
//                .Where(kv =>
//                    kv.Key != "vnp_SecureHash" &&
//                    kv.Key != "vnp_SecureHashType")
//                .OrderBy(kv => kv.Key)
//                .Select(kv =>
//                    $"{kv.Key}={WebUtility.UrlEncode(kv.Value.ToString())}"));

//        var expectedHash = HmacSha512(_settings.HashSecret, signData);

//        Console.WriteLine("SIGN DATA: " + signData);
//        Console.WriteLine("EXPECTED : " + expectedHash);
//        Console.WriteLine("RECEIVED : " + receivedHash);

//        return string.Equals(
//            expectedHash,
//            receivedHash,
//            StringComparison.OrdinalIgnoreCase);
//    }

//    /// <summary>
//    /// Tạo HMAC SHA512, kết quả hex chữ thường.
//    /// </summary>
//    private static string HmacSha512(string key, string data)
//    {
//        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
//        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

//        return Convert.ToHexString(hashBytes).ToLowerInvariant();
//    }
//}

// ── MoMo ─────────────────────────────────────────────────────────

public class MoMoHelper
{
    private readonly MoMoSettings _settings;
    private readonly HttpClient _httpClient;

    public MoMoHelper(MoMoSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gọi API MoMo để tạo giao dịch.
    /// Trả về payUrl để redirect khách đến trang thanh toán MoMo.
    /// </summary>
    public async Task<string> CreatePaymentAsync(
        int bookingId, decimal amount, string orderInfo)
    {
        var orderId = $"SP_{bookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var requestId = Guid.NewGuid().ToString("N");
        var amountStr = ((long)amount).ToString();

        var rawSignature =
            $"accessKey={_settings.AccessKey}" +
            $"&amount={amountStr}" +
            $"&extraData=" +
            $"&ipnUrl={_settings.IpnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={_settings.PartnerCode}" +
            $"&redirectUrl={_settings.ReturnUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={_settings.RequestType}";

        var body = new
        {
            partnerCode = _settings.PartnerCode,
            requestId,
            amount = (long)amount,
            orderId,
            orderInfo,
            redirectUrl = _settings.ReturnUrl,
            ipnUrl = _settings.IpnUrl,
            requestType = _settings.RequestType,
            extraData = string.Empty,
            lang = "vi",
            signature = HmacSha256(_settings.SecretKey, rawSignature)
        };

        //var client = _httpFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_settings.Endpoint, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("payUrl", out var payUrlProp) ||
            string.IsNullOrEmpty(payUrlProp.GetString()))
        {
            var errMsg = doc.RootElement.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Không lấy được URL thanh toán MoMo.";
            throw new InvalidOperationException(errMsg);
        }

        return payUrlProp.GetString()!;
    }

    /// <summary>Xác minh chữ ký IPN từ MoMo.</summary>
    public bool ValidateIpn(
        string partnerCode, string orderId, string requestId,
        string amount, string orderInfo, string orderType,
        string transId, int resultCode, string message,
        string payType, string responseTime, string extraData,
        string receivedSignature)
    {
        var rawSignature =
            $"accessKey={_settings.AccessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&message={message}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&orderType={orderType}" +
            $"&partnerCode={partnerCode}" +
            $"&payType={payType}" +
            $"&requestId={requestId}" +
            $"&responseTime={responseTime}" +
            $"&resultCode={resultCode}" +
            $"&transId={transId}";

        var expected = HmacSha256(_settings.SecretKey, rawSignature);
        return string.Equals(expected, receivedSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Parse bookingId từ orderId dạng "SP_{bookingId}_{timestamp}".</summary>
    public static int ParseBookingId(string orderId)
    {
        var parts = orderId.Split('_');
        return parts.Length >= 2 && int.TryParse(parts[1], out var id) ? id : 0;
    }

    private static string HmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}

public class EmailHelper
{
    private readonly EmailSettings _settings;

    public EmailHelper(EmailSettings settings) => _settings = settings;

    /// <summary>
    /// Gửi OTP đặt lại mật khẩu qua Gmail SMTP.
    /// OTP hết hạn sau 10 phút — thông tin hiển thị trong body email.
    /// </summary>
    public async Task SendOtpAsync(string toEmail, string toName, string otp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "[SportPlus] Mã xác nhận đặt lại mật khẩu";

        message.Body = new TextPart("html")
        {
            Text = BuildOtpEmailBody(toName, otp)
        };

        using var client = new SmtpClient();

        // Gmail SMTP: port 587 + STARTTLS
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);
    }

    private static string BuildOtpEmailBody(string name, string otp) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;">
          <h2 style="color:#2d7a3a;">SportPlus</h2>
          <p>Xin chào <strong>{name}</strong>,</p>
          <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
          <p>Mã OTP của bạn là:</p>
          <div style="font-size:32px;font-weight:bold;letter-spacing:8px;color:#2d7a3a;text-align:center;padding:16px 0;">
            {otp}
          </div>
          <p style="color:#888;font-size:13px;">Mã có hiệu lực trong <strong>10 phút</strong>. Không chia sẻ mã này với bất kỳ ai.</p>
          <p style="color:#888;font-size:13px;">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
          <hr style="border:none;border-top:1px solid #e0e0e0;margin:16px 0;">
          <p style="color:#bbb;font-size:12px;text-align:center;">© 2026 SportPlus</p>
        </div>
        """;
}