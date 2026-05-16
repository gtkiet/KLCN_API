using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Request = KLCN_API.Models.DTOs.Request;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Đăng ký tài khoản khách hàng.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Đăng ký thành công."));
    }

    /// <summary>Đăng nhập bằng email hoặc số điện thoại.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Đăng nhập thành công."));
    }

    /// <summary>Làm mới access token bằng refresh token.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Làm mới token thành công."));
    }

    /// <summary>Đăng xuất — thu hồi toàn bộ refresh token của user hiện tại.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(User.GetUserId());
        return Ok(ApiResponse.Ok("Đăng xuất thành công."));
    }

    // ── Password reset ────────────────────────────────────────────

    /// <summary>
    /// Bước 1 — Yêu cầu gửi OTP về email.
    /// Luôn trả về 200 kể cả email không tồn tại (tránh user enumeration).
    /// OTP có hiệu lực 10 phút.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse.Ok(
            "Nếu email tồn tại trong hệ thống, mã OTP sẽ được gửi đến hộp thư của bạn."));
    }

    /// <summary>
    /// Bước 2 — Xác minh OTP.
    /// Trả về reset token dùng một lần, có hiệu lực 15 phút.
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request);
        return Ok(ApiResponse<VerifyOtpResponse>.Ok(result, "Xác minh OTP thành công."));
    }

    /// <summary>
    /// Bước 3 — Đặt lại mật khẩu bằng reset token từ bước 2.
    /// Sau khi thành công, toàn bộ thiết bị đang đăng nhập sẽ bị đăng xuất.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse.Ok("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại."));
    }
}