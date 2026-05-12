using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>Đăng nhập.</summary>
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
        var userId = User.GetUserId();
        await _authService.LogoutAsync(userId);
        return Ok(ApiResponse.Ok("Đăng xuất thành công."));
    }
}
