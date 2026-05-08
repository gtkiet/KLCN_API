// Models/DTOs/Request/AuthRequest.cs

using System.ComponentModel.DataAnnotations;

namespace KLCN_API.Models.DTOs.Request;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Access token không được để trống.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token không được để trống.")]
    public string RefreshToken { get; set; } = string.Empty;
}