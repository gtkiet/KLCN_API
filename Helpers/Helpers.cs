using KLCN_API.Configurations;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KLCN_API.Helpers;

public class JwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Tạo JWT access token từ thông tin user.
    /// Caller phải đảm bảo user.Role đã được load (Include hoặc eager load),
    /// nếu không claim role sẽ sai và [AuthorizeRoles] sẽ không hoạt động.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // Role.Name ("Admin"/"Staff"/"Customer") phải khớp với RoleEnum.ToString()
        // mà AuthorizeRolesAttribute dùng. Throw sớm thay vì fallback im lặng
        // sang RoleId (số nguyên) — vì nếu claim role = "1" thì Authorize sẽ fail.
        if (user.Role is null)
            throw new InvalidOperationException(
                $"User {user.UserId} chua load navigation Role. " +
                "Them Include(u => u.Role) truoc khi goi GenerateAccessToken.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email,          user.Email),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Role,           user.Role.Name),
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

    /// <summary>Tạo refresh token ngẫu nhiên (opaque token, 64 bytes).</summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

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
            ValidateLifetime = false, // cho phép token đã hết hạn
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
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenException("Token khong hop le.");
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw; // re-throw để caller xử lý 401
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
    /// Trả về 0 nếu claim không tồn tại hoặc không parse được —
    /// caller nên kiểm tra giá trị trả về trước khi dùng.
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

    /// <summary>Lấy email từ claims.</summary>
    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    /// <summary>Lấy họ tên đầy đủ từ claims.</summary>
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
        DbContext ctx,
        string spName,
        params SqlParameter[] parameters)
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
        DbContext ctx,
        string spName,
        Func<IDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var results = new List<T>();
        var conn = ctx.Database.GetDbConnection();

        // Ghi nhớ trạng thái trước khi vào: chỉ đóng nếu chúng ta tự mở.
        var wasOpen = conn.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;

            // Gắn transaction hiện tại nếu có (tránh "connection is part of transaction" error)
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
            // Chỉ đóng nếu chúng ta là người mở
            if (!wasOpen && conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }

        return results;
    }

    // ── Factory methods cho các SP thường dùng ──────────────────

    /// <summary>
    /// sp_HoldSlots — giữ slot, kiểm tra ràng buộc đặt trước.
    /// Nhận danh sách ID dạng IEnumerable&lt;int&gt;, tự join thành CSV.
    /// </summary>
    public static Task HoldSlotsAsync(
        DbContext ctx, IEnumerable<int> fieldSlotIds, int? userId = null)
        => HoldSlotsAsync(ctx, string.Join(",", fieldSlotIds), userId);

    /// <summary>Overload nhận CSV string — dùng khi đã có sẵn chuỗi.</summary>
    public static Task HoldSlotsAsync(
        DbContext ctx, string fieldSlotIds, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_HoldSlots",
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmBooking — xác nhận booking, tính tiền, tạo deposit nếu cần.</summary>
    public static Task ConfirmBookingAsync(
        DbContext ctx, int bookingId, string fieldSlotIds,
        bool isFullPayment = true, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmBooking",
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

    // ── Internal ─────────────────────────────────────────────────

    private static string BuildParamString(SqlParameter[] parameters)
        => string.Join(", ", parameters.Select(p => p.ParameterName));
}

// ── Pagination ────────────────────────────────────────────────────

public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Phân trang IQueryable — thực thi 2 query: Count và dữ liệu trang.
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

// ── VNPayHelper ───────────────────────────────────────────────────

public class VNPayHelper
{
    private readonly VNPaySettings _settings;

    public VNPayHelper(VNPaySettings settings) => _settings = settings;

    /// <summary>Tạo URL thanh toán VNPay.</summary>
    public string CreatePaymentUrl(
        int bookingId, decimal amount, string orderInfo, string ipAddress)
    {
        var txnRef = $"{bookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // SortedDictionary đảm bảo thứ tự key khi build query string
        var vnpay = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(), // VNPay tính theo đồng
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
        };

        // Build query string và ký — UrlEncode value khi build URL nhưng KHÔNG encode khi hash
        var rawData = string.Join("&", vnpay.Select(kv => $"{kv.Key}={kv.Value}"));
        var urlData = string.Join("&", vnpay.Select(kv =>
            $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
        var signature = HmacSha512(_settings.HashSecret, rawData);

        return $"{_settings.BaseUrl}?{urlData}&vnp_SecureHash={signature}";
    }

    /// <summary>
    /// Xác minh chữ ký IPN/Return từ VNPay.
    /// IQueryCollection đã decode value → dùng raw value để hash, KHÔNG encode lại.
    /// </summary>
    public bool ValidateSignature(
        IQueryCollection query, out string txnRef, out bool isSuccess)
    {
        txnRef = query["vnp_TxnRef"].ToString();
        isSuccess = query["vnp_ResponseCode"] == "00";

        var receivedHash = query["vnp_SecureHash"].ToString();

        // Lấy tất cả param trừ vnp_SecureHash, sắp xếp theo key, hash raw value
        var data = string.Join("&", query
            .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}"));  // KHÔNG UrlEncode — đã decode rồi

        var expectedHash = HmacSha512(_settings.HashSecret, data);
        return string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}

// ── MoMoHelper ────────────────────────────────────────────────────

public class MoMoHelper
{
    private readonly MoMoSettings _settings;
    private readonly IHttpClientFactory _httpFactory;

    public MoMoHelper(MoMoSettings settings, IHttpClientFactory httpFactory)
    {
        _settings = settings;
        _httpFactory = httpFactory;
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

        // Chuỗi ký theo đúng thứ tự field MoMo quy định
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

        var signature = HmacSha256(_settings.SecretKey, rawSignature);

        var body = new
        {
            partnerCode = _settings.PartnerCode,
            requestId,
            amount = amountStr,
            orderId,
            orderInfo,
            redirectUrl = _settings.ReturnUrl,
            ipnUrl = _settings.IpnUrl,
            requestType = _settings.RequestType,
            extraData = string.Empty,
            lang = "vi",
            signature
        };

        var client = _httpFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_settings.Endpoint, content);

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

    /// <summary>
    /// Xác minh chữ ký IPN từ MoMo.
    /// MoMo gửi JSON body với field "signature".
    /// </summary>
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

public static class FieldImageHelper
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Lưu file ảnh vào thư mục Uploads/fields/.
    /// Tên file = GUID mới (tránh trùng, tránh path traversal).
    /// Trả về relative URL "/Uploads/fields/{guid}.ext" để lưu vào DB.
    /// </summary>
    public static async Task<string> SaveAsync(
        IFormFile file, string contentRootPath)
    {
        // Validate extension
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new BusinessException(
                "Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .webp.", 400);

        // Validate size
        if (file.Length == 0)
            throw new BusinessException("File không được rỗng.", 400);

        if (file.Length > MaxFileSizeBytes)
            throw new BusinessException("File ảnh không được vượt quá 5MB.", 400);

        // Tạo thư mục nếu chưa có
        var folder = Path.Combine(contentRootPath, "Uploads", "fields");
        Directory.CreateDirectory(folder); // no-op nếu đã tồn tại

        // Tên file an toàn: GUID + extension gốc (đã validate ở trên)
        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);

        // Relative URL — frontend ghép host
        return $"/Uploads/fields/{fileName}";
    }

    /// <summary>
    /// Xóa file ảnh cũ nếu tồn tại. Silent fail nếu file không có.
    /// oldRelativeUrl: "/Uploads/fields/xxx.jpg"
    /// </summary>
    public static void DeleteIfExists(string? oldRelativeUrl, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(oldRelativeUrl)) return;

        // Chuyển relative URL → absolute path, block path traversal
        var relativePath = oldRelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, relativePath));
        var uploadRoot = Path.GetFullPath(Path.Combine(contentRootPath, "Uploads"));

        // Bảo vệ: không xóa file ngoài thư mục Uploads
        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase)) return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}

/// <summary>
/// Helper upload ảnh dùng chung cho mọi entity (Field, Service, Profile...).
/// Tên file = GUID mới, tránh trùng và path traversal.
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
    /// <param name="file">File từ request.</param>
    /// <param name="contentRootPath">env.ContentRootPath từ IWebHostEnvironment.</param>
    /// <param name="subfolder">Tên thư mục con, ví dụ "fields", "services", "avatars".</param>
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
    /// <param name="oldRelativeUrl">Ví dụ: "/Uploads/fields/abc.jpg"</param>
    public static void DeleteIfExists(string? oldRelativeUrl, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(oldRelativeUrl)) return;

        var relativePath = oldRelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(contentRootPath, relativePath));
        var uploadRoot = Path.GetFullPath(Path.Combine(contentRootPath, "Uploads"));

        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase)) return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
