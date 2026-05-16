using KLCN_API.Configurations;
using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;
using System.Security.Cryptography;

namespace KLCN_API.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly IUserRepository _userRepo;
    private readonly JwtHelper _jwt;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailHelper _emailHelper;

    private const int OtpExpiryMinutes = 10;
    private const int ResetTokenExpiryMinutes = 15;

    public AuthService(
        IAuthRepository authRepo,
        IUserRepository userRepo,
        JwtHelper jwt,
        JwtSettings jwtSettings,
        EmailHelper emailHelper)
    {
        _authRepo = authRepo;
        _userRepo = userRepo;
        _jwt = jwt;
        _jwtSettings = jwtSettings;
        _emailHelper = emailHelper;
    }

    // ── Register ──────────────────────────────────────────────────

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var phone = request.Phone.Trim();

        if (await _authRepo.EmailExistsAsync(email))
            throw new ConflictException("Email đã được sử dụng.");

        if (await _authRepo.PhoneExistsAsync(phone))
            throw new ConflictException("Số điện thoại đã được sử dụng.");

        var user = new User
        {
            Email = email,
            Phone = phone,
            FullName = request.FullName.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            RoleId = (int)RoleEnum.Customer,
            StatusId = (int)UserStatusEnum.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _authRepo.CreateUserAsync(user, new Profile());
        return await BuildLoginResponseAsync(created);
    }

    // ── Login ─────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var identifier = request.Identifier.Trim();
        var isEmail = identifier.Contains('@');

        User? user = isEmail
            ? await _authRepo.GetByEmailAsync(identifier.ToLower())
            : await _authRepo.GetByPhoneAsync(identifier);

        if (user is null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new BusinessException("Thông tin đăng nhập không chính xác.", 400);

        if (user.StatusId == (int)UserStatusEnum.Locked)
            throw new BusinessException(
                "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.", 403);

        return await BuildLoginResponseAsync(user);
    }

    // ── Refresh token ─────────────────────────────────────────────

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        int userId;

        try
        {
            var principal = _jwt.GetPrincipalFromExpiredToken(request.AccessToken);
            userId = _jwt.GetUserIdFromPrincipal(principal);
        }
        catch
        {
            throw new UnauthorizedException("Access token không hợp lệ.");
        }

        if (userId == 0)
            throw new UnauthorizedException("Access token không hợp lệ.");

        var storedToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

        if (storedToken is null || storedToken.UserId != userId)
            throw new UnauthorizedException("Refresh token không hợp lệ.");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");

        await _authRepo.RevokeRefreshTokenAsync(request.RefreshToken);

        var newAccessToken = _jwt.GenerateAccessToken(storedToken.User);
        var newRefreshToken = await IssueRefreshTokenAsync(storedToken.UserId);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)
        };
    }

    // ── Logout ────────────────────────────────────────────────────

    public async Task LogoutAsync(int userId)
        => await _authRepo.RevokeAllRefreshTokensAsync(userId);

    // ── Forgot password ───────────────────────────────────────────

    /// <summary>
    /// Bước 1: Sinh OTP 6 số, hash rồi lưu cache, gửi email.
    /// Luôn trả về 200 kể cả email không tồn tại — tránh user enumeration.
    /// </summary>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _authRepo.GetByEmailAsync(email);

        // Không throw nếu email không tồn tại — trả về 200 bình thường
        if (user is null) return;

        if (user.StatusId == (int)UserStatusEnum.Locked)
            throw new BusinessException(
                "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.", 403);

        var otp = GenerateOtp();
        var otpHash = HashToken(otp);
        var expiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);

        await _authRepo.SaveOtpAsync(user.UserId, otpHash, expiresAt);
        await _emailHelper.SendOtpAsync(email, user.FullName, otp);
    }

    // ── Verify OTP ────────────────────────────────────────────────

    /// <summary>
    /// Bước 2: Xác minh OTP, trả về reset token dùng một lần (15 phút).
    /// OTP bị xoá ngay sau khi verify thành công.
    /// </summary>
    public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _authRepo.GetByEmailAsync(email)
            ?? throw new BusinessException("Thông tin không hợp lệ.", 400);

        var record = await _authRepo.GetValidOtpAsync(user.UserId);

        if (record is null)
            throw new BusinessException("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);

        var inputHash = HashToken(request.Otp.Trim());
        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(inputHash),
                System.Text.Encoding.UTF8.GetBytes(record.Value.OtpHash)))
            throw new BusinessException("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);

        // Xoá OTP ngay sau khi dùng — chỉ dùng được 1 lần
        await _authRepo.ClearOtpAsync(user.UserId);

        // Sinh reset token ngẫu nhiên, lưu hash vào cache
        var resetToken = GenerateResetToken();
        var resetTokenHash = HashToken(resetToken);
        var expiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes);

        await _authRepo.SaveResetTokenAsync(user.UserId, resetTokenHash, expiresAt);

        return new VerifyOtpResponse { ResetToken = resetToken };
    }

    // ── Reset password ────────────────────────────────────────────

    /// <summary>
    /// Bước 3: Đặt lại mật khẩu bằng reset token từ bước 2.
    /// Token bị xoá sau khi dùng. Thu hồi toàn bộ refresh token.
    /// </summary>
    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.ResetToken.Trim());
        var record = await _authRepo.GetValidResetTokenAsync(tokenHash)
            ?? throw new BusinessException("Reset token không hợp lệ hoặc đã hết hạn.", 400);

        var newHash = PasswordHelper.HashPassword(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(record.UserId, newHash);

        // Xoá token sau khi dùng — chỉ dùng được 1 lần
        await _authRepo.ClearResetTokenAsync(tokenHash);

        // Thu hồi tất cả refresh token — buộc đăng nhập lại trên mọi thiết bị
        await _authRepo.RevokeAllRefreshTokensAsync(record.UserId);
    }

    // ── Private helpers ───────────────────────────────────────────

    private async Task<string> IssueRefreshTokenAsync(int userId)
    {
        var token = _jwt.GenerateRefreshToken();
        await _authRepo.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });
        return token;
    }

    private async Task<LoginResponse> BuildLoginResponseAsync(User user)
    {
        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.UserId);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            User = UserMapper.ToResponse(user)
        };
    }

    /// <summary>Sinh OTP 6 chữ số ngẫu nhiên dùng cryptographic RNG.</summary>
    private static string GenerateOtp()
    {
        var num = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return num.ToString("D6");
    }

    /// <summary>Sinh reset token ngẫu nhiên 32 bytes (URL-safe base64).</summary>
    private static string GenerateResetToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

    /// <summary>Hash token bằng SHA-256 trước khi lưu cache — không lưu plaintext.</summary>
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLower();
    }
}