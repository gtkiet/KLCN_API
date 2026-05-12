using KLCN_API.Configurations;
using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;


public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly JwtHelper _jwt;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IAuthRepository authRepo, JwtHelper jwt, JwtSettings jwtSettings)
    {
        _authRepo = authRepo;
        _jwt = jwt;
        _jwtSettings = jwtSettings;
    }

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

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _authRepo.GetByEmailAsync(request.Email.Trim().ToLower());

        if (user is null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new BusinessException("Email hoặc mật khẩu không chính xác.", 400);

        if (user.StatusId == (int)UserStatusEnum.Locked)
            throw new BusinessException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.", 403);

        return await BuildLoginResponseAsync(user);
    }

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

    public async Task LogoutAsync(int userId)
        => await _authRepo.RevokeAllRefreshTokensAsync(userId);

    // ── Helpers ──────────────────────────────────────────────────

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
}
